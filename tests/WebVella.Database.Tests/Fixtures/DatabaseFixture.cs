using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests.Fixtures;

/// <summary>
/// Provides a shared database context for integration tests.
/// Handles service configuration, dependency injection, and database table setup.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
	public IServiceProvider ServiceProvider { get; private set; } = null!;
	public IDbService DbService { get; private set; } = null!;
	public IConfiguration Configuration { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		// Build configuration
		Configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
			.Build();

		var connectionString = Configuration.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");

		// Setup dependency injection using the extension method
		var services = new ServiceCollection();

		// This registers all type handlers and scans all AppDomain assemblies for [JsonColumn] attributes
		services.AddWebVellaDatabase(connectionString);

		ServiceProvider = services.BuildServiceProvider();

		DbService = ServiceProvider.GetRequiredService<IDbService>();

		// Create test tables
		await CreateTestTablesAsync();
	}

	public async Task DisposeAsync()
	{
		// Cleanup: Drop test tables
		await DropTestTablesAsync();

		if (ServiceProvider is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	/// <summary>
	/// Creates the test tables in the database, dropping them first if they exist.
	/// </summary>
	private async Task CreateTestTablesAsync()
	{
		// Custom Repository implementation converts PascalCase property names to snake_case:
		//   Id → id, IsActive → is_active, CreatedAt → created_at
		// Table columns must match this snake_case convention.
		// Using UUID with gen_random_uuid() as default for primary keys.
		const string createTableSql = """
			DROP TABLE IF EXISTS "test_products" CASCADE;
			DROP TABLE IF EXISTS test_products CASCADE;
			DROP TABLE IF EXISTS "test_order_items" CASCADE;
			DROP TABLE IF EXISTS test_order_items CASCADE;
			DROP TABLE IF EXISTS "test_cacheable_products" CASCADE;
			DROP TABLE IF EXISTS test_cacheable_products CASCADE;
			DROP TABLE IF EXISTS "test_orders" CASCADE;
			DROP TABLE IF EXISTS test_orders CASCADE;
			DROP TABLE IF EXISTS "test_order_lines" CASCADE;
			DROP TABLE IF EXISTS test_order_lines CASCADE;
			DROP TABLE IF EXISTS "test_order_notes" CASCADE;
				DROP TABLE IF EXISTS test_order_notes CASCADE;
				DROP TABLE IF EXISTS "test_db_column_entities" CASCADE;
				DROP TABLE IF EXISTS test_db_column_entities CASCADE;

				CREATE TABLE test_db_column_entities (
					entity_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
					full_name VARCHAR(255) NOT NULL,
					email_address VARCHAR(255) NOT NULL,
					description TEXT NOT NULL DEFAULT ''
				);

				CREATE TABLE test_products (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				name VARCHAR(255) NOT NULL,
				description TEXT,
				price DECIMAL(18, 2) NOT NULL DEFAULT 0,
				quantity INTEGER NOT NULL DEFAULT 0,
				is_active BOOLEAN NOT NULL DEFAULT TRUE,
				status INTEGER NOT NULL DEFAULT 0,
				created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
				updated_at TIMESTAMP,
				release_date DATE NOT NULL DEFAULT CURRENT_DATE,
				discontinued_date DATE,
				published_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
				last_reviewed_at TIMESTAMPTZ,
				metadata JSONB
			);

			CREATE TABLE test_order_items (
				order_id UUID DEFAULT gen_random_uuid(),
				product_id UUID DEFAULT gen_random_uuid(),
				quantity INTEGER NOT NULL DEFAULT 1,
				unit_price DECIMAL(18, 2) NOT NULL DEFAULT 0,
				total_price DECIMAL(18, 2) NOT NULL DEFAULT 0,
				created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
				PRIMARY KEY (order_id, product_id)
			);

			CREATE TABLE test_cacheable_products (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				name VARCHAR(255) NOT NULL,
				price DECIMAL(18, 2) NOT NULL DEFAULT 0,
				is_active BOOLEAN NOT NULL DEFAULT TRUE,
				created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
			);

			CREATE TABLE test_orders (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				customer_name VARCHAR(255) NOT NULL,
				total_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
				created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
			);

			CREATE TABLE test_order_lines (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				order_id UUID NOT NULL REFERENCES test_orders(id) ON DELETE CASCADE,
				product_name VARCHAR(255) NOT NULL,
				quantity INTEGER NOT NULL DEFAULT 1,
				unit_price DECIMAL(18, 2) NOT NULL DEFAULT 0
			);

			CREATE TABLE test_order_notes (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				order_id UUID NOT NULL REFERENCES test_orders(id) ON DELETE CASCADE,
				text TEXT NOT NULL,
				created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
			);
			""";

		await DbService.ExecuteAsync(createTableSql);
	}

	/// <summary>
	/// Drops the test tables from the database.
	/// </summary>
	private async Task DropTestTablesAsync()
	{
		const string dropTableSql = """
			DROP TABLE IF EXISTS test_order_lines CASCADE;
			DROP TABLE IF EXISTS test_order_notes CASCADE;
			DROP TABLE IF EXISTS test_orders CASCADE;
			DROP TABLE IF EXISTS test_products CASCADE;
			DROP TABLE IF EXISTS test_order_items CASCADE;
			DROP TABLE IF EXISTS test_cacheable_products CASCADE;
			DROP TABLE IF EXISTS test_db_column_entities CASCADE;
			""";
		await DbService.ExecuteAsync(dropTableSql);
	}

	/// <summary>
	/// Clears all data from the test_products table.
	/// </summary>
	public async Task ClearTestProductsAsync()
	{
		await DbService.ExecuteAsync("TRUNCATE TABLE test_products;");
	}

	/// <summary>
	/// Clears all data from the test_order_items table.
	/// </summary>
	public async Task ClearTestOrderItemsAsync()
	{
		await DbService.ExecuteAsync("TRUNCATE TABLE test_order_items;");
	}

	/// <summary>
	/// Clears all data from the test_cacheable_products table.
	/// </summary>
	public async Task ClearTestCacheableProductsAsync()
	{
		await DbService.ExecuteAsync("TRUNCATE TABLE test_cacheable_products;");
	}

	/// <summary>
	/// Clears all data from the test_orders, test_order_lines, and test_order_notes tables.
	/// </summary>
	public async Task ClearTestOrdersAsync()
	{
		await DbService.ExecuteAsync(
			"TRUNCATE TABLE test_order_lines, test_order_notes, test_orders CASCADE;");
	}

	/// <summary>
	/// Clears all data from the test_db_column_entities table.
	/// </summary>
	public async Task ClearTestDbColumnEntitiesAsync()
	{
		await DbService.ExecuteAsync("TRUNCATE TABLE test_db_column_entities;");
	}

	/// <summary>
	/// Clears all test data from all test tables.
	/// </summary>
	public async Task ClearAllTestDataAsync()
	{
		await ClearTestProductsAsync();
		await ClearTestOrderItemsAsync();
		await ClearTestCacheableProductsAsync();
		await ClearTestOrdersAsync();
		await ClearTestDbColumnEntitiesAsync();
	}
}
