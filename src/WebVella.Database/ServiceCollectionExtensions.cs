using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WebVella.Database.Migrations;

namespace WebVella.Database;

/// <summary>
/// Extension methods for configuring WebVella.Database services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
	private static bool _typeHandlersRegistered;
	private static readonly object _lock = new();

	/// <summary>
	/// Adds WebVella.Database services to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="connectionString">The PostgreSQL connection string.</param>
	/// <param name="assemblies">
	/// Optional assemblies to scan for entities with [JsonColumn] attributes.
	/// If not provided, all loaded assemblies in the current AppDomain will be scanned.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddWebVellaDatabase(
		this IServiceCollection services,
		string connectionString,
		params Assembly[] assemblies)
	{
		return AddWebVellaDatabase(services, connectionString, enableCaching: false, assemblies);
	}

	/// <summary>
	/// Adds WebVella.Database services to the specified <see cref="IServiceCollection"/> with optional caching.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="connectionString">The PostgreSQL connection string.</param>
	/// <param name="enableCaching">Whether to enable entity caching for types marked with [Cacheable].</param>
	/// <param name="assemblies">
	/// Optional assemblies to scan for entities with [JsonColumn] attributes.
	/// If not provided, all loaded assemblies in the current AppDomain will be scanned.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddWebVellaDatabase(
		this IServiceCollection services,
		string connectionString,
		bool enableCaching,
		params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

		// Register Dapper type handlers (only once)
		RegisterDapperTypeHandlers();

		// Scan assemblies for JsonColumn attributes
		var assembliesToScan = assemblies.Length > 0
			? assemblies
			: GetApplicationAssemblies();

		foreach (var assembly in assembliesToScan)
		{
			JsonColumnTypeHandlerExtensions.RegisterJsonColumnsFromAssembly(assembly);
		}

		// Register caching if enabled
		if (enableCaching)
		{
			services.AddMemoryCache();
			services.AddSingleton<IDbEntityCache, DbEntityCache>();
		}
		else
		{
			services.AddSingleton<IDbEntityCache>(NullDbEntityCache.Instance);
		}

		// Register DbService
		services.AddScoped<IDbService>(sp =>
		{
			var cache = sp.GetRequiredService<IDbEntityCache>();
			return new DbService(connectionString, cache);
		});

		return services;
	}

	/// <summary>
	/// Adds WebVella.Database services to the specified <see cref="IServiceCollection"/> with a factory.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="connectionStringFactory">
	/// A factory function that resolves the connection string from the service provider.
	/// </param>
	/// <param name="assemblies">
	/// Optional assemblies to scan for entities with [JsonColumn] attributes.
	/// If not provided, all loaded assemblies in the current AppDomain will be scanned.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddWebVellaDatabase(
		this IServiceCollection services,
		Func<IServiceProvider, string> connectionStringFactory,
		params Assembly[] assemblies)
	{
		return AddWebVellaDatabase(services, connectionStringFactory, enableCaching: false, assemblies);
	}

	/// <summary>
	/// Adds WebVella.Database services to the specified <see cref="IServiceCollection"/> with a factory and caching.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="connectionStringFactory">
	/// A factory function that resolves the connection string from the service provider.
	/// </param>
	/// <param name="enableCaching">Whether to enable entity caching for types marked with [Cacheable].</param>
	/// <param name="assemblies">
	/// Optional assemblies to scan for entities with [JsonColumn] attributes.
	/// If not provided, all loaded assemblies in the current AppDomain will be scanned.
	/// </param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddWebVellaDatabase(
		this IServiceCollection services,
		Func<IServiceProvider, string> connectionStringFactory,
		bool enableCaching,
		params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(connectionStringFactory);

		// Register Dapper type handlers (only once)
		RegisterDapperTypeHandlers();

		// Scan assemblies for JsonColumn attributes
		var assembliesToScan = assemblies.Length > 0
			? assemblies
			: GetApplicationAssemblies();

		foreach (var assembly in assembliesToScan)
		{
			JsonColumnTypeHandlerExtensions.RegisterJsonColumnsFromAssembly(assembly);
		}

		// Register caching if enabled
		if (enableCaching)
		{
			services.AddMemoryCache();
			services.AddSingleton<IDbEntityCache, DbEntityCache>();
		}
		else
		{
			services.AddSingleton<IDbEntityCache>(NullDbEntityCache.Instance);
		}

		// Register DbService with factory
		services.AddScoped<IDbService>(sp =>
		{
			var connectionString = connectionStringFactory(sp);
			var cache = sp.GetRequiredService<IDbEntityCache>();
			return new DbService(connectionString, cache);
		});

		return services;
	}

	/// <summary>
	/// Adds WebVella.Database migration services to the specified <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddWebVellaDatabaseMigrations(this IServiceCollection services)
	{
		return AddWebVellaDatabaseMigrations(services, new DbMigrationOptions());
	}

	/// <summary>
	/// Adds WebVella.Database migration services to the specified <see cref="IServiceCollection"/> with options.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="options">The migration options to configure the service.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddWebVellaDatabaseMigrations(
		this IServiceCollection services,
		DbMigrationOptions options)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(options);

		services.AddScoped<IDbMigrationService>(sp =>
		{
			var db = sp.GetRequiredService<IDbService>();
			return new DbMigrationService(sp, db, options);
		});

		return services;
	}

	/// <summary>
	/// Adds WebVella.Database migration services to the specified <see cref="IServiceCollection"/>
	/// with a configuration action.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
	/// <param name="configureOptions">An action to configure the migration options.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection AddWebVellaDatabaseMigrations(
		this IServiceCollection services,
		Action<DbMigrationOptions> configureOptions)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configureOptions);

		var options = new DbMigrationOptions();
		configureOptions(options);

		return AddWebVellaDatabaseMigrations(services, options);
	}

	/// <summary>
	/// Registers all Dapper type handlers for PostgreSQL types.
	/// This method is thread-safe and will only register handlers once.
	/// </summary>
	public static void RegisterDapperTypeHandlers()
	{
		if (_typeHandlersRegistered)
			return;

		lock (_lock)
		{
			if (_typeHandlersRegistered)
				return;

			// Enable snake_case to PascalCase property mapping for Dapper
			DefaultTypeMap.MatchNamesWithUnderscores = true;

			// Register DateOnly handlers
			SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
			SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());

			// Register DateTimeOffset handlers
			SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
			SqlMapper.AddTypeHandler(new NullableDateTimeOffsetTypeHandler());

			_typeHandlersRegistered = true;
		}
	}

	/// <summary>
	/// Scans the specified assemblies for entity types with [JsonColumn] properties
	/// and registers the appropriate type handlers.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> (not modified, used for chaining).</param>
	/// <param name="assemblies">The assemblies to scan.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	public static IServiceCollection ScanJsonColumnTypes(
		this IServiceCollection services,
		params Assembly[] assemblies)
	{
		foreach (var assembly in assemblies)
		{
			JsonColumnTypeHandlerExtensions.RegisterJsonColumnsFromAssembly(assembly);
		}

		return services;
	}

	/// <summary>
	/// Gets all application assemblies from the current AppDomain, excluding system and framework assemblies.
	/// </summary>
	/// <returns>An array of application assemblies.</returns>
	internal static Assembly[] GetApplicationAssemblies()
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic)
			.Where(a =>
			{
				var name = a.GetName().Name;
				if (string.IsNullOrEmpty(name))
					return false;

				// Exclude system and common framework assemblies
				return !name.StartsWith("System", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("Npgsql", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("Dapper", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("xunit", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("FluentAssertions", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("Newtonsoft", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("NuGet", StringComparison.OrdinalIgnoreCase)
					&& !name.StartsWith("testhost", StringComparison.OrdinalIgnoreCase);
			})
			.ToArray();
	}
}
