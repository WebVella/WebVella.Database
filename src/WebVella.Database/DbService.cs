namespace WebVella.Database;

/// <summary>
/// A lightweight, high-performance PostgreSQL data access library built on Dapper.
/// Provides entity CRUD operations, nested transactions, advisory locks, and Row Level Security (RLS).
/// </summary>
/// <remarks>
/// <para>WebVella.Database simplifies PostgreSQL data access with the following key features:</para>
/// <list type="bullet">
/// <item>🔄 <strong>Nested Transactions</strong>: Proper savepoint handling for complex workflows</item>
/// <item>🔒 <strong>Advisory Locks</strong>: Distributed coordination with simple scope management</item>
/// <item>🛡️ <strong>Row Level Security (RLS)</strong>: Automatic session context for multi-tenant applications</item>
/// <item>📦 <strong>Entity Caching</strong>: Optional caching with automatic invalidation (RLS-aware)</item>
/// <item>🚀 <strong>Database Migrations</strong>: Version-controlled schema changes</item>
/// <item>🎯 <strong>JSON Columns</strong>: Automatic serialization/deserialization</item>
/// </list>
/// 
/// <para><strong>Quick Start:</strong></para>
/// <code>
/// // Registration
/// builder.Services.AddWebVellaDatabase(connectionString);
/// 
/// // Basic CRUD
/// var user = await dbService.GetAsync&lt;User&gt;(userId);
/// var newUser = await dbService.InsertAsync(new User { Name = "John" });
/// await dbService.UpdateAsync(user);
/// await dbService.DeleteAsync&lt;User&gt;(userId);
/// 
/// // Transactions with automatic nesting
/// await using var scope = await dbService.CreateTransactionScopeAsync();
/// await dbService.InsertAsync(user);
/// await dbService.InsertAsync(order);
/// await scope.CompleteAsync();
/// </code>
/// 
/// <para>For complete documentation and examples, visit: https://github.com/WebVella/WebVella.Database/blob/main/docs/webvella.database.docs.md</para>
/// </remarks>
/// <example>
/// <para>Complete service example:</para>
/// <code>
/// public class UserService
/// {
///     private readonly IDbService _db;
///     
///     public UserService(IDbService db) => _db = db;
///     
///     public async Task&lt;User&gt; CreateUserAsync(User user) => await _db.InsertAsync(user);
///     public async Task&lt;User?&gt; GetUserAsync(Guid id) => await _db.GetAsync&lt;User&gt;(id);
///     public async Task&lt;IEnumerable&lt;User&gt;&gt; GetActiveUsersAsync() => 
///         await _db.QueryAsync&lt;User&gt;("SELECT * FROM users WHERE is_active = true");
///     public async Task&lt;bool&gt; UpdateUserAsync(User user) => await _db.UpdateAsync(user);
///     public async Task&lt;bool&gt; DeleteUserAsync(Guid id) => await _db.DeleteAsync&lt;User&gt;(id);
/// }
/// </code>
/// </example>
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

	/// <summary>
	/// Executes a query and returns an <see cref="IDataReader"/> for reading the result set.
	/// </summary>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>An <see cref="IDataReader"/> for reading the result set.</returns>
	IDataReader ExecuteReader(string sql, object? parameters = null);

	/// <summary>
	/// Asynchronously executes a query and returns a <see cref="DbDataReader"/> for reading the result set.
	/// </summary>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A <see cref="DbDataReader"/> for reading the result set.</returns>
	Task<DbDataReader> ExecuteReaderAsync(string sql, object? parameters = null);

	/// <summary>
	/// Executes a query and returns the first column of the first row as the specified type.
	/// </summary>
	/// <typeparam name="T">The type to return.</typeparam>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>The first column of the first row, or default if no results.</returns>
	T? ExecuteScalar<T>(string sql, object? parameters = null);

	/// <summary>
	/// Asynchronously executes a query and returns the first column of the first row as the specified type.
	/// </summary>
	/// <typeparam name="T">The type to return.</typeparam>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>The first column of the first row, or default if no results.</returns>
	Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);

	/// <summary>
	/// Executes a query and fills a <see cref="DataTable"/> with the result set.
	/// </summary>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A <see cref="DataTable"/> containing the result set.</returns>
	DataTable GetDataTable(string sql, object? parameters = null);

	/// <summary>
	/// Asynchronously executes a query and fills a <see cref="DataTable"/> with the result set.
	/// </summary>
	/// <param name="sql">The SQL query to execute.</param>
	/// <param name="parameters">The parameters for the query.</param>
	/// <returns>A <see cref="DataTable"/> containing the result set.</returns>
	Task<DataTable> GetDataTableAsync(string sql, object? parameters = null);

	#endregion

	#region <=== Insert ===>

		/// <summary>
		/// Inserts an entity into the database and returns the inserted entity with generated values.
		/// The key properties (marked with [Key]) will be populated via RETURNING.
		/// </summary>
		/// <typeparam name="T">The entity type.</typeparam>
		/// <param name="entity">The entity to insert.</param>
		/// <returns>The inserted entity with generated key values populated.</returns>
		T Insert<T>(T entity) where T : class;

		/// <summary>
		/// Asynchronously inserts an entity into the database and returns the inserted entity with
		/// generated values. The key properties (marked with [Key]) will be populated via RETURNING.
		/// </summary>
		/// <typeparam name="T">The entity type.</typeparam>
		/// <param name="entity">The entity to insert.</param>
		/// <returns>The inserted entity with generated key values populated.</returns>
		Task<T> InsertAsync<T>(T entity) where T : class;

		/// <summary>
		/// Inserts a new entity by mapping properties from an anonymous object or any object instance
		/// to the entity type <typeparamref name="T"/>. Only properties with matching names are mapped.
		/// Property types must match exactly. Throws if the object contains properties not found on
		/// the entity type.
		/// </summary>
		/// <typeparam name="T">The entity type.</typeparam>
		/// <param name="obj">
		/// An anonymous object, class, or record whose properties will be mapped to the entity.
		/// All properties must exist on the entity type.
		/// </param>
		/// <returns>The inserted entity with generated key values populated.</returns>
		T Insert<T>(object obj) where T : class, new();

		/// <summary>
		/// Asynchronously inserts a new entity by mapping properties from an anonymous object or any
		/// object instance to the entity type <typeparamref name="T"/>. Only properties with matching
		/// names are mapped. Property types must match exactly. Throws if the object contains
		/// properties not found on the entity type.
		/// </summary>
		/// <typeparam name="T">The entity type.</typeparam>
		/// <param name="obj">
		/// An anonymous object, class, or record whose properties will be mapped to the entity.
		/// All properties must exist on the entity type.
		/// </param>
		/// <returns>The inserted entity with generated key values populated.</returns>
		Task<T> InsertAsync<T>(object obj) where T : class, new();

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

	/// <summary>
	/// Updates an entity by mapping properties from an anonymous object or any object instance
	/// to the entity type <typeparamref name="T"/>. The object must contain all key properties.
	/// Only the non-key properties present in the object are updated.
	/// Property types must match exactly. Throws if the object contains properties not found on
	/// the entity type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="obj">
	/// An anonymous object, class, or record whose properties will be mapped to the entity.
	/// Must include all key properties of the entity type.
	/// All properties must exist on the entity type.
	/// </param>
	/// <returns>True if the entity was updated; otherwise, false.</returns>
	bool Update<T>(object obj) where T : class, new();

	/// <summary>
	/// Asynchronously updates an entity by mapping properties from an anonymous object or any
	/// object instance to the entity type <typeparamref name="T"/>. The object must contain all
	/// key properties. Only the non-key properties present in the object are updated.
	/// Property types must match exactly. Throws if the object contains properties not found on
	/// the entity type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="obj">
	/// An anonymous object, class, or record whose properties will be mapped to the entity.
	/// Must include all key properties of the entity type.
	/// All properties must exist on the entity type.
	/// </param>
	/// <returns>True if the entity was updated; otherwise, false.</returns>
	Task<bool> UpdateAsync<T>(object obj) where T : class, new();

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

	/// <summary>
	/// Deletes an entity from the database by its composite primary key using an anonymous object.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">An anonymous object with properties matching the key property names and Guid values.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	bool Delete<T>(object keys) where T : class;

	/// <summary>
	/// Asynchronously deletes an entity from the database by its composite primary key using an anonymous
	/// object.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">An anonymous object with properties matching the key property names and Guid values.</param>
	/// <returns>True if the entity was deleted; otherwise, false.</returns>
	Task<bool> DeleteAsync<T>(object keys) where T : class;

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

	/// <summary>
	/// Retrieves an entity by its composite primary key using an anonymous object.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">An anonymous object with properties matching the key property names and Guid values.</param>
	/// <returns>The entity if found; otherwise, null.</returns>
	T? Get<T>(object keys) where T : class;

	/// <summary>
	/// Asynchronously retrieves an entity by its composite primary key using an anonymous object.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">An anonymous object with properties matching the key property names and Guid values.</param>
	/// <returns>The entity if found; otherwise, null.</returns>
	Task<T?> GetAsync<T>(object keys) where T : class;

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

	/// <summary>
	/// Retrieves multiple entities by their composite primary keys using anonymous objects.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keysList">
	/// A collection of anonymous objects with properties matching the key property names and Guid values.
	/// </param>
	/// <returns>A collection of entities that were found.</returns>
	IEnumerable<T> GetList<T>(IEnumerable<object> keysList) where T : class;

	/// <summary>
	/// Asynchronously retrieves multiple entities by their composite primary keys using anonymous objects.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keysList">
	/// A collection of anonymous objects with properties matching the key property names and Guid values.
	/// </param>
	/// <returns>A collection of entities that were found.</returns>
	Task<IEnumerable<T>> GetListAsync<T>(IEnumerable<object> keysList) where T : class;

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
	private readonly Security.RlsSessionInitializer? _rlsInitializer;
	private readonly Security.IRlsContextProvider? _rlsContextProvider;

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
		: this(connectionString, cache, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DbService"/> class with RLS support.
	/// </summary>
	/// <param name="connectionString">The PostgreSQL connection string.</param>
	/// <param name="cache">The entity cache implementation.</param>
	/// <param name="rlsContextProvider">
	/// The RLS context provider for setting session variables. Pass <c>null</c> to disable RLS.
	/// </param>
	/// <param name="rlsOptions">The RLS options. If <c>null</c>, default options are used.</param>
	public DbService(
		string connectionString,
		IDbEntityCache cache,
		Security.IRlsContextProvider? rlsContextProvider,
		Security.RlsOptions? rlsOptions)
	{
		_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_rlsContextProvider = rlsContextProvider;

		if (rlsContextProvider != null)
		{
			_rlsInitializer = new Security.RlsSessionInitializer(
				rlsContextProvider,
				rlsOptions ?? new Security.RlsOptions());
		}
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

	/// <inheritdoc/>
	public IDataReader ExecuteReader(string sql, object? parameters = null)
	{
		var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		return npgsqlConn.ExecuteReader(sql, parameters, transaction: null, commandTimeout: null,
			commandType: null);
	}

	/// <inheritdoc/>
	public async Task<DbDataReader> ExecuteReaderAsync(string sql, object? parameters = null)
	{
		var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		return await npgsqlConn.ExecuteReaderAsync(sql, parameters, transaction: null, commandTimeout: null,
			commandType: null);
	}

	/// <inheritdoc/>
	public T? ExecuteScalar<T>(string sql, object? parameters = null)
	{
		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		return npgsqlConn.ExecuteScalar<T>(sql, parameters, transaction: null);
	}

	/// <inheritdoc/>
	public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
	{
		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		return await npgsqlConn.ExecuteScalarAsync<T>(sql, parameters, transaction: null);
	}

	/// <inheritdoc/>
	public DataTable GetDataTable(string sql, object? parameters = null)
	{
		using var conn = CreateConnection();
		var npgsqlConn = conn.GetUnderlyingConnection();

		using var reader = npgsqlConn.ExecuteReader(sql, parameters, transaction: null);
		var dataTable = new DataTable();
		dataTable.Load(reader);
		return dataTable;
	}

	/// <inheritdoc/>
	public async Task<DataTable> GetDataTableAsync(string sql, object? parameters = null)
	{
		await using var conn = await CreateConnectionAsync();
		var npgsqlConn = conn.GetUnderlyingConnection();

		await using var reader = await npgsqlConn.ExecuteReaderAsync(sql, parameters, transaction: null);
		var dataTable = new DataTable();
		dataTable.Load(reader);
		return dataTable;
	}

	#endregion

	#region <=== Insert ===>

		/// <inheritdoc/>
		public T Insert<T>(T entity) where T : class
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

			if (metadata.HasSingleKey)
			{
				var id = npgsqlConn.ExecuteScalar<Guid>(sql, entity, transaction: null);
				metadata.KeyProperties[0].SetValue(entity, id);
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
						keyProp.SetValue(entity, (Guid)value);
					}
				}
			}

			if (metadata.IsCacheable)
			{
				_cache.Invalidate<T>();
			}

			return entity;
		}

		/// <inheritdoc/>
		public async Task<T> InsertAsync<T>(T entity) where T : class
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

			if (metadata.HasSingleKey)
			{
				var id = await npgsqlConn.ExecuteScalarAsync<Guid>(sql, entity, transaction: null);
				metadata.KeyProperties[0].SetValue(entity, id);
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
						keyProp.SetValue(entity, (Guid)value);
					}
				}
			}

			if (metadata.IsCacheable)
			{
				_cache.Invalidate<T>();
			}

			return entity;
		}

		/// <inheritdoc/>
		public T Insert<T>(object obj) where T : class, new()
		{
			ArgumentNullException.ThrowIfNull(obj);
			var entity = MapToEntityForInsert<T>(obj);
			return Insert(entity);
		}

		/// <inheritdoc/>
		public async Task<T> InsertAsync<T>(object obj) where T : class, new()
		{
			ArgumentNullException.ThrowIfNull(obj);
			var entity = MapToEntityForInsert<T>(obj);
			return await InsertAsync(entity);
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

	/// <inheritdoc/>
	public bool Update<T>(object obj) where T : class, new()
	{
		ArgumentNullException.ThrowIfNull(obj);
		var (entity, propertyNames) = MapToEntityForUpdate<T>(obj);
		if (propertyNames.Length == 0)
			return false;
		return Update(entity, propertyNames);
	}

	/// <inheritdoc/>
	public async Task<bool> UpdateAsync<T>(object obj) where T : class, new()
	{
		ArgumentNullException.ThrowIfNull(obj);
		var (entity, propertyNames) = MapToEntityForUpdate<T>(obj);
		if (propertyNames.Length == 0)
			return false;
		return await UpdateAsync(entity, propertyNames);
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

	/// <inheritdoc/>
	public bool Delete<T>(object keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);
		var keysDictionary = ConvertToKeysDictionary(keys);
		return Delete<T>(keysDictionary);
	}

	/// <inheritdoc/>
	public async Task<bool> DeleteAsync<T>(object keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);
		var keysDictionary = ConvertToKeysDictionary(keys);
		return await DeleteAsync<T>(keysDictionary);
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
			var cacheKey = _cache.GenerateKey<T>(keys, GetRlsCacheContext());
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
			var cacheKey = _cache.GenerateKey<T>(keys, GetRlsCacheContext());
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
			var cacheKey = _cache.GenerateKey<T>(keys, GetRlsCacheContext());
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
			var cacheKey = _cache.GenerateKey<T>(keys, GetRlsCacheContext());
			_cache.Set(cacheKey, entity, metadata.CacheDurationSeconds, metadata.CacheSlidingExpiration);
		}

		return entity;
	}

	/// <inheritdoc/>
	public T? Get<T>(object keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);
		var keysDictionary = ConvertToKeysDictionary(keys);
		return Get<T>(keysDictionary);
	}

	/// <inheritdoc/>
	public async Task<T?> GetAsync<T>(object keys) where T : class
	{
		ArgumentNullException.ThrowIfNull(keys);
		var keysDictionary = ConvertToKeysDictionary(keys);
		return await GetAsync<T>(keysDictionary);
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
			var cacheKey = _cache.GenerateCollectionKey<T>(null, GetRlsCacheContext());
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
			var cacheKey = _cache.GenerateCollectionKey<T>(null, GetRlsCacheContext());
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
			var cacheKey = _cache.GenerateCollectionKey<T>(null, GetRlsCacheContext());
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
			var cacheKey = _cache.GenerateCollectionKey<T>(null, GetRlsCacheContext());
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

	/// <inheritdoc/>
	public IEnumerable<T> GetList<T>(IEnumerable<object> keysList) where T : class
	{
		ArgumentNullException.ThrowIfNull(keysList);
		var dictionaryList = keysList.Select(ConvertToKeysDictionary).ToList();
		return GetList<T>(dictionaryList);
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<T>> GetListAsync<T>(IEnumerable<object> keysList) where T : class
	{
		ArgumentNullException.ThrowIfNull(keysList);
		var dictionaryList = keysList.Select(ConvertToKeysDictionary).ToList();
		return await GetListAsync<T>(dictionaryList);
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

		var connection = currentCtx.CreateConnection();

		if (_rlsInitializer != null && _rlsInitializer.HasContext)
		{
			_rlsInitializer.InitializeSession(connection.GetUnderlyingConnection());
		}

		return connection;
	}

	/// <inheritdoc/>
	public async Task<IDbConnection> CreateConnectionAsync()
	{
		var currentCtx = DbConnectionContext.GetCurrentContext();

		if (currentCtx is null)
		{
			currentCtx = DbConnectionContext.CreateContext(_connectionString);
		}

		var connection = await currentCtx.CreateConnectionAsync();

		if (_rlsInitializer != null && _rlsInitializer.HasContext)
		{
			await _rlsInitializer.InitializeSessionAsync(connection.GetUnderlyingConnection());
		}

		return connection;
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

	/// <summary>
	/// Gets the current RLS context string for cache key generation.
	/// Returns null if no RLS context is configured.
	/// </summary>
	private string? GetRlsCacheContext()
	{
		if (_rlsContextProvider == null)
			return null;

		var parts = new List<string>();

		if (_rlsContextProvider.TenantId.HasValue)
			parts.Add($"t:{_rlsContextProvider.TenantId}");

		if (_rlsContextProvider.UserId.HasValue)
			parts.Add($"u:{_rlsContextProvider.UserId}");

		if (_rlsContextProvider.CustomClaims.Count > 0)
		{
			var sortedClaims = _rlsContextProvider.CustomClaims
				.OrderBy(c => c.Key)
				.Select(c => $"{c.Key}:{c.Value}");
			parts.Add($"c:{string.Join(",", sortedClaims)}");
		}

		return parts.Count > 0 ? string.Join("|", parts) : null;
	}

	private static long GenerateLongHashFromString(string? key)
	{
		if (string.IsNullOrEmpty(key))
			return 0;

		using var sha256 = System.Security.Cryptography.SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));

		return BitConverter.ToInt64(hashBytes, 0);
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

	private static Dictionary<string, Guid> ConvertToKeysDictionary(object keys)
	{
		ArgumentNullException.ThrowIfNull(keys);

		var result = new Dictionary<string, Guid>();
		var properties = keys.GetType().GetProperties(
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

		foreach (var prop in properties)
		{
			if (prop.PropertyType != typeof(Guid))
			{
				throw new ArgumentException(
					$"Property '{prop.Name}' must be of type Guid, but was {prop.PropertyType.Name}.",
					nameof(keys));
			}

			var value = prop.GetValue(keys);
			if (value is Guid guidValue)
			{
				result[prop.Name] = guidValue;
			}
		}

		if (result.Count == 0)
		{
			throw new ArgumentException(
				"The keys object must have at least one Guid property.",
				nameof(keys));
		}

		return result;
	}

	/// <summary>
	/// Creates a new instance of <typeparamref name="T"/> and maps matching properties
	/// from the source object. Validates that property types match and that all source
	/// properties exist on the entity type.
	/// </summary>
	private static T MapToEntityForInsert<T>(object obj) where T : class, new()
	{
		var entity = new T();
		var entityProperties = typeof(T).GetProperties(
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		var entityPropsByName = entityProperties
			.Where(p => p.CanWrite)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		var sourceProperties = obj.GetType().GetProperties(
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

		var typeMismatches = new List<string>();
		var unknownProperties = new List<string>();

		foreach (var sourceProp in sourceProperties)
		{
			if (!sourceProp.CanRead)
				continue;

			if (!entityPropsByName.TryGetValue(sourceProp.Name, out var entityProp))
			{
				unknownProperties.Add(sourceProp.Name);
				continue;
			}

			if (sourceProp.PropertyType != entityProp.PropertyType)
			{
				typeMismatches.Add(
					$"'{sourceProp.Name}' (source: {sourceProp.PropertyType.Name}, " +
					$"entity: {entityProp.PropertyType.Name})");
				continue;
			}

			var value = sourceProp.GetValue(obj);
			entityProp.SetValue(entity, value);
		}

		if (unknownProperties.Count > 0)
		{
			throw new ArgumentException(
				$"Unknown properties for entity {typeof(T).Name}: " +
				string.Join(", ", unknownProperties),
				nameof(obj));
		}

		if (typeMismatches.Count > 0)
		{
			throw new ArgumentException(
				$"Type mismatch for properties on entity {typeof(T).Name}: " +
				string.Join(", ", typeMismatches),
				nameof(obj));
		}

		return entity;
	}

	/// <summary>
	/// Creates a new instance of <typeparamref name="T"/> and maps matching properties
	/// from the source object for update. Requires all key properties to be present.
	/// Validates that property types match and that all source properties exist on the
	/// entity type. Returns the entity and the list of non-key writable property names
	/// that were mapped.
	/// </summary>
	private static (T Entity, string[] PropertyNames) MapToEntityForUpdate<T>(object obj)
		where T : class, new()
	{
		var entity = new T();
		var metadata = EntityMetadata.GetOrCreate<T>();

		var sourceProperties = obj.GetType().GetProperties(
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		var sourcePropsByName = sourceProperties
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		var keyPropertyNames = metadata.KeyProperties
			.Select(p => p.Name)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var typeMismatches = new List<string>();
		var missingKeys = new List<string>();

		foreach (var keyProp in metadata.KeyProperties)
		{
			if (!sourcePropsByName.TryGetValue(keyProp.Name, out var sourceProp))
			{
				missingKeys.Add(keyProp.Name);
				continue;
			}

			if (sourceProp.PropertyType != keyProp.PropertyType)
			{
				typeMismatches.Add(
					$"'{keyProp.Name}' (source: {sourceProp.PropertyType.Name}, " +
					$"entity: {keyProp.PropertyType.Name})");
				continue;
			}

			keyProp.SetValue(entity, sourceProp.GetValue(obj));
		}

		if (missingKeys.Count > 0)
		{
			throw new ArgumentException(
				$"Missing required key properties for entity {typeof(T).Name}: " +
				string.Join(", ", missingKeys),
				nameof(obj));
		}

		var updatedPropertyNames = new List<string>();
		var unknownProperties = new List<string>();

		foreach (var sourceProp in sourceProperties)
		{
			if (!sourceProp.CanRead)
				continue;

			if (keyPropertyNames.Contains(sourceProp.Name))
				continue;

			if (!metadata.WritablePropertiesByName.TryGetValue(
				sourceProp.Name, out var entityProp))
			{
				unknownProperties.Add(sourceProp.Name);
				continue;
			}

			if (sourceProp.PropertyType != entityProp.PropertyType)
			{
				typeMismatches.Add(
					$"'{sourceProp.Name}' (source: {sourceProp.PropertyType.Name}, " +
					$"entity: {entityProp.PropertyType.Name})");
				continue;
			}

			entityProp.SetValue(entity, sourceProp.GetValue(obj));
			updatedPropertyNames.Add(entityProp.Name);
		}

		if (unknownProperties.Count > 0)
		{
			throw new ArgumentException(
				$"Unknown properties for entity {typeof(T).Name}: " +
				string.Join(", ", unknownProperties),
				nameof(obj));
		}

		if (typeMismatches.Count > 0)
		{
			throw new ArgumentException(
				$"Type mismatch for properties on entity {typeof(T).Name}: " +
				string.Join(", ", typeMismatches),
				nameof(obj));
		}

		return (entity, updatedPropertyNames.ToArray());
	}

	#endregion
}
