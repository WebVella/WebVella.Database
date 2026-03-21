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
///       Create an embedded resource file in the same assembly. The base implementation searches
///       for a matching resource using the following priority (all case-insensitive):
///       <list type="number">
///         <item><description><c>{TypeName}.Script.psql</c></description></item>
///         <item><description><c>{TypeName}.Script.sql</c></description></item>
///         <item><description><c>{FullTypeName}.Script.psql</c></description></item>
///         <item><description><c>{FullTypeName}.Script.sql</c></description></item>
///       </list>
///       Alternatively, set <see cref="DbMigrationAttribute.ScriptPath"/> to explicitly specify
///       the embedded resource name, bypassing automatic discovery entirely.
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
/// // Method 2: Automatic embedded resource discovery (no override needed)
/// [DbMigration("1.0.1.0")]
/// public class Migration_1_0_1_0 : DbMigration { }
/// // Requires: Migration_1_0_1_0.Script.psql or Migration_1_0_1_0.Script.sql as embedded resource
/// 
/// // Method 3: Explicit embedded resource path
/// [DbMigration("1.0.2.0", "MyApp.Migrations.Shared.CommonSetup.Script.psql")]
/// public class Migration_1_0_2_0 : DbMigration { }
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
	/// If <see cref="DbMigrationAttribute.ScriptPath"/> is set on the migration class, only that
	/// specific embedded resource is used (case-insensitive lookup). A <see cref="DbMigrationException"/>
	/// is thrown immediately if the specified resource cannot be found.
	/// </para>
	/// <para>
	/// When no <see cref="DbMigrationAttribute.ScriptPath"/> is set, the following resource name
	/// patterns are tried in order (all case-insensitive). Returns an empty string if none match:
	/// <list type="number">
	///   <item><description>Resource ending with <c>{TypeName}.Script.psql</c></description></item>
	///   <item><description>Resource ending with <c>{TypeName}.Script.sql</c></description></item>
	///   <item><description>Resource ending with <c>{FullTypeName}.Script.psql</c></description></item>
	///   <item><description>Resource ending with <c>{FullTypeName}.Script.sql</c></description></item>
	/// </list>
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
		var allResources = assembly.GetManifestResourceNames();

		var attr = type.GetCustomAttributes(typeof(DbMigrationAttribute), inherit: false)
			.OfType<DbMigrationAttribute>()
			.FirstOrDefault();

		if (!string.IsNullOrWhiteSpace(attr?.ScriptPath))
		{
			var explicitResource =
				allResources.FirstOrDefault(r => r.Equals(attr.ScriptPath, StringComparison.OrdinalIgnoreCase))
				?? allResources.FirstOrDefault(r => r.EndsWith(attr.ScriptPath, StringComparison.OrdinalIgnoreCase));

			if (explicitResource is null)
				throw new DbMigrationException(
					$"Embedded resource script '{attr.ScriptPath}' specified on migration " +
					$"'{type.FullName}' was not found in assembly '{assembly.GetName().Name}'.",
					[]);

			using var explicitStream = assembly.GetManifestResourceStream(explicitResource)!;
			using var explicitReader = new StreamReader(explicitStream);
			return await explicitReader.ReadToEndAsync();
		}

		var resourceName =
			allResources.FirstOrDefault(r => r.EndsWith($"{type.Name}.Script.psql", StringComparison.OrdinalIgnoreCase))
			?? allResources.FirstOrDefault(r => r.EndsWith($"{type.Name}.Script.sql", StringComparison.OrdinalIgnoreCase))
			?? allResources.FirstOrDefault(r => r.EndsWith($"{type.FullName}.Script.psql", StringComparison.OrdinalIgnoreCase))
			?? allResources.FirstOrDefault(r => r.EndsWith($"{type.FullName}.Script.sql", StringComparison.OrdinalIgnoreCase));

		if (resourceName is null)
			return string.Empty;

		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream is null)
			return string.Empty;

		using var reader = new StreamReader(stream);
		return await reader.ReadToEndAsync();
	}

	/// <summary>
	/// Executes custom logic before the migration SQL is applied.
	/// </summary>
	/// <param name="serviceProvider">
	/// The service provider for resolving dependencies during pre-migration processing.
	/// </param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>
	/// <para>
	/// This method is called before <see cref="GenerateSqlAsync"/> SQL is executed, inside the
	/// migration transaction. Use this for preparatory steps such as disabling triggers,
	/// dropping indexes before bulk operations, or validating preconditions.
	/// </para>
	/// <para>
	/// The default implementation does nothing. Override to add custom pre-migration logic.
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// public override async Task PreMigrateAsync(IServiceProvider serviceProvider)
	/// {
	///     var db = serviceProvider.GetRequiredService&lt;IDbService&gt;();
	///     await db.ExecuteAsync("ALTER TABLE orders DISABLE TRIGGER ALL");
	/// }
	/// </code>
	/// </example>
	public virtual Task PreMigrateAsync(IServiceProvider serviceProvider)
	{
		return Task.CompletedTask;
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
