[![Project Homepage](https://img.shields.io/badge/Homepage-blue?style=for-the-badge)](https://github.com/WebVella/WebVella.Database)
[![Dotnet](https://img.shields.io/badge/platform-.NET-blue?style=for-the-badge)](https://github.com/WebVella/WebVella.Database)
[![GitHub Repo stars](https://img.shields.io/github/stars/WebVella/WebVella.Database?style=for-the-badge)](https://github.com/WebVella/WebVella.Database/stargazers)
[![Nuget version](https://img.shields.io/nuget/v/WebVella.Database?style=for-the-badge)](https://www.nuget.org/packages/WebVella.Database/)
[![Nuget download](https://img.shields.io/nuget/dt/WebVella.Database?style=for-the-badge)](https://www.nuget.org/packages/WebVella.Database/)
[![License](https://img.shields.io/badge/LICENSE%20details-Community%20MIT%20and%20professional-green?style=for-the-badge)](https://github.com/WebVella/WebVella.Database/blob/main/LICENSE/)

Checkout our other projects:  
[WebVella ERP](https://github.com/WebVella/WebVella-ERP)  
[Data collaboration - Tefter.bg](https://github.com/WebVella/WebVella.Tefter)  
[Document template generation](https://github.com/WebVella/WebVella.DocumentTemplates)  


## What is WebVella.Database?
A lightweight, high-performance Postgres data access library built on Dapper. It simplifies data object mapping, migrations and complex database workflows by providing first-class support for nested transactions and effortless advisory lock management.

## How to get it
You can either clone this repository or get the [Nuget package](https://www.nuget.org/packages/WebVella.Database/)

## Please help by giving a star
GitHub stars guide developers toward great tools. If you find this project valuable, please give it a star – it helps the community and takes just a second!⭐


## Features

- **Dapper-based CRUD operations** - Simple Insert, Update, Delete, Get, and Query methods
- **Nested transaction support** - Create transaction scopes that properly handle nesting
- **PostgreSQL advisory locks** - Easy-to-use advisory lock scopes for distributed locking
- **Row Level Security (RLS)** - Built-in support for PostgreSQL RLS with automatic session context
- **Entity caching** - Optional in-memory caching with automatic invalidation (RLS-aware)
- **JSON column support** - Automatic serialization/deserialization of JSON columns
- **Attribute-based mapping** - Use attributes like `[Table]`, `[Key]`, `[JsonColumn]`, and more
- **Database migrations** - Version-controlled schema migrations with rollback support

## Setup

### Basic Registration

```csharp
using WebVella.Database;

var builder = WebApplication.CreateBuilder(args);

// Add WebVella.Database services
builder.Services.AddWebVellaDatabase("Host=localhost;Database=mydb;Username=user;Password=pass");
```

### With Entity Caching

```csharp
builder.Services.AddWebVellaDatabase(
    "Host=localhost;Database=mydb;Username=user;Password=pass",
    enableCaching: true);
```

### With Factory Pattern

```csharp
builder.Services.AddWebVellaDatabase(
    sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!);
```

### With Row Level Security (RLS)

For multi-tenant applications, enable RLS to automatically set PostgreSQL session variables:

```csharp
// Implement IRlsContextProvider to provide tenant/user context
public class HttpRlsContextProvider : IRlsContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRlsContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId => GetClaimAsGuid("tenant_id");
    public Guid? UserId => GetClaimAsGuid("sub");
    public IReadOnlyDictionary<string, string> CustomClaims => new Dictionary<string, string>
    {
        ["role"] = GetClaim("role") ?? "user"
    };

    private Guid? GetClaimAsGuid(string type) =>
        Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value, out var g) ? g : null;
    private string? GetClaim(string type) =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
}

// Register with RLS support
builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(connectionString);

// Or with caching and custom options
builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(
    connectionString,
    enableCaching: true,
    rlsOptions: new RlsOptions { Prefix = "app" });
```

Each connection automatically sets session variables that PostgreSQL RLS policies can use:
```sql
-- Create RLS policy using the session variable
CREATE POLICY tenant_isolation ON orders
    USING (tenant_id = current_setting('app.tenant_id', true)::uuid);
```

## Usage Examples

### Define an Entity

```csharp
using WebVella.Database;

[Table("users")]
[Cacheable(DurationSeconds = 600)]
public class User
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [JsonColumn]
    public UserSettings? Settings { get; set; }

    [External]
    public List<Order>? Orders { get; set; }
}

public class UserSettings
{
    public string Theme { get; set; } = "light";
    public bool NotificationsEnabled { get; set; } = true;
}
```

### Basic CRUD Operations

```csharp
public class UserService
{
    private readonly IDbService _db;

    public UserService(IDbService db)
    {
        _db = db;
    }

    // Insert - returns the entity with generated Id populated
    public async Task<User> CreateUserAsync(User user)
    {
        return await _db.InsertAsync(user);
    }

    // Get by ID
    public async Task<User?> GetUserAsync(Guid id)
    {
        return await _db.GetAsync<User>(id);
    }

    // Get by composite key using anonymous object
    public async Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId)
    {
        return await _db.GetAsync<UserRole>(new { UserId = userId, RoleId = roleId });
    }

    // Get all
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _db.GetListAsync<User>();
    }

    // Update
    public async Task<bool> UpdateUserAsync(User user)
    {
        return await _db.UpdateAsync(user);
    }

    // Update specific properties only
    public async Task<bool> UpdateUserEmailAsync(User user)
    {
        return await _db.UpdateAsync(user, ["Email"]);
    }

    // Delete
    public async Task<bool> DeleteUserAsync(Guid id)
    {
        return await _db.DeleteAsync<User>(id);
    }

    // Delete by composite key using anonymous object
    public async Task<bool> DeleteUserRoleAsync(Guid userId, Guid roleId)
    {
        return await _db.DeleteAsync<UserRole>(new { UserId = userId, RoleId = roleId });
    }
}
```

### Custom Queries

```csharp
// Query with parameters
var activeUsers = await _db.QueryAsync<User>(
    "SELECT * FROM users WHERE is_active = @IsActive",
    new { IsActive = true });

// Execute commands
var rowsAffected = await _db.ExecuteAsync(
    "UPDATE users SET last_login = @Now WHERE id = @Id",
    new { Now = DateTime.UtcNow, Id = userId });
```

### Transaction Scope

```csharp
// Simple transaction
await using var scope = await _db.CreateTransactionScopeAsync();

await _db.InsertAsync(new User { Name = "John" });
await _db.InsertAsync(new Order { UserId = userId, Amount = 100 });

await scope.CompleteAsync(); // Commit transaction

// Nested transactions are automatically handled
await using var outerScope = await _db.CreateTransactionScopeAsync();
{
    await _db.InsertAsync(user);

    await using var innerScope = await _db.CreateTransactionScopeAsync();
    {
        await _db.InsertAsync(order);
        await innerScope.CompleteAsync();
    }

    await outerScope.CompleteAsync();
}
```

### Transaction with Advisory Lock

```csharp
// Acquire advisory lock with transaction
await using var scope = await _db.CreateTransactionScopeAsync(lockKey: 12345L);

// Or use a string key (automatically hashed)
await using var scope = await _db.CreateTransactionScopeAsync(lockKey: "user-update-lock");

// Perform operations with exclusive lock
await _db.UpdateAsync(user);

await scope.CompleteAsync();
```

### Advisory Lock Scope (without transaction)

```csharp
// Acquire advisory lock without transaction
await using var lockScope = await _db.CreateAdvisoryLockScopeAsync(lockKey: 12345L);

// Perform operations with exclusive lock
var user = await _db.GetAsync<User>(userId);
user.Balance += 100;
await _db.UpdateAsync(user);

await lockScope.CompleteAsync();
```

## Database Migrations

WebVella.Database provides a simple yet powerful migration system for managing database schema changes. Migrations automatically bypass RLS to ensure unrestricted schema access.

### Migration Setup

```csharp
// Register migration services
builder.Services.AddWebVellaDatabase(connectionString);
builder.Services.AddWebVellaDatabaseMigrations();

// Or with custom options
builder.Services.AddWebVellaDatabaseMigrations(options =>
{
    options.VersionTableName = "_my_db_version";
});
```

### Creating Migrations

Create a class that inherits from `DbMigration` and apply the `[DbMigration]` attribute:

```csharp
using WebVella.Database.Migrations;

[DbMigration("1.0.0.0")]
public class InitialSchema : DbMigration
{
    public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
    {
        return Task.FromResult("""
            CREATE TABLE users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);
    }
}
```

### Using Embedded SQL Files

For larger migrations, use embedded SQL resource files:

```csharp
[DbMigration("1.0.1.0")]
public class AddUserProfile : DbMigration { }
// Requires: AddUserProfile.Script.sql as embedded resource in same namespace
```

### Post-Migration Logic

Execute custom code after SQL migration:

```csharp
[DbMigration("1.0.2.0")]
public class SeedData : DbMigration
{
    public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
    {
        return Task.FromResult("CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT);");
    }

    public override async Task PostMigrateAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<IDbService>();
        await db.ExecuteAsync("INSERT INTO settings VALUES ('app_version', '1.0.0')");
    }
}
```

### Running Migrations

```csharp
// In Program.cs or startup
using var scope = app.Services.CreateScope();
var migrationService = scope.ServiceProvider.GetRequiredService<IDbMigrationService>();
await migrationService.ExecutePendingMigrationsAsync();
```

## Entity Attributes

| Attribute | Description |
|-----------|-------------|
| `[Table("name")]` | Specifies the database table name |
| `[Key]` | Marks a property as auto-generated primary key (UUID) |
| `[ExplicitKey]` | Marks a property as explicit primary key (not auto-generated) |
| `[External]` | Excludes property from INSERT and UPDATE operations |
| `[Write(false)]` | Controls whether a property is written to the database |
| `[JsonColumn]` | Property is serialized/deserialized as JSON |
| `[Cacheable]` | Enables entity caching with automatic invalidation |
| `[MultiQuery]` | Marks a class as container for multiple result sets |
| `[ResultSet(index)]` | Maps property to a result set index in multi-query |

## Multi-Query Support

### QueryMultiple with Container Class

Use `QueryMultiple<T>` with a container class to map multiple result sets:

```csharp
[MultiQuery]
public class UserDashboard
{
    [ResultSet(0)]
    public User? Profile { get; set; }

    [ResultSet(1)]
    public List<Order> RecentOrders { get; set; } = [];

    [ResultSet(2)]
    public List<Notification> Alerts { get; set; } = [];
}

var sql = @"
    SELECT * FROM users WHERE id = @UserId;
    SELECT * FROM orders WHERE user_id = @UserId ORDER BY created_at DESC LIMIT 10;
    SELECT * FROM notifications WHERE user_id = @UserId AND is_read = false;
";

var dashboard = await _db.QueryMultipleAsync<UserDashboard>(sql, new { UserId = userId });
```

### QueryMultipleList with Parent-Child Mapping

Use `QueryMultipleList<T>` to fetch a list of parent entities with child collections automatically mapped:

```csharp
public class Order
{
    [Key]
    public Guid Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    [External]
    [ResultSet(1, ForeignKey = "OrderId")]
    public List<OrderLine> Lines { get; set; } = [];

    [External]
    [ResultSet(2, ForeignKey = "OrderId")]
    public List<OrderNote> Notes { get; set; } = [];
}

public class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }  // Foreign key to Order.Id
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class OrderNote
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }  // Foreign key to Order.Id
    public string Text { get; set; } = string.Empty;
}

var sql = @"
    SELECT * FROM orders WHERE customer_id = @CustomerId;
    SELECT ol.* FROM order_lines ol 
        JOIN orders o ON ol.order_id = o.id WHERE o.customer_id = @CustomerId;
    SELECT n.* FROM order_notes n 
        JOIN orders o ON n.order_id = o.id WHERE o.customer_id = @CustomerId;
";

// Each order will have its Lines and Notes collections populated
var orders = await _db.QueryMultipleListAsync<Order>(sql, new { CustomerId = customerId });
```

The `[ResultSet]` attribute supports:
- `Index` - The result set index (0-based, first result set is parent entities)
- `ForeignKey` - The property name in child entity that references parent's key
- `ParentKey` - The parent's key property name (defaults to "Id")

### QueryWithJoin for Single Query Mapping

Use `QueryWithJoin` when you want to use a single SQL query with JOINs instead of multiple result sets:

```csharp
// Single child collection
var sql = @"
    SELECT o.*, l.*
    FROM orders o
    LEFT JOIN order_lines l ON l.order_id = o.id
    WHERE o.customer_id = @CustomerId
    ORDER BY o.id, l.id
";

var orders = await _db.QueryWithJoinAsync<Order, OrderLine>(
    sql,
    parent => parent.Lines,           // Child collection selector
    parent => parent.Id,              // Parent key selector
    child => child.Id,                // Child primary key selector (for deduplication)
    splitOn: "Id",                    // Column to split results on
    parameters: new { CustomerId = customerId });
```

```csharp
// Two child collections (handles Cartesian product deduplication)
var sql = @"
    SELECT o.*, l.*, n.*
    FROM orders o
    LEFT JOIN order_lines l ON l.order_id = o.id
    LEFT JOIN order_notes n ON n.order_id = o.id
    ORDER BY o.id
";

var orders = await _db.QueryWithJoinAsync<Order, OrderLine, OrderNote>(
    sql,
    parent => parent.Lines,
    parent => parent.Notes,
    parent => parent.Id,
    child1 => child1.Id,              // Child1 primary key (for deduplication)
    child2 => child2.Id,              // Child2 primary key (for deduplication)
    splitOn: "Id,Id");
```

**Note:** The `splitOn` parameter tells Dapper where to split the columns between entity types. For multiple children, use comma-separated column names. The child key selectors should return each child's unique identifier (primary key) for proper deduplication when JOINs produce Cartesian products.

## Documentation
[View complete documentation](https://github.com/WebVella/WebVella.Database/blob/main/docs/index.md)

## License
[![Library license details](https://img.shields.io/badge/%F0%9F%93%9C%0A%20read-license%20details-blue?style=for-the-badge)](https://github.com/WebVella/WebVella.Database/blob/main/LICENSE/)
