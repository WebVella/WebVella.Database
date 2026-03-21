namespace WebVella.Database;

/// <summary>
/// A fluent query builder for JOIN queries that map parent entities
/// with a single child collection.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TChild">The child entity type.</typeparam>
/// <remarks>
/// Obtain an instance via
/// <see cref="IDbService.QueryWithJoin{TParent, TChild}()"/>.
/// Chain builder methods to configure the query, then call a terminal
/// method to execute.
///
/// <para><strong>SQL-free example (recommended):</strong></para>
/// <code>
/// var orders = await _db.QueryWithJoin&lt;Order, OrderItem&gt;()
///     .ChildSelector(o => o.Items)
///     .Where(o => o.Status == OrderStatus.Active)
///     .OrderByDescending(o => o.CreatedOn)
///     .Limit(50)
///     .ToListAsync();
/// </code>
///
/// <para><strong>Raw SQL example:</strong></para>
/// <code>
/// var orders = await _db.QueryWithJoin&lt;Order, OrderItem&gt;()
///     .Sql("SELECT o.*, oi.* FROM orders o " +
///          "LEFT JOIN order_items oi ON o.id = oi.order_id")
///     .ChildSelector(o => o.Items)
///     .ParentKey(o => o.Id)
///     .ChildKey(oi => oi.Id)
///     .ToListAsync();
/// </code>
/// </remarks>
public sealed class DbJoinQuery<TParent, TChild>
	where TParent : class where TChild : class
{
	private readonly IDbService _db;
	private string? _sql;
	private Func<TParent, IList<TChild>>? _childSelector;
	private Func<TParent, object>? _parentKeySelector;
	private Func<TChild, object>? _childKeySelector;
	private string _splitOn = "Id";
	private bool _splitOnUserSet;
	private object? _parameters;
	private readonly List<Expression<Func<TParent, bool>>> _where = [];
	private readonly List<(string PropertyName, bool Descending)>
		_orderBy = [];
	private int? _limit;
	private int? _offset;

	internal DbJoinQuery(IDbService db)
	{
		_db = db;
	}

	#region <=== Builder Methods ===>

	/// <summary>
	/// Sets the SQL query with JOIN to execute. When set, SQL is used
	/// as-is and expression-based methods are ignored.
	/// </summary>
	/// <param name="sql">The SQL query with JOIN.</param>
	public DbJoinQuery<TParent, TChild> Sql(string sql)
	{
		ArgumentNullException.ThrowIfNull(sql);
		_sql = sql;
		return this;
	}

	/// <summary>
	/// Sets the function to get the child collection property from
	/// the parent. Required for both raw SQL and SQL-free modes.
	/// </summary>
	/// <param name="childSelector">
	/// Function to get the child collection property from the parent.
	/// </param>
	public DbJoinQuery<TParent, TChild> ChildSelector(
		Func<TParent, IList<TChild>> childSelector)
	{
		ArgumentNullException.ThrowIfNull(childSelector);
		_childSelector = childSelector;
		return this;
	}

	/// <summary>
	/// Sets the function to get the key value from the parent entity.
	/// Required for raw SQL mode; auto-derived in SQL-free mode.
	/// </summary>
	/// <param name="parentKeySelector">
	/// Function to get the key value from the parent.
	/// </param>
	public DbJoinQuery<TParent, TChild> ParentKey(
		Func<TParent, object> parentKeySelector)
	{
		ArgumentNullException.ThrowIfNull(parentKeySelector);
		_parentKeySelector = parentKeySelector;
		return this;
	}

	/// <summary>
	/// Sets the function to get the unique key from the child entity
	/// for deduplication. Required for raw SQL mode; auto-derived in
	/// SQL-free mode.
	/// </summary>
	/// <param name="childKeySelector">
	/// Function to get the unique key (primary key) from the child.
	/// </param>
	public DbJoinQuery<TParent, TChild> ChildKey(
		Func<TChild, object> childKeySelector)
	{
		ArgumentNullException.ThrowIfNull(childKeySelector);
		_childKeySelector = childKeySelector;
		return this;
	}

	/// <summary>
	/// Sets the column name to split the result on (default: "Id").
	/// Auto-derived in SQL-free mode from the child entity's first
	/// key property.
	/// </summary>
	/// <param name="splitOn">
	/// The column name to split the result on.
	/// </param>
	public DbJoinQuery<TParent, TChild> SplitOn(string splitOn)
	{
		ArgumentNullException.ThrowIfNull(splitOn);
		_splitOn = splitOn;
		_splitOnUserSet = true;
		return this;
	}

	/// <summary>
	/// Sets the parameters for a raw SQL query.
	/// </summary>
	/// <param name="parameters">The parameters for the query.</param>
	public DbJoinQuery<TParent, TChild> Parameters(object parameters)
	{
		_parameters = parameters;
		return this;
	}

	/// <summary>
	/// Adds a WHERE predicate on the parent entity. Multiple calls are
	/// combined with AND. Only used in SQL-free mode.
	/// </summary>
	/// <param name="predicate">
	/// A lambda expression that describes the filter condition.
	/// </param>
	public DbJoinQuery<TParent, TChild> Where(
		Expression<Func<TParent, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		_where.Add(predicate);
		return this;
	}

	/// <summary>
	/// Adds a primary ascending ORDER BY for the specified property.
	/// </summary>
	public DbJoinQuery<TParent, TChild> OrderBy<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), false));
		return this;
	}

	/// <summary>
	/// Adds a primary descending ORDER BY for the specified property.
	/// </summary>
	public DbJoinQuery<TParent, TChild> OrderByDescending<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), true));
		return this;
	}

	/// <summary>
	/// Adds a secondary ascending ORDER BY for the specified property.
	/// </summary>
	public DbJoinQuery<TParent, TChild> ThenBy<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), false));
		return this;
	}

	/// <summary>
	/// Adds a secondary descending ORDER BY for the specified property.
	/// </summary>
	public DbJoinQuery<TParent, TChild> ThenByDescending<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), true));
		return this;
	}

	/// <summary>
	/// Limits the number of rows returned (SQL <c>LIMIT</c>).
	/// </summary>
	/// <param name="count">
	/// Maximum number of rows. Must be ≥ 0.
	/// </param>
	public DbJoinQuery<TParent, TChild> Limit(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_limit = count;
		return this;
	}

	/// <summary>
	/// Skips the specified number of rows (SQL <c>OFFSET</c>).
	/// </summary>
	/// <param name="count">Number of rows to skip. Must be ≥ 0.</param>
	public DbJoinQuery<TParent, TChild> Offset(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_offset = count;
		return this;
	}

	/// <summary>
	/// Applies 1-based page-number pagination.
	/// </summary>
	public DbJoinQuery<TParent, TChild> WithPaging(
		int? page, int? pageSize)
	{
		if (page is null && pageSize is null)
			throw new ArgumentException(
				$"At least one of '{nameof(page)}' or " +
				$"'{nameof(pageSize)}' must be provided.");

		var resolvedPage = page ?? 1;
		var resolvedPageSize = pageSize ?? 10;

		ArgumentOutOfRangeException.ThrowIfLessThan(
			resolvedPage, 1, nameof(page));
		ArgumentOutOfRangeException.ThrowIfLessThan(
			resolvedPageSize, 1, nameof(pageSize));

		_limit = resolvedPageSize;
		_offset = (resolvedPage - 1) * resolvedPageSize;
		return this;
	}

	#endregion

	#region <=== Terminal Methods — Async ===>

	/// <summary>
	/// Executes the JOIN query asynchronously and returns parent
	/// entities with their child collections populated.
	/// </summary>
	public async Task<List<TParent>> ToListAsync()
	{
		var (sql, childSel, parentKey, childKey, split, parms) =
			ResolveQuery();
		return await _db.QueryWithJoinAsync(
			sql, childSel, parentKey, childKey, split, parms);
	}

	#endregion

	#region <=== Terminal Methods — Sync ===>

	/// <summary>
	/// Executes the JOIN query synchronously and returns parent
	/// entities with their child collections populated.
	/// </summary>
	public List<TParent> ToList()
	{
		var (sql, childSel, parentKey, childKey, split, parms) =
			ResolveQuery();
		return _db.QueryWithJoin(
			sql, childSel, parentKey, childKey, split, parms);
	}

	#endregion

	#region <=== SQL Building ===>

	private (string Sql,
		Func<TParent, IList<TChild>> ChildSelector,
		Func<TParent, object> ParentKeySelector,
		Func<TChild, object> ChildKeySelector,
		string SplitOn, object? Parameters) ResolveQuery()
	{
		if (_sql is not null)
		{
			if (_childSelector is null)
				throw new InvalidOperationException(
					"Child selector is required. " +
					"Call .ChildSelector() before executing.");
			if (_parentKeySelector is null)
				throw new InvalidOperationException(
					"Parent key selector is required. " +
					"Call .ParentKey() before executing.");
			if (_childKeySelector is null)
				throw new InvalidOperationException(
					"Child key selector is required. " +
					"Call .ChildKey() before executing.");
			return (_sql, _childSelector, _parentKeySelector,
				_childKeySelector, _splitOn, _parameters);
		}

		return BuildJoinSql();
	}

	private (string Sql,
		Func<TParent, IList<TChild>> ChildSelector,
		Func<TParent, object> ParentKeySelector,
		Func<TChild, object> ChildKeySelector,
		string SplitOn, object? Parameters) BuildJoinSql()
	{
		var parentMeta = EntityMetadata.GetOrCreate<TParent>();
		var childMeta = EntityMetadata.GetOrCreate<TChild>();

		var mapping = FindChildMapping()
			?? throw new InvalidOperationException(
				$"No [ResultSet] attribute with ForeignKey " +
				$"found on {typeof(TParent).Name} for child " +
				$"type {typeof(TChild).Name}. " +
				"Use .Sql() to provide raw SQL instead.");

		var fkCol = childMeta.GetColumnName(mapping.ForeignKey);
		var pkCol = parentMeta.GetColumnName(mapping.ParentKey);

		var (where, dp) = BuildWhere(parentMeta, "p");

		var sb = new StringBuilder();
		sb.Append(
			$"SELECT {parentMeta.GetAliasedSelectColumns("p")}" +
			$", {childMeta.GetAliasedSelectColumns("c")}");
		sb.Append(
			$" FROM {parentMeta.TableName} p" +
			$" LEFT JOIN {childMeta.TableName} c" +
			$" ON c.{fkCol} = p.{pkCol}");

		if (where is not null)
			sb.Append($" WHERE {where}");

		AppendOrderBy(sb, parentMeta, "p");

		if (_limit.HasValue)
			sb.Append($" LIMIT {_limit.Value}");
		if (_offset.HasValue)
			sb.Append($" OFFSET {_offset.Value}");

		var childSel = _childSelector
			?? CreateChildSelector(mapping.Property);
		var parentKey = _parentKeySelector
			?? CreateKeySelector<TParent>(mapping.ParentKey);
		var childKey = _childKeySelector
			?? CreateKeySelector<TChild>(
				childMeta.FirstKeyPropertyName);
		var split = _splitOnUserSet
			? _splitOn : childMeta.FirstKeyPropertyName;

		return (sb.ToString(), childSel, parentKey,
			childKey, split, dp);
	}

	private static
		(string ForeignKey, string ParentKey, PropertyInfo Property)?
		FindChildMapping()
	{
		var props = typeof(TParent).GetProperties(
			BindingFlags.Public | BindingFlags.Instance);

		foreach (var prop in props)
		{
			var attr = prop.GetCustomAttribute<ResultSetAttribute>();
			if (attr?.ForeignKey is null) continue;

			if (!prop.PropertyType.IsGenericType) continue;
			var genArgs = prop.PropertyType.GetGenericArguments();
			if (genArgs.Length == 1
				&& genArgs[0] == typeof(TChild))
				return (attr.ForeignKey, attr.ParentKey, prop);
		}

		return null;
	}

	private static Func<TParent, IList<TChild>> CreateChildSelector(
		PropertyInfo property)
	{
		return p => (IList<TChild>)property.GetValue(p)!;
	}

	private static Func<TEntity, object> CreateKeySelector<TEntity>(
		string propertyName)
	{
		var prop = typeof(TEntity).GetProperty(
			propertyName,
			BindingFlags.Public | BindingFlags.Instance)!;
		return e => prop.GetValue(e)!;
	}

	private (string? Where, DynamicParameters Parameters)
		BuildWhere(EntityMetadata meta, string? tableAlias = null)
	{
		if (_where.Count == 0)
			return (null, new DynamicParameters());

		var translator =
			new DbExpressionTranslator<TParent>(meta, tableAlias);
		var clauses = _where
			.Select(e => translator.Translate(e)).ToList();
		var where = clauses.Count == 1
			? clauses[0]
			: string.Join(" AND ", clauses);
		return (where, translator.GetParameters());
	}

	private void AppendOrderBy(
		StringBuilder sb, EntityMetadata meta,
		string? tableAlias = null)
	{
		if (_orderBy.Count == 0)
			return;

		var parts = _orderBy.Select(o =>
		{
			var col = meta.GetColumnName(o.PropertyName);
			if (tableAlias is not null)
				col = $"{tableAlias}.{col}";
			return $"{col}{(o.Descending ? " DESC" : "")}";
		});
		sb.Append($" ORDER BY {string.Join(", ", parts)}");
	}

	private static string GetMemberName<TKey>(
		Expression<Func<TParent, TKey>> selector)
	{
		return selector.Body switch
		{
			MemberExpression m => m.Member.Name,
			UnaryExpression { Operand: MemberExpression m2 }
				=> m2.Member.Name,
			_ => throw new ArgumentException(
				"The selector must be a simple property " +
				"expression (e.g. e => e.Property).",
				nameof(selector))
		};
	}

	#endregion
}

/// <summary>
/// A fluent query builder for JOIN queries that map parent entities
/// with two child collections. Handles Cartesian product deduplication
/// automatically.
/// </summary>
/// <typeparam name="TParent">The parent entity type.</typeparam>
/// <typeparam name="TChild1">The first child entity type.</typeparam>
/// <typeparam name="TChild2">The second child entity type.</typeparam>
/// <remarks>
/// Obtain an instance via
/// <see cref="IDbService.QueryWithJoin{TParent, TChild1, TChild2}()"/>.
/// Chain builder methods to configure the query, then call a terminal
/// method to execute.
///
/// <para><strong>SQL-free example (recommended):</strong></para>
/// <code>
/// var orders = await _db
///     .QueryWithJoin&lt;Order, OrderItem, OrderNote&gt;()
///     .ChildSelector1(o => o.Items)
///     .ChildSelector2(o => o.Notes)
///     .Where(o => o.Status == OrderStatus.Active)
///     .ToListAsync();
/// </code>
///
/// <para><strong>Raw SQL example:</strong></para>
/// <code>
/// var orders = await _db
///     .QueryWithJoin&lt;Order, OrderItem, OrderNote&gt;()
///     .Sql("SELECT o.*, oi.*, n.* FROM orders o ...")
///     .ChildSelector1(o => o.Items)
///     .ChildSelector2(o => o.Notes)
///     .ParentKey(o => o.Id)
///     .ChildKey1(oi => oi.Id)
///     .ChildKey2(n => n.Id)
///     .SplitOn("Id,Id")
///     .ToListAsync();
/// </code>
/// </remarks>
public sealed class DbJoinQuery<TParent, TChild1, TChild2>
	where TParent : class where TChild1 : class where TChild2 : class
{
	private readonly IDbService _db;
	private string? _sql;
	private Func<TParent, IList<TChild1>>? _childSelector1;
	private Func<TParent, IList<TChild2>>? _childSelector2;
	private Func<TParent, object>? _parentKeySelector;
	private Func<TChild1, object>? _childKeySelector1;
	private Func<TChild2, object>? _childKeySelector2;
	private string _splitOn = "Id,Id";
	private bool _splitOnUserSet;
	private object? _parameters;
	private readonly List<Expression<Func<TParent, bool>>> _where = [];
	private readonly List<(string PropertyName, bool Descending)>
		_orderBy = [];
	private int? _limit;
	private int? _offset;

	internal DbJoinQuery(IDbService db)
	{
		_db = db;
	}

	#region <=== Builder Methods ===>

	/// <summary>
	/// Sets the SQL query with JOINs to execute.
	/// </summary>
	/// <param name="sql">The SQL query with JOINs.</param>
	public DbJoinQuery<TParent, TChild1, TChild2> Sql(string sql)
	{
		ArgumentNullException.ThrowIfNull(sql);
		_sql = sql;
		return this;
	}

	/// <summary>
	/// Sets the function to get the first child collection from
	/// the parent.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> ChildSelector1(
		Func<TParent, IList<TChild1>> childSelector)
	{
		ArgumentNullException.ThrowIfNull(childSelector);
		_childSelector1 = childSelector;
		return this;
	}

	/// <summary>
	/// Sets the function to get the second child collection from
	/// the parent.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> ChildSelector2(
		Func<TParent, IList<TChild2>> childSelector)
	{
		ArgumentNullException.ThrowIfNull(childSelector);
		_childSelector2 = childSelector;
		return this;
	}

	/// <summary>
	/// Sets the function to get the key value from the parent entity.
	/// Required for raw SQL mode; auto-derived in SQL-free mode.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> ParentKey(
		Func<TParent, object> parentKeySelector)
	{
		ArgumentNullException.ThrowIfNull(parentKeySelector);
		_parentKeySelector = parentKeySelector;
		return this;
	}

	/// <summary>
	/// Sets the function to get the unique key from the first child
	/// entity for deduplication.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> ChildKey1(
		Func<TChild1, object> childKeySelector)
	{
		ArgumentNullException.ThrowIfNull(childKeySelector);
		_childKeySelector1 = childKeySelector;
		return this;
	}

	/// <summary>
	/// Sets the function to get the unique key from the second child
	/// entity for deduplication.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> ChildKey2(
		Func<TChild2, object> childKeySelector)
	{
		ArgumentNullException.ThrowIfNull(childKeySelector);
		_childKeySelector2 = childKeySelector;
		return this;
	}

	/// <summary>
	/// Sets the comma-separated column names to split the results.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> SplitOn(
		string splitOn)
	{
		ArgumentNullException.ThrowIfNull(splitOn);
		_splitOn = splitOn;
		_splitOnUserSet = true;
		return this;
	}

	/// <summary>
	/// Sets the parameters for a raw SQL query.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> Parameters(
		object parameters)
	{
		_parameters = parameters;
		return this;
	}

	/// <summary>
	/// Adds a WHERE predicate on the parent entity.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> Where(
		Expression<Func<TParent, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		_where.Add(predicate);
		return this;
	}

	/// <summary>
	/// Adds a primary ascending ORDER BY.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> OrderBy<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), false));
		return this;
	}

	/// <summary>
	/// Adds a primary descending ORDER BY.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2>
		OrderByDescending<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), true));
		return this;
	}

	/// <summary>
	/// Adds a secondary ascending ORDER BY.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> ThenBy<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), false));
		return this;
	}

	/// <summary>
	/// Adds a secondary descending ORDER BY.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2>
		ThenByDescending<TKey>(
		Expression<Func<TParent, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), true));
		return this;
	}

	/// <summary>
	/// Limits the number of rows returned.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> Limit(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_limit = count;
		return this;
	}

	/// <summary>
	/// Skips the specified number of rows.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> Offset(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_offset = count;
		return this;
	}

	/// <summary>
	/// Applies 1-based page-number pagination.
	/// </summary>
	public DbJoinQuery<TParent, TChild1, TChild2> WithPaging(
		int? page, int? pageSize)
	{
		if (page is null && pageSize is null)
			throw new ArgumentException(
				$"At least one of '{nameof(page)}' or " +
				$"'{nameof(pageSize)}' must be provided.");

		var resolvedPage = page ?? 1;
		var resolvedPageSize = pageSize ?? 10;

		ArgumentOutOfRangeException.ThrowIfLessThan(
			resolvedPage, 1, nameof(page));
		ArgumentOutOfRangeException.ThrowIfLessThan(
			resolvedPageSize, 1, nameof(pageSize));

		_limit = resolvedPageSize;
		_offset = (resolvedPage - 1) * resolvedPageSize;
		return this;
	}

	#endregion

	#region <=== Terminal Methods — Async ===>

	/// <summary>
	/// Executes the JOIN query asynchronously and returns parent
	/// entities with their child collections populated.
	/// </summary>
	public async Task<List<TParent>> ToListAsync()
	{
		var ctx = ResolveQuery();
		return await _db.QueryWithJoinAsync(
			ctx.Sql, ctx.ChildSel1, ctx.ChildSel2,
			ctx.ParentKey, ctx.ChildKey1, ctx.ChildKey2,
			ctx.SplitOn, ctx.Parameters);
	}

	#endregion

	#region <=== Terminal Methods — Sync ===>

	/// <summary>
	/// Executes the JOIN query synchronously and returns parent
	/// entities with their child collections populated.
	/// </summary>
	public List<TParent> ToList()
	{
		var ctx = ResolveQuery();
		return _db.QueryWithJoin(
			ctx.Sql, ctx.ChildSel1, ctx.ChildSel2,
			ctx.ParentKey, ctx.ChildKey1, ctx.ChildKey2,
			ctx.SplitOn, ctx.Parameters);
	}

	#endregion

	#region <=== SQL Building ===>

	private JoinQueryContext ResolveQuery()
	{
		if (_sql is not null)
		{
			if (_childSelector1 is null)
				throw new InvalidOperationException(
					"First child selector is required. " +
					"Call .ChildSelector1() before executing.");
			if (_childSelector2 is null)
				throw new InvalidOperationException(
					"Second child selector is required. " +
					"Call .ChildSelector2() before executing.");
			if (_parentKeySelector is null)
				throw new InvalidOperationException(
					"Parent key selector is required. " +
					"Call .ParentKey() before executing.");
			if (_childKeySelector1 is null)
				throw new InvalidOperationException(
					"First child key selector is required. " +
					"Call .ChildKey1() before executing.");
			if (_childKeySelector2 is null)
				throw new InvalidOperationException(
					"Second child key selector is required. " +
					"Call .ChildKey2() before executing.");
			return new JoinQueryContext(
				_sql, _childSelector1, _childSelector2,
				_parentKeySelector, _childKeySelector1,
				_childKeySelector2, _splitOn, _parameters);
		}

		return BuildJoinSql();
	}

	private JoinQueryContext BuildJoinSql()
	{
		var parentMeta = EntityMetadata.GetOrCreate<TParent>();
		var child1Meta = EntityMetadata.GetOrCreate<TChild1>();
		var child2Meta = EntityMetadata.GetOrCreate<TChild2>();

		var m1 = FindChildMapping<TChild1>()
			?? throw new InvalidOperationException(
				$"No [ResultSet] attribute with ForeignKey " +
				$"found on {typeof(TParent).Name} for child " +
				$"type {typeof(TChild1).Name}.");
		var m2 = FindChildMapping<TChild2>()
			?? throw new InvalidOperationException(
				$"No [ResultSet] attribute with ForeignKey " +
				$"found on {typeof(TParent).Name} for child " +
				$"type {typeof(TChild2).Name}.");

		var fk1Col = child1Meta.GetColumnName(m1.ForeignKey);
		var pk1Col = parentMeta.GetColumnName(m1.ParentKey);
		var fk2Col = child2Meta.GetColumnName(m2.ForeignKey);
		var pk2Col = parentMeta.GetColumnName(m2.ParentKey);

		var (where, dp) = BuildWhere(parentMeta, "p");

		var sb = new StringBuilder();
		sb.Append(
			$"SELECT " +
			$"{parentMeta.GetAliasedSelectColumns("p")}, " +
			$"{child1Meta.GetAliasedSelectColumns("c1")}, " +
			$"{child2Meta.GetAliasedSelectColumns("c2")}");
		sb.Append(
			$" FROM {parentMeta.TableName} p" +
			$" LEFT JOIN {child1Meta.TableName} c1" +
			$" ON c1.{fk1Col} = p.{pk1Col}" +
			$" LEFT JOIN {child2Meta.TableName} c2" +
			$" ON c2.{fk2Col} = p.{pk2Col}");

		if (where is not null)
			sb.Append($" WHERE {where}");

		AppendOrderBy(sb, parentMeta, "p");

		if (_limit.HasValue)
			sb.Append($" LIMIT {_limit.Value}");
		if (_offset.HasValue)
			sb.Append($" OFFSET {_offset.Value}");

		var childSel1 = _childSelector1
			?? CreateChildSelector<TChild1>(m1.Property);
		var childSel2 = _childSelector2
			?? CreateChildSelector<TChild2>(m2.Property);
		var parentKey = _parentKeySelector
			?? CreateKeySelector<TParent>(m1.ParentKey);
		var childKey1 = _childKeySelector1
			?? CreateKeySelector<TChild1>(
				child1Meta.FirstKeyPropertyName);
		var childKey2 = _childKeySelector2
			?? CreateKeySelector<TChild2>(
				child2Meta.FirstKeyPropertyName);
		var split = _splitOnUserSet
			? _splitOn
			: $"{child1Meta.FirstKeyPropertyName}," +
			  $"{child2Meta.FirstKeyPropertyName}";

		return new JoinQueryContext(
			sb.ToString(), childSel1, childSel2, parentKey,
			childKey1, childKey2, split, dp);
	}

	private static
		(string ForeignKey, string ParentKey, PropertyInfo Property)?
		FindChildMapping<TChildType>()
	{
		var props = typeof(TParent).GetProperties(
			BindingFlags.Public | BindingFlags.Instance);

		foreach (var prop in props)
		{
			var attr = prop.GetCustomAttribute<ResultSetAttribute>();
			if (attr?.ForeignKey is null) continue;

			if (!prop.PropertyType.IsGenericType) continue;
			var genArgs = prop.PropertyType.GetGenericArguments();
			if (genArgs.Length == 1
				&& genArgs[0] == typeof(TChildType))
				return (attr.ForeignKey, attr.ParentKey, prop);
		}

		return null;
	}

	private static Func<TParent, IList<TChildType>>
		CreateChildSelector<TChildType>(PropertyInfo property)
	{
		return p => (IList<TChildType>)property.GetValue(p)!;
	}

	private static Func<TEntity, object>
		CreateKeySelector<TEntity>(string propertyName)
	{
		var prop = typeof(TEntity).GetProperty(
			propertyName,
			BindingFlags.Public | BindingFlags.Instance)!;
		return e => prop.GetValue(e)!;
	}

	private (string? Where, DynamicParameters Parameters)
		BuildWhere(EntityMetadata meta, string? tableAlias = null)
	{
		if (_where.Count == 0)
			return (null, new DynamicParameters());

		var translator =
			new DbExpressionTranslator<TParent>(meta, tableAlias);
		var clauses = _where
			.Select(e => translator.Translate(e)).ToList();
		var where = clauses.Count == 1
			? clauses[0]
			: string.Join(" AND ", clauses);
		return (where, translator.GetParameters());
	}

	private void AppendOrderBy(
		StringBuilder sb, EntityMetadata meta,
		string? tableAlias = null)
	{
		if (_orderBy.Count == 0)
			return;

		var parts = _orderBy.Select(o =>
		{
			var col = meta.GetColumnName(o.PropertyName);
			if (tableAlias is not null)
				col = $"{tableAlias}.{col}";
			return $"{col}{(o.Descending ? " DESC" : "")}";
		});
		sb.Append($" ORDER BY {string.Join(", ", parts)}");
	}

	private static string GetMemberName<TKey>(
		Expression<Func<TParent, TKey>> selector)
	{
		return selector.Body switch
		{
			MemberExpression m => m.Member.Name,
			UnaryExpression { Operand: MemberExpression m2 }
				=> m2.Member.Name,
			_ => throw new ArgumentException(
				"The selector must be a simple property " +
				"expression (e.g. e => e.Property).",
				nameof(selector))
		};
	}

	private readonly record struct JoinQueryContext(
		string Sql,
		Func<TParent, IList<TChild1>> ChildSel1,
		Func<TParent, IList<TChild2>> ChildSel2,
		Func<TParent, object> ParentKey,
		Func<TChild1, object> ChildKey1,
		Func<TChild2, object> ChildKey2,
		string SplitOn,
		object? Parameters);

	#endregion
}
