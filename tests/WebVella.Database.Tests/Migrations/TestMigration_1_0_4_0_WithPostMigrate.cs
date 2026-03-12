using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration with PostMigrateAsync implementation.
/// </summary>
[DbMigration("1.0.4.0")]
public class TestMigration_1_0_4_0_WithPostMigrate : DbMigration
{
	public static bool PostMigrateCalled { get; set; }

	public override Task<string> GenerateSqlAsync(IServiceProvider serviceprovider)
	{
		return Task.FromResult("""
			INSERT INTO test_migration_table (name, description) VALUES ('PostMigrate Test', 'Before PostMigrate');
			""");
	}

	public override Task PostMigrateAsync(IServiceProvider serviceprovider)
	{
		PostMigrateCalled = true;
		return Task.CompletedTask;
	}
}
