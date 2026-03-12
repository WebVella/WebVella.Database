using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebVella.Database.Migrations;
using Xunit;

namespace WebVella.Database.Tests.Fixtures;

/// <summary>
/// Provides a shared database context for migration integration tests.
/// Uses isolated configuration names to avoid conflicts with other tests.
/// </summary>
public class MigrationFixture : IAsyncLifetime
{
	public IServiceProvider ServiceProvider { get; private set; } = null!;
	public IDbService DbService { get; private set; } = null!;
	public IConfiguration Configuration { get; private set; } = null!;
	public string ConnectionString { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		Configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
			.Build();

		ConnectionString = Configuration.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException(
				"Connection string 'DefaultConnection' not found in appsettings.json");

		var services = new ServiceCollection();
		services.AddWebVellaDatabase(ConnectionString);
		services.AddWebVellaDatabaseMigrations();

		ServiceProvider = services.BuildServiceProvider();
		DbService = ServiceProvider.GetRequiredService<IDbService>();

		await CleanupMigrationArtifactsAsync();
	}

	public async Task DisposeAsync()
	{
		await CleanupMigrationArtifactsAsync();

		if (ServiceProvider is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	/// <summary>
	/// Creates a new DbMigrationService with custom options for isolated testing.
	/// </summary>
	public DbMigrationService CreateMigrationService(DbMigrationOptions? options = null)
	{
		options ??= new DbMigrationOptions();
		return new DbMigrationService(ServiceProvider, DbService, options);
	}

	/// <summary>
	/// Creates a new IDbService for isolated testing with a fresh connection.
	/// </summary>
	public IDbService CreateIsolatedDbService()
	{
		var services = new ServiceCollection();
		services.AddWebVellaDatabase(ConnectionString);
		var sp = services.BuildServiceProvider();
		return sp.GetRequiredService<IDbService>();
	}

	/// <summary>
	/// Cleans up any migration artifacts (tables, functions) from previous test runs.
	/// </summary>
	public async Task CleanupMigrationArtifactsAsync()
	{
		try
		{
			await DbService.ExecuteAsync($"DROP TABLE IF EXISTS {DbMigrationOptions.DefaultUpdateLogTableName};");
		}
		catch { }

		try
		{
			await DbService.ExecuteAsync($"DROP FUNCTION IF EXISTS {DbMigrationOptions.DefaultUpdateFunctionName}();");
		}
		catch { }

		try
		{
			await DbService.ExecuteAsync($"DROP TABLE IF EXISTS {DbMigrationOptions.DefaultVersionTableName};");
		}
		catch { }

		try
		{
			await DbService.ExecuteAsync("DROP TABLE IF EXISTS test_migration_table CASCADE;");
		}
		catch { }

		try
		{
			await DbService.ExecuteAsync("DROP TABLE IF EXISTS test_migration_table_v2 CASCADE;");
		}
		catch { }
	}
}

/// <summary>
/// Collection definition for migration tests to share the MigrationFixture.
/// </summary>
[CollectionDefinition("Migration")]
public class MigrationCollection : ICollectionFixture<MigrationFixture>
{
}
