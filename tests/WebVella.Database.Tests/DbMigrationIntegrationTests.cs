using FluentAssertions;
using WebVella.Database.Migrations;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Migrations;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Integration tests for <see cref="DbMigrationService"/> using a real PostgreSQL database.
/// </summary>
[Collection("Migration")]
public class DbMigrationIntegrationTests : IAsyncLifetime
{
	private readonly MigrationFixture _fixture;
	private readonly IDbService _dbService;

	public DbMigrationIntegrationTests(MigrationFixture fixture)
	{
		_fixture = fixture;
		_dbService = fixture.DbService;
	}

	public async Task InitializeAsync()
	{
		await _fixture.CleanupMigrationArtifactsAsync();
		TestMigration_1_0_4_0_WithPostMigrate.PostMigrateCalled = false;
	}

	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== GetCurrentDbVersionAsync Tests ===>

	[Fact]
	public async Task GetCurrentDbVersionAsync_WithNoVersionSet_ShouldReturnZeroVersion()
	{
		var options = new DbMigrationOptions { VersionTableName = "_test_version_fresh_" + Guid.NewGuid() };
		var migrationService = _fixture.CreateMigrationService(options);

		var version = await migrationService.GetCurrentDbVersionAsync();

		version.Should().Be(new Version(0, 0, 0, 0));
	}

	#endregion

	#region <=== DbMigrationOptions Tests ===>

	[Fact]
	public void DbMigrationOptions_DefaultValues_ShouldBeSet()
	{
		var options = new DbMigrationOptions();

		options.VersionTableName.Should().Be(DbMigrationOptions.DefaultVersionTableName);
		options.UpdateFunctionName.Should().Be(DbMigrationOptions.DefaultUpdateFunctionName);
		options.UpdateLogTableName.Should().Be(DbMigrationOptions.DefaultUpdateLogTableName);
	}

	[Fact]
	public void DbMigrationOptions_Constants_ShouldHaveExpectedValues()
	{
		DbMigrationOptions.DefaultVersionTableName.Should().Be("_db_version");
		DbMigrationOptions.DefaultUpdateFunctionName.Should().Be("_db_update");
		DbMigrationOptions.DefaultUpdateLogTableName.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public void DbMigrationOptions_CustomValues_ShouldBeApplied()
	{
		var options = new DbMigrationOptions
		{
			VersionTableName = "custom_version",
			UpdateFunctionName = "custom_function",
			UpdateLogTableName = "custom_log"
		};

		options.VersionTableName.Should().Be("custom_version");
		options.UpdateFunctionName.Should().Be("custom_function");
		options.UpdateLogTableName.Should().Be("custom_log");
	}

	#endregion

	#region <=== Constructor Tests ===>

	[Fact]
	public void DbMigrationService_WithNullOptions_ShouldThrowArgumentNullException()
	{
		var act = () => new DbMigrationService(_fixture.ServiceProvider, _dbService, null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("options");
	}

	[Fact]
	public void DbMigrationService_WithDefaultConstructor_ShouldUseDefaultOptions()
	{
		var service = new DbMigrationService(_fixture.ServiceProvider, _dbService);

		service.Should().NotBeNull();
	}

	[Fact]
	public void DbMigrationService_WithCustomOptions_ShouldNotThrow()
	{
		var options = new DbMigrationOptions
		{
			VersionTableName = "_custom_version",
			UpdateFunctionName = "_custom_update",
			UpdateLogTableName = "custom_log_table"
		};

		var service = new DbMigrationService(_fixture.ServiceProvider, _dbService, options);

		service.Should().NotBeNull();
	}

	#endregion

	#region <=== Interface Tests ===>

	[Fact]
	public void DbMigrationService_ShouldImplementIDbMigrationService()
	{
		var service = new DbMigrationService(_fixture.ServiceProvider, _dbService);

		service.Should().BeAssignableTo<IDbMigrationService>();
	}

	#endregion

	#region <=== ExecutePendingMigrationsAsync Tests ===>

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldCreateTable()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var tableExists = await isolatedDbService.ExecuteScalarAsync<bool>("""
			SELECT EXISTS (
				SELECT FROM information_schema.tables 
				WHERE table_name = 'test_migration_table'
			);
			""");

		tableExists.Should().BeTrue();
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldCreateAllColumns()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var columns = await isolatedDbService.QueryAsync<dynamic>("""
			SELECT column_name 
			FROM information_schema.columns 
			WHERE table_name = 'test_migration_table'
			ORDER BY ordinal_position;
			""");

		var columnNames = columns.Select(c => (string)c.column_name).ToList();
		columnNames.Should().Contain("id");
		columnNames.Should().Contain("name");
		columnNames.Should().Contain("created_at");
		columnNames.Should().Contain("description");
		columnNames.Should().Contain("is_active");
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldInsertSeededData()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var count = await isolatedDbService.ExecuteScalarAsync<long>(
			"SELECT COUNT(*) FROM test_migration_table WHERE name = 'Seeded Item'");

		count.Should().Be(1);
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldUpdateVersion()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var version = await migrationService.GetCurrentDbVersionAsync();
		version.Should().BeGreaterThan(new Version(0, 0, 0, 0));
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_CalledTwice_ShouldNotDuplicateData()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();
		var countAfterFirst = await isolatedDbService.ExecuteScalarAsync<long>(
			"SELECT COUNT(*) FROM test_migration_table WHERE name = 'Seeded Item'");

		await migrationService.ExecutePendingMigrationsAsync();
		var countAfterSecond = await isolatedDbService.ExecuteScalarAsync<long>(
			"SELECT COUNT(*) FROM test_migration_table WHERE name = 'Seeded Item'");

		countAfterFirst.Should().Be(1);
		countAfterSecond.Should().Be(1, "second run should not re-execute migrations");
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldCallPostMigrateAsync()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);
		TestMigration_1_0_4_0_WithPostMigrate.PostMigrateCalled = false;

		await migrationService.ExecutePendingMigrationsAsync();

		TestMigration_1_0_4_0_WithPostMigrate.PostMigrateCalled.Should().BeTrue();
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldCleanupTempArtifacts()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var functionExists = await isolatedDbService.ExecuteScalarAsync<bool>($"""
			SELECT EXISTS (
				SELECT FROM pg_proc WHERE proname = '{DbMigrationOptions.DefaultUpdateFunctionName}'
			);
			""");

		functionExists.Should().BeFalse("update function should be dropped after migration");
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldCreateVersionTable()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var tableExists = await isolatedDbService.ExecuteScalarAsync<bool>($"""
			SELECT EXISTS (
				SELECT FROM information_schema.tables 
				WHERE table_name = '{DbMigrationOptions.DefaultVersionTableName}'
			);
			""");

		tableExists.Should().BeTrue("version table should be created");
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldExecuteEmbeddedResourceMigration()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var count = await isolatedDbService.ExecuteScalarAsync<long>(
			"SELECT COUNT(*) FROM test_migration_table WHERE embedded_source = 'embedded'");

		count.Should().Be(1, "embedded resource migration should insert data");
	}

	[Fact]
	public async Task ExecutePendingMigrationsAsync_ShouldCreateColumnFromEmbeddedResource()
	{
		var isolatedDbService = _fixture.CreateIsolatedDbService();
		var options = new DbMigrationOptions();
		var migrationService = new DbMigrationService(_fixture.ServiceProvider, isolatedDbService, options);

		await migrationService.ExecutePendingMigrationsAsync();

		var columnExists = await isolatedDbService.ExecuteScalarAsync<bool>("""
			SELECT EXISTS (
				SELECT FROM information_schema.columns 
				WHERE table_name = 'test_migration_table' AND column_name = 'embedded_source'
			);
			""");

		columnExists.Should().BeTrue("embedded_source column should be created by embedded resource migration");
	}

	#endregion

	#region <=== DbMigration Tests ===>

	[Fact]
	public async Task DbMigration_GenerateSqlAsync_ShouldReturnSql()
	{
		var migration = new TestMigration_1_0_0_0();

		var sql = await migration.GenerateSqlAsync(_fixture.ServiceProvider);

		sql.Should().NotBeNullOrEmpty();
		sql.Should().Contain("CREATE TABLE");
		sql.Should().Contain("test_migration_table");
	}

	[Fact]
	public async Task DbMigration_EmptyMigration_ShouldReturnEmptyString()
	{
		var migration = new TestMigration_1_0_3_0_Empty();

		var sql = await migration.GenerateSqlAsync(_fixture.ServiceProvider);

		sql.Should().BeEmpty();
	}

	[Fact]
	public async Task DbMigration_PostMigrateAsync_ShouldBeCallable()
	{
		var migration = new TestMigration_1_0_4_0_WithPostMigrate();
		TestMigration_1_0_4_0_WithPostMigrate.PostMigrateCalled = false;

		await migration.PostMigrateAsync(_fixture.ServiceProvider);

		TestMigration_1_0_4_0_WithPostMigrate.PostMigrateCalled.Should().BeTrue();
	}

	[Fact]
	public async Task DbMigration_FromEmbeddedResource_ShouldLoadSqlFromFile()
	{
		var migration = new TestMigration_1_0_5_0_FromEmbeddedResource();

		var sql = await migration.GenerateSqlAsync(_fixture.ServiceProvider);

		sql.Should().NotBeNullOrEmpty();
		sql.Should().Contain("ALTER TABLE test_migration_table");
		sql.Should().Contain("embedded_source");
		sql.Should().Contain("Embedded Resource Item");
	}

	[Fact]
	public async Task DbMigration_FromEmbeddedResource_ShouldReturnValidSql()
	{
		var migration = new TestMigration_1_0_5_0_FromEmbeddedResource();

		var sql = await migration.GenerateSqlAsync(_fixture.ServiceProvider);

		sql.Should().Contain("ADD COLUMN IF NOT EXISTS embedded_source");
		sql.Should().Contain("INSERT INTO test_migration_table");
	}

	#endregion

	#region <=== DbMigrationAttribute Tests ===>

	[Fact]
	public void DbMigrationAttribute_ShouldParseVersion()
	{
		var type = typeof(TestMigration_1_0_0_0);
		var attr = (DbMigrationAttribute?)Attribute.GetCustomAttribute(type, typeof(DbMigrationAttribute));

		attr.Should().NotBeNull();
		attr!.Version.Should().Be(new Version(1, 0, 0, 0));
	}

	[Fact]
	public void DbMigrationAttribute_ShouldParseComplexVersion()
	{
		var type = typeof(TestMigration_1_0_2_0);
		var attr = (DbMigrationAttribute?)Attribute.GetCustomAttribute(type, typeof(DbMigrationAttribute));

		attr.Should().NotBeNull();
		attr!.Version.Should().Be(new Version(1, 0, 2, 0));
	}

	#endregion
}
