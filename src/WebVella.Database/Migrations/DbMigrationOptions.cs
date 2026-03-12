namespace WebVella.Database.Migrations;

/// <summary>
/// Options for configuring the database migration service.
/// </summary>
public class DbMigrationOptions
{
	/// <summary>
	/// The default table name used to store the database version.
	/// </summary>
	public const string DefaultVersionTableName = "_db_version";

	/// <summary>
	/// The default name for the temporary update function.
	/// </summary>
	public const string DefaultUpdateFunctionName = "_db_update";

	/// <summary>
	/// The default name for the temporary update log table.
	/// </summary>
	public const string DefaultUpdateLogTableName = "_db_update_log_tbl";

	/// <summary>
	/// Gets or sets the name of the table used to track the current database version.
	/// Defaults to "_db_version".
	/// </summary>
	public string VersionTableName { get; set; } = DefaultVersionTableName;

	/// <summary>
	/// Gets or sets the name of the temporary PostgreSQL function used during migrations.
	/// Defaults to "_db_update".
	/// </summary>
	public string UpdateFunctionName { get; set; } = DefaultUpdateFunctionName;

	/// <summary>
	/// Gets or sets the name of the temporary table used to log migration statements.
	/// Defaults to "_db_update_log_tbl".
	/// </summary>
	public string UpdateLogTableName { get; set; } = DefaultUpdateLogTableName;
}
