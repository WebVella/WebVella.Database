namespace WebVella.Database;

/// <summary>
/// Provides access to the database connection string.
/// Used internally by migration service to create RLS-free connections.
/// </summary>
public interface IDbConnectionStringAccessor
{
	/// <summary>
	/// Gets the database connection string.
	/// </summary>
	string ConnectionString { get; }
}

/// <summary>
/// Default implementation of <see cref="IDbConnectionStringAccessor"/>.
/// </summary>
internal class DbConnectionStringAccessor : IDbConnectionStringAccessor
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DbConnectionStringAccessor"/> class.
	/// </summary>
	/// <param name="connectionString">The database connection string.</param>
	public DbConnectionStringAccessor(string connectionString)
	{
		ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
	}

	/// <inheritdoc />
	public string ConnectionString { get; }
}
