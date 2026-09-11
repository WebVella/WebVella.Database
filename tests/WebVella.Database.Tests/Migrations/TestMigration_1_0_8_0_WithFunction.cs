using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration version 1.0.8.0 - Loads SQL from an embedded resource with an IF statement
/// inside a PostgreSQL function body.
/// </summary>
[DbMigration("1.0.8.0")]
public class TestMigration_1_0_8_0_WithFunction : DbMigration
{
}
