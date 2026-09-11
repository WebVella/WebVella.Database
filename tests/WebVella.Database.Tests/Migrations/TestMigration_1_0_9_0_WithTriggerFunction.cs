using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration version 1.0.9.0 - Loads SQL from an embedded resource with a trigger function
/// that contains IF and ELSIF blocks.
/// </summary>
[DbMigration("1.0.9.0")]
public class TestMigration_1_0_9_0_WithTriggerFunction : DbMigration
{
}
