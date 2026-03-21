namespace WebVella.Database;

/// <summary>
/// String extension methods that act as expression-tree markers for the
/// <see cref="DbExpressionTranslator{T}"/> to emit PostgreSQL <c>ILIKE</c>
/// predicates (case-insensitive pattern matching).
/// </summary>
/// <remarks>
/// These methods are <b>not</b> intended to be called at runtime. Use them
/// only inside <c>.Where()</c> expression predicates passed to
/// <see cref="DbQuery{T}"/>. Calling them directly throws
/// <see cref="InvalidOperationException"/>.
/// <para><strong>Usage:</strong></para>
/// <code>
/// var results = await _db.Query&lt;User&gt;()
///     .Where(e => e.Name.ILikeContains("admin"))
///     .ToListAsync();
/// // SQL: WHERE name ILIKE '%admin%'
/// </code>
/// </remarks>
public static class DbStringExtensions
{
	/// <summary>
	/// Marker method. Translates to <c>column ILIKE '%value%'</c> inside a
	/// query expression predicate.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool ILikeContains(this string? source, string value)
		=> throw new InvalidOperationException(
			$"{nameof(ILikeContains)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method. Translates to <c>column ILIKE 'value%'</c> inside a
	/// query expression predicate.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool ILikeStartsWith(this string? source, string value)
		=> throw new InvalidOperationException(
			$"{nameof(ILikeStartsWith)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method. Translates to <c>column ILIKE '%value'</c> inside a
	/// query expression predicate.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool ILikeEndsWith(this string? source, string value)
		=> throw new InvalidOperationException(
			$"{nameof(ILikeEndsWith)} is only supported inside query expression predicates.");
}
