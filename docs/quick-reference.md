# WebVella.Database - Quick Reference

> **Note**: This is a condensed reference. See [webvella.database.quick-ref.md](webvella.database.quick-ref.md) for detailed quick reference or [webvella.database.docs.md](webvella.database.docs.md) for complete documentation.

## 🚀 Latest: v1.4.0 - HybridCache Migration
- ✨ Async-first caching with HybridCache v10.4.0
- ✨ Tag-based invalidation
- ✨ Distributed cache ready
- 💥 IDbEntityCache methods now async
- 💥 RlsOptions.UseLocalSettings removed

## Quick Links
- 📖 [Complete Documentation](webvella.database.docs.md)
- 📋 [Detailed Quick Reference](webvella.database.quick-ref.md)
- 🔄 [Migration Guide](HybridCache-Migration-Guide.md)
- 📊 [Test Coverage](HybridCache-Testing-Summary.md)
- 📝 [Changelog](../CHANGELOG.md)

## Installation
```bash
dotnet add package WebVella.Database
```

## Basic Setup
```csharp
// Simple
builder.Services.AddWebVellaDatabase("Host=localhost;Database=mydb;...");

// With caching (HybridCache v10.4.0)
builder.Services.AddWebVellaDatabase(connectionString, enableCaching: true);

// With RLS
builder.Services.AddWebVellaDatabaseWithRls<MyRlsProvider>(connectionString);
```

## Entity Definition
```csharp
[Table("users")]
[Cacheable(DurationSeconds = 600)]  // Auto-cached with HybridCache v10.4.0
public class User
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [JsonColumn]
    public UserSettings? Settings { get; set; }
}
```

## CRUD Operations
```csharp
// Create
var user = await _db.InsertAsync(new User { Name = "John" });

// Read
var user = await _db.GetAsync<User>(id);
var users = await _db.GetListAsync<User>();

// Update
await _db.UpdateAsync(user);

// Delete
await _db.DeleteAsync<User>(id);
```

## Query Builder
```csharp
var orders = await _db.Query<Order>()
    .Where(o => o.Status == OrderStatus.Active && o.Total > 100)
    .OrderByDescending(o => o.CreatedOn)
    .Limit(20)
    .ToListAsync();
```

## Transactions
```csharp
await using var scope = await _db.CreateTransactionScopeAsync();
await _db.InsertAsync(user);
await _db.InsertAsync(order);
await scope.CompleteAsync();
```

## Advisory Locks
```csharp
await using var scope = await _db.CreateTransactionScopeAsync(lockKey: "inventory-update");
await _db.UpdateAsync(inventory);
await scope.CompleteAsync();
```

## Caching (HybridCache v10.4.0)
```csharp
[Cacheable(DurationSeconds = 600)]
public class Product { }

// Automatic caching on reads, automatic invalidation on writes
var products = await _db.GetListAsync<Product>(); // Cached
await _db.InsertAsync(product); // Auto-invalidates cache via tag
```

## Row Level Security
```csharp
public class HttpRlsContextProvider : IRlsContextProvider
{
    public string? EntityId => GetClaim("entity_id");
    public IReadOnlyDictionary<string, string> CustomClaims => 
        new Dictionary<string, string> { ["role"] = GetClaim("role") ?? "user" };
    
    private string? GetClaim(string type) => /* get from HttpContext */;
}

builder.Services.AddWebVellaDatabaseWithRls<HttpRlsContextProvider>(connectionString);
```
