namespace WebVella.Database.Migrations;

/// <summary>
/// Marks a class as a database migration and specifies its version.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to classes that inherit from <see cref="DbMigration"/> to register them
/// as database migrations. Migrations are executed in version order (lowest to highest).
/// </para>
/// <para>
/// The version string must follow the standard .NET <see cref="System.Version"/> format:
/// <c>major.minor[.build[.revision]]</c> (e.g., "1.0.0.0", "1.2.3", "2.0").
/// </para>
/// <para>
/// Optionally, a <see cref="ScriptPath"/> can be provided to explicitly point to an embedded
/// resource name. When set, the automatic resource discovery is skipped and only the specified
/// resource is used. A <see cref="DbMigrationException"/> is thrown if the resource is not found.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [DbMigration("1.0.0.0")]
/// public class InitialSchema : DbMigration
/// {
///     public override Task&lt;string&gt; GenerateSqlAsync(IServiceProvider serviceProvider)
///     {
///         return Task.FromResult("CREATE TABLE users (id UUID PRIMARY KEY);");
///     }
/// }
/// 
/// [DbMigration("1.0.1.0", "MyApp.Migrations.Custom.InitialSchema.Script.psql")]
/// public class InitialSchema_WithExplicitScript : DbMigration { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class DbMigrationAttribute : Attribute
{
	/// <summary>
	/// Gets the version of this migration.
	/// </summary>
	public Version Version { get; private set; }

	/// <summary>
	/// Gets the explicit embedded resource name to load the migration script from.
	/// When <c>null</c>, the base implementation performs automatic resource discovery.
	/// </summary>
	public string? ScriptPath { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DbMigrationAttribute"/> class.
	/// </summary>
	/// <param name="version">
	/// The version string in .NET Version format (e.g., "1.0.0.0", "1.2.3").
	/// Migrations are executed in ascending version order.
	/// </param>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="version"/> is not a valid version string.
	/// </exception>
	public DbMigrationAttribute(string version)
	{
		Version = new Version(version);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DbMigrationAttribute"/> class
	/// with an explicit embedded resource script path.
	/// </summary>
	/// <param name="version">
	/// The version string in .NET Version format (e.g., "1.0.0.0", "1.2.3").
	/// Migrations are executed in ascending version order.
	/// </param>
	/// <param name="scriptPath">
	/// The embedded resource name (full or suffix) of the SQL script to execute.
	/// The lookup is case-insensitive. If the resource is not found a
	/// <see cref="DbMigrationException"/> is thrown during migration execution.
	/// </param>
	/// <exception cref="ArgumentException">
	/// Thrown when <paramref name="version"/> is not a valid version string.
	/// </exception>
	public DbMigrationAttribute(string version, string? scriptPath)
	{
		Version = new Version(version);
		ScriptPath = scriptPath;
	}
}
