namespace WebVella.Database.Security;

/// <summary>
/// Provides the current security context for PostgreSQL Row Level Security (RLS) policies.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to provide an entity identifier and custom claims that will be set
/// as PostgreSQL session variables before each database operation. These variables can then
/// be referenced in RLS policies using <c>current_setting('app.variable_name')</c>.
/// </para>
/// <para>
/// Example RLS policy using entity isolation:
/// <code>
/// CREATE POLICY entity_isolation ON orders
///     USING (entity_id = current_setting('app.entity_id'));
/// </code>
/// </para>
/// </remarks>
public interface IRlsContextProvider
{
	/// <summary>
	/// Gets the current entity identifier for RLS filtering.
	/// </summary>
	/// <remarks>
	/// When set, this value will be available in PostgreSQL as <c>current_setting('app.entity_id')</c>.
	/// </remarks>
	string? EntityId { get; }

	/// <summary>
	/// Gets additional custom claims to be set as session variables.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Each key-value pair will be set as a PostgreSQL session variable with the format
	/// <c>app.{key}</c>. For example, a claim with key "role" will be accessible as
	/// <c>current_setting('app.role')</c>.
	/// </para>
	/// <para>
	/// Keys should contain only alphanumeric characters and underscores.
	/// Values will be properly escaped to prevent SQL injection.
	/// </para>
	/// </remarks>
	IReadOnlyDictionary<string, string> CustomClaims { get; }
}
