using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration with PreMigrateAsync implementation.
/// </summary>
[DbMigration("1.0.6.0")]
public class TestMigration_1_0_6_0_WithPreMigrate : DbMigration
{
	public static bool PreMigrateCalled { get; set; }

	public override Task PreMigrateAsync(IServiceProvider serviceProvider)
	{
		PreMigrateCalled = true;
		return Task.CompletedTask;
	}

	public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
	{
		return Task.FromResult("""
			INSERT INTO test_migration_table (name, description) VALUES ('PreMigrate Test', 'Added with PreMigrate');
			""");
	}
}
