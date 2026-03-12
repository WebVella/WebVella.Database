namespace WebVella.Database;


/// <summary>
/// Defines a database service interface for entity CRUD operations using Dapper.
/// </summary>
public interface IDbService
{
	#region <=== Query ===>

	/// <summary>
	/// Executes a query and maps the result to a collection of the specified type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A collection of the specified type.</returns>
	IEnumerable<T> Query<T>(string sql, object? parameters = null) where T : class;

	/// <summary>
	/// Asynchronously executes a query and maps the result to a collection of the specified type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A collection of the specified type.</returns>
	Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null) where T : class;

	/// <summary>
	/// Executes a query with multiple result sets and maps each result set to a property
	/// in the container type based on the <see cref="ResultSetAttribute"/> index.
	/// </summary>
	/// <typeparam name="T">
	/// The container type marked with <see cref="MultiQueryAttribute"/> containing properties
	/// decorated with <see cref="ResultSetAttribute"/>.
	/// </typeparam>
	/// <param name="sql">The SQL query containing multiple SELECT statements.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>An instance of the container type with mapped result sets.</returns>
	T QueryMultiple<T>(string sql, object? parameters = null) where T : class, new();

	/// <summary>
	/// Asynchronously executes a query with multiple result sets and maps each result set to a property
	/// in the container type based on the <see cref="ResultSetAttribute"/> index.
	/// </summary>
	/// <typeparam name="T">
	/// The container type marked with <see cref="MultiQueryAttribute"/> containing properties
	/// decorated with <see cref="ResultSetAttribute"/>.
	/// </typeparam>
	/// <param name="sql">The SQL query containing multiple SELECT statements.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>An instance of the container type with mapped result sets.</returns>
	Task<T> QueryMultipleAsync<T>(string sql, object? parameters = null) where T : class, new();

	/// <summary>
	/// Executes a query with multiple result sets where the first result set contains parent entities
	/// and subsequent result sets contain child entities mapped via <see cref="ResultSetAttribute.ForeignKey"/>.
	/// </summary>
	/// <typeparam name="T">
	/// The entity type containing properties decorated with <see cref="ResultSetAttribute"/>
	/// that specify ForeignKey for child mapping.
	/// </typeparam>
	/// <param name="sql">The SQL query containing multiple SELECT statements.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A list of parent entities with their child collections populated.</returns>
	List<T> QueryMultipleList<T>(string sql, object? parameters = null) where T : class, new();

	/// <summary>
	/// Asynchronously executes a query with multiple result sets where the first result set contains
	/// parent entities and subsequent result sets contain child entities mapped via
	/// <see cref="ResultSetAttribute.ForeignKey"/>.
	/// </summary>
	/// <typeparam name="T">
	/// The entity type containing properties decorated with <see cref="ResultSetAttribute"/>
	/// that specify ForeignKey for child mapping.
	/// </typeparam>
	/// <param name="sql">The SQL query containing multiple SELECT statements.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A list of parent entities with their child collections populated.</returns>
	Task<List<T>> QueryMultipleListAsync<T>(string sql, object? parameters = null) where T : class, new();

	/// <summary>
	/// Executes a JOIN query and maps the result to parent entities with a single child collection.
	/// </summary>
	/// <typeparam name="TParent">The parent entity type.</typeparam>
	/// <typeparam name="TChild">The child entity type.</typeparam>
	/// <param name="sql">The SQL query with JOIN.</param>
	/// <param name="childSelector">
	/// Function to get the child collection property from the parent.
	/// </param>
	/// <param name="parentKeySelector">Function to get the key value from the parent.</param>
	/// <param name="childKeySelector">
	/// Function to get the unique key (primary key) from the child for deduplication.
	/// </param>
	/// <param name="splitOn">The column name to split the result on (default: "Id").</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A list of parent entities with their child collections populated.</returns>
	List<TParent> QueryWithJoin<TParent, TChild>(
		string sql,
		Func<TParent, IList<TChild>> childSelector,
		Func<TParent, object> parentKeySelector,
		Func<TChild, object> childKeySelector,
		string splitOn = "Id",
		object? parameters = null)
		where TParent : class where TChild : class;

	/// <summary>
	/// Asynchronously executes a JOIN query and maps the result to parent entities with a single child
	/// collection.
	/// </summary>
	/// <typeparam name="TParent">The parent entity type.</typeparam>
	/// <typeparam name="TChild">The child entity type.</typeparam>
	/// <param name="sql">The SQL query with JOIN.</param>
	/// <param name="childSelector">
	/// Function to get the child collection property from the parent.
	/// </param>
	/// <param name="parentKeySelector">Function to get the key value from the parent.</param>
	/// <param name="childKeySelector">
	/// Function to get the unique key (primary key) from the child for deduplication.
	/// </param>
	/// <param name="splitOn">The column name to split the result on (default: "Id").</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A list of parent entities with their child collections populated.</returns>
	Task<List<TParent>> QueryWithJoinAsync<TParent, TChild>(
		string sql,
		Func<TParent, IList<TChild>> childSelector,
		Func<TParent, object> parentKeySelector,
		Func<TChild, object> childKeySelector,
		string splitOn = "Id",
		object? parameters = null)
		where TParent : class where TChild : class;

	/// <summary>
	/// Executes a JOIN query and maps the result to parent entities with two child collections.
	/// Handles Cartesian product deduplication automatically.
	/// </summary>
	/// <typeparam name="TParent">The parent entity type.</typeparam>
	/// <typeparam name="TChild1">The first child entity type.</typeparam>
	/// <typeparam name="TChild2">The second child entity type.</typeparam>
	/// <param name="sql">The SQL query with JOINs.</param>
	/// <param name="childSelector1">Function to get the first child collection from the parent.</param>
	/// <param name="childSelector2">Function to get the second child collection from the parent.</param>
	/// <param name="parentKeySelector">Function to get the key value from the parent.</param>
	/// <param name="childKeySelector1">
	/// Function to get the unique key (primary key) from the first child for deduplication.
	/// </param>
	/// <param name="childKeySelector2">
	/// Function to get the unique key (primary key) from the second child for deduplication.
	/// </param>
	/// <param name="splitOn">Comma-separated column names to split the results (e.g., "Id,Id").</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A list of parent entities with their child collections populated.</returns>
	List<TParent> QueryWithJoin<TParent, TChild1, TChild2>(
		string sql,
		Func<TParent, IList<TChild1>> childSelector1,
		Func<TParent, IList<TChild2>> childSelector2,
		Func<TParent, object> parentKeySelector,
		Func<TChild1, object> childKeySelector1,
		Func<TChild2, object> childKeySelector2,
		string splitOn = "Id,Id",
		object? parameters = null)
		where TParent : class where TChild1 : class where TChild2 : class;

	/// <summary>
	/// Asynchronously executes a JOIN query and maps the result to parent entities with two child
	/// collections. Handles Cartesian product deduplication automatically.
	/// </summary>
	/// <typeparam name="TParent">The parent entity type.</typeparam>
	/// <typeparam name="TChild1">The first child entity type.</typeparam>
	/// <typeparam name="TChild2">The second child entity type.</typeparam>
	/// <param name="sql">The SQL query with JOINs.</param>
	/// <param name="childSelector1">Function to get the first child collection from the parent.</param>
	/// <param name="childSelector2">Function to get the second child collection from the parent.</param>
	/// <param name="parentKeySelector">Function to get the key value from the parent.</param>
	/// <param name="childKeySelector1">
	/// Function to get the unique key (primary key) from the first child for deduplication.
	/// </param>
	/// <param name="childKeySelector2">
	/// Function to get the unique key (primary key) from the second child for deduplication.
	/// </param>
	/// <param name="splitOn">Comma-separated column names to split the results (e.g., "Id,Id").</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A list of parent entities with their child collections populated.</returns>
	Task<List<TParent>> QueryWithJoinAsync<TParent, TChild1, TChild2>(
		string sql,
		Func<TParent, IList<TChild1>> childSelector1,
		Func<TParent, IList<TChild2>> childSelector2,
		Func<TParent, object> parentKeySelector,
		Func<TChild1, object> childKeySelector1,
		Func<TChild2, object> childKeySelector2,
		string splitOn = "Id,Id",
		object? parameters = null)
		where TParent : class where TChild1 : class where TChild2 : class;

	#endregion

	#region <=== Execute ===>

	/// <summary>
	/// Executes a command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
	/// </summary>
	/// <param name="sql">The SQL command to execute.</param>
	/// <param name="parameters">The parameters for the command.</param>
	/// <returns>The number of affected rows.</returns>
	int Execute(string sql, object? parameters = null);

	/// <summary>
	/// Asynchronously executes a command (INSERT, UPDATE, DELETE) and returns the number of affected rows.
	/// </summary>
	/// <param name="sql">The SQL command to execute.</param>
	/// <param name="parameters">The parameters for the command.</param>
	/// <returns>The number of affected rows.</returns>
	Task<int> ExecuteAsync(string sql, object? parameters = null);

	#endregion

	#region <=== Insert ===>

	/// <summary>
	/// Inserts an entity into the database and returns the generated key value(s).
	/// The key properties (marked with [Key]) will be populated via RETURNING.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entity">The entity to insert.</param>
	/// <returns>A dictionary of property names to Guids.</returns>
	Dictionary<string, Guid> Insert<T>(T entity) where T : class;

	/// <summary>
	/// Asynchronously inserts an entity into the database and returns the generated key value(s).
	/// The key properties (marked with [Key]) will be populated via RETURNING.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entity">The entity to insert.</param>
	/// <returns>A dictionary of property names to Guids.</returns>
	Task<Dictionary<string, Guid>> InsertAsync<T>(T entity) where T : class;

	#endregion

	#region <=== Update ===>

	/// <summary>
	/// Updates an existing entity in the database.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entity">The entity to update.</param>
	/// <param name="propertyNamesUpdateOnly">
	/// Optional array of property names to update. If null, all writable properties are updated.
	/// </param>
	/// <returns>True if the entity was updated; otherwise, false.</returns>
	bool Update<T>(T entity, string[]? propertyNamesUpdateOnly = null) where T : class;

	/// <summary>
	/// Asynchronously updates an existing entity in the database.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entity">The entity to update.</param>
	/// <param name="propertyNamesUpdateOnly">
	/// Optional array of property names to update. If null, all writable properties are updated.
	/// </param>
	/// <returns>True if the entity was updated; otherwise, false.</returns>
	Task<bool> UpdateAsync<T>(T entity, string[]? propertyNamesUpdateOnly = null) where T : class;

	#endregion

	#region <=== Delete ===>

	/// <summary>
	/// Deletes an entity from the database.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entity">The entity to delete.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	bool Delete<T>(T entity) where T : class;

	/// <summary>
	/// Asynchronously deletes an entity from the database.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="entity">The entity to delete.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	Task<bool> DeleteAsync<T>(T entity) where T : class;

	/// <summary>
	/// Deletes an entity from the database by its single Guid primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="id">The Guid primary key value of the entity to delete.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	bool Delete<T>(Guid id) where T : class;

	/// <summary>
	/// Asynchronously deletes an entity from the database by its single Guid primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="id">The Guid primary key value of the entity to delete.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	Task<bool> DeleteAsync<T>(Guid id) where T : class;

	/// <summary>
	/// Deletes an entity from the database by its composite primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">A dictionary mapping key property names to their Guid values.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	bool Delete<T>(Dictionary<string, Guid> keys) where T : class;

	/// <summary>
	/// Asynchronously deletes an entity from the database by its composite primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">A dictionary mapping key property names to their Guid values.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	Task<bool> DeleteAsync<T>(Dictionary<string, Guid> keys) where T : class;

	#endregion

	#region <=== Get ===>

	/// <summary>
	/// Retrieves an entity by its single Guid primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="id">The Guid primary key value of the entity.</param>
	/// <returns>The entity if found; otherwise, null.</returns>
	T? Get<T>(Guid id) where T : class;

	/// <summary>
	/// Asynchronously retrieves an entity by its single Guid primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="id">The Guid primary key value of the entity.</param>
	/// <returns>The entity if found; otherwise, null.</returns>
	Task<T?> GetAsync<T>(Guid id) where T : class;

	/// <summary>
	/// Retrieves an entity by its composite primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">A dictionary mapping key property names to their Guid values.</param>
	/// <returns>The entity if found; otherwise, null.</returns>
	T? Get<T>(Dictionary<string, Guid> keys) where T : class;

	/// <summary>
	/// Asynchronously retrieves an entity by its composite primary key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">A dictionary mapping key property names to their Guid values.</param>
	/// <returns>The entity if found; otherwise, null.</returns>
	Task<T?> GetAsync<T>(Dictionary<string, Guid> keys) where T : class;

	#endregion

	#region <=== GetList ===>

	/// <summary>
	/// Retrieves all entities.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <returns>A collection of entities.</returns>
	IEnumerable<T> GetList<T>() where T : class;

	/// <summary>
	/// Asynchronously retrieves all entities.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <returns>A collection of entities.</returns>
	Task<IEnumerable<T>> GetListAsync<T>() where T : class;

	/// <summary>
	/// Retrieves multiple entities by their single Guid primary keys.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="ids">The collection of Guid primary key values.</param>
	/// <returns>A collection of entities that were found.</returns>
	IEnumerable<T> GetList<T>(IEnumerable<Guid> ids) where T : class;

	/// <summary>
	/// Asynchronously retrieves multiple entities by their single Guid primary keys.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="ids">The collection of Guid primary key values.</param>
	/// <returns>A collection of entities that were found.</returns>
	Task<IEnumerable<T>> GetListAsync<T>(IEnumerable<Guid> ids) where T : class;

	/// <summary>
	/// Retrieves multiple entities by their composite primary keys.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keysList">A collection of dictionaries mapping key property names to their Guid values.</param>
	/// <returns>A collection of entities that were found.</returns>
	IEnumerable<T> GetList<T>(IEnumerable<Dictionary<string, Guid>> keysList) where T : class;

	/// <summary>
	/// Asynchronously retrieves multiple entities by their composite primary keys.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keysList">A collection of dictionaries mapping key property names to their Guid values.</param>
	/// <returns>A collection of entities that were found.</returns>
	Task<IEnumerable<T>> GetListAsync<T>(IEnumerable<Dictionary<string, Guid>> keysList) where T : class;

	#endregion

	#region <=== Connection & Transaction ===>

	/// <summary>
	/// Creates a new database connection.
	/// </summary>
	/// <returns>A new <see cref="IDbConnection"/> instance.</returns>
	IDbConnection CreateConnection();

	/// <summary>
	/// Asynchronously creates a new database connection.
	/// </summary>
	/// <returns>A new <see cref="IDbConnection"/> instance.</returns>
	Task<IDbConnection> CreateConnectionAsync();

	/// <summary>
	/// Creates a new transaction scope for database operations.
	/// </summary>
	/// <param name="lockKey">An optional advisory lock key.</param>
	/// <returns>A new <see cref="IDbTransactionScope"/> instance.</returns>
	IDbTransactionScope CreateTransactionScope(long? lockKey = null);

	/// <summary>
	/// Asynchronously creates a new transaction scope without an advisory lock.
	/// </summary>
	/// <returns>An instance of <see cref="IDbTransactionScope"/>.</returns>
	Task<IDbTransactionScope> CreateTransactionScopeAsync();

	/// <summary>
	/// Asynchronously creates a new transaction scope with an optional numeric advisory lock.
	/// </summary>
	/// <param name="lockKey">The numeric advisory lock key.</param>
	/// <returns>An instance of <see cref="IDbTransactionScope"/>.</returns>
	Task<IDbTransactionScope> CreateTransactionScopeAsync(long? lockKey);

	/// <summary>
	/// Asynchronously creates a new transaction scope with a string-based advisory lock.
	/// </summary>
	/// <param name="lockKey">The string advisory lock key (will be hashed to a long value).</param>
	/// <returns>An instance of <see cref="IDbTransactionScope"/>.</returns>
	Task<IDbTransactionScope> CreateTransactionScopeAsync(string lockKey);

	/// <summary>
	/// Creates a new advisory lock scope for database operations.
	/// </summary>
	/// <param name="lockKey">The advisory lock key.</param>
	/// <returns>A new <see cref="IDbAdvisoryLockScope"/> instance.</returns>
	IDbAdvisoryLockScope CreateAdvisoryLockScope(long lockKey);

	/// <summary>
	/// Asynchronously creates a new advisory lock scope for database operations.
	/// </summary>
	/// <param name="lockKey">The advisory lock key.</param>
	/// <returns>A new <see cref="IDbAdvisoryLockScope"/> instance.</returns>
	Task<IDbAdvisoryLockScope> CreateAdvisoryLockScopeAsync(long lockKey);

	#endregion
}

/// <summary>
/// Provides a database service implementation for entity CRUD operations using Dapper
/// with reflection-based metadata caching.
/// </summary>
public class DbService : IDbService
{
	private readonly string _connectionString;
	private readonly IDbEntityCache _cache;

	/// <summary>
	/// Initializes a new instance of the <see cref="DbService"/> class.
	/// </summary>
	/// <param name="connectionString">The PostgreSQL connection string.</param>
	public DbService(string connectionString)
		: this(connectionString, NullDbEntityCache.Instance)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DbService"/> class with caching support.
	/// </summary>
	/// <param name="connectionString">The PostgreSQL connection string.</param>
	/// <param name="cache">The entity cache implementation.</param>
	public DbService(string connectionString, IDbEntityCache cache)
	{
		_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
	}

	#region <=== Query ===>

	/// <inheritdoc/>
	public IEnumerable<T> Query<T>(string sql, object? parameters = null) where T : class
	{
		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var result = npgsqlConn.Query<T>(sql, parameters, transaction: null);
		return result;
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null) where T : class
	{
		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var result = await npgsqlConn.QueryAsync<T>(sql, parameters, transaction: null);
		return result;
	}

	/// <inheritdoc/>
	public T QueryMultiple<T>(string sql, object? parameters = null) where T : class, new()
	{
		var metadata = MultiQueryMetadata.GetOrCreate<T>();

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		using var multi = npgsqlConn.QueryMultiple(sql, parameters, transaction: null);

		var result = new T();

		foreach (var mapping in metadata.ResultSetMappings.OrderBy(m => m.Index))
		{
			if (multi.IsConsumed)
				break;

			var value = mapping.IsCollection
				? multi.Read(mapping.ElementType).ToListOfType(mapping.ElementType)
				: multi.ReadFirstOrDefault(mapping.ElementType);

			mapping.Property.SetValue(result, value);
		}

		return result;
	}

	/// <inheritdoc/>
	public async Task<T> QueryMultipleAsync<T>(string sql, object? parameters = null) where T : class, new()
	{
		var metadata = MultiQueryMetadata.GetOrCreate<T>();

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		using var multi = await npgsqlConn.QueryMultipleAsync(sql, parameters, transaction: null);

		var result = new T();

		foreach (var mapping in metadata.ResultSetMappings.OrderBy(m => m.Index))
		{
			if (multi.IsConsumed)
				break;

			var value = mapping.IsCollection
				? (await multi.ReadAsync(mapping.ElementType)).ToListOfType(mapping.ElementType)
				: await multi.ReadFirstOrDefaultAsync(mapping.ElementType);

			mapping.Property.SetValue(result, value);
		}

		return result;
	}

	/// <inheritdoc/>
	public List<T> QueryMultipleList<T>(string sql, object? parameters = null) where T : class, new()
	{
		var metadata = MultiQueryListMetadata.GetOrCreate<T>();

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		using var multi = npgsqlConn.QueryMultiple(sql, parameters, transaction: null);

		var parents = multi.Read<T>().ToList();

		if (parents.Count == 0)
			return parents;

		var parentLookup = new Dictionary<object, T>();
		foreach (var parent in parents)
		{
			var keyValue = metadata.ParentKeyProperty.GetValue(parent);
			if (keyValue != null)
			{
				parentLookup[keyValue] = parent;
			}
		}

		foreach (var mapping in metadata.ChildMappings)
		{
			if (multi.IsConsumed)
				break;

			var children = multi.Read(mapping.ElementType).ToList();
			var childrenByParent = new Dictionary<object, System.Collections.IList>();

			foreach (var child in children)
			{
				var fkValue = mapping.ForeignKeyProperty?.GetValue(child);
				if (fkValue == null)
					continue;

				if (!childrenByParent.TryGetValue(fkValue, out var list))
				{
					list = (System.Collections.IList)MultiQueryExtensions.CreateEmptyList(mapping.ElementType);
					childrenByParent[fkValue] = list;
				}

				list.Add(child);
			}

			foreach (var parent in parents)
			{
				var keyValue = metadata.ParentKeyProperty.GetValue(parent);
				if (keyValue != null && childrenByParent.TryGetValue(keyValue, out var childList))
				{
					mapping.Property.SetValue(parent, childList);
				}
				else
				{
					mapping.Property.SetValue(parent, MultiQueryExtensions.CreateEmptyList(mapping.ElementType));
				}
			}
		}

		return parents;
	}

	/// <inheritdoc/>
	public async Task<List<T>> QueryMultipleListAsync<T>(string sql, object? parameters = null) where T : class, new()
	{
		var metadata = MultiQueryListMetadata.GetOrCreate<T>();

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		using var multi = await npgsqlConn.QueryMultipleAsync(sql, parameters, transaction: null);

		var parents = (await multi.ReadAsync<T>()).ToList();

		if (parents.Count == 0)
			return parents;

		var parentLookup = new Dictionary<object, T>();
		foreach (var parent in parents)
		{
			var keyValue = metadata.ParentKeyProperty.GetValue(parent);
			if (keyValue != null)
			{
				parentLookup[keyValue] = parent;
			}
		}

		foreach (var mapping in metadata.ChildMappings)
		{
			if (multi.IsConsumed)
				break;

			var children = (await multi.ReadAsync(mapping.ElementType)).ToList();
			var childrenByParent = new Dictionary<object, System.Collections.IList>();

			foreach (var child in children)
			{
				var fkValue = mapping.ForeignKeyProperty?.GetValue(child);
				if (fkValue == null)
					continue;

				if (!childrenByParent.TryGetValue(fkValue, out var list))
				{
					list = (System.Collections.IList)MultiQueryExtensions.CreateEmptyList(mapping.ElementType);
					childrenByParent[fkValue] = list;
				}

				list.Add(child);
			}

			foreach (var parent in parents)
			{
				var keyValue = metadata.ParentKeyProperty.GetValue(parent);
				if (keyValue != null && childrenByParent.TryGetValue(keyValue, out var childList))
				{
					mapping.Property.SetValue(parent, childList);
				}
				else
				{
					mapping.Property.SetValue(parent, MultiQueryExtensions.CreateEmptyList(mapping.ElementType));
				}
			}
		}

		return parents;
	}

	/// <inheritdoc/>
	public List<TParent> QueryWithJoin<TParent, TChild>(
		string sql,
		Func<TParent, IList<TChild>> childSelector,
		Func<TParent, object> parentKeySelector,
		Func<TChild, object> childKeySelector,
		string splitOn = "Id",
		object? parameters = null)
		where TParent : class where TChild : class
	{
		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parentLookup = new Dictionary<object, TParent>();
		var childAdded = new HashSet<(object parentKey, object childKey)>();

		npgsqlConn.Query<TParent, TChild, TParent>(
			sql,
			(parent, child) =>
			{
				var parentKey = parentKeySelector(parent);

				if (!parentLookup.TryGetValue(parentKey, out var existingParent))
				{
					existingParent = parent;
					parentLookup[parentKey] = existingParent;
				}

				if (child != null)
				{
					var childKey = childKeySelector(child);
					if (childKey != null && childAdded.Add((parentKey, childKey)))
					{
						childSelector(existingParent).Add(child);
					}
				}

				return existingParent;
			},
			parameters,
			transaction: null,
			splitOn: splitOn);

		return parentLookup.Values.ToList();
	}

	/// <inheritdoc/>
	public async Task<List<TParent>> QueryWithJoinAsync<TParent, TChild>(
		string sql,
		Func<TParent, IList<TChild>> childSelector,
		Func<TParent, object> parentKeySelector,
		Func<TChild, object> childKeySelector,
		string splitOn = "Id",
		object? parameters = null)
		where TParent : class where TChild : class
	{
		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parentLookup = new Dictionary<object, TParent>();
		var childAdded = new HashSet<(object parentKey, object childKey)>();

		await npgsqlConn.QueryAsync<TParent, TChild, TParent>(
			sql,
			(parent, child) =>
			{
				var parentKey = parentKeySelector(parent);

				if (!parentLookup.TryGetValue(parentKey, out var existingParent))
				{
					existingParent = parent;
					parentLookup[parentKey] = existingParent;
				}

				if (child != null)
				{
					var childKey = childKeySelector(child);
					if (childKey != null && childAdded.Add((parentKey, childKey)))
					{
						childSelector(existingParent).Add(child);
					}
				}

				return existingParent;
			},
			parameters,
			transaction: null,
			splitOn: splitOn);

		return parentLookup.Values.ToList();
	}

	/// <inheritdoc/>
	public List<TParent> QueryWithJoin<TParent, TChild1, TChild2>(
		string sql,
		Func<TParent, IList<TChild1>> childSelector1,
		Func<TParent, IList<TChild2>> childSelector2,
		Func<TParent, object> parentKeySelector,
		Func<TChild1, object> childKeySelector1,
		Func<TChild2, object> childKeySelector2,
		string splitOn = "Id,Id",
		object? parameters = null)
		where TParent : class where TChild1 : class where TChild2 : class
	{
		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parentLookup = new Dictionary<object, TParent>();
		var child1Added = new HashSet<(object parentKey, object childKey)>();
		var child2Added = new HashSet<(object parentKey, object childKey)>();

		npgsqlConn.Query<TParent, TChild1, TChild2, TParent>(
			sql,
			(parent, child1, child2) =>
			{
				var parentKey = parentKeySelector(parent);

				if (!parentLookup.TryGetValue(parentKey, out var existingParent))
				{
					existingParent = parent;
					parentLookup[parentKey] = existingParent;
				}

				if (child1 != null)
				{
					var childKey1 = childKeySelector1(child1);
					if (childKey1 != null && child1Added.Add((parentKey, childKey1)))
					{
						childSelector1(existingParent).Add(child1);
					}
				}

				if (child2 != null)
				{
					var childKey2 = childKeySelector2(child2);
					if (childKey2 != null && child2Added.Add((parentKey, childKey2)))
					{
						childSelector2(existingParent).Add(child2);
					}
				}

				return existingParent;
			},
			parameters,
			transaction: null,
			splitOn: splitOn);

		return parentLookup.Values.ToList();
	}

	/// <inheritdoc/>
	public async Task<List<TParent>> QueryWithJoinAsync<TParent, TChild1, TChild2>(
		string sql,
		Func<TParent, IList<TChild1>> childSelector1,
		Func<TParent, IList<TChild2>> childSelector2,
		Func<TParent, object> parentKeySelector,
		Func<TChild1, object> childKeySelector1,
		Func<TChild2, object> childKeySelector2,
		string splitOn = "Id,Id",
		object? parameters = null)
		where TParent : class where TChild1 : class where TChild2 : class
	{
		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parentLookup = new Dictionary<object, TParent>();
		var child1Added = new HashSet<(object parentKey, object childKey)>();
		var child2Added = new HashSet<(object parentKey, object childKey)>();

		await npgsqlConn.QueryAsync<TParent, TChild1, TChild2, TParent>(
			sql,
			(parent, child1, child2) =>
			{
				var parentKey = parentKeySelector(parent);

				if (!parentLookup.TryGetValue(parentKey, out var existingParent))
				{
					existingParent = parent;
					parentLookup[parentKey] = existingParent;
				}

				if (child1 != null)
				{
					var childKey1 = childKeySelector1(child1);
					if (childKey1 != null && child1Added.Add((parentKey, childKey1)))
					{
						childSelector1(existingParent).Add(child1);
					}
				}

				if (child2 != null)
				{
					var childKey2 = childKeySelector2(child2);
					if (childKey2 != null && child2Added.Add((parentKey, childKey2)))
					{
						childSelector2(existingParent).Add(child2);
					}
				}

				return existingParent;
			},
			parameters,
			transaction: null,
			splitOn: splitOn);

		return parentLookup.Values.ToList();
	}

	#endregion

	#region <=== Execute ===>

	/// <inheritdoc/>
	public int Execute(string sql, object? parameters = null)
	{
		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var affected = npgsqlConn.Execute(sql, parameters, transaction: null);
		return affected;
	}

	/// <inheritdoc/>
	public async Task<int> ExecuteAsync(string sql, object? parameters = null)
	{
		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var affected = await npgsqlConn.ExecuteAsync(sql, parameters, transaction: null);
		return affected;
	}

	#endregion

	#region <=== Insert ===>

	/// <inheritdoc/>
	public Dictionary<string, Guid> Insert<T>(T entity) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		foreach (var keyProp in metadata.KeyProperties)
		{
			var currentValue = (Guid)keyProp.GetValue(entity)!;
			if (currentValue == Guid.Empty)
			{
				keyProp.SetValue(entity, Guid.NewGuid());
			}
		}

		var sql = $"INSERT INTO {metadata.TableName} ({metadata.InsertColumns}) " +
			$"VALUES ({metadata.InsertParameters}) RETURNING {metadata.ReturningColumns}";

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var result = new Dictionary<string, Guid>();

		if (metadata.HasSingleKey)
		{
			var id = npgsqlConn.ExecuteScalar<Guid>(sql, entity, transaction: null);
			result[metadata.FirstKeyPropertyName] = id;
		}
		else
		{
			var row = npgsqlConn.QueryFirst(sql, entity, transaction: null);
			var rowDict = (IDictionary<string, object>)row;
			foreach (var keyProp in metadata.KeyProperties)
			{
				if (metadata.KeyPropertyColumnNames.TryGetValue(keyProp.Name, out var columnName) &&
					rowDict.TryGetValue(columnName, out var value))
				{
					result[keyProp.Name] = (Guid)value;
				}
			}
		}

		if (metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return result;
	}

	/// <inheritdoc/>
	public async Task<Dictionary<string, Guid>> InsertAsync<T>(T entity) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		foreach (var keyProp in metadata.KeyProperties)
		{
			var currentValue = (Guid)keyProp.GetValue(entity)!;
			if (currentValue == Guid.Empty)
			{
				keyProp.SetValue(entity, Guid.NewGuid());
			}
		}

		var sql = $"INSERT INTO {metadata.TableName} ({metadata.InsertColumns}) " +
			$"VALUES ({metadata.InsertParameters}) RETURNING {metadata.ReturningColumns}";

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var result = new Dictionary<string, Guid>();

		if (metadata.HasSingleKey)
		{
			var id = await npgsqlConn.ExecuteScalarAsync<Guid>(sql, entity, transaction: null);
			result[metadata.FirstKeyPropertyName] = id;
		}
		else
		{
			var row = await npgsqlConn.QueryFirstAsync(sql, entity, transaction: null);
			var rowDict = (IDictionary<string, object>)row;
			foreach (var keyProp in metadata.KeyProperties)
			{
				if (metadata.KeyPropertyColumnNames.TryGetValue(keyProp.Name, out var columnName) &&
					rowDict.TryGetValue(columnName, out var value))
				{
					result[keyProp.Name] = (Guid)value;
				}
			}
		}

		if (metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return result;
	}

	#endregion

	#region <=== Update ===>

	/// <inheritdoc/>
	public bool Update<T>(T entity, string[]? propertyNamesUpdateOnly = null) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		string setClause;
		if (propertyNamesUpdateOnly is { Length: > 0 })
		{
			var properties = new List<PropertyInfo>();
			var invalidNames = new List<string>();

			foreach (var name in propertyNamesUpdateOnly)
			{
				if (metadata.WritablePropertiesByName.TryGetValue(name, out var prop))
				{
					properties.Add(prop);
				}
				else
				{
					invalidNames.Add(name);
				}
			}

			if (invalidNames.Count > 0)
			{
				throw new ArgumentException(
					$"Invalid property names for type {typeof(T).Name}: {string.Join(", ", invalidNames)}",
					nameof(propertyNamesUpdateOnly));
			}

			if (properties.Count == 0)
			{
				return false;
			}

			setClause = metadata.BuildUpdateSetClause(properties);
		}
		else
		{
			if (metadata.WritableProperties.Count == 0)
			{
				return false;
			}

			setClause = metadata.UpdateSetClause;
		}

		var sql = $"UPDATE {metadata.TableName} SET {setClause} WHERE {metadata.KeyWhereClause}";

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var affected = npgsqlConn.Execute(sql, entity, transaction: null);

		if (affected > 0 && metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return affected > 0;
	}

	/// <inheritdoc/>
	public async Task<bool> UpdateAsync<T>(T entity, string[]? propertyNamesUpdateOnly = null) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		string setClause;
		if (propertyNamesUpdateOnly is { Length: > 0 })
		{
			var properties = new List<PropertyInfo>();
			var invalidNames = new List<string>();

			foreach (var name in propertyNamesUpdateOnly)
			{
				if (metadata.WritablePropertiesByName.TryGetValue(name, out var prop))
				{
					properties.Add(prop);
				}
				else
				{
					invalidNames.Add(name);
				}
			}

			if (invalidNames.Count > 0)
			{
				throw new ArgumentException(
					$"Invalid property names for type {typeof(T).Name}: {string.Join(", ", invalidNames)}",
					nameof(propertyNamesUpdateOnly));
			}

			if (properties.Count == 0)
			{
				return false;
			}

			setClause = metadata.BuildUpdateSetClause(properties);
		}
		else
		{
			if (metadata.WritableProperties.Count == 0)
			{
				return false;
			}

			setClause = metadata.UpdateSetClause;
		}

		var sql = $"UPDATE {metadata.TableName} SET {setClause} WHERE {metadata.KeyWhereClause}";

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var affected = await npgsqlConn.ExecuteAsync(sql, entity, transaction: null);

		if (affected > 0 && metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return affected > 0;
	}

	#endregion

	#region <=== Delete ===>

	/// <inheritdoc/>
	public bool Delete<T>(T entity) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();
		var sql = $"DELETE FROM {metadata.TableName} WHERE {metadata.KeyWhereClause}";

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var affected = npgsqlConn.Execute(sql, entity, transaction: null);

		if (affected > 0 && metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return affected > 0;
	}

	/// <inheritdoc/>
	public async Task<bool> DeleteAsync<T>(T entity) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();
		var sql = $"DELETE FROM {metadata.TableName} WHERE {metadata.KeyWhereClause}";

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var affected = await npgsqlConn.ExecuteAsync(sql, entity, transaction: null);

		if (affected > 0 && metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return affected > 0;
	}

	/// <inheritdoc/>
	public bool Delete<T>(Guid id) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		if (!metadata.HasSingleKey)
		{
			throw new InvalidOperationException(
				$"Entity type {typeof(T).Name} has {metadata.KeyProperties.Count} key properties. " +
				"Use Delete(Dictionary<string, Guid>) overload for composite keys.");
		}

		var keys = new Dictionary<string, Guid>
		{
			[metadata.FirstKeyPropertyName] = id
		};

		return Delete<T>(keys);
	}

	/// <inheritdoc/>
	public async Task<bool> DeleteAsync<T>(Guid id) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		if (!metadata.HasSingleKey)
		{
			throw new InvalidOperationException(
				$"Entity type {typeof(T).Name} has {metadata.KeyProperties.Count} key properties. " +
				"Use DeleteAsync(Dictionary<string, Guid>) overload for composite keys.");
		}

		var keys = new Dictionary<string, Guid>
		{
			[metadata.FirstKeyPropertyName] = id
		};

		return await DeleteAsync<T>(keys);
	}

	/// <inheritdoc/>
	public bool Delete<T>(Dictionary<string, Guid> keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);

		var metadata = EntityMetadata.GetOrCreate<T>();

		foreach (var keyProp in metadata.KeyProperties)
		{
			if (!keys.ContainsKey(keyProp.Name))
			{
				throw new ArgumentException(
					$"Missing key property '{keyProp.Name}' in the keys dictionary " +
					$"for entity type {typeof(T).Name}.",
					nameof(keys));
			}
		}

		var sql = $"DELETE FROM {metadata.TableName} WHERE {metadata.KeyWhereClause}";

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parameters = new DynamicParameters();
		foreach (var keyProp in metadata.KeyProperties)
		{
			parameters.Add(keyProp.Name, keys[keyProp.Name]);
		}

		var affected = npgsqlConn.Execute(sql, parameters, transaction: null);

		if (affected > 0 && metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return affected > 0;
	}

	/// <inheritdoc/>
	public async Task<bool> DeleteAsync<T>(Dictionary<string, Guid> keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);

		var metadata = EntityMetadata.GetOrCreate<T>();

		foreach (var keyProp in metadata.KeyProperties)
		{
			if (!keys.ContainsKey(keyProp.Name))
			{
				throw new ArgumentException(
					$"Missing key property '{keyProp.Name}' in the keys dictionary " +
					$"for entity type {typeof(T).Name}.",
					nameof(keys));
			}
		}

		var sql = $"DELETE FROM {metadata.TableName} WHERE {metadata.KeyWhereClause}";

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parameters = new DynamicParameters();
		foreach (var keyProp in metadata.KeyProperties)
		{
			parameters.Add(keyProp.Name, keys[keyProp.Name]);
		}

		var affected = await npgsqlConn.ExecuteAsync(sql, parameters, transaction: null);

		if (affected > 0 && metadata.IsCacheable)
		{
			_cache.Invalidate<T>();
		}

		return affected > 0;
	}

	#endregion

	#region <=== Get ===>

	/// <inheritdoc/>
	public T? Get<T>(Guid id) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		if (!metadata.HasSingleKey)
		{
			throw new InvalidOperationException(
				$"Entity type {typeof(T).Name} has {metadata.KeyProperties.Count} key properties. " +
				"Use Get(Dictionary<string, Guid>) overload for composite keys.");
		}

		var keys = new Dictionary<string, Guid>
		{
			[metadata.FirstKeyPropertyName] = id
		};

		return Get<T>(keys);
	}

	/// <inheritdoc/>
	public async Task<T?> GetAsync<T>(Guid id) where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		if (!metadata.HasSingleKey)
		{
			throw new InvalidOperationException(
				$"Entity type {typeof(T).Name} has {metadata.KeyProperties.Count} key properties. " +
				"Use GetAsync(Dictionary<string, Guid>) overload for composite keys.");
		}

		var keys = new Dictionary<string, Guid>
		{
			[metadata.FirstKeyPropertyName] = id
		};

		return await GetAsync<T>(keys);
	}

	/// <inheritdoc/>
	public T? Get<T>(Dictionary<string, Guid> keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);

		var metadata = EntityMetadata.GetOrCreate<T>();

		foreach (var keyProp in metadata.KeyProperties)
		{
			if (!keys.ContainsKey(keyProp.Name))
			{
				throw new ArgumentException(
					$"Missing key property '{keyProp.Name}' in the keys dictionary " +
					$"for entity type {typeof(T).Name}.",
					nameof(keys));
			}
		}

		// Try cache first
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateKey<T>(keys);
			if (_cache.TryGet<T>(cacheKey, out var cached))
			{
				return cached;
			}
		}

		var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} WHERE {metadata.KeyWhereClause}";

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parameters = new DynamicParameters();
		foreach (var keyProp in metadata.KeyProperties)
		{
			parameters.Add(keyProp.Name, keys[keyProp.Name]);
		}

		var entity = npgsqlConn.QueryFirstOrDefault<T>(sql, parameters, transaction: null);

		// Cache the result
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateKey<T>(keys);
			_cache.Set(cacheKey, entity, metadata.CacheDurationSeconds, metadata.CacheSlidingExpiration);
		}

		return entity;
	}

	/// <inheritdoc/>
	public async Task<T?> GetAsync<T>(Dictionary<string, Guid> keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);

		var metadata = EntityMetadata.GetOrCreate<T>();

		foreach (var keyProp in metadata.KeyProperties)
		{
			if (!keys.ContainsKey(keyProp.Name))
			{
				throw new ArgumentException(
					$"Missing key property '{keyProp.Name}' in the keys dictionary " +
					$"for entity type {typeof(T).Name}.",
					nameof(keys));
			}
		}

		// Try cache first
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateKey<T>(keys);
			if (_cache.TryGet<T>(cacheKey, out var cached))
			{
				return cached;
			}
		}

		var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} WHERE {metadata.KeyWhereClause}";

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var parameters = new DynamicParameters();
		foreach (var keyProp in metadata.KeyProperties)
		{
			parameters.Add(keyProp.Name, keys[keyProp.Name]);
		}

		var entity = await npgsqlConn.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction: null);

		// Cache the result
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateKey<T>(keys);
			_cache.Set(cacheKey, entity, metadata.CacheDurationSeconds, metadata.CacheSlidingExpiration);
		}

		return entity;
	}

	#endregion

	#region <=== GetList ===>

	/// <inheritdoc/>
	public IEnumerable<T> GetList<T>() where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		// Try cache first
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateCollectionKey<T>();
			if (_cache.TryGetCollection<T>(cacheKey, out var cached))
			{
				return cached!;
			}
		}

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName}";
		var entities = npgsqlConn.Query<T>(sql, transaction: null).ToList();

		// Cache the result
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateCollectionKey<T>();
			_cache.SetCollection(cacheKey, entities, metadata.CacheDurationSeconds, metadata.CacheSlidingExpiration);
		}

		return entities;
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<T>> GetListAsync<T>() where T : class
	{
		var metadata = EntityMetadata.GetOrCreate<T>();

		// Try cache first
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateCollectionKey<T>();
			if (_cache.TryGetCollection<T>(cacheKey, out var cached))
			{
				return cached!;
			}
		}

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName}";
		var entities = (await npgsqlConn.QueryAsync<T>(sql, transaction: null)).ToList();

		// Cache the result
		if (metadata.IsCacheable)
		{
			var cacheKey = _cache.GenerateCollectionKey<T>();
			_cache.SetCollection(cacheKey, entities, metadata.CacheDurationSeconds, metadata.CacheSlidingExpiration);
		}

		return entities;
	}

	/// <inheritdoc/>
	public IEnumerable<T> GetList<T>(IEnumerable<Guid> ids) where T : class
	{
		ArgumentNullException.ThrowIfNull(ids);

		var metadata = EntityMetadata.GetOrCreate<T>();

		if (!metadata.HasSingleKey)
		{
			throw new InvalidOperationException(
				$"Entity type {typeof(T).Name} has {metadata.KeyProperties.Count} key properties. " +
				"Use GetList(IEnumerable<Dictionary<string, Guid>>) overload for composite keys.");
		}

		var idList = ids.ToList();
		if (idList.Count == 0)
		{
			return [];
		}

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} " +
			$"WHERE {metadata.FirstKeyColumnName} = ANY(@Ids)";
		var result = npgsqlConn.Query<T>(sql, new { Ids = idList }, transaction: null);
		return result;
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<T>> GetListAsync<T>(IEnumerable<Guid> ids) where T : class
	{
		ArgumentNullException.ThrowIfNull(ids);

		var metadata = EntityMetadata.GetOrCreate<T>();

		if (!metadata.HasSingleKey)
		{
			throw new InvalidOperationException(
				$"Entity type {typeof(T).Name} has {metadata.KeyProperties.Count} key properties. " +
				"Use GetListAsync(IEnumerable<Dictionary<string, Guid>>) overload for composite keys.");
		}

		var idList = ids.ToList();
		if (idList.Count == 0)
		{
			return [];
		}

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} " +
			$"WHERE {metadata.FirstKeyColumnName} = ANY(@Ids)";
		var result = await npgsqlConn.QueryAsync<T>(sql, new { Ids = idList }, transaction: null);
		return result;
	}

	/// <inheritdoc/>
	public IEnumerable<T> GetList<T>(IEnumerable<Dictionary<string, Guid>> keysList) where T : class
	{
		ArgumentNullException.ThrowIfNull(keysList);

		var metadata = EntityMetadata.GetOrCreate<T>();
		var keysArray = keysList.ToList();

		if (keysArray.Count == 0)
		{
			return [];
		}

		foreach (var keys in keysArray)
		{
			foreach (var keyProp in metadata.KeyProperties)
			{
				if (!keys.ContainsKey(keyProp.Name))
				{
					throw new ArgumentException(
						$"Missing key property '{keyProp.Name}' in a keys dictionary " +
						$"for entity type {typeof(T).Name}.",
						nameof(keysList));
				}
			}
		}

		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		if (metadata.HasSingleKey)
		{
			var ids = keysArray.Select(k => k[metadata.FirstKeyPropertyName]).ToList();
			var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} " +
				$"WHERE {metadata.FirstKeyColumnName} = ANY(@Ids)";

			var entities = npgsqlConn.Query<T>(sql, new { Ids = ids }, transaction: null);
			return entities;
		}
		else
		{
			var results = new List<T>();
			var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} " +
				$"WHERE {metadata.KeyWhereClause}";

			foreach (var keys in keysArray)
			{
				var parameters = new DynamicParameters();
				foreach (var keyProp in metadata.KeyProperties)
				{
					parameters.Add(keyProp.Name, keys[keyProp.Name]);
				}

				var entity = npgsqlConn.QueryFirstOrDefault<T>(sql, parameters, transaction: null);
				if (entity != null)
				{
					results.Add(entity);
				}
			}
			return results;
		}
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<T>> GetListAsync<T>(IEnumerable<Dictionary<string, Guid>> keysList) where T : class
	{
		ArgumentNullException.ThrowIfNull(keysList);

		var metadata = EntityMetadata.GetOrCreate<T>();
		var keysArray = keysList.ToList();

		if (keysArray.Count == 0)
		{
			return [];
		}

		foreach (var keys in keysArray)
		{
			foreach (var keyProp in metadata.KeyProperties)
			{
				if (!keys.ContainsKey(keyProp.Name))
				{
					throw new ArgumentException(
						$"Missing key property '{keyProp.Name}' in a keys dictionary " +
						$"for entity type {typeof(T).Name}.",
						nameof(keysList));
				}
			}
		}

		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		if (metadata.HasSingleKey)
		{
			var ids = keysArray.Select(k => k[metadata.FirstKeyPropertyName]).ToList();
			var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} " +
				$"WHERE {metadata.FirstKeyColumnName} = ANY(@Ids)";

			var entities = await npgsqlConn.QueryAsync<T>(sql, new { Ids = ids }, transaction: null);
			return entities;
		}
		else
		{
			var results = new List<T>();
			var sql = $"SELECT {metadata.SelectColumns} FROM {metadata.TableName} " +
				$"WHERE {metadata.KeyWhereClause}";

			foreach (var keys in keysArray)
			{
				var parameters = new DynamicParameters();
				foreach (var keyProp in metadata.KeyProperties)
				{
					parameters.Add(keyProp.Name, keys[keyProp.Name]);
				}

				var entity = await npgsqlConn.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction: null);
				if (entity != null)
				{
					results.Add(entity);
				}
			}
			return results;
		}
	}

	#endregion

	#region <=== Connection & Transaction ===>

	/// <inheritdoc/>
	public IDbConnection CreateConnection()
	{
		var currentCtx = DbConnectionContext.GetCurrentContext();

		if (currentCtx is null)
		{
			currentCtx = DbConnectionContext.CreateContext(_connectionString);
		}

		return currentCtx.CreateConnection();
	}

	/// <inheritdoc/>
	public Task<IDbConnection> CreateConnectionAsync()
	{
		var currentCtx = DbConnectionContext.GetCurrentContext();

		if (currentCtx is null)
		{
			currentCtx = DbConnectionContext.CreateContext(_connectionString);
		}

		return CreateConnectionInternalAsync(currentCtx);
	}

	/// <inheritdoc/>
	public IDbTransactionScope CreateTransactionScope(long? lockKey = null)
	{
		return new DbTransactionScope(_connectionString, lockKey);
	}

	/// <inheritdoc/>
	public Task<IDbTransactionScope> CreateTransactionScopeAsync()
	{
		return CreateTransactionScopeAsync((long?)null);
	}

	/// <inheritdoc/>
	public Task<IDbTransactionScope> CreateTransactionScopeAsync(long? lockKey)
	{
		var currentCtx = DbConnectionContext.GetCurrentContext();
		bool shouldDispose;

		if (currentCtx != null)
		{
			shouldDispose = false;
		}
		else
		{
			currentCtx = DbConnectionContext.CreateContext(_connectionString);
			shouldDispose = true;
		}

		return CreateTransactionScopeInternalAsync(currentCtx, shouldDispose, lockKey);
	}

	/// <inheritdoc/>
	public async Task<IDbTransactionScope> CreateTransactionScopeAsync(string lockKey)
	{
		ArgumentNullException.ThrowIfNull(lockKey);
		long lockKeyHash = GenerateLongHashFromString(lockKey);
		return await CreateTransactionScopeAsync(lockKeyHash);
	}

	/// <inheritdoc/>
	public IDbAdvisoryLockScope CreateAdvisoryLockScope(long lockKey)
	{
		return new DbAdvisoryLockScope(_connectionString, lockKey);
	}

	/// <inheritdoc/>
	public Task<IDbAdvisoryLockScope> CreateAdvisoryLockScopeAsync(long lockKey)
	{
		var currentCtx = DbConnectionContext.GetCurrentContext();
		bool shouldDispose;

		if (currentCtx != null)
		{
			shouldDispose = false;
		}
		else
		{
			currentCtx = DbConnectionContext.CreateContext(_connectionString);
			shouldDispose = true;
		}

		return CreateAdvisoryLockScopeInternalAsync(currentCtx, shouldDispose, lockKey);
	}

	#endregion

	#region <=== Private Helpers ===>

	private static long GenerateLongHashFromString(string? key)
	{
		if (string.IsNullOrEmpty(key))
			return 0;

		using var sha256 = System.Security.Cryptography.SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));

		return BitConverter.ToInt64(hashBytes, 0);
	}

	private static async Task<IDbConnection> CreateConnectionInternalAsync(DbConnectionContext connectionCtx)
	{
		return await connectionCtx.CreateConnectionAsync();
	}

	private static async Task<IDbTransactionScope> CreateTransactionScopeInternalAsync(
		DbConnectionContext connectionCtx, bool shouldDispose, long? lockKey)
	{
		return await DbTransactionScope.CreateAsync(connectionCtx, shouldDispose, lockKey);
	}

	private static async Task<IDbAdvisoryLockScope> CreateAdvisoryLockScopeInternalAsync(
		DbConnectionContext connectionCtx, bool shouldDispose, long lockKey)
	{
		return await DbAdvisoryLockScope.CreateAsync(connectionCtx, shouldDispose, lockKey);
	}

	#endregion
}
