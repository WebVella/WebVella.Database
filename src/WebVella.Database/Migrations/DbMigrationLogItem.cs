namespace WebVella.Database.Migrations;

/// <summary>
/// Represents a log entry for a single SQL statement executed during a database migration.
/// </summary>
/// <remarks>
/// These log items are collected during migration execution and are included in
/// <see cref="DbMigrationException"/> when a migration fails, providing detailed
/// information for debugging migration issues.
/// </remarks>
public class DbMigrationLogItem
{
	/// <summary>
	/// Gets or sets the migration version that this statement belongs to.
	/// </summary>
	public string? Version { get; set; }

	/// <summary>
	/// Gets or sets the SQL statement that was executed.
	/// </summary>
	public string? Statement { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the SQL statement executed successfully.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets the SQL error message if the statement failed; otherwise, <c>null</c>.
	/// </summary>
	public string? SqlError { get; set; }
}