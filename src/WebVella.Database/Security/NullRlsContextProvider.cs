namespace WebVella.Database.Security;

/// <summary>
/// A null implementation of <see cref="IRlsContextProvider"/> that provides no security context.
/// </summary>
/// <remarks>
/// This implementation is used when no RLS context provider is configured, effectively
/// disabling Row Level Security filtering at the application level.
/// </remarks>
public sealed class NullRlsContextProvider : IRlsContextProvider
{
	/// <summary>
	/// Gets the singleton instance of <see cref="NullRlsContextProvider"/>.
	/// </summary>
	public static NullRlsContextProvider Instance { get; } = new();

	private static readonly IReadOnlyDictionary<string, string> EmptyDictionary =
		new Dictionary<string, string>().AsReadOnly();

	private NullRlsContextProvider() { }

	/// <inheritdoc />
	/// <remarks>Always returns <c>null</c>.</remarks>
	public string? EntityId => null;

	/// <inheritdoc />
	/// <remarks>Always returns an empty dictionary.</remarks>
	public IReadOnlyDictionary<string, string> CustomClaims => EmptyDictionary;
}
