# WebVella.Database - Complete Documentation

A lightweight, high-performance Postgres data access library built on Dapper. This document provides comprehensive documentation for all features and methods.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Entity Attributes](#entity-attributes)
3. [Sample Database Schema](#sample-database-schema)
4. [Sample Entity Models](#sample-entity-models)
5. [Basic CRUD Operations](#basic-crud-operations)
6. [Query Methods](#query-methods)
   - [Fluent Query Builder](#fluent-query-builder)
   - [Raw SQL Queries](#raw-sql-queries)
7. [Execute Methods](#execute-methods)
8. [Multi-Query Methods](#multi-query-methods)
   - [QueryMultiple (Container Class)](#querymultiple-container-class)
   - [QueryMultipleList (Parent-Child Mapping)](#querymultiplelist-parent-child-mapping)
   - [QueryMultipleList Fluent Builder](#querymultiplelist-fluent-builder)
9. [QueryWithJoin Methods](#querywith-join-methods)
   - [QueryWithJoin Fluent Builder](#querywith-join-fluent-builder)
   - [QueryMultipleList vs QueryWithJoin](#querymultiplelist-vs-querywith-join)
10. [Transaction Management](#transaction-management)
11. [Advisory Locks](#advisory-locks)
12. [Caching](#caching)
13. [Row Level Security](#row-level-security)
14. [Database Migrations](#database-migrations)
15. [Best Practices](#best-practices)
16. [Version History](#version-history)

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
| `[DbColumn("name")]` | Property | Specifies explicit database column name, overriding auto snake_case conversion |
| `[External]` | Property | Excludes property from INSERT/UPDATE/SELECT operations |
| `[ReadOnly]` | Property | Excludes property from INSERT/UPDATE but includes in SELECT (for computed/generated columns) |
| `[Write(false)]` | Property | Prevents property from being written to database (equivalent to `[ReadOnly]`) |
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

### Entity with Custom Column Names

```csharp
// Use [DbColumn] to override the default snake_case column name mapping.
// Properties without [DbColumn] still use auto snake_case conversion.
[Table("legacy_users")]
public class LegacyUser
{
    [Key]
    [DbColumn("usr_id")]        // maps to "usr_id" instead of "id"
    public Guid Id { get; set; }

    [DbColumn("full_name")]      // maps to "full_name" instead of "display_name"
    public string DisplayName { get; set; } = string.Empty;

    [DbColumn("email_address")]  // maps to "email_address" instead of "email"
    public string Email { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty; // uses default: "description"
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

// Insert from anonymous object - maps matching properties to entity
var newUser = await _db.InsertAsync<User>(new
{
    Email = "jane@example.com",
    Username = "jane_doe",
    PasswordHash = "hashed_password",
    IsActive = true,
    CreatedAt = DateTime.UtcNow
});
// newUser.Id is auto-generated, unmapped properties use defaults

// Insert from anonymous object (sync)
var newUser = _db.Insert<User>(new
{
    Email = "jane@example.com",
    Username = "jane_doe",
    CreatedAt = DateTime.UtcNow
});

// Type mismatches throw ArgumentException
// _db.Insert<User>(new { Email = 123 }); // throws: Type mismatch for 'Email'
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

// Update from anonymous object - partial update with only specified properties
// The object must include all key properties (Id) for record lookup
bool updated = await _db.UpdateAsync<User>(new
{
    Id = userId,
    Email = "updated@example.com",
    UpdatedAt = DateTime.UtcNow
});
// Only Email and UpdatedAt are updated; all other columns remain unchanged

// Update from anonymous object (sync)
bool updated = _db.Update<User>(new
{
    Id = userId,
    Username = "new_username"
});

// Key-only object returns false (nothing to update)
bool updated = await _db.UpdateAsync<User>(new { Id = userId }); // false

// Missing key throws ArgumentException
// _db.Update<User>(new { Email = "x" }); // throws: Missing required key properties

// Type mismatches throw ArgumentException
// _db.Update<User>(new { Id = userId, Email = 123 }); // throws: Type mismatch
```

### Delete

```csharp
// Delete by entity
var user = await _db.GetAsync<User>(userId);
bool deleted = await _db.DeleteAsync(user);

// Delete by single key (more efficient)
bool deleted = await _db.DeleteAsync<User>(userId);

// Delete by composite key using dictionary
var keys = new Dictionary<string, Guid>
{
    ["UserId"] = userId,
    ["RoleId"] = roleId
};
bool deleted = await _db.DeleteAsync<UserRole>(keys);

// Delete by composite key using anonymous object (simpler syntax)
bool deleted = await _db.DeleteAsync<UserRole>(new { UserId = userId, RoleId = roleId });

// Sync versions
bool deleted = _db.Delete(user);
bool deleted = _db.Delete<User>(userId);
bool deleted = _db.Delete<UserRole>(keys);
bool deleted = _db.Delete<UserRole>(new { UserId = userId, RoleId = roleId });

// Delete returns false if entity not found
bool deleted = await _db.DeleteAsync<User>(Guid.NewGuid()); // false
```

---

## Query Methods

### Fluent Query Builder

`Query<T>()` returns a `DbQuery<T>` that translates LINQ expression predicates into
parameterised PostgreSQL SQL. No SQL strings needed.

```csharp
var orders = await _db.Query<Order>()
    .Where(o => o.Status == OrderStatus.Pending && o.TotalAmount > 50m)
    .OrderByDescending(o => o.CreatedAt)
    .Limit(20)
    .ToListAsync();
```

#### Builder Methods

| Method | SQL Produced | Notes |
|--------|-------------|-------|
| `.Where(predicate)` | `WHERE ...` | Multiple calls combined with `AND` |
| `.OrderBy(prop)` | `ORDER BY col` | Primary ascending sort |
| `.OrderByDescending(prop)` | `ORDER BY col DESC` | Primary descending sort |
| `.ThenBy(prop)` | `, col` | Secondary ascending sort |
| `.ThenByDescending(prop)` | `, col DESC` | Secondary descending sort |
| `.Limit(n)` | `LIMIT n` | Maximum rows to return; must be ≥ 0 |
| `.Offset(n)` | `OFFSET n` | Rows to skip; must be ≥ 0 |
| `.WithPaging(page, pageSize)` | `LIMIT n OFFSET m` | 1-based page-number pagination |

#### Terminal Methods

| Method | SQL | Returns |
|--------|-----|---------|
| `ToListAsync()` | `SELECT * FROM ...` | All matching rows as `IEnumerable<T>` |
| `FirstOrDefaultAsync()` | `SELECT ... LIMIT 1` | First match or `null` |
| `CountAsync()` | `SELECT COUNT(*) FROM ...` | Total matching rows as `long` |
| `ExistsAsync()` | `SELECT EXISTS(SELECT 1 ...)` | `true` if any row matches |
| `ToList()` | `SELECT * FROM ...` | Sync — all matching rows |
| `FirstOrDefault()` | `SELECT ... LIMIT 1` | Sync — first match or `null` |
| `Count()` | `SELECT COUNT(*) FROM ...` | Sync — total as `long` |
| `Exists()` | `SELECT EXISTS(SELECT 1 ...)` | Sync — `true` if any row matches |

#### Supported WHERE Patterns

##### Equality and Comparison

```csharp
.Where(u => u.Name == "Alice")              // name = @p0
.Where(u => u.Name != "Alice")              // name != @p0
.Where(u => u.Price < 100m)                 // price < @p0
.Where(u => u.Price <= 100m)                // price <= @p0
.Where(u => u.Price > 10m)                  // price > @p0
.Where(u => u.Price >= 10m)                 // price >= @p0
```

##### Boolean Shorthand

```csharp
.Where(u => u.IsActive)                     // is_active = @p0  (true)
.Where(u => !u.IsActive)                    // is_active = @p0  (false)
.Where(u => u.IsActive == true)             // is_active = @p0  (true)
.Where(u => u.IsActive == false)            // is_active = @p0  (false)
```

##### Null Checks

```csharp
.Where(u => u.Description == null)          // description IS NULL
.Where(u => u.Description != null)          // description IS NOT NULL
```

##### Logical Operators

```csharp
.Where(u => u.IsActive && u.Price > 10m)    // (is_active = @p0 AND price > @p1)
.Where(u => u.IsActive || u.IsAdmin)        // (is_active = @p0 OR is_admin = @p1)
.Where(u => !(u.Price > 100m))              // NOT (price > @p0)

// Nested grouping preserves operator precedence
.Where(u => (u.Role == Role.Admin || u.Role == Role.Manager) && u.IsActive)
// ((role = @p0 OR role = @p1) AND is_active = @p2)
```

##### Multiple Where Calls (combined with AND)

```csharp
await _db.Query<Order>()
    .Where(o => o.IsActive)
    .Where(o => o.TotalAmount > 50m)
    .Where(o => o.CreatedAt > cutoffDate)
    .ToListAsync();
// WHERE is_active = @p0 AND total_amount > @p1 AND created_at > @p2
```

##### Enum Values (auto-converted to int)

```csharp
.Where(u => u.Status == UserStatus.Active)           // status = @p0  (value: 1)
.Where(u => u.Role   != UserRole.Admin)              // role   != @p0
.Where(u => u.Status == UserStatus.Active
         || u.Status == UserStatus.Pending)          // (status = @p0 OR status = @p1)
```

##### Captured Variables and Closures

```csharp
var minAmount = 50m;
var cutoff    = DateTime.UtcNow.AddDays(-30);

await _db.Query<Order>()
    .Where(o => o.TotalAmount >= minAmount && o.CreatedAt > cutoff)
    .ToListAsync();
```

##### String Methods — LIKE (case-sensitive)

`%`, `_`, and `\` in the search value are automatically escaped.

```csharp
.Where(u => u.Email.Contains("@example.com"))    // email LIKE @p0  ('%@example.com%')
.Where(u => u.Name.StartsWith("John"))           // name  LIKE @p0  ('John%')
.Where(u => u.Name.EndsWith("son"))              // name  LIKE @p0  ('%son')
.Where(u => u.Name.Contains("50%"))              // name  LIKE @p0  ('%50\%%')  — % escaped
.Where(u => u.Code.Contains("item_1"))           // code  LIKE @p0  ('%item\_1%') — _ escaped
```

##### String Methods — ILIKE (case-insensitive)

Extension methods from `DbStringExtensions`. Valid inside `.Where()` predicates only;
calling them directly always throws `InvalidOperationException`.

```csharp
.Where(u => u.Name.ILikeContains("admin"))       // name ILIKE @p0  ('%admin%')
.Where(u => u.Name.ILikeStartsWith("john"))      // name ILIKE @p0  ('john%')
.Where(u => u.Name.ILikeEndsWith("son"))         // name ILIKE @p0  ('%son')
```

##### Case Folding — LOWER / UPPER

```csharp
.Where(u => u.Email.ToLower()          == "alice@example.com")  // LOWER(email) = @p0
.Where(u => u.Email.ToLowerInvariant() == "alice@example.com")  // LOWER(email) = @p0
.Where(u => u.Code.ToUpper()           == "ABC-001")             // UPPER(code)  = @p0
.Where(u => u.Code.ToUpperInvariant()  == "ABC-001")             // UPPER(code)  = @p0
```

##### Collection Membership — ANY

```csharp
// Instance Contains
var ids = new List<Guid> { id1, id2, id3 };
.Where(u => ids.Contains(u.Id))                  // id = ANY(@p0)

// Static Enumerable.Contains
var ids = new[] { id1, id2 };
.Where(u => Enumerable.Contains(ids, u.Id))      // id = ANY(@p0)

// Empty collection short-circuits to no rows (no database call for evaluation)
var empty = new List<Guid>();
.Where(u => empty.Contains(u.Id))                // 1 = 0
```

#### Unsupported Patterns

The following expressions throw `NotSupportedException` at query execution time.
Use the raw SQL `QueryAsync<T>` / `Query<T>` methods for these cases.

```csharp
// ❌ Arithmetic operators inside WHERE
.Where(u => (u.Price + 10m) > 100m)
.Where(u => (u.Quantity * u.Price) > 500m)

// ❌ String methods other than Contains / StartsWith / EndsWith / ToLower / ToUpper
.Where(u => u.Name.Trim() == "Alice")
.Where(u => u.Name.Replace(" ", "") == "Alice")

// ❌ Static methods (other than Enumerable.Contains)
.Where(u => Math.Abs(u.Delta) > 5)
.Where(u => string.IsNullOrEmpty(u.Name))

// ❌ Nested property navigation
.Where(u => u.Address.City == "Sofia")

// ❌ Ternary / conditional expressions
.Where(u => (u.IsActive ? u.Price : 0m) > 10m)

// ❌ Index / element access
.Where(u => u.Tags[0] == "featured")
```

#### Ordering

```csharp
// Single column ascending
await _db.Query<Order>().OrderBy(o => o.CreatedAt).ToListAsync();

// Single column descending
await _db.Query<Order>().OrderByDescending(o => o.TotalAmount).ToListAsync();

// Multiple columns
await _db.Query<Order>()
    .OrderBy(o => o.Status)
    .ThenByDescending(o => o.TotalAmount)
    .ThenBy(o => o.CreatedAt)
    .ToListAsync();
// ORDER BY status, total_amount DESC, created_at
```

#### Pagination

```csharp
// Explicit Limit / Offset
var page = await _db.Query<Order>()
    .Where(o => o.IsActive)
    .OrderBy(o => o.CreatedAt)
    .Offset(20)
    .Limit(10)
    .ToListAsync();

// Page-number helper (1-based)
var page = await _db.Query<Order>()
    .Where(o => o.IsActive)
    .OrderBy(o => o.CreatedAt)
    .WithPaging(page: 3, pageSize: 10)    // → LIMIT 10 OFFSET 20
    .ToListAsync();

// Null defaults: page defaults to 1, pageSize defaults to 10
.WithPaging(null, 20)    // → LIMIT 20 OFFSET 0
.WithPaging(2,    null)  // → LIMIT 10 OFFSET 10
```

#### Aggregate and Existence Checks

```csharp
// Count all matching rows
long count = await _db.Query<Order>()
    .Where(o => o.Status == OrderStatus.Pending)
    .CountAsync();

// Check if any row matches
bool exists = await _db.Query<Order>()
    .Where(o => o.Id == orderId)
    .ExistsAsync();

// CountAsync counts the full filtered set, independent of Limit / Offset
long total = await _db.Query<Order>().Where(o => o.IsActive).CountAsync();
```

#### Combined Real-World Example

```csharp
// Page 2 of active high-value orders for a set of users, with total count
var userIds = new List<Guid> { /* ... */ };

var orders = await _db.Query<Order>()
    .Where(o => o.IsActive)
    .Where(o => o.TotalAmount > 500m || o.Priority == Priority.High)
    .Where(o => userIds.Contains(o.UserId))
    .OrderByDescending(o => o.CreatedAt)
    .ThenBy(o => o.OrderNumber)
    .WithPaging(page: 2, pageSize: 25)
    .ToListAsync();

long totalCount = await _db.Query<Order>()
    .Where(o => o.IsActive)
    .Where(o => o.TotalAmount > 500m || o.Priority == Priority.High)
    .Where(o => userIds.Contains(o.UserId))
    .CountAsync();
```

---

### Raw SQL Queries

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
```

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

---

## Execute Methods

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

### ExecuteScalar

Returns the first column of the first row from a query result.

```csharp
// Get count
int count = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users WHERE is_active = true");

// Get sum with parameters
decimal totalRevenue = await _db.ExecuteScalarAsync<decimal>(
    "SELECT COALESCE(SUM(total_amount), 0) FROM orders WHERE status = @Status",
    new { Status = OrderStatus.Delivered });

// Get max value
decimal maxPrice = await _db.ExecuteScalarAsync<decimal>("SELECT MAX(price) FROM products");

// Get single value
string? email = await _db.ExecuteScalarAsync<string>(
    "SELECT email FROM users WHERE id = @UserId",
    new { UserId = userId });

// Returns null/default when no results
int? result = await _db.ExecuteScalarAsync<int?>(
    "SELECT quantity FROM products WHERE id = @Id",
    new { Id = Guid.NewGuid() }); // null if not found

// Check existence
bool exists = await _db.ExecuteScalarAsync<bool>(
    "SELECT EXISTS(SELECT 1 FROM users WHERE email = @Email)",
    new { Email = "test@example.com" });

// Sync version
int count = _db.ExecuteScalar<int>("SELECT COUNT(*) FROM users");
```

### ExecuteReader

Returns a data reader for low-level row-by-row processing.

```csharp
// Read rows manually with async reader
await using var reader = await _db.ExecuteReaderAsync(
    "SELECT id, username, email FROM users WHERE is_active = @IsActive ORDER BY username",
    new { IsActive = true });

var users = new List<(Guid Id, string Username, string Email)>();
while (await reader.ReadAsync())
{
    users.Add((
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2)
    ));
}

// Sync version
using var reader = _db.ExecuteReader("SELECT name, price FROM products ORDER BY price");

while (reader.Read())
{
    Console.WriteLine($"{reader.GetString(0)}: ${reader.GetDecimal(1)}");
}

// Process large datasets without loading all into memory
await using var reader = await _db.ExecuteReaderAsync(
    "SELECT * FROM large_table WHERE created_at > @Since",
    new { Since = DateTime.UtcNow.AddDays(-30) });

while (await reader.ReadAsync())
{
    await ProcessRowAsync(reader);
}
```

### GetDataTable

Returns query results as a DataTable for scenarios requiring tabular data manipulation.

```csharp
// Get data as DataTable
var dataTable = await _db.GetDataTableAsync(
    "SELECT name, price, quantity FROM products WHERE is_active = true ORDER BY name");

Console.WriteLine($"Rows: {dataTable.Rows.Count}");
Console.WriteLine($"Columns: {string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

// Access data
foreach (DataRow row in dataTable.Rows)
{
    Console.WriteLine($"{row["name"]}: ${row["price"]} ({row["quantity"]} in stock)");
}

// With parameters
var salesReport = await _db.GetDataTableAsync(
    """
    SELECT 
        DATE(created_at) as sale_date,
        COUNT(*) as order_count,
        SUM(total_amount) as total_sales
    FROM orders
    WHERE created_at BETWEEN @StartDate AND @EndDate
    GROUP BY DATE(created_at)
    ORDER BY sale_date
    """,
    new { StartDate = startDate, EndDate = endDate });

// Export to CSV, Excel, or other formats
ExportToCsv(salesReport, "sales_report.csv");

// Sync version
var dataTable = _db.GetDataTable("SELECT * FROM categories ORDER BY sort_order");

// Handle NULL values
var products = await _db.GetDataTableAsync("SELECT name, description FROM products");
foreach (DataRow row in products.Rows)
{
    string name = row["name"].ToString()!;
    string description = row["description"] == DBNull.Value ? "N/A" : row["description"].ToString()!;
}
```

### When to Use Each Method

| Method | Use Case |
|--------|----------|
| `Execute` | INSERT, UPDATE, DELETE operations; need affected row count |
| `ExecuteScalar` | Single value queries (COUNT, SUM, MAX, EXISTS, single column) |
| `ExecuteReader` | Large datasets; streaming; row-by-row processing |
| `GetDataTable` | Reporting; data export; grid binding; dynamic column handling |

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

### QueryMultipleList Fluent Builder

`QueryMultipleList<T>()` returns a `DbMultiQueryList<T>` that can auto-generate
multi-SELECT SQL from entity metadata — no SQL strings needed. The builder reads
`[Table]`, `[Key]`, `[ResultSet(ForeignKey)]`, and column mappings to produce the
parent SELECT plus one child SELECT per `[ResultSet]` property.

Child SELECTs are automatically filtered with a `WHERE fk IN (SELECT pk FROM parent ...)`
subquery that mirrors the parent's WHERE / ORDER BY / LIMIT / OFFSET conditions.

#### SQL-Free Usage (Recommended)

```csharp
// Simplest form — returns all parents with all children mapped
var orders = await _db.QueryMultipleList<Order>().ToListAsync();

// With WHERE — children are automatically filtered to match
var orders = await _db.QueryMultipleList<Order>()
    .Where(o => o.Status == OrderStatus.Active)
    .Where(o => o.TotalAmount > 100m)
    .OrderByDescending(o => o.CreatedOn)
    .Limit(50)
    .ToListAsync();

// With pagination
var page3 = await _db.QueryMultipleList<Order>()
    .Where(o => o.UserId == userId)
    .OrderBy(o => o.CreatedOn)
    .WithPaging(page: 3, pageSize: 25)
    .ToListAsync();

// Sync version
var orders = _db.QueryMultipleList<Order>()
    .Where(o => o.IsActive)
    .ToList();
```

#### Builder Methods

| Method | Description |
|--------|-------------|
| `.Sql(sql)` | Use raw SQL instead of auto-generation |
| `.Parameters(obj)` | Parameters for raw SQL mode |
| `.Where(predicate)` | Add WHERE predicate; multiple calls combined with `AND` |
| `.OrderBy(prop)` | Primary ascending `ORDER BY` |
| `.OrderByDescending(prop)` | Primary descending `ORDER BY` |
| `.ThenBy(prop)` | Secondary ascending sort |
| `.ThenByDescending(prop)` | Secondary descending sort |
| `.Limit(n)` | Maximum parent rows (`LIMIT n`) |
| `.Offset(n)` | Skip parent rows (`OFFSET n`) |
| `.WithPaging(page, pageSize)` | 1-based page-number pagination |

#### Terminal Methods

| Method | Returns |
|--------|---------|
| `ToListAsync()` | `Task<List<T>>` — parents with children populated |
| `ToList()` | `List<T>` — sync version |

> **Note:** `.Sql()` and expression-based methods (`.Where()`, `.OrderBy()`, etc.) are
> independent modes. When `.Sql()` is set, the raw SQL is used as-is and expression
> methods are ignored.

The same [WHERE patterns](#supported-where-patterns) supported by `DbQuery<T>` are
available here: equality, comparisons, boolean shorthand, null checks, logical operators,
string methods, ILIKE, LOWER/UPPER, collection membership (ANY), enum auto-cast, and
captured variables.

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

### QueryWithJoin Fluent Builder

`QueryWithJoin<TParent, TChild>()` and `QueryWithJoin<TParent, TChild1, TChild2>()`
return fluent builders that can auto-generate JOIN SQL from entity metadata. The builder
discovers the foreign key relationship from the `[ResultSet(ForeignKey)]` attribute on
the parent's collection property and builds the `LEFT JOIN` clause automatically.

In SQL-free mode, `ParentKey()`, `ChildKey()`, `SplitOn()`, and even `ChildSelector()`
are auto-derived from metadata — you only need to call the expression methods you want.

#### SQL-Free — Single Child Collection

```csharp
// Minimal — everything auto-derived from [ResultSet] metadata
var orders = await _db.QueryWithJoin<Order, OrderLine>()
    .ToListAsync();

// With explicit child selector and filtering
var orders = await _db.QueryWithJoin<Order, OrderLine>()
    .ChildSelector(o => o.Lines)
    .Where(o => o.Status == OrderStatus.Active)
    .OrderByDescending(o => o.CreatedOn)
    .Limit(50)
    .ToListAsync();

// Sync version
var orders = _db.QueryWithJoin<Order, OrderLine>()
    .Where(o => o.UserId == userId)
    .ToList();
```

#### SQL-Free — Two Child Collections

```csharp
// Minimal — auto-derived
var orders = await _db
    .QueryWithJoin<Order, OrderLine, OrderNote>()
    .Where(o => o.Status == OrderStatus.Active)
    .OrderBy(o => o.CreatedOn)
    .ToListAsync();

// With explicit child selectors
var orders = await _db
    .QueryWithJoin<Order, OrderLine, OrderNote>()
    .ChildSelector1(o => o.Lines)
    .ChildSelector2(o => o.Notes)
    .Where(o => o.TotalAmount > 100m)
    .ToListAsync();
```

#### Raw SQL Mode

When `.Sql()` is called, the builder uses raw SQL and requires explicit configuration
of `ChildSelector`, `ParentKey`, `ChildKey`, and `SplitOn` — the same as calling the
direct `QueryWithJoin` / `QueryWithJoinAsync` methods.

```csharp
// Raw SQL — single child
var orders = await _db.QueryWithJoin<Order, OrderLine>()
    .Sql("""
        SELECT o.*, l.*
        FROM orders o
        LEFT JOIN order_lines l ON l.order_id = o.id
        WHERE o.user_id = @UserId
        """)
    .ChildSelector(o => o.Lines)
    .ParentKey(o => o.Id)
    .ChildKey(l => l.Id)
    .SplitOn("Id")
    .Parameters(new { UserId = userId })
    .ToListAsync();

// Raw SQL — two children
var orders = await _db
    .QueryWithJoin<Order, OrderLine, OrderNote>()
    .Sql("""
        SELECT o.*, l.*, n.*
        FROM orders o
        LEFT JOIN order_lines l ON l.order_id = o.id
        LEFT JOIN order_notes n ON n.order_id = o.id
        """)
    .ChildSelector1(o => o.Lines)
    .ChildSelector2(o => o.Notes)
    .ParentKey(o => o.Id)
    .ChildKey1(l => l.Id)
    .ChildKey2(n => n.Id)
    .SplitOn("Id,Id")
    .ToListAsync();
```

#### Builder Methods — Single Child (`DbJoinQuery<TParent, TChild>`)

| Method | Description |
|--------|-------------|
| `.Sql(sql)` | Use raw SQL instead of auto-generation |
| `.ChildSelector(expr)` | Set child collection property; auto-derived if omitted |
| `.ParentKey(expr)` | Set parent key selector; auto-derived in SQL-free mode |
| `.ChildKey(expr)` | Set child key selector; auto-derived in SQL-free mode |
| `.SplitOn(col)` | Set split column; auto-derived in SQL-free mode |
| `.Parameters(obj)` | Parameters for raw SQL mode |
| `.Where(predicate)` | Add WHERE predicate on parent; SQL-free mode only |
| `.OrderBy(prop)` | Primary ascending `ORDER BY` |
| `.OrderByDescending(prop)` | Primary descending `ORDER BY` |
| `.ThenBy(prop)` / `.ThenByDescending(prop)` | Secondary sort |
| `.Limit(n)` / `.Offset(n)` | Pagination |
| `.WithPaging(page, pageSize)` | 1-based page-number pagination |

#### Builder Methods — Two Children (`DbJoinQuery<TParent, TChild1, TChild2>`)

Same as above, with `ChildSelector1` / `ChildSelector2`, `ChildKey1` / `ChildKey2`.

#### Terminal Methods

| Method | Returns |
|--------|---------|
| `ToListAsync()` | `Task<List<TParent>>` — parents with children populated |
| `ToList()` | `List<TParent>` — sync version |

### QueryMultipleList vs QueryWithJoin

| Feature | QueryMultipleList | QueryWithJoin |
|---------|-------------------|---------------|
| SQL Queries | Multiple SELECT statements | Single SELECT with JOINs |
| SQL-Free Mode | ✅ Auto-generates from metadata | ✅ Auto-generates from metadata |
| Database Roundtrips | 1 (sends all queries together) | 1 |
| Configuration | Attribute-based ([ResultSet]) | Lambda-based or auto-derived |
| Deduplication | Automatic (FK-based) | Automatic (PK-based) |
| Max Children | Unlimited (based on result sets) | 2 (current implementation) |
| Expression Filters | `.Where()`, `.OrderBy()`, etc. | `.Where()`, `.OrderBy()`, etc. |
| Best For | Known entity structures | Ad-hoc queries, simple JOINs |

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

WebVella.Database uses **Microsoft.Extensions.Caching.Hybrid** for modern, high-performance caching with async-first operations and tag-based invalidation.

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
// First call: fetches from database and caches with table tag
var categories = await _db.GetListAsync<Category>();

// Second call: returns from cache
var categories = await _db.GetListAsync<Category>();

// Insert/Update/Delete automatically invalidates cache by table tag
await _db.InsertAsync(new Category { Name = "New Category" });
// Cache is invalidated via tag: "table:categories"

// Next call fetches fresh data
var categories = await _db.GetListAsync<Category>();
```

### HybridCache Features

WebVella.Database leverages HybridCache for:

- **Async-first**: All cache operations are fully async for better scalability
- **Tag-based invalidation**: Automatic invalidation of all cached entries for a table
- **Distributed cache ready**: Built-in support for L1 (in-memory) and L2 (distributed) caching
- **Stampede protection**: Prevents cache stampede when multiple requests hit the same key
- **RLS-aware**: Cache keys include RLS context to isolate cached data per tenant/user

### Cache Registration

```csharp
// Enable caching during registration
builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);

// Or disable caching
builder.Services.AddWebVellaDatabase(connectionString, enableCaching: false);
```

### Cache Invalidation

Cache invalidation happens automatically:

```csharp
// Automatic invalidation on mutations
await _db.InsertAsync(product);  // Invalidates "table:products" tag
await _db.UpdateAsync(product);  // Invalidates "table:products" tag
await _db.DeleteAsync<Product>(id); // Invalidates "table:products" tag

// All cached queries for products are now invalidated
```

### RLS-Aware Caching

When using Row Level Security, cache keys include the RLS context:

```csharp
// User A (entity_id = "tenant-1")
var products = await _db.GetListAsync<Product>(); // Cached with key containing "tenant-1"

// User B (entity_id = "tenant-2")
var products = await _db.GetListAsync<Product>(); // Different cache entry for "tenant-2"
```
```

### Enabling Cache

```csharp
// Enable caching in service registration
builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);
```

---

## Row Level Security

WebVella.Database provides built-in support for PostgreSQL Row Level Security (RLS) through automatic session variable injection. This allows you to implement entity-based data isolation and role-based access control at the database level.

### Key Features

- **Automatic session context** - Security context is automatically set on each connection
- **PostgreSQL native RLS** - Leverages PostgreSQL's built-in row-level security policies
- **Flexible context provider** - Implement your own context provider to integrate with any authentication system
- **Custom claims support** - Pass additional claims beyond the entity identifier
- **Transaction-aware** - Session variables respect transaction boundaries

### How It Works

1. Implement `IRlsContextProvider` to provide security context (entity ID and custom claims)
2. Register the provider with `AddWebVellaDatabaseWithRls<T>()`
3. Each database connection automatically sets PostgreSQL session variables
4. PostgreSQL RLS policies use `current_setting('app.variable_name')` to filter data

### Implementing an RLS Context Provider

```csharp
using WebVella.Database.Security;

public class HttpRlsContextProvider : IRlsContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRlsContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? EntityId => GetClaim("entity_id");

    public IReadOnlyDictionary<string, string> CustomClaims => new Dictionary<string, string>
    {
        ["role"] = GetClaim("role") ?? "user",
        ["department"] = GetClaim("department") ?? string.Empty
    };

    private string? GetClaim(string claimType) =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
}
```

### Service Registration

```csharp
// Basic RLS registration
builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(connectionString);

// With caching enabled
builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(
    connectionString,
    enableCaching: true);

// With custom RLS options
builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(
    connectionString,
    enableCaching: false,
    rlsOptions: new RlsOptions
    {
        SettingName = "myapp.user_id", // Default: "app.user_id"
        Enabled = true,                 // Default: true
        SqlUser = "app_user",          // PostgreSQL role for RLS
        SqlPassword = "password"       // Password for RLS role
    });

// With factory pattern
builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(
    sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!,
    enableCaching: true);

// With custom provider factory
builder.Services.AddWebVellaDatabaseWithRls(
    connectionString,
    rlsContextProviderFactory: sp => new CustomRlsProvider(sp),
    enableCaching: true);
```

### PostgreSQL RLS Policy Setup

Create RLS policies in your database that reference the session variables:

```sql
-- Enable RLS on table
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;

-- Force RLS for table owner (optional but recommended)
ALTER TABLE orders FORCE ROW LEVEL SECURITY;

-- Create policy for entity isolation
CREATE POLICY entity_isolation ON orders
    USING (entity_id = current_setting('app.entity_id', true));

-- Create policy using custom claims
CREATE POLICY admin_access ON orders
    FOR ALL
    USING (current_setting('app.role', true) = 'admin');

-- Combined policy example
CREATE POLICY order_access ON orders
    USING (
        entity_id = current_setting('app.entity_id', true)
        OR current_setting('app.role', true) = 'admin'
    );
```

### RLS Options

| Option | Default | Description |
|--------|---------|-------------|
| `SettingName` | `"app.user_id"` | Full PostgreSQL session variable name for the entity identifier (e.g., `app.user_id`). Variables are always transaction-scoped (LOCAL). |
| `Enabled` | `true` | Set to `false` to bypass RLS (useful for admin/migration scenarios) |

### Session Variables Reference

When `RlsOptions.SettingName = "app.user_id"` (default):

| Context Property | PostgreSQL Variable | Access in Policy |
|------------------|---------------------|------------------|
| `EntityId` | `app.user_id` | `current_setting('app.user_id', true)` |
| Custom claim "role" | `app.role` | `current_setting('app.role', true)` |

### Bypassing RLS for Admin Operations

```csharp
// Option 1: Use NullRlsContextProvider
var adminDbService = new DbService(
    connectionString,
    NullDbEntityCache.Instance,
    NullRlsContextProvider.Instance,  // No RLS context
    null);

// Option 2: Disable in options
var options = new RlsOptions { Enabled = false };

// Option 3: Create a separate service for admin operations
builder.Services.AddKeyedScoped<IDbService>("admin", (sp, key) =>
{
    var cache = sp.GetRequiredService<IDbEntityCache>();
    return new DbService(connectionString, cache, null, null);
});
```

### Database Migration for RLS

Create a migration to set up RLS policies:

```csharp
[DbMigration("1.1.0.0")]
public class EnableRowLevelSecurity : DbMigration
{
    public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
    {
        return Task.FromResult("""
            -- Add entity_id column if not exists
            ALTER TABLE orders ADD COLUMN IF NOT EXISTS entity_id TEXT;

            -- Enable RLS
            ALTER TABLE orders ENABLE ROW LEVEL SECURITY;
            ALTER TABLE orders FORCE ROW LEVEL SECURITY;

            -- Create entity isolation policy
            DROP POLICY IF EXISTS entity_isolation ON orders;
            CREATE POLICY entity_isolation ON orders
                USING (entity_id = current_setting('app.entity_id', true));

            -- Create index for performance
            CREATE INDEX IF NOT EXISTS idx_orders_entity ON orders(entity_id);
            """);
    }
}
```

### Temporarily Disabling RLS

Use `DisableRls` / `EnableRls` (and their async counterparts) when you need to run queries without RLS
restrictions inside the same service instance — for example, during admin operations or background jobs
that run in a context where an `IRlsContextProvider` is registered but should be bypassed for a specific
block of code.

```csharp
// Temporarily bypass RLS for an admin lookup
await db.DisableRlsAsync();
try
{
    var allOrders = await db.GetListAsync<Order>();
}
finally
{
    await db.EnableRlsAsync();
}
```

When called inside an active transaction scope the change applies to the transaction connection
immediately. For connections created after the call, RLS is suppressed (or restored) for the remainder
of the current async context.

> **Note**: This is a low-level escape hatch. Prefer dedicated admin service instances
> (`NullRlsContextProvider` or a separate `DbService` registration) where possible.

---

### Best Practices for RLS

1. **Always use `current_setting(name, true)`** - The second parameter returns NULL instead of throwing an error when the variable is not set

2. **Create indexes on RLS columns** - Policies add filter conditions to every query; indexes are essential for performance

3. **Test with different contexts** - Verify that policies correctly filter data for different tenants/users

4. **Use FORCE ROW LEVEL SECURITY** - Ensures RLS applies even to table owners

5. **Consider policy performance** - Complex policies can impact query performance; keep them simple

6. **Handle missing context gracefully** - Design policies to handle NULL values appropriately:
   ```sql
   -- Returns no rows if entity_id is not set
   USING (entity_id = current_setting('app.entity_id', true));

   -- Returns all rows if entity_id is not set (dangerous!)
   USING (entity_id = COALESCE(current_setting('app.entity_id', true), entity_id));
   ```

### RLS-Aware Caching

When using both RLS and entity caching (`[Cacheable]`), the cache automatically includes the RLS context in cache keys. This ensures **complete data isolation between tenants/users**:

```
// Without RLS context:
Entity:MyApp.Product:Id:abc123

// With entity context:
Entity:MyApp.Product:Id:abc123:Rls:e:my-entity-value

// With custom claims (e.g., role-based access):
Entity:MyApp.Product:Id:abc123:Rls:e:my-entity-value|c:department:sales,role:manager
```

**How it works:**
- Cache keys automatically include entity ID and custom claims from `IRlsContextProvider`
- Different entities/roles get separate cache entries for the same record
- Custom claims are sorted alphabetically for consistent key generation
- Cache invalidation still works per-entity-type (invalidates all context variants)

**Important considerations:**

1. **Memory usage** - Each unique RLS context combination creates separate cache entries. For high-cardinality scenarios (many custom claims), consider shorter cache durations or disabling caching

2. **Global data** - For entities shared across all tenants (e.g., countries, currencies), consider:
   - Not using RLS for these tables
   - Using a separate `DbService` instance without RLS for global data queries

3. **Cache invalidation** - When an entity is modified, all cached versions (across all contexts) are invalidated to ensure consistency

---

## Database Migrations

WebVella.Database provides a simple yet powerful migration system for managing database schema changes. Migrations are version-controlled, executed in order, and support rollback on failure.

### Key Features

- **Automatic discovery** - Migrations are discovered by scanning all loaded assemblies
- **Version ordering** - Migrations are executed in ascending version order
- **Transactional execution** - Each migration runs in a transaction with automatic rollback on failure
- **Detailed logging** - Each SQL statement is logged with success/failure status
- **Pre/Post-migration hooks** - Execute custom .NET code before and after SQL migration
- **Embedded SQL support** - Load SQL from embedded resource files
- **RLS bypass** - Migrations automatically run without RLS context for unrestricted schema access

### Migration Service Registration

```csharp
// Basic registration with default options
builder.Services.AddWebVellaDatabase(connectionString);
builder.Services.AddWebVellaDatabaseMigrations();

// With custom table names
builder.Services.AddWebVellaDatabaseMigrations(options =>
{
    options.VersionTableName = "_my_db_version";
    options.UpdateFunctionName = "_my_db_update";
    options.UpdateLogTableName = "_my_db_update_log";
});

// Or pass options directly
builder.Services.AddWebVellaDatabaseMigrations(new DbMigrationOptions
{
    VersionTableName = "_custom_version"
});
```

> **Note:** When using RLS (`AddWebVellaDatabaseWithRls`), the migration service automatically creates
> a separate database connection without RLS context. This ensures migrations can modify schema and
> seed data without being affected by tenant or user isolation policies.

### Creating Migrations

#### Method 1: Override GenerateSqlAsync

Create a class that inherits from `DbMigration` and apply the `[DbMigration]` attribute with a version string:

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
                username VARCHAR(100) NOT NULL,
                password_hash VARCHAR(255) NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX idx_users_email ON users(email);
            CREATE INDEX idx_users_username ON users(username);
            """);
    }
}

[DbMigration("1.0.1.0")]
public class AddUserProfile : DbMigration
{
    public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
    {
        return Task.FromResult("""
            ALTER TABLE users ADD COLUMN profile_image_url TEXT;
            ALTER TABLE users ADD COLUMN bio TEXT;
            ALTER TABLE users ADD COLUMN updated_at TIMESTAMP;
            """);
    }
}
```

#### Method 2: Embedded SQL Resource Files

For larger migrations, store SQL in embedded resource files. The base `GenerateSqlAsync` implementation automatically loads SQL from a file named `{FullTypeName}.Script.sql`:

```csharp
// Migration class - no override needed
[DbMigration("1.0.2.0")]
public class CreateOrdersTables : DbMigration { }
```

Create an embedded resource file `CreateOrdersTables.Script.sql` in the same namespace:

```sql
-- CreateOrdersTables.Script.sql
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    order_number VARCHAR(50) NOT NULL,
    total_amount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    status INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE order_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_name VARCHAR(255) NOT NULL,
    quantity INTEGER NOT NULL DEFAULT 1,
    unit_price DECIMAL(18, 2) NOT NULL
);

CREATE INDEX idx_orders_user ON orders(user_id);
CREATE INDEX idx_orders_number ON orders(order_number);
```

**Important:** Mark the SQL file as an embedded resource in your `.csproj`:

```xml
<ItemGroup>
    <EmbeddedResource Include="Migrations\CreateOrdersTables.Script.sql" />
</ItemGroup>
```

### Pre-Migration Logic

Use `PreMigrateAsync` to execute custom .NET code before migration SQL runs. It is called
inside the migration transaction, so a failure here rolls back the entire migration.

```csharp
[DbMigration("1.0.3.0")]
public class RebuildStatusIndex : DbMigration
{
    public override async Task PreMigrateAsync(IServiceProvider serviceProvider)
    {
        // Drop the old index before adding the new compound one — avoids duplicate index
        // during the migration and is faster for large tables.
        var db = serviceProvider.GetRequiredService<IDbService>();
        await db.ExecuteAsync("DROP INDEX IF EXISTS idx_orders_status;");
    }

    public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
    {
        return Task.FromResult("""
            ALTER TABLE orders ADD COLUMN priority INTEGER NOT NULL DEFAULT 0;
            CREATE INDEX idx_orders_status_priority ON orders(status, priority);
            """);
    }
}
```

Typical uses:
- Drop an index or constraint before a bulk schema change
- Disable triggers during a data transformation
- Validate preconditions and throw to abort the migration early
- Archive rows before a destructive change

### Post-Migration Logic

Use `PostMigrateAsync` to execute custom .NET code after SQL migration completes:

```csharp
[DbMigration("1.0.3.0")]
public class SeedInitialData : DbMigration
{
    public override Task<string> GenerateSqlAsync(IServiceProvider serviceProvider)
    {
        return Task.FromResult("""
            CREATE TABLE settings (
                key VARCHAR(100) PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """);
    }

    public override async Task PostMigrateAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<IDbService>();

        await db.ExecuteAsync("""
            INSERT INTO settings (key, value) VALUES
            ('app_version', '1.0.0'),
            ('maintenance_mode', 'false'),
            ('max_upload_size', '10485760')
            """);
    }
}
```

### Running Migrations

Execute pending migrations at application startup:

```csharp
var app = builder.Build();

// Run migrations before handling requests
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDbMigrationService>();
    await migrationService.ExecutePendingMigrationsAsync();
}

app.Run();
```

### Checking Current Version

```csharp
var migrationService = serviceProvider.GetRequiredService<IDbMigrationService>();
var currentVersion = await migrationService.GetCurrentDbVersionAsync();
Console.WriteLine($"Database version: {currentVersion}");
```

### Migration Options

| Option | Default | Description |
|--------|---------|-------------|
| `VersionTableName` | `_db_version` | Table that stores the current database version |
| `UpdateFunctionName` | `_db_update` | Temporary PostgreSQL function name used during migration |
| `UpdateLogTableName` | `_db_update_log_tbl` | Temporary table for logging migration statements |

### Handling Migration Errors

When a migration fails, a `DbMigrationException` is thrown with detailed logs:

```csharp
try
{
    await migrationService.ExecutePendingMigrationsAsync();
}
catch (DbMigrationException ex)
{
    Console.WriteLine($"Migration failed: {ex.Message}");

    foreach (var log in ex.MigrationLogs)
    {
        var status = log.Success ? "✓" : "✗";
        Console.WriteLine($"  [{status}] Version {log.Version}: {log.Statement}");
        if (!log.Success && log.SqlError != null)
        {
            Console.WriteLine($"      Error: {log.SqlError}");
        }
    }
}
```

### Migration Best Practices

1. **Use semantic versioning** - Follow `major.minor.build.revision` format (e.g., "1.0.0.0", "1.2.3.0")

2. **One concern per migration** - Keep migrations focused on a single schema change

3. **Make migrations idempotent** - Use `IF NOT EXISTS` and `IF EXISTS` clauses:
   ```sql
   CREATE TABLE IF NOT EXISTS users (...);
   ALTER TABLE users ADD COLUMN IF NOT EXISTS email TEXT;
   DROP TABLE IF EXISTS old_table;
   ```

4. **Never modify past migrations** - Create new migrations for changes; don't edit existing ones

5. **Test migrations** - Run migrations against a copy of production data before deploying

6. **Keep migrations in version control** - Migrations are code and should be tracked

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
| `Query<T>()` | Create fluent expression-based query builder (`DbQuery<T>`) |
| `QueryWithJoin<TParent, TChild>()` | Create fluent JOIN query builder (`DbJoinQuery`) — SQL-free or raw SQL |
| `QueryWithJoin<TParent, TChild1, TChild2>()` | Create fluent JOIN query builder with two children |
| `QueryMultiple<T>()` | Create fluent multi-result query builder (`DbMultiQuery<T>`) |
| `QueryMultipleList<T>()` | Create fluent parent-child multi-query builder (`DbMultiQueryList<T>`) — SQL-free or raw SQL |
| `Query<T>(sql, params)` | Execute raw SQL query and return collection |
| `QueryAsync<T>(sql, params)` | Async version of raw SQL Query |
| `QueryMultiple<T>(sql, params)` | Execute multi-result query into container (raw SQL) |
| `QueryMultipleAsync<T>(sql, params)` | Async version of QueryMultiple |
| `QueryMultipleList<T>(sql, params)` | Execute multi-result query with parent-child mapping (raw SQL) |
| `QueryMultipleListAsync<T>(sql, params)` | Async version of QueryMultipleList |
| `QueryWithJoin<P, C>(sql, ...)` | Execute JOIN query with single child collection (raw SQL) |
| `QueryWithJoinAsync<P, C>(sql, ...)` | Async version with single child |
| `QueryWithJoin<P, C1, C2>(sql, ...)` | Execute JOIN query with two child collections (raw SQL) |
| `QueryWithJoinAsync<P, C1, C2>(sql, ...)` | Async version with two children |
| `Execute` | Execute command, return affected rows |
| `ExecuteAsync` | Async version of Execute |
| `ExecuteReader` | Execute query and return IDataReader |
| `ExecuteReaderAsync` | Async version, returns DbDataReader |
| `ExecuteScalar<T>` | Execute query and return first column of first row |
| `ExecuteScalarAsync<T>` | Async version of ExecuteScalar |
| `GetDataTable` | Execute query and return DataTable |
| `GetDataTableAsync` | Async version of GetDataTable |
| `Insert<T>(T)` | Insert entity, return inserted entity with generated keys |
| `Insert<T>(object)` | Insert from anonymous object with property mapping and type validation |
| `InsertAsync<T>` | Async versions of Insert (2 overloads) |
| `Update<T>(T)` | Update entity (all or specific properties) |
| `Update<T>(object)` | Partial update from anonymous object; requires key properties |
| `UpdateAsync<T>` | Async versions of Update (2 overloads) |
| `Delete<T>(T)` | Delete entity by entity reference |
| `Delete<T>(Guid)` | Delete entity by single key |
| `Delete<T>(Dictionary)` | Delete entity by composite key using dictionary |
| `Delete<T>(object)` | Delete entity by composite key using anonymous object |
| `DeleteAsync<T>` | Async versions of Delete (4 overloads) |
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
| `ConnectionString` | Get the PostgreSQL connection string used by this service instance |
| `EnableRls` | Re-apply RLS session variables on the current connection/context |
| `EnableRlsAsync` | Async version of EnableRls |
| `DisableRls` | Reset RLS session variables to empty strings for the current async context |
| `DisableRlsAsync` | Async version of DisableRls |

### DbQuery\<T\> Builder

Obtained via `IDbService.Query<T>()`. Chain builder methods, then call a terminal method.

#### Builder Methods

| Method | Description |
|--------|-------------|
| `.Where(predicate)` | Add WHERE predicate; multiple calls combined with `AND` |
| `.OrderBy(prop)` | Add primary ascending `ORDER BY` |
| `.OrderByDescending(prop)` | Add primary descending `ORDER BY` |
| `.ThenBy(prop)` | Append secondary ascending sort |
| `.ThenByDescending(prop)` | Append secondary descending sort |
| `.Limit(n)` | Set `LIMIT` (n ≥ 0) |
| `.Offset(n)` | Set `OFFSET` (n ≥ 0) |
| `.WithPaging(page, pageSize)` | Set `LIMIT` / `OFFSET` from 1-based page number |

#### Terminal Methods

| Method | SQL | Returns |
|--------|-----|---------|
| `ToListAsync()` / `ToList()` | `SELECT * FROM ...` | `IEnumerable<T>` |
| `FirstOrDefaultAsync()` / `FirstOrDefault()` | `SELECT ... LIMIT 1` | `T?` |
| `CountAsync()` / `Count()` | `SELECT COUNT(*) FROM ...` | `long` |
| `ExistsAsync()` / `Exists()` | `SELECT EXISTS(SELECT 1 ...)` | `bool` |

### DbMultiQueryList\<T\> Builder

Obtained via `IDbService.QueryMultipleList<T>()`. Supports SQL-free and raw SQL modes.

#### Builder Methods

| Method | Description |
|--------|-------------|
| `.Sql(sql)` | Use raw SQL (disables auto-generation) |
| `.Parameters(obj)` | Parameters for raw SQL mode |
| `.Where(predicate)` | Add WHERE predicate; multiple calls combined with `AND` |
| `.OrderBy(prop)` | Primary ascending `ORDER BY` |
| `.OrderByDescending(prop)` | Primary descending `ORDER BY` |
| `.ThenBy(prop)` / `.ThenByDescending(prop)` | Secondary sort |
| `.Limit(n)` | Set `LIMIT` (n ≥ 0) |
| `.Offset(n)` | Set `OFFSET` (n ≥ 0) |
| `.WithPaging(page, pageSize)` | 1-based page-number pagination |

#### Terminal Methods

| Method | Returns |
|--------|---------|
| `ToListAsync()` | `Task<List<T>>` — parents with children populated |
| `ToList()` | `List<T>` — sync version |

### DbJoinQuery\<TParent, TChild\> Builder

Obtained via `IDbService.QueryWithJoin<TParent, TChild>()`. Supports SQL-free and raw
SQL modes.

#### Builder Methods

| Method | SQL-Free | Raw SQL | Description |
|--------|----------|---------|-------------|
| `.Sql(sql)` | — | Required | Use raw SQL |
| `.ChildSelector(expr)` | Auto-derived | Required | Child collection property |
| `.ParentKey(expr)` | Auto-derived | Required | Parent key selector |
| `.ChildKey(expr)` | Auto-derived | Required | Child key selector |
| `.SplitOn(col)` | Auto-derived | Optional | Column to split on |
| `.Parameters(obj)` | — | Optional | Raw SQL parameters |
| `.Where(predicate)` | ✅ | — | WHERE on parent |
| `.OrderBy(prop)` | ✅ | — | ORDER BY |
| `.OrderByDescending(prop)` | ✅ | — | ORDER BY DESC |
| `.ThenBy(prop)` / `.ThenByDescending(prop)` | ✅ | — | Secondary sort |
| `.Limit(n)` / `.Offset(n)` | ✅ | — | Pagination |
| `.WithPaging(page, pageSize)` | ✅ | — | 1-based pagination |

#### Terminal Methods

| Method | Returns |
|--------|---------|
| `ToListAsync()` | `Task<List<TParent>>` — parents with children populated |
| `ToList()` | `List<TParent>` — sync version |

### DbJoinQuery\<TParent, TChild1, TChild2\> Builder

Obtained via `IDbService.QueryWithJoin<TParent, TChild1, TChild2>()`. Same methods
as the single-child builder, with `ChildSelector1`/`ChildSelector2` and
`ChildKey1`/`ChildKey2`.

### IDbMigrationService Interface

| Method | Description |
|--------|-------------|
| `ExecutePendingMigrationsAsync` | Execute all pending migrations in version order |
| `GetCurrentDbVersionAsync` | Get the current database schema version |

### Migration Classes

| Class | Description |
|-------|-------------|
| `DbMigration` | Abstract base class for creating migrations |
| `DbMigrationAttribute` | Marks a class as a migration with a version |
| `DbMigrationOptions` | Configuration options for the migration service |
| `DbMigrationException` | Exception thrown when a migration fails |
| `DbMigrationLogItem` | Log entry for a single SQL statement execution |

### Row Level Security Classes

| Class/Interface | Description |
|-----------------|-------------|
| `IRlsContextProvider` | Interface for providing tenant ID, user ID, and custom claims |
| `RlsOptions` | Configuration options for RLS session context initialization |
| `NullRlsContextProvider` | Null implementation that provides no security context |

---

## Version History

### v1.3.1
- **`ConnectionString` property**: Added `string ConnectionString { get; }` to `IDbService` — exposes the
  PostgreSQL connection string used by the service instance
- **Runtime RLS Enable / Disable**: Added `EnableRls()` / `EnableRlsAsync()` and `DisableRls()` /
  `DisableRlsAsync()` to `IDbService` for temporarily suppressing or restoring RLS session variable
  injection within the current async context; when inside a transaction the change applies to the
  active connection immediately

---

### v1.3.0
- **Breaking**: `IRlsContextProvider` simplified — `Guid? TenantId` and `Guid? UserId` replaced with
  `string? EntityId` as the single primary RLS identifier
- **Breaking**: `RlsOptions.Prefix` renamed to `SettingName`; now holds the full PostgreSQL session
  variable name (default `"app.user_id"`); custom-claim namespace is derived from the part before the
  first dot
- **Breaking**: RLS cache key format changed from `t:<guid>|u:<guid>` to `e:<value>`

---

### v1.2.4
- **SQL-Free Fluent Builders for QueryMultipleList and QueryWithJoin**: Added expression-based
  query building to `DbMultiQueryList<T>` and `DbJoinQuery` — auto-generates SQL from entity
  metadata (`[Table]`, `[Key]`, `[ResultSet(ForeignKey)]`) without writing SQL strings
- **DbMultiQueryList\<T\> Builder**: New `.Where()`, `.OrderBy()`, `.OrderByDescending()`,
  `.ThenBy()`, `.ThenByDescending()`, `.Limit()`, `.Offset()`, `.WithPaging()` methods;
  child SELECTs are automatically filtered with `WHERE fk IN (SELECT pk FROM parent ...)`
  subqueries that mirror parent conditions
- **DbJoinQuery Builder**: New expression-based methods for single-child and two-child
  JOIN queries; `ChildSelector`, `ParentKey`, `ChildKey`, and `SplitOn` are auto-derived
  from `[ResultSet]` metadata in SQL-free mode
- **Table Alias Support**: `DbExpressionTranslator` now supports optional table alias
  prefixing for JOIN WHERE clauses (e.g., `p.customer_name = @p0`)
- **EntityMetadata.GetAliasedSelectColumns**: New internal method that generates aliased
  SELECT columns (e.g., `p.column_name AS "PropertyName"`) for JOIN SQL generation

---

### v1.2.3
- **Fluent Query Builder**: Added `IDbService.Query<T>()` returning `DbQuery<T>` — an
  expression-tree-based query builder that generates parameterised PostgreSQL SQL without
  writing SQL strings
- **ILike Extension Methods**: Added `DbStringExtensions` with `ILikeContains`,
  `ILikeStartsWith`, and `ILikeEndsWith` for case-insensitive `ILIKE` pattern matching inside
  `.Where()` predicates
- **PreMigrateAsync hook**: Added `DbMigration.PreMigrateAsync(IServiceProvider)` virtual
  method — called inside the migration transaction before SQL execution; override to drop
  indexes, disable triggers, or validate preconditions
- **Breaking change**: `DbQuery<T>.Take(int)` renamed to `Limit(int)` to align with
  PostgreSQL keyword
- **Breaking change**: `DbQuery<T>.Skip(int)` renamed to `Offset(int)` to align with
  PostgreSQL keyword

Supported WHERE patterns: `==` `!=` `<` `<=` `>` `>=` · `&&` `||` `!` ·
boolean shorthand · null checks · `.Contains()` `.StartsWith()` `.EndsWith()` (LIKE) ·
`.ILikeContains()` `.ILikeStartsWith()` `.ILikeEndsWith()` (ILIKE) ·
`.ToLower()` `.ToLowerInvariant()` `.ToUpper()` `.ToUpperInvariant()` (LOWER/UPPER) ·
`list.Contains(e.Prop)` / `Enumerable.Contains(list, e.Prop)` (ANY) ·
enum auto-cast to `int` · captured variables and closures

---

### v1.2.2
- Added `[DbColumn("name")]` attribute for specifying explicit database column names, overriding the default snake_case conversion
- Added `Insert<T>(object)` and `InsertAsync<T>(object)` for inserting entities from anonymous objects with property mapping and type validation
- Added `Update<T>(object)` and `UpdateAsync<T>(object)` for partial updates from anonymous objects — requires key properties, updates only the non-key properties present in the object


### v1.2.1
- **Enhanced Developer Experience**: Comprehensive documentation integration with IntelliSense-friendly examples
- **MSBuild Integration**: Added `.targets` file that automatically makes documentation available to consuming projects
- **AI/Copilot Integration**: Source Link support and AdditionalFiles integration for better AI tool support
- **Package Enhancements**: Improved package metadata, tags, and symbol packages for enhanced debugging
- **Documentation Updates**: Added quick reference guide and enhanced XML documentation with practical examples
- **Welcome Messages**: Helpful build-time guidance with quick-start examples for new users
- **Technical Improvements**: XML documentation generation and timezone test fixes

### v1.2.0
- Added Row Level Security (RLS) support with automatic session variable injection
- Added `IRlsContextProvider` interface for implementing custom security context providers
- Added `RlsOptions` for configuring RLS behavior (setting name, local settings, enable/disable)
- Added `NullRlsContextProvider` for scenarios where RLS should be bypassed
- Added `AddWebVellaDatabaseWithRls<T>()` extension methods for easy DI registration
- Session variables are automatically set on each connection for use in PostgreSQL RLS policies
- RLS-aware caching: cache keys include tenant ID, user ID, and custom claims for data isolation
- Migrations automatically bypass RLS for unrestricted schema and data access

### v1.1.0
- Added `ExecuteScalar<T>` and `ExecuteScalarAsync<T>` methods for single value queries
- Added `ExecuteReader` and `ExecuteReaderAsync` methods for low-level data reading
- Added `GetDataTable` and `GetDataTableAsync` methods for DataTable results
- Added database migration support with `IDbMigrationService`
- Added `DbMigration` base class for creating versioned migrations
- Added support for embedded SQL resource files in migrations
- Added `PostMigrateAsync` hook for post-migration .NET code execution

### v1.0.0
- Initial release with full CRUD support
- Transaction and advisory lock management
- Multi-query support (QueryMultiple, QueryMultipleList)
- QueryWithJoin for single-query parent-child mapping
- Entity caching with automatic invalidation
- JSON column support
- Composite key support
