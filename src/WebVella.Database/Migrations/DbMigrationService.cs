using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebVella.Database.Security;

namespace WebVella.Database.Migrations;

/// <summary>
/// Service interface for managing database migrations.
/// </summary>
public interface IDbMigrationService
{
	/// <summary>
	/// Executes all pending migrations that have not yet been applied to the database.
	/// </summary>
	/// <returns>A task representing the asynchronous migration operation.</returns>
	/// <exception cref="DbMigrationException">
	/// Thrown when a migration fails. Contains detailed logs of executed statements.
	/// </exception>
	Task ExecutePendingMigrationsAsync();

	/// <summary>
	/// Gets the current database schema version.
	/// </summary>
	/// <returns>
	/// A task that returns the current version, or <c>0.0.0.0</c> if no migrations have been applied.
	/// </returns>
	Task<Version> GetCurrentDbVersionAsync();
}

/// <summary>
/// Service that discovers and executes database migrations in version order.
/// </summary>
/// <remarks>
/// <para>
/// The migration service automatically discovers all classes that inherit from <see cref="DbMigration"/>
/// and have the <see cref="DbMigrationAttribute"/> applied. Migrations are executed in ascending
/// version order within a database transaction.
/// </para>
/// <para>
/// The service maintains a version table in the database to track which migrations have been applied.
/// Only migrations with versions greater than the current database version are executed.
/// </para>
/// <para>
/// Each migration's SQL statements are wrapped in a PostgreSQL function that provides detailed
/// logging of each statement's execution status. If any statement fails, the entire migration
/// is rolled back and a <see cref="DbMigrationException"/> is thrown with the execution logs.
/// </para>
/// </remarks>
public class DbMigrationService : IDbMigrationService
{
	private readonly IDbService _db;
	private readonly IServiceProvider _serviceProvider;
	private readonly string _versionTableName;
	private readonly string _updateFunctionName;
	private readonly string _updateLogTableName;
	private readonly RlsOptions? _rlsOptions;

	#region <=== SQL Templates ===>

	private const string FUNCTION_CREATE_WRAPPER_TEMPLATE = @"
		CREATE OR REPLACE FUNCTION $$$FUNCTION_NAME$$$() 
		RETURNS TABLE(version TEXT, statement TEXT, success BOOL, sql_error TEXT) AS $$ 
		DECLARE 
			error_occurred bool; 
		BEGIN 
			error_occurred := false; 
			$$$FUNCTION_BODY$$$ 
			RETURN QUERY SELECT * FROM $$$LOG_TABLE$$$; 
		END; 
		$$ LANGUAGE plpgsql;";

	private const string FUNCTION_EXECUTE_TEMPLATE = @"SELECT * FROM $$$FUNCTION_NAME$$$();";

	private const string STATEMENT_WRAPPER_TEMPLATE = @"
		IF not error_occurred THEN 
			BEGIN 
				$$$STATEMENT$$$ 
				INSERT INTO $$$LOG_TABLE$$$(version, statement, success, sql_error) 
				VALUES('$$$VERSION$$$', '$$$STATEMENT_ENCODED$$$', TRUE, null); 
			EXCEPTION WHEN OTHERS THEN 
				INSERT INTO $$$LOG_TABLE$$$(version, statement, success, sql_error) 
				VALUES('$$$VERSION$$$', '$$$STATEMENT_ENCODED$$$', FALSE, SQLERRM); 
				error_occurred := true; 
			END; 
		END IF;";

	#endregion

	/// <summary>
	/// Initializes a new instance of the <see cref="DbMigrationService"/> class with default options.
	/// </summary>
	/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
	/// <param name="db">The database service for executing SQL.</param>
	public DbMigrationService(IServiceProvider serviceProvider, IDbService db)
		: this(serviceProvider, db, new DbMigrationOptions())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DbMigrationService"/> class with custom options.
	/// </summary>
	/// <param name="serviceProvider">The service provider for resolving dependencies.</param>
	/// <param name="db">The database service for executing SQL.</param>
	/// <param name="options">The migration options for customizing table and function names.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
	public DbMigrationService(IServiceProvider serviceProvider, IDbService db, DbMigrationOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_db = db;
		_serviceProvider = serviceProvider;
		_versionTableName = options.VersionTableName ?? DbMigrationOptions.DefaultVersionTableName;
		_updateFunctionName = options.UpdateFunctionName ?? DbMigrationOptions.DefaultUpdateFunctionName;
		_updateLogTableName = options.UpdateLogTableName ?? DbMigrationOptions.DefaultUpdateLogTableName;

		var rlsSection = serviceProvider.GetService<IConfiguration>()?.GetSection(RlsOptions.DefaultSectionName);
		var sqlUser = rlsSection?["SqlUser"];
		var sqlPassword = rlsSection?["SqlPassword"];
		if (!string.IsNullOrWhiteSpace(sqlUser) && !string.IsNullOrWhiteSpace(sqlPassword))
			_rlsOptions = new RlsOptions { SqlUser = sqlUser, SqlPassword = sqlPassword };
	}

	/// <inheritdoc />
	public async Task ExecutePendingMigrationsAsync()
	{
		await ReleaseAllAdvisoryLocks();
		await EnsureVersionTableExistsAsync();
		await using var scope = await _db.CreateTransactionScopeAsync();

		await _db.ExecuteAsync($@"
			CREATE TEMP TABLE IF NOT EXISTS {_updateLogTableName} (
				version TEXT, statement TEXT, success BOOL, sql_error TEXT
			) ON COMMIT PRESERVE ROWS;");

		var currentVersion = await GetCurrentDbVersionAsync();
		var allMigrations = await ScanAndGetMigrationMeta();
		var pendingMigrations = allMigrations.Where(m => m.Version > currentVersion).OrderBy(m => m.Version).ToList();

		try
		{
			foreach (var migration in pendingMigrations)
			{
				await _db.ExecuteAsync($"TRUNCATE TABLE {_updateLogTableName};");
				if (_rlsOptions != null) await _db.EnsureGlobalRlsPermissionsAsync(_rlsOptions);
				await migration.Instance.PreMigrateAsync(_serviceProvider);
				var rawSql = await migration.Instance.GenerateSqlAsync(_serviceProvider);

				if (!string.IsNullOrWhiteSpace(rawSql))
				{
					var statements = Regex.Split(rawSql, @"(?<=[;])\s*[\r\n]+", RegexOptions.Multiline)
						.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

					var functionBody = new StringBuilder();
					foreach (var statement in statements)
					{
						var clean = statement.Trim();
						if (string.IsNullOrEmpty(clean)) continue;

						var wrapped = STATEMENT_WRAPPER_TEMPLATE
							.Replace("$$$LOG_TABLE$$$", _updateLogTableName)
							.Replace("$$$VERSION$$$", migration.Version.ToString())
							.Replace("$$$STATEMENT$$$", clean)
							.Replace("$$$STATEMENT_ENCODED$$$", clean.Replace("'", "''"));
						functionBody.AppendLine(wrapped);
					}

					if (functionBody.Length > 0)
					{
						await _db.ExecuteAsync(
							FUNCTION_CREATE_WRAPPER_TEMPLATE
								.Replace("$$$FUNCTION_NAME$$$", _updateFunctionName)
								.Replace("$$$LOG_TABLE$$$", _updateLogTableName)
								.Replace("$$$FUNCTION_BODY$$$", functionBody.ToString()));

						var functionExecuteSql = FUNCTION_EXECUTE_TEMPLATE
							.Replace("$$$FUNCTION_NAME$$$", _updateFunctionName);
						var logs = (await _db.QueryAsync<DbMigrationLogItem>(functionExecuteSql)).ToList();

						if (logs.Any(x => !x.Success))
						{
							throw new DbMigrationException($"Migration version {migration.Version} failed.", logs);
						}
					}
				}
				if (_rlsOptions != null) await _db.EnsureGlobalRlsPermissionsAsync(_rlsOptions);
				await migration.Instance.PostMigrateAsync(_serviceProvider);
				await UpdateDbVersionAsync(migration.Version);
			}

			await CleanupMigrationArtifactsAsync();
			await scope.CompleteAsync();
			if (_rlsOptions != null) await _db.EnsureGlobalRlsPermissionsAsync(_rlsOptions);
		}
		catch (Exception ex)
		{
			await SafeCleanupMigrationArtifactsAsync();
			if (ex is DbMigrationException)
				throw;
			throw new DbMigrationException(
				$"An unexpected error occurred during migration: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Ensures the version tracking table exists in the database.
	/// </summary>
	private async Task EnsureVersionTableExistsAsync()
	{
		await _db.ExecuteAsync($@"
			CREATE TABLE IF NOT EXISTS {_versionTableName} (
				id INTEGER PRIMARY KEY DEFAULT 1 CHECK (id = 1),
				version TEXT NOT NULL,
				updated_on TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
			);");
	}

	/// <summary>
	/// Cleans up migration artifacts (temp table and function) after successful migration.
	/// </summary>
	private async Task CleanupMigrationArtifactsAsync()
	{
		await _db.ExecuteAsync($"DROP TABLE IF EXISTS {_updateLogTableName};");
		await _db.ExecuteAsync($"DROP FUNCTION IF EXISTS {_updateFunctionName}();");
	}

	/// <summary>
	/// Safely cleans up migration artifacts, ignoring any errors that may occur
	/// if the transaction has been aborted.
	/// </summary>
	private async Task SafeCleanupMigrationArtifactsAsync()
	{
		try
		{
			await _db.ExecuteAsync($"DROP TABLE IF EXISTS {_updateLogTableName};");
		}
		catch
		{
			// Ignore cleanup errors - transaction may be aborted
		}

		try
		{
			await _db.ExecuteAsync($"DROP FUNCTION IF EXISTS {_updateFunctionName}();");
		}
		catch
		{
			// Ignore cleanup errors - transaction may be aborted
		}
	}

	/// <inheritdoc />
	public async Task<Version> GetCurrentDbVersionAsync()
	{
		try
		{
			await EnsureVersionTableExistsAsync();
			var sql = $"SELECT version FROM {_versionTableName} WHERE id = 1";
			var v = await _db.ExecuteScalarAsync<string>(sql);
			return string.IsNullOrEmpty(v) ? new Version(0, 0, 0, 0) : new Version(v);
		}
		catch { return new Version(0, 0, 0, 0); }
	}

	/// <summary>
	/// Updates the database version to the specified version.
	/// </summary>
	private async Task UpdateDbVersionAsync(Version version)
	{
		var sql = $@"
				INSERT INTO {_versionTableName} (id, version, updated_on) 
				VALUES (1, @version, CURRENT_TIMESTAMP)
				ON CONFLICT (id) DO UPDATE SET version = @version, updated_on = CURRENT_TIMESTAMP";
		await _db.ExecuteAsync(sql, new { version = version.ToString() });
	}

	/// <summary>
	/// Releases all advisory locks before starting migrations to prevent deadlocks.
	/// </summary>
	private async Task ReleaseAllAdvisoryLocks()
	{
		var sql = "SELECT pg_terminate_backend(pid) FROM pg_locks WHERE locktype = 'advisory';";
		await _db.ExecuteAsync(sql);
	}

	/// <summary>
	/// Scans all loaded assemblies for migration classes and returns their metadata.
	/// </summary>
	private async Task<List<DbMigrationMeta>> ScanAndGetMigrationMeta()
	{
		var result = new List<DbMigrationMeta>();
		var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic);
		foreach (var assembly in assemblies)
		{
			foreach (var type in assembly.GetTypes())
			{
				if (type.IsSubclassOf(typeof(DbMigration)) && !type.IsAbstract)
				{
					var attr = type.GetCustomAttribute<DbMigrationAttribute>();
					if (attr != null && type.FullName != null)
					{
						var instance = Activator.CreateInstance(type) as DbMigration;
						if (instance != null)
							result.Add(new DbMigrationMeta
							{
								Version = attr.Version,
								MigrationClassName = type.FullName,
								Instance = instance
							});
					}
				}
			}
		}
		return await Task.FromResult(result);
	}
}