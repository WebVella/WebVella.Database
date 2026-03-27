using BaseDbCommand = System.Data.Common.DbCommand;
using BaseDbConnection = System.Data.Common.DbConnection;
using BaseDbTransaction = System.Data.Common.DbTransaction;

namespace WebVella.Database.Security;

/// <summary>
/// A <see cref="BaseDbConnection"/> wrapper that returns <see cref="RlsDbCommand"/> instances
/// from <see cref="CreateDbCommand"/>, ensuring every Dapper command has the RLS session
/// variables injected before execution.
/// </summary>
/// <remarks>
/// This wrapper does not own the inner <see cref="NpgsqlConnection"/>; the connection lifecycle
/// is managed by <see cref="DbConnectionContext"/>.
/// </remarks>
internal sealed class RlsDbConnection : BaseDbConnection
{
	private readonly NpgsqlConnection _inner;
	private readonly IRlsContextProvider _contextProvider;
	private readonly RlsOptions _options;
	private readonly Func<bool> _isSuppressed;

	internal RlsDbConnection(
		NpgsqlConnection inner,
		IRlsContextProvider contextProvider,
		RlsOptions options,
		Func<bool> isSuppressed)
	{
		_inner = inner;
		_contextProvider = contextProvider;
		_options = options;
		_isSuppressed = isSuppressed;
	}

	public override string ConnectionString
	{
		get => _inner.ConnectionString;
		set => _inner.ConnectionString = value;
	}

	public override string Database => _inner.Database;

	public override ConnectionState State => _inner.State;

	public override string DataSource => _inner.DataSource;

	public override string ServerVersion => _inner.ServerVersion;

	public override void Open() => _inner.Open();

	public override Task OpenAsync(CancellationToken cancellationToken)
		=> _inner.OpenAsync(cancellationToken);

	public override void Close() => _inner.Close();

	public override Task CloseAsync() => _inner.CloseAsync();

	public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

	protected override BaseDbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		=> _inner.BeginTransaction(isolationLevel);

	protected override async ValueTask<BaseDbTransaction> BeginDbTransactionAsync(
		IsolationLevel isolationLevel, CancellationToken cancellationToken)
		=> await _inner.BeginTransactionAsync(isolationLevel, cancellationToken);

	/// <summary>
	/// Creates an <see cref="RlsDbCommand"/> that wraps a new <see cref="NpgsqlCommand"/>,
	/// so all Dapper-generated commands automatically receive the RLS prefix.
	/// </summary>
	protected override BaseDbCommand CreateDbCommand()
	{
		var cmd = _inner.CreateCommand();
		return new RlsDbCommand(cmd, _contextProvider, _options, _isSuppressed);
	}

	protected override void Dispose(bool disposing)
	{
		// _inner is owned by DbConnectionContext — do not dispose it here.
		base.Dispose(disposing);
	}

	public override ValueTask DisposeAsync()
	{
		// _inner is owned by DbConnectionContext — do not dispose it here.
		return base.DisposeAsync();
	}
}
