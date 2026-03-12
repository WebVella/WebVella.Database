namespace WebVella.Database.Migrations;

/// <summary>
/// Exception thrown when a database migration fails during execution.
/// </summary>
/// <remarks>
/// This exception contains detailed logs of all SQL statements executed during the migration,
/// including which statements succeeded and which failed. Use the <see cref="MigrationLogs"/>
/// property to inspect the execution details and identify the failing statement.
/// </remarks>
public class DbMigrationException : Exception
{
	/// <summary>
	/// Gets the log items containing details about each SQL statement executed during the migration.
	/// </summary>
	/// <remarks>
	/// Each log item contains the SQL statement, whether it succeeded, and any error message
	/// if it failed. The logs are in execution order, so the last item with <c>Success = false</c>
	/// indicates the statement that caused the migration to fail.
	/// </remarks>
	public IEnumerable<DbMigrationLogItem> MigrationLogs { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DbMigrationException"/> class.
	/// </summary>
	/// <param name="message">The error message describing the migration failure.</param>
	/// <param name="logs">The collection of log items from the migration execution.</param>
	public DbMigrationException(string message, IEnumerable<DbMigrationLogItem> logs) : base(message)
	{
		MigrationLogs = logs;
	}
}