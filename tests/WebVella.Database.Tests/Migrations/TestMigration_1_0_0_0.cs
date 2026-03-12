using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration version 1.0.0.0 - Creates a simple test table.
/// </summary>
[DbMigration("1.0.0.0")]
public class TestMigration_1_0_0_0 : DbMigration
{
	public override Task<string> GenerateSqlAsync(IServiceProvider serviceprovider)
	{
		return Task.FromResult("""
			CREATE TABLE IF NOT EXISTS test_migration_table (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				name VARCHAR(255) NOT NULL,
				created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
			);
			""");
	}
}
