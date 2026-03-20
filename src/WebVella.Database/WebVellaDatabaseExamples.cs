namespace WebVella.Database;

/// <summary>
/// Code examples and usage patterns for WebVella.Database.
/// This class provides IntelliSense-friendly examples that Copilot can reference and suggest.
/// </summary>
/// <remarks>
/// WebVella.Database is a lightweight, high-performance PostgreSQL data access library built on Dapper.
/// Key features include:
/// - Nested transaction support with proper PostgreSQL savepoints
/// - Advisory locks for distributed coordination
/// - Row Level Security (RLS) with automatic session context
/// - Database migrations with version control
/// - Entity caching with automatic invalidation
/// - JSON column support with automatic serialization
/// 
/// For complete documentation:
/// https://github.com/WebVella/WebVella.Database/blob/main/docs/webvella.database.docs.md
/// </remarks>
public static class WebVellaDatabaseExamples
{
	/// <summary>
	/// Service registration examples for dependency injection.
	/// </summary>
	/// <example>
	/// Basic registration:
	/// <code>
	/// builder.Services.AddWebVellaDatabase(
	///     "Host=localhost;Database=mydb;Username=user;Password=pass");
	/// </code>
	/// 
	/// With entity caching:
	/// <code>
	/// builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);
	/// </code>
	/// 
	/// With factory pattern:
	/// <code>
	/// builder.Services.AddWebVellaDatabase(
	///     sp => sp.GetRequiredService&lt;IConfiguration&gt;()
	///              .GetConnectionString("DefaultConnection")!);
	/// </code>
	/// 
	/// With Row Level Security (RLS):
	/// <code>
	/// builder.Services.AddWebVellaDatabaseWithRls&lt;HttpRlsContextProvider&gt;(
	///     connectionString);
	/// </code>
	/// 
	/// With migrations:
	/// <code>
	/// builder.Services.AddWebVellaDatabase(connectionString);
	/// builder.Services.AddWebVellaDatabaseMigrations();
	/// 
	/// using var scope = app.Services.CreateScope();
	/// var migrationService = scope.ServiceProvider
	///     .GetRequiredService&lt;IDbMigrationService&gt;();
	/// await migrationService.ExecutePendingMigrationsAsync();
	/// </code>
	/// </example>
	public static class ServiceRegistration { }

	/// <summary>
	/// Entity definition examples with attributes.
	/// </summary>
	/// <example>
	/// Basic entity with auto-generated key:
	/// <code>
	/// [Table("users")]
	/// [Cacheable(DurationSeconds = 600)]
	/// public class User
	/// {
	///     [Key]
	///     public Guid Id { get; set; }
	///     
	///     public string Name { get; set; } = string.Empty;
	///     public string Email { get; set; } = string.Empty;
	///     
	///     [JsonColumn]
	///     public UserSettings? Settings { get; set; }
	///     
	///     [Write(false)]
	///     public string DisplayName => $"{Name} ({Email})";
	///     
	///     [External]
	///     public List&lt;Order&gt;? Orders { get; set; }
	/// }
	/// </code>
	/// 
	/// Entity with custom column names:
	/// <code>
	/// [Table("legacy_users")]
	/// public class LegacyUser
	/// {
	///     [Key]
	///     [DbColumn("usr_id")]
	///     public Guid Id { get; set; }
	///     
	///     [DbColumn("full_name")]
	///     public string DisplayName { get; set; } = string.Empty;
	///     
	///     [DbColumn("email_address")]
	///     public string Email { get; set; } = string.Empty;
	///     
	///     public string Description { get; set; } = string.Empty;
	/// }
	/// </code>
	/// 
	/// Composite key entity:
	/// <code>
	/// [Table("user_roles")]
	/// public class UserRole
	/// {
	///     [ExplicitKey]
	///     public Guid UserId { get; set; }
	///     
	///     [ExplicitKey]
	///     public Guid RoleId { get; set; }
	///     
	///     public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
	/// }
	/// </code>
	/// 
	/// Multi-query container:
	/// <code>
	/// [MultiQuery]
	/// public class UserDashboard
	/// {
	///     [ResultSet(0)]
	///     public User? Profile { get; set; }
	///     
	///     [ResultSet(1)]
	///     public List&lt;Order&gt; RecentOrders { get; set; } = [];
	/// }
	/// </code>
	/// 
	/// Parent entity with child mapping:
	/// <code>
	/// [Table("orders")]
	/// public class Order
	/// {
	///     [Key]
	///     public Guid Id { get; set; }
	///     public Guid UserId { get; set; }
	///     public decimal TotalAmount { get; set; }
	///     
	///     [External]
	///     [ResultSet(1, ForeignKey = "OrderId")]
	///     public List&lt;OrderLine&gt; Lines { get; set; } = [];
	///     
	///     [External]
	///     [ResultSet(2, ForeignKey = "OrderId")]
	///     public List&lt;OrderNote&gt; Notes { get; set; } = [];
	/// }
	/// </code>
	/// </example>
	public static class EntityDefinitions { }

	/// <summary>
	/// Examples for <see cref="IDbService.Query{T}"/>
	/// and <see cref="IDbService.QueryAsync{T}"/>.
	/// Executes SQL and maps results to a collection of entities.
	/// </summary>
	/// <example>
	/// <code>
	/// // Query with parameters
	/// var activeUsers = await _db.QueryAsync&lt;User&gt;(
	///     "SELECT * FROM users WHERE is_active = @IsActive",
	///     new { IsActive = true });
	/// 
	/// // Query with multiple parameters
	/// var users = await _db.QueryAsync&lt;User&gt;(
	///     """
	///     SELECT * FROM users
	///     WHERE role = @Role AND created_at >= @Since
	///     ORDER BY username
	///     """,
	///     new { Role = 2, Since = DateTime.UtcNow.AddDays(-30) });
	/// 
	/// // Query with LIKE pattern
	/// var users = await _db.QueryAsync&lt;User&gt;(
	///     "SELECT * FROM users WHERE email LIKE @Pattern",
	///     new { Pattern = "%@example.com" });
	/// 
	/// // Query with IN clause (PostgreSQL ANY)
	/// var ids = new List&lt;Guid&gt; { id1, id2, id3 };
	/// var users = await _db.QueryAsync&lt;User&gt;(
	///     "SELECT * FROM users WHERE id = ANY(@Ids)",
	///     new { Ids = ids });
	/// 
	/// // Query returning custom DTO
	/// var summaries = await _db.QueryAsync&lt;UserSummary&gt;(
	///     """
	///     SELECT u.id AS "Id", u.username AS "Username",
	///            COUNT(o.id) AS "OrderCount"
	///     FROM users u
	///     LEFT JOIN orders o ON o.user_id = u.id
	///     GROUP BY u.id, u.username
	///     """);
	/// 
	/// // Sync version
	/// var users = _db.Query&lt;User&gt;(
	///     "SELECT * FROM users WHERE is_active = true");
	/// </code>
	/// </example>
	public static class QueryExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.QueryMultiple{T}"/>
	/// and <see cref="IDbService.QueryMultipleAsync{T}"/>.
	/// Executes multiple SELECT statements and maps each result set
	/// to properties in a container class marked with
	/// <see cref="MultiQueryAttribute"/>.
	/// </summary>
	/// <example>
	/// <code>
	/// [MultiQuery]
	/// public class UserDashboard
	/// {
	///     [ResultSet(0)]
	///     public User? Profile { get; set; }
	///     
	///     [ResultSet(1)]
	///     public List&lt;Order&gt; RecentOrders { get; set; } = [];
	///     
	///     [ResultSet(2)]
	///     public DashboardStats? Stats { get; set; }
	/// }
	/// 
	/// var sql = """
	///     SELECT * FROM users WHERE id = @UserId;
	///     SELECT * FROM orders WHERE user_id = @UserId
	///         ORDER BY created_at DESC LIMIT 10;
	///     SELECT COUNT(*) AS "TotalOrders",
	///            COALESCE(SUM(total_amount), 0) AS "TotalSpent"
	///     FROM orders WHERE user_id = @UserId;
	///     """;
	/// 
	/// // Async
	/// var dashboard = await _db.QueryMultipleAsync&lt;UserDashboard&gt;(
	///     sql, new { UserId = userId });
	/// 
	/// // Sync
	/// var dashboard = _db.QueryMultiple&lt;UserDashboard&gt;(
	///     sql, new { UserId = userId });
	/// </code>
	/// </example>
	public static class QueryMultipleExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.QueryMultipleList{T}"/>
	/// and <see cref="IDbService.QueryMultipleListAsync{T}"/>.
	/// Executes multiple SELECT statements where the first result set
	/// contains parent entities and subsequent sets contain child entities
	/// mapped via <see cref="ResultSetAttribute.ForeignKey"/>.
	/// </summary>
	/// <example>
	/// <code>
	/// [Table("orders")]
	/// public class Order
	/// {
	///     [Key]
	///     public Guid Id { get; set; }
	///     public Guid UserId { get; set; }
	///     public string OrderNumber { get; set; } = string.Empty;
	///     
	///     [External]
	///     [ResultSet(1, ForeignKey = "OrderId")]
	///     public List&lt;OrderLine&gt; Lines { get; set; } = [];
	///     
	///     [External]
	///     [ResultSet(2, ForeignKey = "OrderId")]
	///     public List&lt;OrderNote&gt; Notes { get; set; } = [];
	/// }
	/// 
	/// var sql = """
	///     SELECT * FROM orders WHERE user_id = @UserId;
	///     SELECT * FROM order_lines
	///         WHERE order_id IN (
	///             SELECT id FROM orders WHERE user_id = @UserId);
	///     SELECT * FROM order_notes
	///         WHERE order_id IN (
	///             SELECT id FROM orders WHERE user_id = @UserId);
	///     """;
	/// 
	/// // Async - child collections populated automatically
	/// var orders = await _db.QueryMultipleListAsync&lt;Order&gt;(
	///     sql, new { UserId = userId });
	/// 
	/// foreach (var order in orders)
	/// {
	///     Console.WriteLine($"Order {order.OrderNumber}");
	///     Console.WriteLine($"  Lines: {order.Lines.Count}");
	///     Console.WriteLine($"  Notes: {order.Notes.Count}");
	/// }
	/// 
	/// // Sync
	/// var orders = _db.QueryMultipleList&lt;Order&gt;(
	///     sql, new { UserId = userId });
	/// </code>
	/// </example>
	public static class QueryMultipleListExamples { }

	/// <summary>
	/// Examples for QueryWithJoin methods with one child collection.
	/// Uses a single SQL JOIN query with lambda-based mapping.
	/// </summary>
	/// <example>
	/// <code>
	/// var sql = """
	///     SELECT o.id AS "Id", o.user_id AS "UserId",
	///            o.order_number AS "OrderNumber",
	///            o.total_amount AS "TotalAmount",
	///            l.id AS "Id", l.order_id AS "OrderId",
	///            l.product_name AS "ProductName",
	///            l.quantity AS "Quantity",
	///            l.unit_price AS "UnitPrice"
	///     FROM orders o
	///     LEFT JOIN order_lines l ON l.order_id = o.id
	///     WHERE o.user_id = @UserId
	///     ORDER BY o.created_at DESC
	///     """;
	/// 
	/// // Async
	/// var orders = await _db.QueryWithJoinAsync&lt;Order, OrderLine&gt;(
	///     sql,
	///     parent => parent.Lines,
	///     parent => parent.Id,
	///     child => child.Id,
	///     splitOn: "Id",
	///     parameters: new { UserId = userId });
	/// 
	/// // Sync
	/// var orders = _db.QueryWithJoin&lt;Order, OrderLine&gt;(
	///     sql,
	///     parent => parent.Lines,
	///     parent => parent.Id,
	///     child => child.Id,
	///     splitOn: "Id",
	///     parameters: new { UserId = userId });
	/// </code>
	/// </example>
	public static class QueryWithJoinOneChildExamples { }

	/// <summary>
	/// Examples for QueryWithJoin methods with two child collections.
	/// Uses a single SQL JOIN query with automatic Cartesian product
	/// deduplication.
	/// </summary>
	/// <example>
	/// <code>
	/// var sql = """
	///     SELECT o.id AS "Id", o.user_id AS "UserId",
	///            o.order_number AS "OrderNumber",
	///            l.id AS "Id", l.order_id AS "OrderId",
	///            l.product_name AS "ProductName",
	///            n.id AS "Id", n.order_id AS "OrderId",
	///            n.text AS "Text"
	///     FROM orders o
	///     LEFT JOIN order_lines l ON l.order_id = o.id
	///     LEFT JOIN order_notes n ON n.order_id = o.id
	///     WHERE o.user_id = @UserId
	///     """;
	/// 
	/// // Async
	/// var orders =
	///     await _db.QueryWithJoinAsync&lt;Order, OrderLine, OrderNote&gt;(
	///         sql,
	///         parent => parent.Lines,
	///         parent => parent.Notes,
	///         parent => parent.Id,
	///         child1 => child1.Id,
	///         child2 => child2.Id,
	///         splitOn: "Id,Id",
	///         parameters: new { UserId = userId });
	/// 
	/// // Sync
	/// var orders =
	///     _db.QueryWithJoin&lt;Order, OrderLine, OrderNote&gt;(
	///         sql,
	///         parent => parent.Lines,
	///         parent => parent.Notes,
	///         parent => parent.Id,
	///         child1 => child1.Id,
	///         child2 => child2.Id,
	///         splitOn: "Id,Id",
	///         parameters: new { UserId = userId });
	/// </code>
	/// </example>
	public static class QueryWithJoinTwoChildrenExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.Execute"/>
	/// and <see cref="IDbService.ExecuteAsync"/>.
	/// Executes INSERT/UPDATE/DELETE commands and returns the number
	/// of affected rows.
	/// </summary>
	/// <example>
	/// <code>
	/// // Update with parameters
	/// int rowsAffected = await _db.ExecuteAsync(
	///     "UPDATE users SET last_login_at = @Now WHERE id = @Id",
	///     new { Now = DateTimeOffset.UtcNow, Id = userId });
	/// 
	/// // Bulk update
	/// int updated = await _db.ExecuteAsync(
	///     "UPDATE products SET is_active = false "
	///     + "WHERE stock_quantity = 0");
	/// 
	/// // Delete with condition
	/// int deleted = await _db.ExecuteAsync(
	///     "DELETE FROM order_notes WHERE created_at &lt; @Cutoff",
	///     new { Cutoff = DateTime.UtcNow.AddYears(-1) });
	/// 
	/// // Sync version
	/// int affected = _db.Execute(
	///     "UPDATE users SET is_active = false WHERE id = @Id",
	///     new { Id = userId });
	/// </code>
	/// </example>
	public static class ExecuteExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.ExecuteReader"/>
	/// and <see cref="IDbService.ExecuteReaderAsync"/>.
	/// Returns a data reader for low-level, row-by-row processing.
	/// </summary>
	/// <example>
	/// <code>
	/// // Async reader
	/// await using var reader = await _db.ExecuteReaderAsync(
	///     "SELECT id, username, email FROM users "
	///     + "WHERE is_active = @IsActive",
	///     new { IsActive = true });
	/// 
	/// while (await reader.ReadAsync())
	/// {
	///     var id = reader.GetGuid(0);
	///     var username = reader.GetString(1);
	///     var email = reader.GetString(2);
	/// }
	/// 
	/// // Sync reader
	/// using var reader = _db.ExecuteReader(
	///     "SELECT name, price FROM products ORDER BY price");
	/// 
	/// while (reader.Read())
	/// {
	///     var name = reader.GetString(0);
	///     var price = reader.GetDecimal(1);
	/// }
	/// </code>
	/// </example>
	public static class ExecuteReaderExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.ExecuteScalar{T}"/>
	/// and <see cref="IDbService.ExecuteScalarAsync{T}"/>.
	/// Returns the first column of the first row from a query.
	/// </summary>
	/// <example>
	/// <code>
	/// // Count
	/// int count = await _db.ExecuteScalarAsync&lt;int&gt;(
	///     "SELECT COUNT(*) FROM users WHERE is_active = true");
	/// 
	/// // Sum with parameters
	/// decimal revenue = await _db.ExecuteScalarAsync&lt;decimal&gt;(
	///     "SELECT COALESCE(SUM(total_amount), 0) "
	///     + "FROM orders WHERE status = @Status",
	///     new { Status = 3 });
	/// 
	/// // Single string value
	/// string? email = await _db.ExecuteScalarAsync&lt;string&gt;(
	///     "SELECT email FROM users WHERE id = @Id",
	///     new { Id = userId });
	/// 
	/// // Check existence
	/// bool exists = await _db.ExecuteScalarAsync&lt;bool&gt;(
	///     "SELECT EXISTS(SELECT 1 FROM users WHERE email = @Email)",
	///     new { Email = "test@example.com" });
	/// 
	/// // Returns null when no results
	/// int? result = _db.ExecuteScalar&lt;int?&gt;(
	///     "SELECT quantity FROM products WHERE id = @Id",
	///     new { Id = Guid.NewGuid() });
	/// </code>
	/// </example>
	public static class ExecuteScalarExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.GetDataTable"/>
	/// and <see cref="IDbService.GetDataTableAsync"/>.
	/// Returns query results as a <see cref="System.Data.DataTable"/>.
	/// </summary>
	/// <example>
	/// <code>
	/// // Basic DataTable
	/// var dt = await _db.GetDataTableAsync(
	///     "SELECT name, price, quantity FROM products "
	///     + "WHERE is_active = true ORDER BY name");
	/// 
	/// foreach (System.Data.DataRow row in dt.Rows)
	/// {
	///     Console.WriteLine(
	///         $"{row["name"]}: ${row["price"]} "
	///         + $"({row["quantity"]} in stock)");
	/// }
	/// 
	/// // With parameters
	/// var report = await _db.GetDataTableAsync(
	///     """
	///     SELECT DATE(created_at) as sale_date,
	///            COUNT(*) as order_count,
	///            SUM(total_amount) as total_sales
	///     FROM orders
	///     WHERE created_at BETWEEN @Start AND @End
	///     GROUP BY DATE(created_at)
	///     """,
	///     new { Start = startDate, End = endDate });
	/// 
	/// // Sync version
	/// var dt = _db.GetDataTable(
	///     "SELECT * FROM categories ORDER BY sort_order");
	/// 
	/// // Handle NULL values
	/// foreach (System.Data.DataRow row in dt.Rows)
	/// {
	///     string desc = row["description"] == System.DBNull.Value
	///         ? "N/A"
	///         : row["description"].ToString()!;
	/// }
	/// </code>
	/// </example>
	public static class GetDataTableExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.Insert{T}(T)"/>,
	/// <see cref="IDbService.InsertAsync{T}(T)"/>,
	/// and the object overloads for inserting from anonymous objects.
	/// Returns the inserted entity with generated key values populated.
	/// </summary>
	/// <example>
	/// <code>
	/// // Insert entity - returns entity with generated Id
	/// var user = new User
	/// {
	///     Email = "john@example.com",
	///     Username = "john_doe",
	///     IsActive = true,
	///     CreatedAt = DateTime.UtcNow
	/// };
	/// var inserted = await _db.InsertAsync(user);
	/// Guid userId = inserted.Id; // auto-generated
	/// 
	/// // Sync version
	/// var inserted = _db.Insert(user);
	/// 
	/// // Insert with explicit key (you provide the ID)
	/// var log = new AuditLog
	/// {
	///     Id = Guid.NewGuid(),
	///     EntityType = "User",
	///     Action = "Created",
	///     Timestamp = DateTimeOffset.UtcNow
	/// };
	/// await _db.InsertAsync(log);
	/// 
	/// // Insert with composite key
	/// var userRole = new UserRole
	/// {
	///     UserId = userId,
	///     RoleId = adminRoleId,
	///     AssignedAt = DateTime.UtcNow
	/// };
	/// await _db.InsertAsync(userRole);
	/// 
	/// // Insert from anonymous object (partial properties)
	/// var newUser = await _db.InsertAsync&lt;User&gt;(new
	/// {
	///     Email = "jane@example.com",
	///     Username = "jane_doe",
	///     IsActive = true,
	///     CreatedAt = DateTime.UtcNow
	/// });
	/// // newUser.Id auto-generated, unmapped props use defaults
	/// 
	/// // Sync from anonymous object
	/// var newUser = _db.Insert&lt;User&gt;(new
	/// {
	///     Email = "bob@example.com",
	///     Username = "bob",
	///     CreatedAt = DateTime.UtcNow
	/// });
	/// 
	/// // Extra properties are silently ignored
	/// var u = await _db.InsertAsync&lt;User&gt;(new
	/// {
	///     Email = "test@example.com",
	///     NonExistentProp = "ignored",
	///     CreatedAt = DateTime.UtcNow
	/// });
	/// 
	/// // Type mismatches throw ArgumentException
	/// // await _db.InsertAsync&lt;User&gt;(new { Email = 123 });
	/// </code>
	/// </example>
	public static class InsertExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.Update{T}(T, string[])"/>,
	/// <see cref="IDbService.UpdateAsync{T}(T, string[])"/>,
	/// and the object overloads for partial updates from anonymous objects.
	/// </summary>
	/// <example>
	/// <code>
	/// // Update all writable properties
	/// var user = await _db.GetAsync&lt;User&gt;(userId);
	/// user.Username = "new_username";
	/// user.UpdatedAt = DateTime.UtcNow;
	/// bool updated = await _db.UpdateAsync(user);
	/// 
	/// // Update specific properties only (more efficient)
	/// user.Email = "new@example.com";
	/// user.UpdatedAt = DateTime.UtcNow;
	/// bool updated = await _db.UpdateAsync(
	///     user, ["Email", "UpdatedAt"]);
	/// 
	/// // Sync version
	/// bool updated = _db.Update(user);
	/// bool updated = _db.Update(user, ["Email"]);
	/// 
	/// // Returns false if entity not found
	/// var missing = new User { Id = Guid.NewGuid() };
	/// bool updated = await _db.UpdateAsync(missing); // false
	/// 
	/// // Partial update from anonymous object
	/// // Must include all key properties; only non-key properties
	/// // present in the object are updated
	/// bool updated = await _db.UpdateAsync&lt;User&gt;(new
	/// {
	///     Id = userId,
	///     Email = "updated@example.com",
	///     UpdatedAt = DateTime.UtcNow
	/// });
	/// // Only Email and UpdatedAt updated; others unchanged
	/// 
	/// // Sync from anonymous object
	/// bool updated = _db.Update&lt;User&gt;(new
	/// {
	///     Id = userId,
	///     Username = "new_name"
	/// });
	/// 
	/// // Key-only object returns false (nothing to update)
	/// bool updated = await _db.UpdateAsync&lt;User&gt;(
	///     new { Id = userId }); // false
	/// 
	/// // Missing key throws ArgumentException
	/// // _db.Update&lt;User&gt;(new { Email = "x" });
	/// 
	/// // Type mismatches throw ArgumentException
	/// // _db.Update&lt;User&gt;(new { Id = userId, Email = 123 });
	/// </code>
	/// </example>
	public static class UpdateExamples { }

	/// <summary>
	/// Examples for all Delete overloads:
	/// by entity, by single Guid key, by composite key dictionary,
	/// and by anonymous object.
	/// </summary>
	/// <example>
	/// <code>
	/// // Delete by entity reference
	/// var user = await _db.GetAsync&lt;User&gt;(userId);
	/// bool deleted = await _db.DeleteAsync(user);
	/// 
	/// // Delete by single Guid key (more efficient)
	/// bool deleted = await _db.DeleteAsync&lt;User&gt;(userId);
	/// 
	/// // Delete by composite key using dictionary
	/// var keys = new Dictionary&lt;string, Guid&gt;
	/// {
	///     ["UserId"] = userId,
	///     ["RoleId"] = roleId
	/// };
	/// bool deleted = await _db.DeleteAsync&lt;UserRole&gt;(keys);
	/// 
	/// // Delete by composite key using anonymous object
	/// bool deleted = await _db.DeleteAsync&lt;UserRole&gt;(
	///     new { UserId = userId, RoleId = roleId });
	/// 
	/// // Sync versions
	/// bool deleted = _db.Delete(user);
	/// bool deleted = _db.Delete&lt;User&gt;(userId);
	/// bool deleted = _db.Delete&lt;UserRole&gt;(keys);
	/// bool deleted = _db.Delete&lt;UserRole&gt;(
	///     new { UserId = userId, RoleId = roleId });
	/// 
	/// // Returns false if not found
	/// bool deleted = await _db.DeleteAsync&lt;User&gt;(
	///     Guid.NewGuid()); // false
	/// </code>
	/// </example>
	public static class DeleteExamples { }

	/// <summary>
	/// Examples for all Get overloads:
	/// by single Guid key, by composite key dictionary,
	/// and by anonymous object.
	/// </summary>
	/// <example>
	/// <code>
	/// // Get by single key
	/// var user = await _db.GetAsync&lt;User&gt;(userId);
	/// 
	/// // Sync version
	/// var user = _db.Get&lt;User&gt;(userId);
	/// 
	/// // Get by composite key using dictionary
	/// var keys = new Dictionary&lt;string, Guid&gt;
	/// {
	///     ["UserId"] = userId,
	///     ["RoleId"] = roleId
	/// };
	/// var userRole = await _db.GetAsync&lt;UserRole&gt;(keys);
	/// 
	/// // Get by composite key using anonymous object
	/// var userRole = await _db.GetAsync&lt;UserRole&gt;(
	///     new { UserId = userId, RoleId = roleId });
	/// 
	/// // Sync with anonymous object
	/// var userRole = _db.Get&lt;UserRole&gt;(
	///     new { UserId = userId, RoleId = roleId });
	/// 
	/// // Returns null if not found
	/// var user = await _db.GetAsync&lt;User&gt;(Guid.NewGuid());
	/// if (user is null)
	/// {
	///     // Handle not found
	/// }
	/// </code>
	/// </example>
	public static class GetExamples { }

	/// <summary>
	/// Examples for all GetList overloads:
	/// all entities, by Guid collection, by composite key dictionaries,
	/// and by anonymous object collection.
	/// </summary>
	/// <example>
	/// <code>
	/// // Get all entities
	/// var allUsers = await _db.GetListAsync&lt;User&gt;();
	/// 
	/// // Cacheable entities served from cache
	/// var categories = await _db.GetListAsync&lt;Category&gt;();
	/// 
	/// // Get multiple by IDs
	/// var ids = new List&lt;Guid&gt; { id1, id2, id3 };
	/// var users = await _db.GetListAsync&lt;User&gt;(ids);
	/// 
	/// // Get by composite keys using dictionaries
	/// var keysList = new List&lt;Dictionary&lt;string, Guid&gt;&gt;
	/// {
	///     new() { ["UserId"] = user1Id, ["RoleId"] = role1Id },
	///     new() { ["UserId"] = user2Id, ["RoleId"] = role2Id }
	/// };
	/// var roles = await _db.GetListAsync&lt;UserRole&gt;(keysList);
	/// 
	/// // Get by composite keys using anonymous objects
	/// var roles = await _db.GetListAsync&lt;UserRole&gt;(new[]
	/// {
	///     new { UserId = user1Id, RoleId = role1Id },
	///     new { UserId = user2Id, RoleId = role2Id }
	/// });
	/// 
	/// // Sync versions
	/// var allUsers = _db.GetList&lt;User&gt;();
	/// var users = _db.GetList&lt;User&gt;(ids);
	/// var roles = _db.GetList&lt;UserRole&gt;(keysList);
	/// var roles = _db.GetList&lt;UserRole&gt;(new[]
	/// {
	///     new { UserId = user1Id, RoleId = role1Id },
	///     new { UserId = user2Id, RoleId = role2Id }
	/// });
	/// 
	/// // Empty IDs list returns empty collection
	/// var empty = await _db.GetListAsync&lt;User&gt;(
	///     new List&lt;Guid&gt;());
	/// </code>
	/// </example>
	public static class GetListExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.CreateConnection"/>
	/// and <see cref="IDbService.CreateConnectionAsync"/>.
	/// Creates a raw database connection for advanced scenarios.
	/// </summary>
	/// <example>
	/// <code>
	/// // Async connection
	/// await using var conn = await _db.CreateConnectionAsync();
	/// // Use conn directly with Dapper or ADO.NET
	/// 
	/// // Sync connection
	/// using var conn = _db.CreateConnection();
	/// </code>
	/// </example>
	public static class ConnectionExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.CreateTransactionScope"/>,
	/// <see cref="IDbService.CreateTransactionScopeAsync()"/>,
	/// <see cref="IDbService.CreateTransactionScopeAsync(long?)"/>,
	/// and <see cref="IDbService.CreateTransactionScopeAsync(string)"/>.
	/// Provides nested transaction support with proper PostgreSQL
	/// savepoints.
	/// </summary>
	/// <example>
	/// <code>
	/// // Simple async transaction
	/// await using var scope =
	///     await _db.CreateTransactionScopeAsync();
	/// await _db.InsertAsync(user);
	/// await _db.InsertAsync(order);
	/// await scope.CompleteAsync(); // Commit
	/// // If not completed, rolled back on dispose
	/// 
	/// // Nested transactions (automatic savepoints)
	/// await using var outer =
	///     await _db.CreateTransactionScopeAsync();
	/// await _db.InsertAsync(user);
	/// 
	/// await using (var inner =
	///     await _db.CreateTransactionScopeAsync())
	/// {
	///     await _db.InsertAsync(order);
	///     await inner.CompleteAsync();
	/// }
	/// await outer.CompleteAsync(); // Commits both
	/// 
	/// // Transaction with numeric advisory lock
	/// await using var scope =
	///     await _db.CreateTransactionScopeAsync(lockKey: 12345L);
	/// await _db.UpdateAsync(inventory);
	/// await scope.CompleteAsync();
	/// 
	/// // Transaction with string advisory lock (auto-hashed)
	/// await using var scope =
	///     await _db.CreateTransactionScopeAsync("inventory-update");
	/// await _db.UpdateAsync(inventory);
	/// await scope.CompleteAsync();
	/// 
	/// // Sync transaction with optional lock
	/// using var scope = _db.CreateTransactionScope(lockKey: null);
	/// _db.Insert(user);
	/// scope.Complete();
	/// </code>
	/// </example>
	public static class TransactionExamples { }

	/// <summary>
	/// Examples for <see cref="IDbService.CreateAdvisoryLockScope"/>
	/// and <see cref="IDbService.CreateAdvisoryLockScopeAsync"/>.
	/// Provides distributed locking without a transaction.
	/// </summary>
	/// <example>
	/// <code>
	/// // Async advisory lock (no transaction)
	/// await using var lockScope =
	///     await _db.CreateAdvisoryLockScopeAsync(lockKey: 12345L);
	/// 
	/// var inventory = await _db.GetAsync&lt;Inventory&gt;(productId);
	/// inventory.Quantity -= orderQuantity;
	/// await _db.UpdateAsync(inventory);
	/// 
	/// await lockScope.CompleteAsync(); // Release lock
	/// 
	/// // Sync advisory lock
	/// using var lockScope = _db.CreateAdvisoryLockScope(
	///     lockKey: 12345L);
	/// // ... operations under lock ...
	/// lockScope.Complete();
	/// 
	/// // Strategy: entity-specific lock key
	/// long lockKey = HashCode.Combine("Order", orderId);
	/// await using var scope =
	///     await _db.CreateAdvisoryLockScopeAsync(lockKey);
	/// </code>
	/// </example>
	public static class AdvisoryLockExamples { }

	/// <summary>
	/// Database migration examples.
	/// </summary>
	/// <example>
	/// Creating migrations:
	/// <code>
	/// [DbMigration("1.0.0.0")]
	/// public class InitialSchema : DbMigration
	/// {
	///     public override Task&lt;string&gt; GenerateSqlAsync(
	///         IServiceProvider serviceProvider)
	///     {
	///         return Task.FromResult("""
	///             CREATE TABLE users (
	///                 id UUID PRIMARY KEY
	///                     DEFAULT gen_random_uuid(),
	///                 email VARCHAR(255) NOT NULL UNIQUE,
	///                 name VARCHAR(100) NOT NULL,
	///                 created_at TIMESTAMP NOT NULL
	///                     DEFAULT CURRENT_TIMESTAMP
	///             );
	///             CREATE INDEX idx_users_email ON users(email);
	///             """);
	///     }
	/// }
	/// 
	/// // Embedded SQL resource (no override needed)
	/// [DbMigration("1.0.1.0")]
	/// public class AddUserProfile : DbMigration { }
	/// // Requires: AddUserProfile.Script.sql as embedded resource
	/// 
	/// // Post-migration .NET code
	/// [DbMigration("1.0.2.0")]
	/// public class SeedData : DbMigration
	/// {
	///     public override Task&lt;string&gt; GenerateSqlAsync(
	///         IServiceProvider serviceProvider)
	///     {
	///         return Task.FromResult(
	///             "CREATE TABLE settings "
	///             + "(key TEXT PRIMARY KEY, value TEXT);");
	///     }
	///     
	///     public override async Task PostMigrateAsync(
	///         IServiceProvider serviceProvider)
	///     {
	///         var db = serviceProvider
	///             .GetRequiredService&lt;IDbService&gt;();
	///         await db.ExecuteAsync(
	///             "INSERT INTO settings VALUES "
	///             + "('app_version', '1.0.0')");
	///     }
	/// }
	/// </code>
	/// </example>
	public static class DatabaseMigrations { }
}