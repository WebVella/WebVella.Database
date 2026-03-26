namespace WebVella.Database.Security;

/// <summary>
/// Configuration options for Row Level Security (RLS) session context initialization.
/// </summary>
public class RlsOptions
{
	/// <summary>
	/// Gets or sets the full PostgreSQL session variable name used for the RLS entity identifier.
	/// Default is "app.user_id".
	/// </summary>
	/// <remarks>
	/// This value is used directly as the PostgreSQL variable name when setting the entity identifier,
	/// e.g. <c>set_config('app.user_id', value, true)</c>.
	/// Custom claims derive their namespace from the part before the first dot of this value,
	/// so a setting name of <c>"app.user_id"</c> will place custom claims under <c>app.{key}</c>.
	/// </remarks>
	public string SettingName { get; set; } = "app.user_id";

	/// <summary>
	/// Gets or sets whether to use local (transaction-scoped) settings.
	/// Default is <c>true</c>.
	/// </summary>
	/// <remarks>
	/// When <c>true</c>, session variables are scoped to the current transaction using
	/// <c>set_config(name, value, true)</c>. When <c>false</c>, variables persist for
	/// the entire session using <c>SET</c> statements.
	/// </remarks>
	public bool UseLocalSettings { get; set; } = true;

	/// <summary>
	/// Gets or sets whether RLS context initialization is enabled.
	/// Default is <c>true</c>.
	/// </summary>
	/// <remarks>
	/// When disabled, no session variables will be set even if an <see cref="IRlsContextProvider"/>
	/// is registered. This can be useful for bypassing RLS in administrative scenarios.
	/// </remarks>
	public bool Enabled { get; set; } = true;
}
