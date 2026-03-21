namespace WebVella.Database;

/// <summary>
/// A fluent, expression-tree-based query builder that generates parameterized PostgreSQL
/// SQL without requiring developers to write SQL strings.
/// </summary>
/// <remarks>
/// Obtain an instance via <see cref="IDbService.Query{T}()"/>.
/// Chain methods to build the query, then call a terminal method to execute it.
///
/// <para><strong>Example:</strong></para>
/// <code>
/// var emails = await _db.Query&lt;Email&gt;()
///     .Where(e => e.Status == EmailStatus.Pending &amp;&amp; e.RetryCount &lt; 3)
///     .OrderByDescending(e => e.CreatedOn)
///     .Limit(50)
///     .ToListAsync();
///
/// long count  = await _db.Query&lt;Email&gt;().Where(e => e.IsActive).CountAsync();
/// bool exists = await _db.Query&lt;Email&gt;().Where(e => e.Id == id).ExistsAsync();
/// </code>
///
/// <para><strong>Supported WHERE patterns:</strong></para>
/// <list type="bullet">
/// <item>Equality / comparison: <c>e.Price &gt; 10</c>, <c>e.Name != null</c></item>
/// <item>Logical operators: <c>&amp;&amp;</c>, <c>||</c>, <c>!</c></item>
/// <item>Boolean shorthand: <c>e.IsActive</c>, <c>!e.IsActive</c></item>
/// <item>Null checks: <c>e.Description == null</c> → <c>IS NULL</c></item>
/// <item>String matching: <c>.Contains()</c>, <c>.StartsWith()</c>,
///     <c>.EndsWith()</c></item>
/// <item>Case-insensitive: <c>.ILikeContains()</c>, <c>.ILikeStartsWith()</c>,
///     <c>.ILikeEndsWith()</c> → <c>ILIKE</c></item>
/// <item>Collection membership: <c>ids.Contains(e.Id)</c> →
///     <c>id = ANY(@p)</c></item>
/// <item>Multiple <c>.Where()</c> calls combined with AND</item>
/// </list>
/// </remarks>
/// <typeparam name="T">The entity type to query.</typeparam>
public sealed class DbQuery<T> where T : class
{
	private readonly IDbService _db;
	private readonly List<Expression<Func<T, bool>>> _where = [];
	private readonly List<(string PropertyName, bool Descending)> _orderBy = [];
	private int? _limit;
	private int? _offset;

	internal DbQuery(IDbService db)
	{
		_db = db;
	}

	#region <=== Builder Methods ===>

	/// <summary>
	/// Adds a WHERE predicate. Calling this method multiple times combines all
	/// predicates with AND.
	/// </summary>
	/// <param name="predicate">
	/// A lambda expression that describes the filter condition.
	/// </param>
	public DbQuery<T> Where(Expression<Func<T, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		_where.Add(predicate);
		return this;
	}

	/// <summary>
	/// Adds a primary ascending ORDER BY for the specified property.
	/// </summary>
	public DbQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: false));
		return this;
	}

	/// <summary>
	/// Adds a primary descending ORDER BY for the specified property.
	/// </summary>
	public DbQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: true));
		return this;
	}

	/// <summary>
	/// Adds a secondary ascending ORDER BY (equivalent to SQL ThenBy) for the
	/// specified property.
	/// </summary>
	public DbQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: false));
		return this;
	}

	/// <summary>
	/// Adds a secondary descending ORDER BY for the specified property.
	/// </summary>
	public DbQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: true));
		return this;
	}

	/// <summary>
	/// Limits the number of rows returned (SQL <c>LIMIT</c>).
	/// </summary>
	/// <param name="count">Maximum number of rows to return. Must be ≥ 0.</param>
	public DbQuery<T> Limit(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_limit = count;
		return this;
	}

	/// <summary>
	/// Skips the specified number of rows before returning results (SQL
	/// <c>OFFSET</c>).
	/// </summary>
	/// <param name="count">Number of rows to skip. Must be ≥ 0.</param>
	public DbQuery<T> Offset(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_offset = count;
		return this;
	}

	/// <summary>
	/// Applies 1-based page-number pagination by setting <c>LIMIT</c> and
	/// <c>OFFSET</c> together.
	/// </summary>
	/// <param name="page">
	/// 1-based page number. Defaults to <c>1</c> when <see langword="null"/>.
	/// Must be ≥ 1 after defaulting.
	/// </param>
	/// <param name="pageSize">
	/// Number of rows per page. Defaults to <c>10</c> when
	/// <see langword="null"/>. Must be ≥ 1 after defaulting.
	/// </param>
	/// <exception cref="ArgumentException">
	/// Thrown when both <paramref name="page"/> and <paramref name="pageSize"/>
	/// are <see langword="null"/>.
	/// </exception>
	public DbQuery<T> WithPaging(int? page, int? pageSize)
	{
		if (page is null && pageSize is null)
			throw new ArgumentException(
				$"At least one of '{nameof(page)}' or '{nameof(pageSize)}' must be provided.");

		var resolvedPage     = page     ?? 1;
		var resolvedPageSize = pageSize ?? 10;

		ArgumentOutOfRangeException.ThrowIfLessThan(resolvedPage,     1, nameof(page));
		ArgumentOutOfRangeException.ThrowIfLessThan(resolvedPageSize, 1, nameof(pageSize));

		_limit  = resolvedPageSize;
		_offset = (resolvedPage - 1) * resolvedPageSize;
		return this;
	}

	#endregion

	#region <=== Terminal Methods — Async ===>

	/// <summary>
	/// Executes the query and returns all matching entities.
	/// </summary>
	public async Task<IEnumerable<T>> ToListAsync()
	{
		var (sql, dp) = BuildSelectSql();
		return await _db.QueryAsync<T>(sql, dp);
	}

	/// <summary>
	/// Executes the query and returns the first matching entity, or
	/// <see langword="null"/> if no rows match. Appends <c>LIMIT 1</c>
	/// internally for efficiency.
	/// </summary>
	public async Task<T?> FirstOrDefaultAsync()
	{
		var (sql, dp) = BuildSelectSql(limitOverride: 1);
		return (await _db.QueryAsync<T>(sql, dp)).FirstOrDefault();
	}

	/// <summary>
	/// Executes a <c>SELECT COUNT(*)</c> query and returns the number of
	/// matching rows.
	/// </summary>
	public async Task<long> CountAsync()
	{
		var (sql, dp) = BuildAggregateSql("COUNT(*)");
		return await _db.ExecuteScalarAsync<long>(sql, dp);
	}

	/// <summary>
	/// Returns <see langword="true"/> if at least one row matches the query.
	/// Uses <c>SELECT EXISTS(SELECT 1 ...)</c> for efficiency.
	/// </summary>
	public async Task<bool> ExistsAsync()
	{
		var (inner, dp) = BuildExistsInner();
		return await _db.ExecuteScalarAsync<bool>($"SELECT EXISTS({inner})", dp);
	}

	#endregion

	#region <=== Terminal Methods — Sync ===>

	/// <summary>
	/// Executes the query synchronously and returns all matching entities.
	/// </summary>
	public IEnumerable<T> ToList()
	{
		var (sql, dp) = BuildSelectSql();
		return _db.Query<T>(sql, dp);
	}

	/// <summary>
	/// Executes the query synchronously and returns the first matching entity,
	/// or <see langword="null"/> if no rows match. Appends <c>LIMIT 1</c>.
	/// </summary>
	public T? FirstOrDefault()
	{
		var (sql, dp) = BuildSelectSql(limitOverride: 1);
		return _db.Query<T>(sql, dp).FirstOrDefault();
	}

	/// <summary>
	/// Executes a synchronous <c>SELECT COUNT(*)</c> and returns the count.
	/// </summary>
	public long Count()
	{
		var (sql, dp) = BuildAggregateSql("COUNT(*)");
		return _db.ExecuteScalar<long>(sql, dp);
	}

	/// <summary>
	/// Returns <see langword="true"/> synchronously if at least one row matches.
	/// </summary>
	public bool Exists()
	{
		var (inner, dp) = BuildExistsInner();
		return _db.ExecuteScalar<bool>($"SELECT EXISTS({inner})", dp);
	}

	#endregion

	#region <=== SQL Building ===>

	private (string Sql, DynamicParameters Parameters) BuildSelectSql(
		int? limitOverride = null)
	{
		var meta = EntityMetadata.GetOrCreate<T>();
		var (where, dp) = BuildWhere(meta);

		var sb = new StringBuilder();
		sb.Append($"SELECT {meta.SelectColumns} FROM {meta.TableName}");

		if (where is not null)
			sb.Append($" WHERE {where}");

		AppendOrderBy(sb, meta);

		var effectiveLimit = limitOverride ?? _limit;
		if (effectiveLimit.HasValue)
			sb.Append($" LIMIT {effectiveLimit.Value}");

		if (_offset.HasValue)
			sb.Append($" OFFSET {_offset.Value}");

		return (sb.ToString(), dp);
	}

	private (string Sql, DynamicParameters Parameters) BuildAggregateSql(string aggregate)
	{
		var meta = EntityMetadata.GetOrCreate<T>();
		var (where, dp) = BuildWhere(meta);

		var sb = new StringBuilder();
		sb.Append($"SELECT {aggregate} FROM {meta.TableName}");

		if (where is not null)
			sb.Append($" WHERE {where}");

		return (sb.ToString(), dp);
	}

	private (string Sql, DynamicParameters Parameters) BuildExistsInner()
	{
		var meta = EntityMetadata.GetOrCreate<T>();
		var (where, dp) = BuildWhere(meta);

		var sql = where is not null
			? $"SELECT 1 FROM {meta.TableName} WHERE {where}"
			: $"SELECT 1 FROM {meta.TableName}";

		return (sql, dp);
	}

	/// <summary>
	/// Translates all accumulated WHERE expressions (combined with AND) and
	/// returns the SQL fragment plus the collected parameters.
	/// </summary>
	private (string? Where, DynamicParameters Parameters) BuildWhere(EntityMetadata meta)
	{
		if (_where.Count == 0)
			return (null, new DynamicParameters());

		var translator = new DbExpressionTranslator<T>(meta);
		var clauses = _where.Select(e => translator.Translate(e)).ToList();
		var where = clauses.Count == 1 ? clauses[0] : string.Join(" AND ", clauses);
		return (where, translator.GetParameters());
	}

	private void AppendOrderBy(StringBuilder sb, EntityMetadata meta)
	{
		if (_orderBy.Count == 0)
			return;

		var parts = _orderBy.Select(
			o => $"{meta.GetColumnName(o.PropertyName)}{(o.Descending ? " DESC" : "")}");
		sb.Append($" ORDER BY {string.Join(", ", parts)}");
	}

	private static string GetMemberName<TKey>(Expression<Func<T, TKey>> selector)
	{
		return selector.Body switch
		{
			MemberExpression m => m.Member.Name,
			UnaryExpression { Operand: MemberExpression m2 } => m2.Member.Name,
			_ => throw new ArgumentException(
				"The selector must be a simple property expression (e.g. e => e.Property).",
				nameof(selector))
		};
	}

	#endregion
}
