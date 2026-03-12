using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration version 1.0.2.0 - Adds another column and inserts data.
/// </summary>
[DbMigration("1.0.2.0")]
public class TestMigration_1_0_2_0 : DbMigration
{
	public override Task<string> GenerateSqlAsync(IServiceProvider serviceprovider)
	{
		return Task.FromResult("""
			ALTER TABLE test_migration_table ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
			INSERT INTO test_migration_table (name, description) VALUES ('Seeded Item', 'Added by migration');
			""");
	}
}
