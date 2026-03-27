using BaseDbCommand = System.Data.Common.DbCommand;
using BaseDbConnection = System.Data.Common.DbConnection;
using BaseDbTransaction = System.Data.Common.DbTransaction;
using BaseDbParameter = System.Data.Common.DbParameter;
using BaseDbParameterCollection = System.Data.Common.DbParameterCollection;
using BaseDbDataReader = System.Data.Common.DbDataReader;

namespace WebVella.Database.Security;

/// <summary>
/// A <see cref="BaseDbCommand"/> wrapper that prepends <c>SET SESSION</c> statements for every
/// RLS context variable before the actual command text, ensuring Row Level Security context
/// is always active regardless of connection pool state.
/// </summary>
internal sealed class RlsDbCommand : BaseDbCommand
{
	private readonly NpgsqlCommand _inner;
	private readonly IRlsContextProvider _contextProvider;
	private readonly RlsOptions _options;
	private readonly Func<bool> _isSuppressed;
	private string _commandText = string.Empty;

	internal RlsDbCommand(
		NpgsqlCommand inner,
		IRlsContextProvider contextProvider,
		RlsOptions options,
		Func<bool> isSuppressed)
	{
		_inner = inner;
		_contextProvider = contextProvider;
		_options = options;
		_isSuppressed = isSuppressed;
	}

	/// <summary>
	/// Gets or sets the command text. The setter prepends RLS <c>SET SESSION</c> statements
	/// to the inner command so every execution carries the current security context.
	/// </summary>
	public override string CommandText
	{
		get => _commandText;
		set
		{
			_commandText = value ?? string.Empty;
			_inner.CommandText = BuildEffectiveCommandText(_commandText);
		}
	}

	public override int CommandTimeout
	{
		get => _inner.CommandTimeout;
		set => _inner.CommandTimeout = value;
	}

	public override CommandType CommandType
	{
		get => _inner.CommandType;
		set => _inner.CommandType = value;
	}

	public override bool DesignTimeVisible
	{
		get => _inner.DesignTimeVisible;
		set => _inner.DesignTimeVisible = value;
	}

	public override UpdateRowSource UpdatedRowSource
	{
		get => _inner.UpdatedRowSource;
		set => _inner.UpdatedRowSource = value;
	}

	protected override BaseDbConnection? DbConnection
	{
		get => _inner.Connection;
		set => _inner.Connection = (NpgsqlConnection?)value;
	}

	protected override BaseDbParameterCollection DbParameterCollection => _inner.Parameters;

	protected override BaseDbTransaction? DbTransaction
	{
		get => _inner.Transaction;
		set => _inner.Transaction = (NpgsqlTransaction?)value;
	}

	public override void Cancel() => _inner.Cancel();

	public override int ExecuteNonQuery() => _inner.ExecuteNonQuery();

	public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		=> _inner.ExecuteNonQueryAsync(cancellationToken);

	public override object? ExecuteScalar() => _inner.ExecuteScalar();

	public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
		=> _inner.ExecuteScalarAsync(cancellationToken);

	protected override BaseDbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		=> _inner.ExecuteReader(behavior);

	protected override async Task<BaseDbDataReader> ExecuteDbDataReaderAsync(
		CommandBehavior behavior, CancellationToken cancellationToken)
		=> await _inner.ExecuteReaderAsync(behavior, cancellationToken);

	public override void Prepare() => _inner.Prepare();

	public override Task PrepareAsync(CancellationToken cancellationToken = default)
		=> _inner.PrepareAsync(cancellationToken);

	protected override BaseDbParameter CreateDbParameter() => _inner.CreateParameter();

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_inner.Dispose();
		base.Dispose(disposing);
	}

	public override async ValueTask DisposeAsync()
	{
		await _inner.DisposeAsync();
		await base.DisposeAsync();
	}

	private string BuildEffectiveCommandText(string sql)
	{
		if (string.IsNullOrEmpty(sql))
			return sql;

		var prefix = BuildRlsPrefix();
		return string.IsNullOrEmpty(prefix) ? sql : $"{prefix} {sql}";
	}

	private string BuildRlsPrefix()
	{
		if (!_options.Enabled)
			return string.Empty;

		var settingName = _options.SettingName;
		var claimsNamespace = settingName.Contains('.')
			? settingName[..settingName.IndexOf('.')]
			: settingName;

		var statements = new List<string>();

		if (_isSuppressed())
		{
			statements.Add(BuildSetStatement(settingName, string.Empty));
			foreach (var claim in _contextProvider.CustomClaims)
			{
				statements.Add(BuildSetStatement($"{claimsNamespace}.{SanitizeKey(claim.Key)}", string.Empty));
			}
		}
		else if (_contextProvider.EntityId != null || _contextProvider.CustomClaims.Count > 0)
		{
			if (_contextProvider.EntityId != null)
				statements.Add(BuildSetStatement(settingName, _contextProvider.EntityId));

			foreach (var claim in _contextProvider.CustomClaims)
			{
				statements.Add(BuildSetStatement(
					$"{claimsNamespace}.{SanitizeKey(claim.Key)}",
					claim.Value ?? string.Empty));
			}
		}

		return statements.Count > 0 ? string.Join(" ", statements) : string.Empty;
	}

	private static string BuildSetStatement(string name, string value)
	{
		var escapedValue = value.Replace("'", "''");
		return $"SET SESSION {name} = '{escapedValue}';";
	}

	private static string SanitizeKey(string key)
		=> new string(key.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
}
