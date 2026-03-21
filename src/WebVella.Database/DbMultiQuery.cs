namespace WebVella.Database;

/// <summary>
/// A fluent query builder for multiple result set queries that map each
/// result set to a property in a container type based on the
/// <see cref="ResultSetAttribute"/> index.
/// </summary>
/// <typeparam name="T">
/// The container type marked with <see cref="MultiQueryAttribute"/> containing
/// properties decorated with <see cref="ResultSetAttribute"/>.
/// </typeparam>
/// <remarks>
/// Obtain an instance via <see cref="IDbService.QueryMultiple{T}()"/>.
/// Chain builder methods to configure the query, then call a terminal method
/// to execute.
///
/// <para><strong>Example:</strong></para>
/// <code>
/// var dashboard = await _db.QueryMultiple&lt;DashboardResult&gt;()
///     .Sql("SELECT * FROM users; SELECT * FROM orders;")
///     .Parameters(new { Status = 1 })
///     .ExecuteAsync();
/// </code>
/// </remarks>
public sealed class DbMultiQuery<T> where T : class, new()
{
	private readonly IDbService _db;
	private string? _sql;
	private object? _parameters;

	internal DbMultiQuery(IDbService db)
	{
		_db = db;
	}

	#region <=== Builder Methods ===>

	/// <summary>
	/// Sets the SQL query containing multiple SELECT statements.
	/// </summary>
	/// <param name="sql">
	/// The SQL query containing multiple SELECT statements.
	/// </param>
	public DbMultiQuery<T> Sql(string sql)
	{
		ArgumentNullException.ThrowIfNull(sql);
		_sql = sql;
		return this;
	}

	/// <summary>
	/// Sets the parameters for the query.
	/// </summary>
	/// <param name="parameters">The parameters for the query.</param>
	public DbMultiQuery<T> Parameters(object parameters)
	{
		_parameters = parameters;
		return this;
	}

	#endregion

	#region <=== Terminal Methods — Async ===>

	/// <summary>
	/// Executes the multi-query asynchronously and returns the container
	/// with mapped result sets.
	/// </summary>
	public async Task<T> ExecuteAsync()
	{
		Validate();
		return await _db.QueryMultipleAsync<T>(_sql!, _parameters);
	}

	#endregion

	#region <=== Terminal Methods — Sync ===>

	/// <summary>
	/// Executes the multi-query synchronously and returns the container
	/// with mapped result sets.
	/// </summary>
	public T Execute()
	{
		Validate();
		return _db.QueryMultiple<T>(_sql!, _parameters);
	}

	#endregion

	#region <=== Private Helpers ===>

	private void Validate()
	{
		if (string.IsNullOrEmpty(_sql))
			throw new InvalidOperationException(
				"SQL query is required. Call .Sql() before executing.");
	}

	#endregion
}

/// <summary>
/// A fluent query builder for multiple result set queries where the first
/// result set contains parent entities and subsequent result sets contain
/// child entities mapped via <see cref="ResultSetAttribute.ForeignKey"/>.
/// </summary>
/// <typeparam name="T">
/// The entity type containing properties decorated with
/// <see cref="ResultSetAttribute"/> that specify ForeignKey for child mapping.
/// </typeparam>
/// <remarks>
/// Obtain an instance via <see cref="IDbService.QueryMultipleList{T}()"/>.
/// Chain builder methods to configure the query, then call a terminal method
/// to execute.
///
/// <para><strong>SQL-free example (recommended):</strong></para>
/// <code>
/// var orders = await _db.QueryMultipleList&lt;Order&gt;()
///     .Where(o => o.Status == OrderStatus.Active)
///     .OrderByDescending(o => o.CreatedOn)
///     .Limit(50)
///     .ToListAsync();
/// </code>
///
/// <para><strong>Raw SQL example:</strong></para>
/// <code>
/// var orders = await _db.QueryMultipleList&lt;Order&gt;()
///     .Sql("SELECT * FROM orders; SELECT * FROM order_items;")
///     .Parameters(new { Status = 1 })
///     .ToListAsync();
/// </code>
/// </remarks>
public sealed class DbMultiQueryList<T> where T : class, new()
{
	private readonly IDbService _db;
	private string? _sql;
	private object? _parameters;
	private readonly List<Expression<Func<T, bool>>> _where = [];
	private readonly List<(string PropertyName, bool Descending)> _orderBy = [];
	private int? _limit;
	private int? _offset;

	internal DbMultiQueryList(IDbService db)
	{
		_db = db;
	}

	#region <=== Builder Methods ===>

	/// <summary>
	/// Sets the SQL query containing multiple SELECT statements.
	/// When set, expression-based methods are ignored.
	/// </summary>
	/// <param name="sql">
	/// The SQL query containing multiple SELECT statements.
	/// </param>
	public DbMultiQueryList<T> Sql(string sql)
	{
		ArgumentNullException.ThrowIfNull(sql);
		_sql = sql;
		return this;
	}

	/// <summary>
	/// Sets the parameters for a raw SQL query.
	/// </summary>
	/// <param name="parameters">The parameters for the query.</param>
	public DbMultiQueryList<T> Parameters(object parameters)
	{
		_parameters = parameters;
		return this;
	}

	/// <summary>
	/// Adds a WHERE predicate for the parent entity. Calling this method
	/// multiple times combines all predicates with AND. Child queries
	/// are automatically filtered to match.
	/// </summary>
	/// <param name="predicate">
	/// A lambda expression that describes the filter condition.
	/// </param>
	public DbMultiQueryList<T> Where(
		Expression<Func<T, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		_where.Add(predicate);
		return this;
	}

	/// <summary>
	/// Adds a primary ascending ORDER BY for the specified property.
	/// </summary>
	public DbMultiQueryList<T> OrderBy<TKey>(
		Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: false));
		return this;
	}

	/// <summary>
	/// Adds a primary descending ORDER BY for the specified property.
	/// </summary>
	public DbMultiQueryList<T> OrderByDescending<TKey>(
		Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: true));
		return this;
	}

	/// <summary>
	/// Adds a secondary ascending ORDER BY for the specified property.
	/// </summary>
	public DbMultiQueryList<T> ThenBy<TKey>(
		Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: false));
		return this;
	}

	/// <summary>
	/// Adds a secondary descending ORDER BY for the specified property.
	/// </summary>
	public DbMultiQueryList<T> ThenByDescending<TKey>(
		Expression<Func<T, TKey>> keySelector)
	{
		_orderBy.Add((GetMemberName(keySelector), Descending: true));
		return this;
	}

	/// <summary>
	/// Limits the number of parent rows returned (SQL <c>LIMIT</c>).
	/// </summary>
	/// <param name="count">
	/// Maximum number of parent rows. Must be ≥ 0.
	/// </param>
	public DbMultiQueryList<T> Limit(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_limit = count;
		return this;
	}

	/// <summary>
	/// Skips the specified number of parent rows (SQL <c>OFFSET</c>).
	/// </summary>
	/// <param name="count">Number of rows to skip. Must be ≥ 0.</param>
	public DbMultiQueryList<T> Offset(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		_offset = count;
		return this;
	}

	/// <summary>
	/// Applies 1-based page-number pagination by setting <c>LIMIT</c>
	/// and <c>OFFSET</c> together.
	/// </summary>
	/// <param name="page">
	/// 1-based page number. Defaults to <c>1</c> when
	/// <see langword="null"/>. Must be ≥ 1.
	/// </param>
	/// <param name="pageSize">
	/// Number of rows per page. Defaults to <c>10</c> when
	/// <see langword="null"/>. Must be ≥ 1.
	/// </param>
	public DbMultiQueryList<T> WithPaging(int? page, int? pageSize)
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
	/// Executes the multi-query asynchronously and returns parent entities
	/// with their child collections populated.
	/// </summary>
	public async Task<List<T>> ToListAsync()
	{
		if (_sql is not null)
			return await _db.QueryMultipleListAsync<T>(
				_sql, _parameters);
		var (sql, dp) = BuildSql();
		return await _db.QueryMultipleListAsync<T>(sql, dp);
	}

	#endregion

	#region <=== Terminal Methods — Sync ===>

	/// <summary>
	/// Executes the multi-query synchronously and returns parent entities
	/// with their child collections populated.
	/// </summary>
	public List<T> ToList()
	{
		if (_sql is not null)
			return _db.QueryMultipleList<T>(_sql, _parameters);
		var (sql, dp) = BuildSql();
		return _db.QueryMultipleList<T>(sql, dp);
	}

	#endregion

	#region <=== SQL Building ===>

	private (string Sql, DynamicParameters Parameters) BuildSql()
	{
		var parentMeta = EntityMetadata.GetOrCreate<T>();
		var listMeta = MultiQueryListMetadata.GetOrCreate<T>();
		var (where, dp) = BuildWhere(parentMeta);

		var sb = new StringBuilder();

		sb.Append(
			$"SELECT {parentMeta.SelectColumns} " +
			$"FROM {parentMeta.TableName}");

		if (where is not null)
			sb.Append($" WHERE {where}");

		AppendOrderBy(sb, parentMeta);

		if (_limit.HasValue)
			sb.Append($" LIMIT {_limit.Value}");
		if (_offset.HasValue)
			sb.Append($" OFFSET {_offset.Value}");
		sb.Append(";\n");

		bool needsChildFilter = where is not null
			|| _limit.HasValue || _offset.HasValue;

		foreach (var mapping in listMeta.ChildMappings)
		{
			var childMeta =
				EntityMetadata.GetOrCreate(mapping.ElementType);
			sb.Append(
				$"SELECT {childMeta.SelectColumns} " +
				$"FROM {childMeta.TableName}");

			if (needsChildFilter)
			{
				var fkCol = EntityMetadata.GetColumnName(
					mapping.ForeignKeyProperty!);
				var pkCol = parentMeta.GetColumnName(
					mapping.ParentKey);

				sb.Append($" WHERE {fkCol} IN (");
				sb.Append(
					$"SELECT {pkCol} " +
					$"FROM {parentMeta.TableName}");

				if (where is not null)
					sb.Append($" WHERE {where}");

				if (_limit.HasValue || _offset.HasValue)
				{
					AppendOrderBy(sb, parentMeta);
					if (_limit.HasValue)
						sb.Append($" LIMIT {_limit.Value}");
					if (_offset.HasValue)
						sb.Append($" OFFSET {_offset.Value}");
				}

				sb.Append(')');
			}

			sb.Append(";\n");
		}

		return (sb.ToString(), dp);
	}

	private (string? Where, DynamicParameters Parameters)
		BuildWhere(EntityMetadata meta)
	{
		if (_where.Count == 0)
			return (null, new DynamicParameters());

		var translator = new DbExpressionTranslator<T>(meta);
		var clauses = _where
			.Select(e => translator.Translate(e)).ToList();
		var where = clauses.Count == 1
			? clauses[0]
			: string.Join(" AND ", clauses);
		return (where, translator.GetParameters());
	}

	private void AppendOrderBy(
		StringBuilder sb, EntityMetadata meta)
	{
		if (_orderBy.Count == 0)
			return;

		var parts = _orderBy.Select(
			o => $"{meta.GetColumnName(o.PropertyName)}" +
				$"{(o.Descending ? " DESC" : "")}");
		sb.Append($" ORDER BY {string.Join(", ", parts)}");
	}

	private static string GetMemberName<TKey>(
		Expression<Func<T, TKey>> selector)
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
