namespace WebVella.Database.Migrations;

/// <summary>
/// Internal record that holds metadata about a discovered migration.
/// </summary>
/// <remarks>
/// This record is used internally by <see cref="DbMigrationService"/> to track discovered
/// migrations and their instances during the migration execution process.
/// </remarks>
internal record DbMigrationMeta
{
	/// <summary>
	/// Gets or sets the version of the migration as specified by <see cref="DbMigrationAttribute"/>.
	/// </summary>
	public required Version Version { get; set; }

	/// <summary>
	/// Gets or sets the fully qualified class name of the migration.
	/// </summary>
	public required string MigrationClassName { get; set; }

	/// <summary>
	/// Gets or sets the instantiated migration object.
	/// </summary>
	public required DbMigration Instance { get; set; }
}