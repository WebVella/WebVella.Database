using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration that returns empty SQL to test no-op migrations.
/// </summary>
[DbMigration("1.0.3.0")]
public class TestMigration_1_0_3_0_Empty : DbMigration
{
	public override Task<string> GenerateSqlAsync(IServiceProvider serviceprovider)
	{
		return Task.FromResult(string.Empty);
	}
}
