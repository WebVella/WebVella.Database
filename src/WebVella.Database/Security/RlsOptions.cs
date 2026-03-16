namespace WebVella.Database.Security;

/// <summary>
/// Configuration options for Row Level Security (RLS) session context initialization.
/// </summary>
public class RlsOptions
{
	/// <summary>
	/// Gets or sets the prefix used for all RLS session variables.
	/// Default is "app".
	/// </summary>
	/// <remarks>
	/// Variables will be set as <c>{Prefix}.tenant_id</c>, <c>{Prefix}.user_id</c>, etc.
	/// This allows you to use <c>current_setting('app.tenant_id')</c> in your RLS policies.
	/// </remarks>
	public string Prefix { get; set; } = "app";

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
