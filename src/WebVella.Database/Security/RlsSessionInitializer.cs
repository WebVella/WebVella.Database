namespace WebVella.Database.Security;

/// <summary>
/// Initializes PostgreSQL session variables for Row Level Security (RLS) policy evaluation.
/// </summary>
/// <remarks>
/// <para>
/// This class is responsible for setting session-level variables that PostgreSQL RLS policies
/// can reference using <c>current_setting('app.variable_name')</c>. Variables are set when
/// a new database connection is created.
/// </para>
/// <para>
/// The initializer uses <c>set_config()</c> function which allows setting variables that
/// may not be predefined in <c>postgresql.conf</c>. When using local mode (default),
/// variables are automatically reset when the transaction ends.
/// </para>
/// </remarks>
internal class RlsSessionInitializer
{
	private readonly IRlsContextProvider _contextProvider;
	private readonly RlsOptions _options;

	/// <summary>
	/// Initializes a new instance of <see cref="RlsSessionInitializer"/>.
	/// </summary>
	/// <param name="contextProvider">The provider for RLS context values.</param>
	/// <param name="options">The RLS configuration options.</param>
	public RlsSessionInitializer(IRlsContextProvider contextProvider, RlsOptions options)
	{
		_contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
		_options = options ?? throw new ArgumentNullException(nameof(options));
	}

	/// <summary>
	/// Gets a value indicating whether there is any RLS context to initialize.
	/// </summary>
	public bool HasContext =>
		_options.Enabled &&
		(_contextProvider.EntityId != null ||
		 _contextProvider.CustomClaims.Count > 0);

	/// <summary>
	/// Initializes RLS session variables on the specified connection.
	/// </summary>
	/// <param name="connection">The PostgreSQL connection to initialize.</param>
	public void InitializeSession(NpgsqlConnection connection)
	{
		if (!HasContext)
			return;

		var sql = BuildInitializationSql();
		if (string.IsNullOrEmpty(sql))
			return;

		using var command = new NpgsqlCommand(sql, connection);
		command.ExecuteNonQuery();
	}

	/// <summary>
	/// Asynchronously initializes RLS session variables on the specified connection.
	/// </summary>
	/// <param name="connection">The PostgreSQL connection to initialize.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task InitializeSessionAsync(NpgsqlConnection connection)
	{
		if (!HasContext)
			return;

		var sql = BuildInitializationSql();
		if (string.IsNullOrEmpty(sql))
			return;

		await using var command = new NpgsqlCommand(sql, connection);
		await command.ExecuteNonQueryAsync();
	}

	/// <summary>
	/// Builds the SQL statements to initialize all RLS session variables.
	/// </summary>
	private string BuildInitializationSql()
	{
		var statements = new List<string>();
		var isLocal = _options.UseLocalSettings ? "true" : "false";
		var settingName = _options.SettingName;
		var claimsNamespace = settingName.Contains('.')
			? settingName[..settingName.IndexOf('.')]
			: settingName;

		if (_contextProvider.EntityId != null)
		{
			statements.Add(BuildSetConfigStatement(settingName, _contextProvider.EntityId, isLocal));
		}

		foreach (var claim in _contextProvider.CustomClaims)
		{
			var key = SanitizeKey(claim.Key);
			var value = claim.Value ?? string.Empty;
			statements.Add(BuildSetConfigStatement($"{claimsNamespace}.{key}", value, isLocal));
		}

		return statements.Count > 0 ? string.Join("; ", statements) : string.Empty;
	}

	/// <summary>
	/// Builds a set_config() statement for a single variable.
	/// </summary>
	private static string BuildSetConfigStatement(string name, string value, string isLocal)
	{
		var escapedValue = value.Replace("'", "''");
		return $"SELECT set_config('{name}', '{escapedValue}', {isLocal})";
	}

	/// <summary>
	/// Sanitizes a key to ensure it only contains valid characters for PostgreSQL variable names.
	/// </summary>
	private static string SanitizeKey(string key)
	{
		return new string(key.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
	}
}
