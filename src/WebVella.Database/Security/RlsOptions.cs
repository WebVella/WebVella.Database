namespace WebVella.Database.Security;

/// <summary>
/// Configuration options for Row Level Security (RLS) session context initialization.
/// </summary>
public class RlsOptions
{
	internal static string DefaultSectionName = "RlsOptions";

	/// <summary>
	/// Gets or sets the full PostgreSQL session variable name used for the RLS entity identifier.
	/// Default is "app.user_id".
	/// </summary>
	/// <remarks>
	/// This value is used directly as the PostgreSQL variable name when setting the entity identifier
	/// using transaction-scoped (LOCAL) settings, e.g. <c>SET LOCAL app.user_id = 'value'</c>.
	/// Custom claims derive their namespace from the part before the first dot of this value,
	/// so a setting name of <c>"app.user_id"</c> will place custom claims under <c>app.{key}</c>.
	/// </remarks>
	public string SettingName { get; set; } = "app.user_id";

	/// <summary>
	/// Gets or sets whether RLS context initialization is enabled.
	/// Default is <c>true</c>.
	/// </summary>
	/// <remarks>
	/// When disabled, no session variables will be set even if an <see cref="IRlsContextProvider"/>
	/// is registered. This can be useful for bypassing RLS in administrative scenarios.
	/// </remarks>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the PostgreSQL username to use for RLS-enabled connections.
	/// Required when <see cref="Enabled"/> is <c>true</c>.
	/// </summary>
	/// <remarks>
	/// When <see cref="Enabled"/> is <c>true</c>, the default connection string's username
	/// is replaced with this value so that RLS policies apply to the correct database role.
	/// </remarks>
	public string? SqlUser { get; set; }

	/// <summary>
	/// Gets or sets the PostgreSQL password to use for RLS-enabled connections.
	/// Required when <see cref="Enabled"/> is <c>true</c>.
	/// </summary>
	/// <remarks>
	/// When <see cref="Enabled"/> is <c>true</c>, the default connection string's password
	/// is replaced with this value so that RLS policies apply to the correct database role.
	/// </remarks>
	public string? SqlPassword { get; set; }
}
