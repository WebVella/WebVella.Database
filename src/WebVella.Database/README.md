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
A lightweight, high-performance Postgres data access library built on Dapper. It simplifies data object mapping and complex database workflows by providing first-class support for nested transactions and effortless advisory lock management.

## How to get it
You can either clone this repository or get the [Nuget package](https://www.nuget.org/packages/WebVella.Database/)

## Please help by giving a star
GitHub stars guide developers toward great tools. If you find this project valuable, please give it a star – it helps the community and takes just a second!⭐


## Features

- **Dapper-based CRUD operations** - Simple Insert, Update, Delete, Get, and Query methods
- **Nested transaction support** - Create transaction scopes that properly handle nesting
- **PostgreSQL advisory locks** - Easy-to-use advisory lock scopes for distributed locking
- **Entity caching** - Optional in-memory caching with automatic invalidation
- **JSON column support** - Automatic serialization/deserialization of JSON columns
- **Attribute-based mapping** - Use attributes like `[Table]`, `[Key]`, `[JsonColumn]`, and more

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

    // Insert
    public async Task<Guid> CreateUserAsync(User user)
    {
        var keys = await _db.InsertAsync(user);
        return keys["Id"];
    }

    // Get by ID
    public async Task<User?> GetUserAsync(Guid id)
    {
        return await _db.GetAsync<User>(id);
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


## License
[![Library license details](https://img.shields.io/badge/%F0%9F%93%9C%0A%20read-license%20details-blue?style=for-the-badge)](https://github.com/WebVella/WebVella.Database/blob/main/LICENSE/)
