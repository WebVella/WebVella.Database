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

	#region <=== PostgreSQL ltree Extension Methods ===>

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>column @> 'path'::ltree</c>
	/// (checks if this path is an ancestor of the specified path).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="path">The descendant path to test against.</param>
	/// <returns>True if source is an ancestor of path.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var categories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeIsAncestorOf("root.electronics.phones"))
	///     .ToListAsync();
	/// // SQL: WHERE path @> 'root.electronics.phones'::ltree
	/// // Returns: root, root.electronics
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeIsAncestorOf(this string? source, string path)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeIsAncestorOf)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>column &lt;@ 'path'::ltree</c>
	/// (checks if this path is a descendant of the specified path).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="path">The ancestor path to test against.</param>
	/// <returns>True if source is a descendant of path.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var subcategories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeIsDescendantOf("root.electronics"))
	///     .ToListAsync();
	/// // SQL: WHERE path &lt;@ 'root.electronics'::ltree
	/// // Returns: root.electronics.phones, root.electronics.laptops, etc.
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeIsDescendantOf(this string? source, string path)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeIsDescendantOf)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>(column @> 'path'::ltree OR column = 'path'::ltree)</c>
	/// (checks if this path is an ancestor of or equal to the specified path).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="path">The path to test against.</param>
	/// <returns>True if source is an ancestor of or equal to path.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var categories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeIsAncestorOrEqual("root.electronics"))
	///     .ToListAsync();
	/// // Returns: root, root.electronics (includes the path itself)
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeIsAncestorOrEqual(this string? source, string path)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeIsAncestorOrEqual)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>(column &lt;@ 'path'::ltree OR column = 'path'::ltree)</c>
	/// (checks if this path is a descendant of or equal to the specified path).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="path">The path to test against.</param>
	/// <returns>True if source is a descendant of or equal to path.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var subcategories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeIsDescendantOrEqual("root.electronics"))
	///     .ToListAsync();
	/// // Returns: root.electronics, root.electronics.phones, root.electronics.laptops, etc.
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeIsDescendantOrEqual(this string? source, string path)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeIsDescendantOrEqual)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>column ~ 'pattern'::lquery</c>
	/// (checks if path matches an lquery pattern).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="pattern">The lquery pattern (supports wildcards like *, {}, etc.).</param>
	/// <returns>True if source matches the lquery pattern.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>lquery Pattern Syntax:</strong></para>
	/// <list type="bullet">
	/// <item><c>*</c> - matches any single label</item>
	/// <item><c>*.electronics.*</c> - any path with "electronics" in the middle</item>
	/// <item><c>root.{electronics,appliances}.*</c> - either electronics or appliances</item>
	/// </list>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var categories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeMatchesLQuery("root.*.phones"))
	///     .ToListAsync();
	/// // SQL: WHERE path ~ 'root.*.phones'::lquery
	/// // Returns: root.electronics.phones, root.accessories.phones, etc.
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeMatchesLQuery(this string? source, string pattern)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeMatchesLQuery)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>column @ 'query'::ltxtquery</c>
	/// (checks if path matches an ltxtquery full-text search query).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="query">The ltxtquery search expression.</param>
	/// <returns>True if source matches the ltxtquery.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>ltxtquery Syntax:</strong></para>
	/// <list type="bullet">
	/// <item><c>electronics &amp; phones</c> - path contains both labels</item>
	/// <item><c>electronics | appliances</c> - path contains either label</item>
	/// <item><c>!phones</c> - path does not contain "phones"</item>
	/// </list>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var categories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeMatchesLTxtQuery("electronics &amp; phones"))
	///     .ToListAsync();
	/// // SQL: WHERE path @ 'electronics &amp; phones'::ltxtquery
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeMatchesLTxtQuery(this string? source, string query)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeMatchesLTxtQuery)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>column ? ARRAY['path1'::ltree, 'path2'::ltree]</c>
	/// (checks if path matches any of the specified paths).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="paths">Array of paths to test against.</param>
	/// <returns>True if source matches any of the paths.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var categories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeContainsAny("root.electronics", "root.appliances"))
	///     .ToListAsync();
	/// // SQL: WHERE path ? ARRAY['root.electronics'::ltree, 'root.appliances'::ltree]
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeContainsAny(this string? source, params string[] paths)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeContainsAny)} is only supported inside query expression predicates.");

	/// <summary>
	/// Marker method for PostgreSQL ltree. Translates to <c>column ?&amp; ARRAY['path1'::ltree, 'path2'::ltree]</c>
	/// (checks if path contains all specified labels).
	/// </summary>
	/// <param name="source">The ltree path column.</param>
	/// <param name="paths">Array of paths/labels that must all be present.</param>
	/// <returns>True if source contains all of the specified paths.</returns>
	/// <remarks>
	/// Requires PostgreSQL ltree extension: <c>CREATE EXTENSION ltree;</c>
	/// <para><strong>Example:</strong></para>
	/// <code>
	/// var categories = await _db.Query&lt;Category&gt;()
	///     .Where(c => c.Path.LtreeContainsAll("root", "electronics", "phones"))
	///     .ToListAsync();
	/// // SQL: WHERE path ?&amp; ARRAY['root'::ltree, 'electronics'::ltree, 'phones'::ltree]
	/// </code>
	/// </remarks>
	/// <exception cref="InvalidOperationException">
	/// Always thrown when called directly outside an expression predicate.
	/// </exception>
	public static bool LtreeContainsAll(this string? source, params string[] paths)
		=> throw new InvalidOperationException(
			$"{nameof(LtreeContainsAll)} is only supported inside query expression predicates.");

	#endregion
}
