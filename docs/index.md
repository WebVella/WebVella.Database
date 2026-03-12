# WebVella.Database - Complete Documentation

A lightweight, high-performance Postgres data access library built on Dapper. This document provides comprehensive documentation for all features and methods.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Entity Attributes](#entity-attributes)
3. [Sample Database Schema](#sample-database-schema)
4. [Sample Entity Models](#sample-entity-models)
5. [Basic CRUD Operations](#basic-crud-operations)
6. [Query Methods](#query-methods)
7. [Multi-Query Methods](#multi-query-methods)
8. [QueryWithJoin Methods](#querywith-join-methods)
9. [Transaction Management](#transaction-management)
10. [Advisory Locks](#advisory-locks)
11. [Caching](#caching)
12. [Best Practices](#best-practices)

---

## Getting Started

### Installation

```bash
dotnet add package WebVella.Database
```

### Service Registration

```csharp
// Basic registration
builder.Services.AddWebVellaDatabase("Host=localhost;Database=mydb;Username=user;Password=pass");

// With caching enabled
builder.Services.AddWebVellaDatabase(
    "Host=localhost;Database=mydb;Username=user;Password=pass",
    enableCaching: true);

// With factory pattern
builder.Services.AddWebVellaDatabase(
    sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")!);
```

### Dependency Injection

```csharp
public class MyService
{
    private readonly IDbService _db;

    public MyService(IDbService db)
    {
        _db = db;
    }
}
```

---

## Entity Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[Table("name")]` | Class | Specifies the database table name |
| `[Key]` | Property | Marks property as auto-generated primary key (UUID) |
| `[ExplicitKey]` | Property | Marks property as explicit primary key (user-provided) |
| `[External]` | Property | Excludes property from INSERT/UPDATE/SELECT operations |
| `[Write(false)]` | Property | Prevents property from being written to database |
| `[JsonColumn]` | Property | Property is serialized/deserialized as JSON |
| `[Cacheable]` | Class | Enables entity caching with automatic invalidation |
| `[MultiQuery]` | Class | Marks class as container for multiple result sets |
| `[ResultSet(index)]` | Property | Maps property to result set index in multi-query |

---

## Sample Database Schema

```sql
-- Users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    username VARCHAR(100) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    role INTEGER NOT NULL DEFAULT 0,
    settings JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP,
    last_login_at TIMESTAMPTZ
);

-- Orders table
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    order_number VARCHAR(50) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    total_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    shipping_address JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP
);

-- Order lines table
CREATE TABLE order_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID NOT NULL,
    product_name VARCHAR(255) NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    unit_price DECIMAL(18, 2) NOT NULL,
    discount_percent DECIMAL(5, 2) DEFAULT 0
);

-- Order notes table
CREATE TABLE order_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    author_id UUID REFERENCES users(id),
    text TEXT NOT NULL,
    is_internal BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Products table (with caching)
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sku VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    price DECIMAL(18, 2) NOT NULL,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    category_id UUID,
    metadata JSONB,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Categories table (with caching)
CREATE TABLE categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES categories(id),
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- User roles junction table (composite key)
CREATE TABLE user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL,
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by UUID REFERENCES users(id),
    PRIMARY KEY (user_id, role_id)
);

-- Audit log table (explicit key - application provides ID)
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY,
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,
    old_values JSONB,
    new_values JSONB,
    user_id UUID,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

---

## Sample Entity Models

### Basic Entity with Auto-Generated Key

```csharp
using WebVella.Database;

public enum UserRole
{
    User = 0,
    Moderator = 1,
    Admin = 2
}

public class UserSettings
{
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public bool EmailNotifications { get; set; } = true;
    public Dictionary<string, object> Preferences { get; set; } = [];
}

[Table("users")]
public class User
{
    [Key]
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public UserRole Role { get; set; } = UserRole.User;

    [JsonColumn]
    public UserSettings? Settings { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    // Computed property - not written to database
    [Write(false)]
    public string DisplayName => $"{Username} ({Email})";

    // External property - populated separately
    [External]
    public List<Order>? Orders { get; set; }
}
```

### Entity with Explicit Key

```csharp
[Table("audit_logs")]
public class AuditLog
{
    // Application provides the ID value
    [ExplicitKey]
    public Guid Id { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    [JsonColumn]
    public Dictionary<string, object>? OldValues { get; set; }

    [JsonColumn]
    public Dictionary<string, object>? NewValues { get; set; }

    public Guid? UserId { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
```

### Entity with Composite Key

```csharp
[Table("user_roles")]
public class UserRole
{
    [Key]
    public Guid UserId { get; set; }

    [Key]
    public Guid RoleId { get; set; }

    public DateTime AssignedAt { get; set; }

    public Guid? AssignedBy { get; set; }
}
```

### Cacheable Entity

```csharp
[Table("categories")]
[Cacheable(DurationSeconds = 600, SlidingExpiration = true)]
public class Category
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

[Table("products")]
[Cacheable(DurationSeconds = 300)]
public class Product
{
    [Key]
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? CategoryId { get; set; }

    [JsonColumn]
    public ProductMetadata? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    // External - category loaded separately
    [External]
    public Category? Category { get; set; }
}

public class ProductMetadata
{
    public string? Brand { get; set; }
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Attributes { get; set; } = [];
}
```

### Entity with Child Collections (for QueryMultipleList)

```csharp
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public class ShippingAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

[Table("orders")]
public class Order
{
    [Key]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    [JsonColumn]
    public ShippingAddress? ShippingAddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Child collections for QueryMultipleList
    [External]
    [ResultSet(1, ForeignKey = "OrderId")]
    public List<OrderLine> Lines { get; set; } = [];

    [External]
    [ResultSet(2, ForeignKey = "OrderId")]
    public List<OrderNote> Notes { get; set; } = [];

    // Parent reference - loaded separately
    [External]
    public User? User { get; set; }
}

[Table("order_lines")]
public class OrderLine
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    // Computed property
    [Write(false)]
    public decimal LineTotal => Quantity * UnitPrice * (1 - DiscountPercent / 100);
}

[Table("order_notes")]
public class OrderNote
{
    [Key]
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid? AuthorId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

### MultiQuery Container Class

```csharp
[MultiQuery]
public class UserDashboard
{
    [ResultSet(0)]
    public User? Profile { get; set; }

    [ResultSet(1)]
    public List<Order> RecentOrders { get; set; } = [];

    [ResultSet(2)]
    public DashboardStats? Stats { get; set; }
}

public class DashboardStats
{
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public int PendingOrders { get; set; }
}

[MultiQuery]
public class AdminDashboard
{
    [ResultSet(0)]
    public List<User> RecentUsers { get; set; } = [];

    [ResultSet(1)]
    public List<Order> RecentOrders { get; set; } = [];

    [ResultSet(2)]
    public List<Product> LowStockProducts { get; set; } = [];

    [ResultSet(3)]
    public SystemStats? Stats { get; set; }
}

public class SystemStats
{
    public int TotalUsers { get; set; }
    public int TotalOrders { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalRevenue { get; set; }
}
```

---

## Basic CRUD Operations

### Insert

```csharp
// Insert with auto-generated key
var user = new User
{
    Email = "john@example.com",
    Username = "john_doe",
    PasswordHash = "hashed_password",
    IsActive = true,
    Role = UserRole.User,
    Settings = new UserSettings
    {
        Theme = "dark",
        EmailNotifications = true
    },
    CreatedAt = DateTime.UtcNow
};

// Sync version - returns the entity with generated Id populated
var insertedUser = _db.Insert(user);
Guid userId = insertedUser.Id;

// Async version - returns the entity with generated Id populated
var insertedUser = await _db.InsertAsync(user);
Guid userId = insertedUser.Id;

// Insert with explicit key (you provide the ID)
var auditLog = new AuditLog
{
    Id = Guid.NewGuid(), // Required - you must provide the ID
    EntityType = "User",
    EntityId = userId,
    Action = "Created",
    NewValues = new Dictionary<string, object>
    {
        ["Email"] = "john@example.com",
        ["Username"] = "john_doe"
    },
    UserId = currentUserId,
    Timestamp = DateTimeOffset.UtcNow
};

var insertedLog = await _db.InsertAsync(auditLog);

// Insert with composite key
var userRole = new UserRole
{
    UserId = userId,
    RoleId = adminRoleId,
    AssignedAt = DateTime.UtcNow,
    AssignedBy = currentUserId
};

var insertedRole = await _db.InsertAsync(userRole);
// insertedRole.UserId and insertedRole.RoleId are populated
```

### Get (Single Entity)

```csharp
// Get by single key
var user = await _db.GetAsync<User>(userId);

// Get by single key (sync)
var user = _db.Get<User>(userId);

// Get by composite key using dictionary
var keys = new Dictionary<string, Guid>
{
    ["UserId"] = userId,
    ["RoleId"] = roleId
};
var userRole = await _db.GetAsync<UserRole>(keys);

// Get by composite key using anonymous object (simpler syntax)
var userRole = await _db.GetAsync<UserRole>(new { UserId = userId, RoleId = roleId });

// Sync version with anonymous object
var userRole = _db.Get<UserRole>(new { UserId = userId, RoleId = roleId });

// Returns null if not found
var user = await _db.GetAsync<User>(Guid.NewGuid());
if (user == null)
{
    // Handle not found
}
```

### GetList (Multiple Entities)

```csharp
// Get all entities
var allUsers = await _db.GetListAsync<User>();
var allCategories = await _db.GetListAsync<Category>(); // Uses cache if [Cacheable]

// Get multiple by IDs
var userIds = new List<Guid> { userId1, userId2, userId3 };
var users = await _db.GetListAsync<User>(userIds);

// Get multiple by composite keys using dictionaries
var keysList = new List<Dictionary<string, Guid>>
{
    new() { ["UserId"] = user1Id, ["RoleId"] = role1Id },
    new() { ["UserId"] = user2Id, ["RoleId"] = role2Id }
};
var userRoles = await _db.GetListAsync<UserRole>(keysList);

// Get multiple by composite keys using anonymous objects (simpler syntax)
var userRoles = await _db.GetListAsync<UserRole>(new[]
{
    new { UserId = user1Id, RoleId = role1Id },
    new { UserId = user2Id, RoleId = role2Id }
});

// Sync version with anonymous objects
var userRoles = _db.GetList<UserRole>(new[]
{
    new { UserId = user1Id, RoleId = role1Id },
    new { UserId = user2Id, RoleId = role2Id }
});
```

### Update

```csharp
// Update all writable properties
var user = await _db.GetAsync<User>(userId);
user.Username = "new_username";
user.UpdatedAt = DateTime.UtcNow;
user.Settings = new UserSettings { Theme = "light" };

bool updated = await _db.UpdateAsync(user);

// Update specific properties only (more efficient)
user.Email = "newemail@example.com";
user.UpdatedAt = DateTime.UtcNow;

bool updated = await _db.UpdateAsync(user, ["Email", "UpdatedAt"]);

// Sync version
bool updated = _db.Update(user);
bool updated = _db.Update(user, ["Email"]);

// Update returns false if entity not found
var nonExistentUser = new User { Id = Guid.NewGuid() };
bool updated = await _db.UpdateAsync(nonExistentUser); // false
```

### Delete

```csharp
// Delete by entity
var user = await _db.GetAsync<User>(userId);
bool deleted = await _db.DeleteAsync(user);

// Delete by single key (more efficient)
bool deleted = await _db.DeleteAsync<User>(userId);

// Delete by composite key
var keys = new Dictionary<string, Guid>
{
    ["UserId"] = userId,
    ["RoleId"] = roleId
};
bool deleted = await _db.DeleteAsync<UserRole>(keys);

// Sync versions
bool deleted = _db.Delete(user);
bool deleted = _db.Delete<User>(userId);
bool deleted = _db.Delete<UserRole>(keys);

// Delete returns false if entity not found
bool deleted = await _db.DeleteAsync<User>(Guid.NewGuid()); // false
```

---

## Query Methods

### Basic Query

```csharp
// Query with parameters
var activeUsers = await _db.QueryAsync<User>(
    "SELECT * FROM users WHERE is_active = @IsActive ORDER BY created_at DESC",
    new { IsActive = true });

// Query with multiple parameters
var users = await _db.QueryAsync<User>(
    """
    SELECT * FROM users 
    WHERE role = @Role 
      AND created_at >= @StartDate 
      AND is_active = @IsActive
    ORDER BY username
    """,
    new { Role = UserRole.Admin, StartDate = DateTime.UtcNow.AddDays(-30), IsActive = true });

// Query with LIKE
var users = await _db.QueryAsync<User>(
    "SELECT * FROM users WHERE email LIKE @Pattern",
    new { Pattern = "%@example.com" });

// Query with IN clause (using ANY for PostgreSQL)
var userIds = new List<Guid> { id1, id2, id3 };
var users = await _db.QueryAsync<User>(
    "SELECT * FROM users WHERE id = ANY(@Ids)",
    new { Ids = userIds });

// Query returning custom DTO
public class UserSummary
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

var summaries = await _db.QueryAsync<UserSummary>(
    """
    SELECT 
        u.id AS "Id",
        u.username AS "Username",
        COUNT(o.id) AS "OrderCount",
        COALESCE(SUM(o.total_amount), 0) AS "TotalSpent"
    FROM users u
    LEFT JOIN orders o ON o.user_id = u.id
    GROUP BY u.id, u.username
    ORDER BY "TotalSpent" DESC
    """);

// Sync version
var users = _db.Query<User>("SELECT * FROM users WHERE is_active = true");
```

### Execute (INSERT, UPDATE, DELETE)

```csharp
// Execute returns number of affected rows
int rowsAffected = await _db.ExecuteAsync(
    "UPDATE users SET last_login_at = @Now WHERE id = @UserId",
    new { Now = DateTimeOffset.UtcNow, UserId = userId });

// Bulk update
int updated = await _db.ExecuteAsync(
    "UPDATE products SET is_active = false WHERE stock_quantity = 0");

// Delete with condition
int deleted = await _db.ExecuteAsync(
    "DELETE FROM order_notes WHERE created_at < @CutoffDate AND is_internal = true",
    new { CutoffDate = DateTime.UtcNow.AddYears(-1) });

// Insert with Execute (when you don't need returned keys)
await _db.ExecuteAsync(
    """
    INSERT INTO audit_logs (id, entity_type, entity_id, action, timestamp)
    VALUES (@Id, @EntityType, @EntityId, @Action, @Timestamp)
    """,
    new
    {
        Id = Guid.NewGuid(),
        EntityType = "User",
        EntityId = userId,
        Action = "Login",
        Timestamp = DateTimeOffset.UtcNow
    });

// Sync version
int affected = _db.Execute("UPDATE users SET is_active = false WHERE id = @Id", new { Id = userId });
```

---

## Multi-Query Methods

### QueryMultiple (Container Class)

Use when you need to fetch multiple different result sets into a single container object.

```csharp
[MultiQuery]
public class UserDashboard
{
    [ResultSet(0)]
    public User? Profile { get; set; }

    [ResultSet(1)]
    public List<Order> RecentOrders { get; set; } = [];

    [ResultSet(2)]
    public DashboardStats? Stats { get; set; }
}

// Usage
var sql = """
    -- Result Set 0: User profile
    SELECT * FROM users WHERE id = @UserId;
    
    -- Result Set 1: Recent orders
    SELECT * FROM orders 
    WHERE user_id = @UserId 
    ORDER BY created_at DESC 
    LIMIT 10;
    
    -- Result Set 2: Stats
    SELECT 
        COUNT(*) AS "TotalOrders",
        COALESCE(SUM(total_amount), 0) AS "TotalSpent",
        COUNT(*) FILTER (WHERE status = 0) AS "PendingOrders"
    FROM orders 
    WHERE user_id = @UserId;
    """;

var dashboard = await _db.QueryMultipleAsync<UserDashboard>(sql, new { UserId = userId });

Console.WriteLine($"User: {dashboard.Profile?.Username}");
Console.WriteLine($"Recent Orders: {dashboard.RecentOrders.Count}");
Console.WriteLine($"Total Spent: {dashboard.Stats?.TotalSpent:C}");

// Sync version
var dashboard = _db.QueryMultiple<UserDashboard>(sql, new { UserId = userId });
```

### QueryMultipleList (Parent-Child Mapping)

Use when you need to fetch a list of parent entities with their child collections populated automatically.

```csharp
[Table("orders")]
public class Order
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public string OrderNumber { get; set; } = string.Empty;
    
    public OrderStatus Status { get; set; }
    
    public decimal TotalAmount { get; set; }
    
    public DateTime CreatedAt { get; set; }

    // Child collections - mapped from subsequent result sets
    [External]
    [ResultSet(1, ForeignKey = "OrderId")]
    public List<OrderLine> Lines { get; set; } = [];

    [External]
    [ResultSet(2, ForeignKey = "OrderId")]
    public List<OrderNote> Notes { get; set; } = [];
}

// Usage - Single query with multiple SELECT statements
var sql = """
    -- Result Set 0: Parent entities (orders)
    SELECT id AS "Id", user_id AS "UserId", order_number AS "OrderNumber",
           status AS "Status", total_amount AS "TotalAmount", created_at AS "CreatedAt"
    FROM orders 
    WHERE user_id = @UserId
    ORDER BY created_at DESC;
    
    -- Result Set 1: Child entities (order lines) - mapped via OrderId
    SELECT id AS "Id", order_id AS "OrderId", product_id AS "ProductId",
           product_name AS "ProductName", quantity AS "Quantity", 
           unit_price AS "UnitPrice", discount_percent AS "DiscountPercent"
    FROM order_lines 
    WHERE order_id IN (SELECT id FROM orders WHERE user_id = @UserId);
    
    -- Result Set 2: Child entities (order notes) - mapped via OrderId
    SELECT id AS "Id", order_id AS "OrderId", author_id AS "AuthorId",
           text AS "Text", is_internal AS "IsInternal", created_at AS "CreatedAt"
    FROM order_notes 
    WHERE order_id IN (SELECT id FROM orders WHERE user_id = @UserId)
      AND is_internal = false;
    """;

var orders = await _db.QueryMultipleListAsync<Order>(sql, new { UserId = userId });

foreach (var order in orders)
{
    Console.WriteLine($"Order: {order.OrderNumber}");
    Console.WriteLine($"  Lines: {order.Lines.Count}");
    foreach (var line in order.Lines)
    {
        Console.WriteLine($"    - {line.ProductName} x {line.Quantity}");
    }
    Console.WriteLine($"  Notes: {order.Notes.Count}");
}

// Sync version
var orders = _db.QueryMultipleList<Order>(sql, new { UserId = userId });
```

### ResultSet Attribute Properties

```csharp
[ResultSet(
    index: 1,              // Result set index (0-based, 0 = parent entities)
    ForeignKey = "OrderId", // Property name in child entity that references parent
    ParentKey = "Id"       // Parent entity's key property (defaults to "Id")
)]
public List<OrderLine> Lines { get; set; } = [];

// Custom parent key
[ResultSet(1, ForeignKey = "ParentOrderId", ParentKey = "OrderId")]
public List<OrderLine> Lines { get; set; } = [];
```

---

## QueryWithJoin Methods

Use when you want a single SQL query with JOINs instead of multiple SELECT statements.

### Single Child Collection

```csharp
var sql = """
    SELECT 
        o.id AS "Id", o.user_id AS "UserId", o.order_number AS "OrderNumber",
        o.status AS "Status", o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
        l.id AS "Id", l.order_id AS "OrderId", l.product_id AS "ProductId",
        l.product_name AS "ProductName", l.quantity AS "Quantity", 
        l.unit_price AS "UnitPrice", l.discount_percent AS "DiscountPercent"
    FROM orders o
    LEFT JOIN order_lines l ON l.order_id = o.id
    WHERE o.user_id = @UserId
    ORDER BY o.created_at DESC, l.product_name
    """;

var orders = await _db.QueryWithJoinAsync<Order, OrderLine>(
    sql,
    parent => parent.Lines,     // Child collection selector
    parent => parent.Id,        // Parent primary key selector
    child => child.Id,          // Child primary key selector (for deduplication)
    splitOn: "Id",              // Column to split on (where child columns start)
    parameters: new { UserId = userId });

// Sync version
var orders = _db.QueryWithJoin<Order, OrderLine>(
    sql,
    parent => parent.Lines,
    parent => parent.Id,
    child => child.Id,
    splitOn: "Id",
    parameters: new { UserId = userId });
```

### Two Child Collections

When JOINing multiple child tables, Cartesian products occur. The method automatically handles deduplication.

```csharp
var sql = """
    SELECT 
        o.id AS "Id", o.user_id AS "UserId", o.order_number AS "OrderNumber",
        o.status AS "Status", o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
        l.id AS "Id", l.order_id AS "OrderId", l.product_name AS "ProductName",
        l.quantity AS "Quantity", l.unit_price AS "UnitPrice",
        n.id AS "Id", n.order_id AS "OrderId", n.text AS "Text", 
        n.is_internal AS "IsInternal", n.created_at AS "CreatedAt"
    FROM orders o
    LEFT JOIN order_lines l ON l.order_id = o.id
    LEFT JOIN order_notes n ON n.order_id = o.id
    WHERE o.user_id = @UserId
    ORDER BY o.created_at DESC
    """;

var orders = await _db.QueryWithJoinAsync<Order, OrderLine, OrderNote>(
    sql,
    parent => parent.Lines,         // First child collection
    parent => parent.Notes,         // Second child collection
    parent => parent.Id,            // Parent primary key
    child1 => child1.Id,            // First child primary key (for deduplication)
    child2 => child2.Id,            // Second child primary key (for deduplication)
    splitOn: "Id,Id",               // Comma-separated split columns
    parameters: new { UserId = userId });

// Sync version
var orders = _db.QueryWithJoin<Order, OrderLine, OrderNote>(
    sql,
    parent => parent.Lines,
    parent => parent.Notes,
    parent => parent.Id,
    child1 => child1.Id,
    child2 => child2.Id,
    splitOn: "Id,Id");
```

### QueryMultipleList vs QueryWithJoin

| Feature | QueryMultipleList | QueryWithJoin |
|---------|-------------------|---------------|
| SQL Queries | Multiple SELECT statements | Single SELECT with JOINs |
| Database Roundtrips | 1 (sends all queries together) | 1 |
| Configuration | Attribute-based ([ResultSet]) | Lambda-based (selectors) |
| Deduplication | Automatic (FK-based) | Automatic (PK-based) |
| Max Children | Unlimited (based on result sets) | 2 (current implementation) |
| Best For | Known entity structures | Ad-hoc queries |

---

## Transaction Management

### Basic Transaction

```csharp
// Async transaction
await using var scope = await _db.CreateTransactionScopeAsync();

var user = new User { Email = "new@example.com", Username = "newuser" };
await _db.InsertAsync(user);

var order = new Order { UserId = user.Id, OrderNumber = "ORD-001" };
await _db.InsertAsync(order);

await scope.CompleteAsync(); // Commit transaction

// If CompleteAsync() is not called, transaction is rolled back on dispose
```

### Nested Transactions

Nested transactions are automatically handled. Inner scopes participate in the outer transaction.

```csharp
await using var outerScope = await _db.CreateTransactionScopeAsync();

// Create user
var user = new User { Email = "user@example.com" };
await _db.InsertAsync(user);

await using (var innerScope = await _db.CreateTransactionScopeAsync())
{
    // Create order within inner scope
    var order = new Order { UserId = user.Id };
    await _db.InsertAsync(order);
    
    await innerScope.CompleteAsync(); // Mark inner as complete
}

// If outer scope is not completed, both user and order are rolled back
await outerScope.CompleteAsync(); // Commit everything
```

### Transaction with Error Handling

```csharp
try
{
    await using var scope = await _db.CreateTransactionScopeAsync();
    
    await _db.InsertAsync(user);
    await _db.InsertAsync(order);
    
    // Validate business rules
    if (order.TotalAmount > user.CreditLimit)
    {
        throw new InvalidOperationException("Credit limit exceeded");
        // Transaction will be rolled back
    }
    
    await scope.CompleteAsync();
}
catch (Exception ex)
{
    // Transaction is automatically rolled back
    _logger.LogError(ex, "Order creation failed");
    throw;
}
```

### Sync Transaction

```csharp
using var scope = _db.CreateTransactionScope();

_db.Insert(user);
_db.Insert(order);

scope.Complete(); // Commit
```

---

## Advisory Locks

Advisory locks provide distributed locking across multiple application instances.

### Transaction with Advisory Lock

```csharp
// Numeric lock key
await using var scope = await _db.CreateTransactionScopeAsync(lockKey: 12345L);

// Exclusive lock is held for duration of transaction
await _db.UpdateAsync(inventory);

await scope.CompleteAsync();

// String lock key (automatically hashed to long)
await using var scope = await _db.CreateTransactionScopeAsync(lockKey: "order-processing-lock");

await ProcessOrdersAsync();

await scope.CompleteAsync();
```

### Advisory Lock Scope (without transaction)

```csharp
// When you need a lock but not a transaction
await using var lockScope = await _db.CreateAdvisoryLockScopeAsync(lockKey: 12345L);

// Perform operations with exclusive lock
var inventory = await _db.GetAsync<Inventory>(productId);
inventory.Quantity -= orderQuantity;
await _db.UpdateAsync(inventory);

await lockScope.CompleteAsync(); // Release lock

// Sync version
using var lockScope = _db.CreateAdvisoryLockScope(lockKey: 12345L);
// ... operations ...
lockScope.Complete();
```

### Lock Key Strategies

```csharp
// Global lock (single key)
const long GLOBAL_INVENTORY_LOCK = 1001;

// Entity-specific lock (combine entity type and ID)
long GetEntityLock(string entityType, Guid entityId)
{
    return HashCode.Combine(entityType, entityId);
}

// Usage
await using var scope = await _db.CreateTransactionScopeAsync(
    lockKey: GetEntityLock("Order", orderId));

// String-based lock (hashed automatically)
await using var scope = await _db.CreateTransactionScopeAsync(
    lockKey: $"user:{userId}:balance");
```

---

## Caching

Entities marked with `[Cacheable]` are automatically cached and invalidated.

### Cacheable Entity Configuration

```csharp
// Default cache (5 minutes, absolute expiration)
[Cacheable]
public class Category { }

// Custom duration
[Cacheable(DurationSeconds = 3600)] // 1 hour
public class Product { }

// Sliding expiration (resets on each access)
[Cacheable(DurationSeconds = 600, SlidingExpiration = true)]
public class Setting { }
```

### Cache Behavior

```csharp
// First call: fetches from database and caches
var categories = await _db.GetListAsync<Category>();

// Second call: returns from cache
var categories = await _db.GetListAsync<Category>();

// Insert/Update/Delete automatically invalidates cache
await _db.InsertAsync(new Category { Name = "New Category" });
// Cache is invalidated

// Next call fetches fresh data
var categories = await _db.GetListAsync<Category>();
```

### Enabling Cache

```csharp
// Enable caching in service registration
builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);
```

---

## Best Practices

### 1. Use Async Methods

```csharp
// Prefer async methods for better scalability
var user = await _db.GetAsync<User>(userId);
await _db.InsertAsync(entity);
```

### 2. Use Specific Property Updates

```csharp
// Instead of updating all properties
await _db.UpdateAsync(user);

// Update only changed properties (more efficient)
user.LastLoginAt = DateTimeOffset.UtcNow;
await _db.UpdateAsync(user, ["LastLoginAt"]);
```

### 3. Use Transactions for Related Operations

```csharp
await using var scope = await _db.CreateTransactionScopeAsync();

await _db.InsertAsync(order);
foreach (var line in lines)
{
    await _db.InsertAsync(line);
}

await scope.CompleteAsync();
```

### 4. Use Advisory Locks for Critical Sections

```csharp
await using var scope = await _db.CreateTransactionScopeAsync(
    lockKey: $"inventory:{productId}");

var inventory = await _db.GetAsync<Inventory>(productId);
if (inventory.Quantity >= requestedQuantity)
{
    inventory.Quantity -= requestedQuantity;
    await _db.UpdateAsync(inventory);
}

await scope.CompleteAsync();
```

### 5. Choose the Right Query Method

```csharp
// Simple query → QueryAsync
var users = await _db.QueryAsync<User>("SELECT * FROM users");

// Multiple result sets, container class → QueryMultipleAsync
var dashboard = await _db.QueryMultipleAsync<Dashboard>(sql);

// Parent with children, attribute-based → QueryMultipleListAsync
var orders = await _db.QueryMultipleListAsync<Order>(sql);

// Parent with children, lambda-based → QueryWithJoinAsync
var orders = await _db.QueryWithJoinAsync<Order, OrderLine>(...);
```

### 6. Use Caching for Read-Heavy, Rarely-Changed Data

```csharp
[Cacheable(DurationSeconds = 3600)]
public class Country { } // Good: rarely changes

[Cacheable(DurationSeconds = 60)]
public class ExchangeRate { } // OK: changes periodically

// Don't cache: User, Order, etc. (frequently changing)
```

### 7. Handle Nulls Properly

```csharp
var user = await _db.GetAsync<User>(userId);
if (user is null)
{
    throw new NotFoundException($"User {userId} not found");
}

// Or use pattern matching
if (await _db.GetAsync<User>(userId) is not { } user)
{
    return NotFound();
}
```

### 8. Use Appropriate Column Aliases

```csharp
// PostgreSQL uses lowercase by default
// Use quoted aliases to match C# property names
var sql = """
    SELECT 
        id AS "Id",
        user_name AS "UserName",
        created_at AS "CreatedAt"
    FROM users
    """;
```

---

## Error Handling

### Common Exceptions

```csharp
// InvalidOperationException - wrong key type
try
{
    // Entity has composite key, but using single key method
    await _db.GetAsync<UserRole>(userId);
}
catch (InvalidOperationException ex)
{
    // "Entity type UserRole has 2 key properties. Use Get(Dictionary<string, Guid>) overload"
}

// ArgumentException - missing key in dictionary
try
{
    var keys = new Dictionary<string, Guid> { ["UserId"] = userId };
    // Missing "RoleId"
    await _db.GetAsync<UserRole>(keys);
}
catch (ArgumentException ex)
{
    // "Missing key property 'RoleId' in the keys dictionary"
}

// ArgumentException - invalid property type in anonymous object
try
{
    // Property must be of type Guid
    await _db.GetAsync<UserRole>(new { UserId = userId, RoleId = "invalid" });
}
catch (ArgumentException ex)
{
    // "Property 'RoleId' must be of type Guid, but was String."
}
}

// PostgresException - database errors
try
{
    await _db.InsertAsync(user);
}
catch (Npgsql.PostgresException ex) when (ex.SqlState == "23505")
{
    // Unique constraint violation
    throw new ConflictException("Email already exists");
}
```

---

## API Reference Summary

### IDbService Interface

| Method | Description |
|--------|-------------|
| `Query<T>` | Execute query and return collection |
| `QueryAsync<T>` | Async version of Query |
| `QueryMultiple<T>` | Execute multi-result query into container |
| `QueryMultipleAsync<T>` | Async version of QueryMultiple |
| `QueryMultipleList<T>` | Execute multi-result query with parent-child mapping |
| `QueryMultipleListAsync<T>` | Async version of QueryMultipleList |
| `QueryWithJoin<TParent, TChild>` | Execute JOIN query with single child collection |
| `QueryWithJoinAsync<TParent, TChild>` | Async version with single child |
| `QueryWithJoin<TParent, TChild1, TChild2>` | Execute JOIN query with two child collections |
| `QueryWithJoinAsync<TParent, TChild1, TChild2>` | Async version with two children |
| `Execute` | Execute command, return affected rows |
| `ExecuteAsync` | Async version of Execute |
| `Insert<T>` | Insert entity, return inserted entity with generated keys |
| `InsertAsync<T>` | Async version of Insert |
| `Update<T>` | Update entity (all or specific properties) |
| `UpdateAsync<T>` | Async version of Update |
| `Delete<T>` | Delete entity by entity, single key, or composite key |
| `DeleteAsync<T>` | Async version of Delete |
| `Get<T>(Guid)` | Get entity by single key |
| `Get<T>(Dictionary)` | Get entity by composite key using dictionary |
| `Get<T>(object)` | Get entity by composite key using anonymous object |
| `GetAsync<T>` | Async versions of Get (3 overloads) |
| `GetList<T>()` | Get all entities |
| `GetList<T>(IEnumerable<Guid>)` | Get entities by multiple single keys |
| `GetList<T>(IEnumerable<Dictionary>)` | Get entities by composite keys using dictionaries |
| `GetList<T>(IEnumerable<object>)` | Get entities by composite keys using anonymous objects |
| `GetListAsync<T>` | Async versions of GetList (4 overloads) |
| `CreateConnection` | Create database connection |
| `CreateConnectionAsync` | Async version |
| `CreateTransactionScope` | Create transaction scope with optional lock |
| `CreateTransactionScopeAsync` | Async versions (3 overloads) |
| `CreateAdvisoryLockScope` | Create lock scope without transaction |
| `CreateAdvisoryLockScopeAsync` | Async version |

---

## Version History

### v1.0.0
- Initial release with full CRUD support
- Transaction and advisory lock management
- Multi-query support (QueryMultiple, QueryMultipleList)
- QueryWithJoin for single-query parent-child mapping
- Entity caching with automatic invalidation
- JSON column support
- Composite key support
