using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration version 1.0.5.0 - Loads SQL from embedded resource using base GenerateSqlAsync.
/// The SQL script is loaded from TestMigration_1_0_5_0_FromEmbeddedResource.Script.sql
/// </summary>
[DbMigration("1.0.5.0")]
public class TestMigration_1_0_5_0_FromEmbeddedResource : DbMigration
{
}
