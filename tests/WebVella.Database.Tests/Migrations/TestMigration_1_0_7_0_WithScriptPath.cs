using WebVella.Database.Migrations;

namespace WebVella.Database.Tests.Migrations;

/// <summary>
/// Test migration version 1.0.7.0 - Loads SQL from an explicit embedded resource via ScriptPath attribute.
/// </summary>
[DbMigration("1.0.7.0", "TestMigration_1_0_7_0_WithScriptPath.ExplicitScript.sql")]
public class TestMigration_1_0_7_0_WithScriptPath : DbMigration
{
}
