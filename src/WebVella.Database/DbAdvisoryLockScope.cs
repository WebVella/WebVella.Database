namespace WebVella.Database;

/// <summary>
/// Represents a scope that manages a PostgreSQL advisory lock for a database connection.
/// </summary>
public interface IDbAdvisoryLockScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the database connection associated with this advisory lock scope.
    /// </summary>
	public IDbConnection Connection { get; }

    /// <summary>
    /// Releases the acquired advisory lock and marks the scope as completed.
    /// </summary>
	public void Complete();

    /// <summary>
    /// Asynchronously releases the acquired advisory lock and marks the scope as completed.
    /// </summary>
	public Task CompleteAsync();
}

/// <summary>
/// Provides an implementation of <see cref="IDbAdvisoryLockScope"/> for managing
/// PostgreSQL advisory locks in a database connection.
/// </summary>
internal class DbAdvisoryLockScope : IDbAdvisoryLockScope
{
	private bool _isCompleted = false;
	private bool _shouldDispose = true;
	private DbConnectionContext? _connectionCtx;
	private DbConnection? _connection;

	/// <summary>
	/// Gets the database connection associated with this advisory lock scope.
	/// </summary>
	public IDbConnection Connection { get { return _connection!; } }

	/// <summary>
	/// Initializes a new instance of the <see cref="DbAdvisoryLockScope"/> class for async factory usage.
	/// </summary>
	private DbAdvisoryLockScope() { }

	/// <summary>
	/// Asynchronously creates a new <see cref="DbAdvisoryLockScope"/> and acquires an advisory lock
	/// using the specified connection context and lock key.
	/// </summary>
	/// <remarks>
	/// The caller must ensure that <see cref="DbConnectionContext"/> is created synchronously before
	/// calling this method, so that the <see cref="AsyncLocal{T}"/> context ID is set in the caller's
	/// execution context.
	/// </remarks>
	/// <param name="connectionCtx">The connection context to use.</param>
	/// <param name="shouldDispose">
	/// Whether this scope should dispose the connection and context when disposed.
	/// </param>
	/// <param name="lockKey">The advisory lock key to acquire.</param>
	/// <returns>A configured <see cref="DbAdvisoryLockScope"/> instance.</returns>
	internal static async Task<DbAdvisoryLockScope> CreateAsync(
		DbConnectionContext connectionCtx, bool shouldDispose, long lockKey)
	{
		var scope = new DbAdvisoryLockScope();
		scope._connectionCtx = connectionCtx;
		scope._shouldDispose = shouldDispose;

		if (!shouldDispose)
		{
			if (scope._connectionCtx._connectionStack.Count > 0)
			{
				scope._connection = scope._connectionCtx._connectionStack.Peek();
			}
			else
			{
				scope._connection = scope._connectionCtx.CreateConnection();
			}
		}
		else
		{
			scope._connection = scope._connectionCtx.CreateConnection();
		}

		await scope._connection.AcquireAdvisoryLockAsync(lockKey);

		return scope;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DbAdvisoryLockScope"/> class and acquires
	/// an advisory lock using the specified connection string and lock key.
	/// </summary>
	/// <param name="connectionString">The connection string for the database.</param>
	/// <param name="lockKey">The advisory lock key to acquire.</param>
	internal DbAdvisoryLockScope(string connectionString, long lockKey)
	{
		var currentCtx = DbConnectionContext.GetCurrentContext();

		if (currentCtx != null)
		{
			_connectionCtx = currentCtx;
			if (_connectionCtx._connectionStack.Count > 0)
			{
				_connection = _connectionCtx._connectionStack.Peek();
			}
			else
			{
				_connection = _connectionCtx.CreateConnection();
			}

			_shouldDispose = false;
		}
		else
		{
			_connectionCtx = DbConnectionContext.CreateContext(connectionString);
			_connection = _connectionCtx.CreateConnection();
		}

		_connection.AcquireAdvisoryLock(lockKey);
	}

    /// <summary>
    /// Releases the acquired advisory lock and marks the scope as completed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the advisory lock scope is already completed.</exception>
	public void Complete()
	{
		if (_isCompleted)
		{
			throw new InvalidOperationException("AdvisoryLockScope is already completed.");
		}

		_connection!.ReleaseAdvisoryLock();

		_isCompleted = true;
	}

    /// <summary>
    /// Asynchronously releases the acquired advisory lock and marks the scope as completed.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the advisory lock scope is already completed.</exception>
	public async Task CompleteAsync()
	{
		if (_isCompleted)
		{
			throw new InvalidOperationException("AdvisoryLockScope is already completed.");
		}

		await _connection!.ReleaseAdvisoryLockAsync();

		_isCompleted = true;
	}

	/// <summary>
	/// Releases all resources used by the <see cref="DbAdvisoryLockScope"/> instance.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases the resources used by the <see cref="DbAdvisoryLockScope"/> instance.
	/// </summary>
	/// <param name="disposing">
	/// True to release both managed and unmanaged resources; false to release only unmanaged resources.
	/// </param>
	private void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (!_isCompleted)
			{
				_connection!.ReleaseAdvisoryLock();
			}

			if (_shouldDispose)
			{
				_connection!.Dispose();
				_connection = null;

				_connectionCtx!.Dispose();
				_connectionCtx = null;
			}
		}
	}

	/// <summary>
	/// Asynchronously releases all resources used by the <see cref="DbAdvisoryLockScope"/> instance.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		if (!_isCompleted)
		{
			await _connection!.ReleaseAdvisoryLockAsync();
		}

		if (_shouldDispose)
		{
			await _connection!.DisposeAsync();
			_connection = null;

			await _connectionCtx!.DisposeAsync();
			_connectionCtx = null;
		}

		GC.SuppressFinalize(this);
	}
}
