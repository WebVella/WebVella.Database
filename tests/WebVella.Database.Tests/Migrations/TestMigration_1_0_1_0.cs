using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration version 1.0.1.0 - Adds a column to the test table.
/// </summary>
[DbMigration("1.0.1.0")]
public class TestMigration_1_0_1_0 : DbMigration
{
	public override Task<string> GenerateSqlAsync(IServiceProvider serviceprovider)
	{
		return Task.FromResult("""
			ALTER TABLE test_migration_table ADD COLUMN IF NOT EXISTS description TEXT;
			""");
	}
}
