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
/// For complete documentation: https://github.com/WebVella/WebVella.Database/blob/main/docs/index.md
/// </remarks>
public static class WebVellaDatabaseExamples
{
	/// <summary>
	/// Service registration examples for dependency injection.
	/// </summary>
	/// <example>
	/// Basic registration:
	/// <code>
	/// builder.Services.AddWebVellaDatabase("Host=localhost;Database=mydb;Username=user;Password=pass");
	/// </code>
	/// 
	/// With entity caching:
	/// <code>
	/// builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);
	/// </code>
	/// 
	/// With factory pattern:
	/// <code>
	/// builder.Services.AddWebVellaDatabase(sp => sp.GetRequiredService&lt;IConfiguration&gt;().GetConnectionString("DefaultConnection")!);
	/// </code>
	/// 
	/// With Row Level Security (RLS):
	/// <code>
	/// public class HttpRlsContextProvider : IRlsContextProvider
	/// {
	///     private readonly IHttpContextAccessor _httpContextAccessor;
	///     public HttpRlsContextProvider(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;
	///     public Guid? TenantId => GetClaimAsGuid("tenant_id");
	///     public Guid? UserId => GetClaimAsGuid("sub");
	///     public IReadOnlyDictionary&lt;string, string&gt; CustomClaims => new Dictionary&lt;string, string&gt; { ["role"] = GetClaim("role") ?? "user" };
	///     private Guid? GetClaimAsGuid(string type) => Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value, out var g) ? g : null;
	///     private string? GetClaim(string type) => _httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
	/// }
	/// 
	/// builder.Services.AddWebVellaDatabaseWithRls&lt;HttpRlsContextProvider&gt;(connectionString);
	/// </code>
	/// 
	/// With migrations:
	/// <code>
	/// builder.Services.AddWebVellaDatabase(connectionString);
	/// builder.Services.AddWebVellaDatabaseMigrations();
	/// 
	/// // Run migrations at startup
	/// using var scope = app.Services.CreateScope();
	/// var migrationService = scope.ServiceProvider.GetRequiredService&lt;IDbMigrationService&gt;();
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
	///     [Key]  // Auto-generated UUID
	///     public Guid Id { get; set; }
	///     
	///     public string Name { get; set; } = string.Empty;
	///     public string Email { get; set; } = string.Empty;
	///     
	///     [JsonColumn]  // Serialized as JSON
	///     public UserSettings? Settings { get; set; }
	///     
	///     [External]  // Excluded from INSERT/UPDATE
	///     public List&lt;Order&gt;? Orders { get; set; }
	/// }
	/// 
	/// public class UserSettings
	/// {
	///     public string Theme { get; set; } = "light";
	///     public bool NotificationsEnabled { get; set; } = true;
	/// }
	/// </code>
	/// 
	/// Composite key entity:
	/// <code>
	/// [Table("user_roles")]
	/// public class UserRole
	/// {
	///     [ExplicitKey]  // User-provided key
	///     public Guid UserId { get; set; }
	///     
	///     [ExplicitKey]
	///     public Guid RoleId { get; set; }
	///     
	///     public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
	/// }
	/// </code>
	/// </example>
	public static class EntityDefinitions { }

	/// <summary>
	/// CRUD operations examples.
	/// </summary>
	/// <example>
	/// Basic service with CRUD operations:
	/// <code>
	/// public class UserService
	/// {
	///     private readonly IDbService _db;
	///     
	///     public UserService(IDbService db) => _db = db;
	///     
	///     // Insert - returns entity with generated Id
	///     public async Task&lt;User&gt; CreateUserAsync(User user) => await _db.InsertAsync(user);
	///     
	///     // Get by ID
	///     public async Task&lt;User?&gt; GetUserAsync(Guid id) => await _db.GetAsync&lt;User&gt;(id);
	///     
	///     // Get by composite key
	///     public async Task&lt;UserRole?&gt; GetUserRoleAsync(Guid userId, Guid roleId) => 
	///         await _db.GetAsync&lt;UserRole&gt;(new { UserId = userId, RoleId = roleId });
	///     
	///     // Get all
	///     public async Task&lt;IEnumerable&lt;User&gt;&gt; GetAllUsersAsync() => await _db.GetListAsync&lt;User&gt;();
	///     
	///     // Get specific IDs
	///     public async Task&lt;IEnumerable&lt;User&gt;&gt; GetUsersAsync(IEnumerable&lt;Guid&gt; ids) => await _db.GetListAsync&lt;User&gt;(ids);
	///     
	///     // Update all properties
	///     public async Task&lt;bool&gt; UpdateUserAsync(User user) => await _db.UpdateAsync(user);
	///     
	///     // Update specific properties only
	///     public async Task&lt;bool&gt; UpdateUserEmailAsync(User user) => await _db.UpdateAsync(user, ["Email"]);
	///     
	///     // Delete by ID
	///     public async Task&lt;bool&gt; DeleteUserAsync(Guid id) => await _db.DeleteAsync&lt;User&gt;(id);
	///     
	///     // Delete by composite key
	///     public async Task&lt;bool&gt; DeleteUserRoleAsync(Guid userId, Guid roleId) => 
	///         await _db.DeleteAsync&lt;UserRole&gt;(new { UserId = userId, RoleId = roleId });
	/// }
	/// </code>
	/// </example>
	public static class CrudOperations { }

	/// <summary>
	/// Custom query examples.
	/// </summary>
	/// <example>
	/// Custom queries with parameters:
	/// <code>
	/// // Query with parameters
	/// var activeUsers = await _db.QueryAsync&lt;User&gt;(
	///     "SELECT * FROM users WHERE is_active = @IsActive AND created_at > @Since",
	///     new { IsActive = true, Since = DateTime.UtcNow.AddDays(-30) });
	/// 
	/// // Execute commands
	/// var rowsAffected = await _db.ExecuteAsync(
	///     "UPDATE users SET last_login = @Now WHERE id = @Id",
	///     new { Now = DateTime.UtcNow, Id = userId });
	/// 
	/// // Execute scalar
	/// var userCount = await _db.ExecuteScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM users WHERE is_active = true");
	/// 
	/// // Execute reader
	/// await using var reader = await _db.ExecuteReaderAsync("SELECT id, name FROM users");
	/// while (await reader.ReadAsync())
	/// {
	///     var id = reader.GetGuid("id");
	///     var name = reader.GetString("name");
	/// }
	/// </code>
	/// </example>
	public static class CustomQueries { }

	/// <summary>
	/// Transaction and advisory lock examples.
	/// </summary>
	/// <example>
	/// Transaction scopes:
	/// <code>
	/// // Simple transaction
	/// await using var scope = await _db.CreateTransactionScopeAsync();
	/// await _db.InsertAsync(new User { Name = "John" });
	/// await _db.InsertAsync(new Order { UserId = userId });
	/// await scope.CompleteAsync(); // Commit
	/// 
	/// // Nested transactions (uses savepoints)
	/// await using var outerScope = await _db.CreateTransactionScopeAsync();
	/// {
	///     await _db.InsertAsync(user);
	///     
	///     await using var innerScope = await _db.CreateTransactionScopeAsync();
	///     {
	///         await _db.InsertAsync(order);
	///         await innerScope.CompleteAsync(); // Creates savepoint
	///     }
	///     
	///     await outerScope.CompleteAsync(); // Commits everything
	/// }
	/// 
	/// // Transaction with advisory lock
	/// await using var scope = await _db.CreateTransactionScopeAsync(lockKey: "inventory-update");
	/// // Exclusive lock held - safe for concurrent operations
	/// await _db.UpdateAsync(inventory);
	/// await scope.CompleteAsync();
	/// 
	/// // Advisory lock without transaction
	/// await using var lockScope = await _db.CreateAdvisoryLockScopeAsync(lockKey: 12345L);
	/// var user = await _db.GetAsync&lt;User&gt;(userId);
	/// user.Balance += 100;
	/// await _db.UpdateAsync(user);
	/// await lockScope.CompleteAsync();
	/// </code>
	/// </example>
	public static class TransactionsAndLocks { }

	/// <summary>
	/// Database migration examples.
	/// </summary>
	/// <example>
	/// Creating migrations:
	/// <code>
	/// [DbMigration("1.0.0.0")]
	/// public class InitialSchema : DbMigration
	/// {
	///     public override Task&lt;string&gt; GenerateSqlAsync(IServiceProvider serviceProvider)
	///     {
	///         return Task.FromResult("""
	///             CREATE TABLE users (
	///                 id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
	///                 email VARCHAR(255) NOT NULL UNIQUE,
	///                 name VARCHAR(100) NOT NULL,
	///                 created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
	///             );
	///             
	///             CREATE INDEX idx_users_email ON users(email);
	///             """);
	///     }
	/// }
	/// 
	/// [DbMigration("1.0.1.0")]
	/// public class AddUserProfile : DbMigration { }
	/// // Requires: AddUserProfile.Script.sql as embedded resource
	/// 
	/// [DbMigration("1.0.2.0")]
	/// public class SeedData : DbMigration
	/// {
	///     public override Task&lt;string&gt; GenerateSqlAsync(IServiceProvider serviceProvider)
	///     {
	///         return Task.FromResult("CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT);");
	///     }
	///     
	///     public override async Task PostMigrateAsync(IServiceProvider serviceProvider)
	///     {
	///         var db = serviceProvider.GetRequiredService&lt;IDbService&gt;();
	///         await db.ExecuteAsync("INSERT INTO settings VALUES ('app_version', '1.0.0')");
	///     }
	/// }
	/// </code>
	/// </example>
	public static class DatabaseMigrations { }
}