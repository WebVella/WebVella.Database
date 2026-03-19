# WebVella.Database Quick Reference

## Installation
```bash
dotnet add package WebVella.Database
```

## Service Registration

### Basic Setup
```csharp
builder.Services.AddWebVellaDatabase("Host=localhost;Database=mydb;Username=user;Password=pass");
```

### With Caching
```csharp
builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);
```

### With Row Level Security (RLS)
```csharp
public class HttpRlsContextProvider : IRlsContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public HttpRlsContextProvider(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;
    public Guid? TenantId => GetClaimAsGuid("tenant_id");
    public Guid? UserId => GetClaimAsGuid("sub");
    public IReadOnlyDictionary<string, string> CustomClaims => new Dictionary<string, string> { ["role"] = GetClaim("role") ?? "user" };
    
    private Guid? GetClaimAsGuid(string type) => Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value, out var g) ? g : null;
    private string? GetClaim(string type) => _httpContextAccessor.HttpContext?.User?.FindFirst(type)?.Value;
}

builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(connectionString);
```

### With Migrations
```csharp
builder.Services.AddWebVellaDatabase(connectionString);
builder.Services.AddWebVellaDatabaseMigrations();

// Run at startup
using var scope = app.Services.CreateScope();
var migrationService = scope.ServiceProvider.GetRequiredService<IDbMigrationService>();
await migrationService.ExecutePendingMigrationsAsync();
```

## Entity Definition

```csharp
[Table("users")]
[Cacheable(DurationSeconds = 600)]
public class User
{
    [Key] // Auto-generated UUID
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    [JsonColumn] // Serialized as JSON
    public UserSettings? Settings { get; set; }
    
    [External] // Excluded from INSERT/UPDATE
    public List<Order>? Orders { get; set; }
}
```

## Basic CRUD Operations

```csharp
public class UserService
{
    private readonly IDbService _db;
    public UserService(IDbService db) => _db = db;
    
    // Create
    public async Task<User> CreateAsync(User user) => await _db.InsertAsync(user);
    
    // Read
    public async Task<User?> GetAsync(Guid id) => await _db.GetAsync<User>(id);
    public async Task<IEnumerable<User>> GetAllAsync() => await _db.GetListAsync<User>();
    
    // Update
    public async Task<bool> UpdateAsync(User user) => await _db.UpdateAsync(user);
    public async Task<bool> UpdateEmailAsync(User user) => await _db.UpdateAsync(user, ["Email"]);
    
    // Delete
    public async Task<bool> DeleteAsync(Guid id) => await _db.DeleteAsync<User>(id);
}
```

## Transactions

```csharp
// Simple transaction
await using var scope = await _db.CreateTransactionScopeAsync();
await _db.InsertAsync(user);
await _db.InsertAsync(order);
await scope.CompleteAsync();

// Nested transactions (automatic savepoints)
await using var outer = await _db.CreateTransactionScopeAsync();
{
    await _db.InsertAsync(user);
    
    await using var inner = await _db.CreateTransactionScopeAsync();
    {
        await _db.InsertAsync(order);
        await inner.CompleteAsync();
    }
    
    await outer.CompleteAsync();
}

// With advisory lock
await using var scope = await _db.CreateTransactionScopeAsync(lockKey: "inventory-update");
await _db.UpdateAsync(inventory);
await scope.CompleteAsync();
```

## Custom Queries

```csharp
// Query with parameters
var activeUsers = await _db.QueryAsync<User>(
    "SELECT * FROM users WHERE is_active = @IsActive",
    new { IsActive = true });

// Execute commands
var rowsAffected = await _db.ExecuteAsync(
    "UPDATE users SET last_login = @Now WHERE id = @Id",
    new { Now = DateTime.UtcNow, Id = userId });

// Scalar results
var userCount = await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");
```

## Key Features

| Feature | Description |
|---------|-------------|
| **Nested Transactions** | Automatic PostgreSQL savepoint management |
| **Advisory Locks** | Distributed coordination with simple scopes |
| **Row Level Security** | Automatic session context for multi-tenancy |
| **Entity Caching** | `[Cacheable]` attribute with RLS-aware keys |
| **JSON Columns** | `[JsonColumn]` for automatic serialization |
| **Migrations** | Version-controlled schema changes |
| **Composite Keys** | Support for multi-column primary keys |

## Documentation
- 📖 [Complete Documentation](https://github.com/WebVella/WebVella.Database/blob/main/docs/index.md)
- 📦 [NuGet Package](https://www.nuget.org/packages/WebVella.Database/)
- 📂 [GitHub Repository](https://github.com/WebVella/WebVella.Database)

## License
MIT License - See LICENSE file for details.