namespace WebVella.Database.Migrations;

/// <summary>
/// Abstract base class for database migrations. Inherit from this class and decorate with 
/// <see cref="DbMigrationAttribute"/> to define a migration.
/// </summary>
/// <remarks>
/// <para>
/// Migrations are discovered automatically by scanning all loaded assemblies for classes that inherit
/// from <see cref="DbMigration"/> and have the <see cref="DbMigrationAttribute"/> applied.
/// </para>
/// <para>
/// There are two ways to define migration SQL:
/// <list type="number">
///   <item>
///     <description>Override <see cref="GenerateSqlAsync"/> to return SQL dynamically.</description>
///   </item>
///   <item>
///     <description>
///       Create an embedded resource file named <c>{FullTypeName}.Script.sql</c> in the same assembly.
///       The base implementation will automatically load and return the SQL from this file.
///     </description>
///   </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Method 1: Override GenerateSqlAsync
/// [DbMigration("1.0.0.0")]
/// public class Migration_1_0_0_0 : DbMigration
/// {
///     public override Task&lt;string&gt; GenerateSqlAsync(IServiceProvider serviceProvider)
///     {
///         return Task.FromResult("CREATE TABLE users (id UUID PRIMARY KEY);");
///     }
/// }
/// 
/// // Method 2: Use embedded SQL resource (no override needed)
/// [DbMigration("1.0.1.0")]
/// public class Migration_1_0_1_0 : DbMigration { }
/// // Requires: Migration_1_0_1_0.Script.sql as embedded resource
/// </code>
/// </example>
public abstract class DbMigration
{
	/// <summary>
	/// Generates the SQL statements to execute for this migration.
	/// </summary>
	/// <param name="serviceProvider">
	/// The service provider for resolving dependencies during SQL generation.
	/// </param>
	/// <returns>
	/// A task that returns the SQL statements to execute. Multiple statements should be separated by
	/// semicolons followed by newlines. Returns an empty string if no SQL should be executed.
	/// </returns>
	/// <remarks>
	/// <para>
	/// The default implementation loads SQL from an embedded resource file named
	/// <c>{FullTypeName}.Script.sql</c>. Override this method to provide SQL dynamically.
	/// </para>
	/// <para>
	/// SQL statements are executed within a transaction. If any statement fails, the entire
	/// migration is rolled back and a <see cref="DbMigrationException"/> is thrown.
	/// </para>
	/// </remarks>
	public virtual async Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
	{
		var type = this.GetType();
		var assembly = type.Assembly;

		var resourceName = $"{type.FullName}.Script.sql";

		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream == null)
		{
			return string.Empty;
		}

		using var reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	/// <summary>
	/// Executes custom logic after the migration SQL has been applied.
	/// </summary>
	/// <param name="serviceProvider">
	/// The service provider for resolving dependencies during post-migration processing.
	/// </param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// <para>
	/// This method is called after <see cref="GenerateSqlAsync"/> SQL has been successfully executed
	/// but before the transaction is committed. Use this for data seeding, complex transformations,
	/// or any operations that require .NET code rather than raw SQL.
	/// </para>
	/// <para>
	/// The default implementation does nothing. Override to add custom post-migration logic.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// public override async Task PostMigrateAsync(IServiceProvider serviceProvider)
	/// {
	///     var db = serviceProvider.GetRequiredService&lt;IDbService&gt;();
	///     await db.ExecuteAsync("INSERT INTO settings (key, value) VALUES ('Version', '1.0.0')");
	/// }
	/// </code>
	/// </example>
	public virtual Task PostMigrateAsync(IServiceProvider serviceProvider)
	{
		return Task.CompletedTask;
	}
}
