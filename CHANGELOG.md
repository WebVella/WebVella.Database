# WebVella.Database Changelog

## [1.4.0] - 2026-03-28

### 🚀 Major Changes
- **HybridCache Migration**: Replaced `IMemoryCache` with `Microsoft.Extensions.Caching.Hybrid` v10.4.0 for modern, high-performance caching
  - **Async-first API**: All cache operations are now fully async (`GetOrCreateAsync`, `GetOrCreateCollectionAsync`)
  - **Tag-based invalidation**: Use `InvalidateByTagAsync(tag)` to invalidate multiple cache entries at once
  - **Distributed cache ready**: Built-in support for both in-memory (L1) and distributed (L2) caching
  - **Better performance**: Optimized serialization, stampede protection, and memory management
  - **Backward compatible**: All existing caching functionality preserved

### 💥 Breaking Changes
- **`IDbEntityCache` interface redesigned**: All methods are now async
  - `GetOrCreate` → `GetOrCreateAsync`
  - `GetOrCreateCollection` → `GetOrCreateCollectionAsync`
  - `Invalidate` → `InvalidateByTagAsync`
  - Removed synchronous cache inspection methods (`TryGet`, `TryGetCollection`)
- **`RlsOptions.UseLocalSettings` removed**: RLS session variables are always transaction-scoped (LOCAL) by default
  - Update code: Remove any references to `UseLocalSettings` property
  - Behavior: All RLS variables now use `SET LOCAL` for proper transaction isolation

### ✨ What to do
No changes required for most users - the migration is transparent. If you were using custom cache implementations:

**Before:**
```csharp
var cached = cache.GetOrCreate<User>(
    key,
    () => GetUserFromDatabase(id),
    durationSeconds: 60);
```

**After:**
```csharp
var cached = await cache.GetOrCreateAsync<User>(
    key,
    async ct => await GetUserFromDatabaseAsync(id, ct),
    durationSeconds: 60,
    tags: new[] { "table:users" });

// Later, invalidate all users cache
await cache.InvalidateByTagAsync("table:users");
```

For RLS options:
```csharp
// Before
new RlsOptions { UseLocalSettings = true }

// After
new RlsOptions()  // Always uses LOCAL settings
```

---

## [1.3.1] - 2026-06-02

### ✨ New Features
- **`ConnectionString` property**: Added `string ConnectionString { get; }` to `IDbService` — exposes the
  PostgreSQL connection string used by the service instance, useful for creating secondary connections or
  passing the connection string to lower-level components without re-injecting configuration
- **Runtime RLS Enable / Disable**: Added `EnableRls()` / `EnableRlsAsync()` and `DisableRls()` /
  `DisableRlsAsync()` methods to `IDbService`. `DisableRls` resets all RLS session variables to empty
  strings for subsequent queries in the current async context; `EnableRls` re-applies them. When called
  inside a transaction scope the change takes effect on the active transaction connection immediately

---

## [1.3.0] - 2026-06-01

### 💥 Breaking Changes
- **`IRlsContextProvider` simplified**: Removed `Guid? TenantId` and `Guid? UserId` properties.
  Replaced with a single `string? EntityId` property for the primary RLS identifier.
  - Implementations must be updated to remove `TenantId` / `UserId` and add `string? EntityId`
  - `NullRlsContextProvider` updated accordingly
- **`RlsOptions.Prefix` renamed to `SettingName`**: The property now holds the **full**
  PostgreSQL session variable name for the entity identifier, default changed to `"app.user_id"`.
  Custom claims still use the namespace derived from the part before the first dot.
- **Cache key format changed**: RLS cache context prefix changed from `t:<guid>|u:<guid>` to `e:<value>`

### ✨ What to do
Update your `IRlsContextProvider` implementation:
```csharp
// Before
public Guid? TenantId => GetClaimAsGuid("tenant_id");
public Guid? UserId   => GetClaimAsGuid("sub");

// After
public string? EntityId => GetClaim("entity_id");
```
Update `RlsOptions` usage:
```csharp
// Before
new RlsOptions { Prefix = "app" }

// After
new RlsOptions { SettingName = "app.user_id" }
```
Update your PostgreSQL RLS policies:
```sql
-- Before
USING (tenant_id = current_setting('app.tenant_id', true)::uuid)
-- After
USING (entity_id = current_setting('app.user_id', true))
```

---

## [1.2.4] - 2026-03-22

### ✨ New Features
- **SQL-Free QueryMultipleList Builder**: `QueryMultipleList<T>()` now returns a
  `DbMultiQueryList<T>` fluent builder that auto-generates multi-SELECT SQL from entity
  metadata (`[Table]`, `[Key]`, `[ResultSet(ForeignKey)]`). Supports `.Where()`,
  `.OrderBy()`, `.OrderByDescending()`, `.ThenBy()`, `.ThenByDescending()`, `.Limit()`,
  `.Offset()`, and `.WithPaging()`. Child SELECTs are automatically filtered with
  `WHERE fk IN (SELECT pk FROM parent ...)` subqueries that mirror parent conditions
- **SQL-Free QueryWithJoin Builder**: `QueryWithJoin<TParent, TChild>()` and
  `QueryWithJoin<TParent, TChild1, TChild2>()` now return fluent builders that
  auto-generate JOIN SQL from `[ResultSet(ForeignKey)]` metadata. `ChildSelector`,
  `ParentKey`, `ChildKey`, and `SplitOn` are all auto-derived in SQL-free mode. Supports
  `.Where()`, `.OrderBy()`, `.Limit()`, `.Offset()`, `.WithPaging()`, and all ordering
  variants
- **Table Alias Support**: `DbExpressionTranslator` now accepts an optional table alias
  parameter, prefixing column names (e.g., `p.customer_name = @p0`) for JOIN WHERE
  clauses
- **EntityMetadata.GetAliasedSelectColumns**: New internal method that generates aliased
  SELECT columns (e.g., `p.column_name AS "PropertyName"`) for JOIN SQL generation

### 📝 Notes
- Both `DbMultiQueryList<T>` and `DbJoinQuery` builders support two modes: SQL-free
  (expression-based) and raw SQL (`.Sql()`). When `.Sql()` is called, the raw SQL is
  used as-is. When omitted, SQL is auto-generated from entity metadata
- All [WHERE patterns](#supported-where-patterns) from `DbQuery<T>` are available in
  the new builders

---

## [1.2.3] - 2026-03-21

### ✨ New Features
- **Fluent Query Builder**: Added `IDbService.Query<T>()` returning `DbQuery<T>` — an
  expression-tree-based query builder that generates parameterised PostgreSQL SQL without
  writing SQL strings. Supports `Where`, `OrderBy`, `OrderByDescending`, `ThenBy`,
  `ThenByDescending`, `Limit`, `Offset`, and `WithPaging` builder methods, plus async and
  sync terminal methods: `ToListAsync` / `ToList`, `FirstOrDefaultAsync` / `FirstOrDefault`,
  `CountAsync` / `Count`, `ExistsAsync` / `Exists`
- **ILike Extension Methods**: Added `DbStringExtensions` with `ILikeContains`,
  `ILikeStartsWith`, and `ILikeEndsWith` — marker methods for use inside `.Where()` predicates
  that translate to PostgreSQL `ILIKE` for case-insensitive pattern matching
- **PreMigrateAsync hook**: Added `DbMigration.PreMigrateAsync(IServiceProvider)` virtual
  method — called inside the migration transaction before `GenerateSqlAsync` SQL is executed;
  override to drop indexes, disable triggers, or validate preconditions before schema changes
- **DbMigrationAttribute ScriptPath**: Added optional `scriptPath` constructor parameter to
  `[DbMigration]` — when set, `GenerateSqlAsync` loads SQL exclusively from the named embedded
  resource (case-insensitive full-name or suffix match), bypassing automatic
  `{TypeName}.Script.sql/.psql` discovery; throws `DbMigrationException` if the resource
  cannot be found in the assembly

### Supported WHERE Patterns
`==` `!=` `<` `<=` `>` `>=` · `&&` `||` `!` · boolean shorthand · null checks ·
`.Contains()` `.StartsWith()` `.EndsWith()` (LIKE) ·
`.ILikeContains()` `.ILikeStartsWith()` `.ILikeEndsWith()` (ILIKE) ·
`.ToLower()` `.ToLowerInvariant()` `.ToUpper()` `.ToUpperInvariant()` (LOWER/UPPER) ·
`list.Contains(e.Prop)` / `Enumerable.Contains(list, e.Prop)` (ANY) ·
enum auto-cast to `int` · captured variables and closures

### Breaking Changes
- `DbQuery<T>.Take(int)` renamed to `Limit(int)` to match PostgreSQL keyword
- `DbQuery<T>.Skip(int)` renamed to `Offset(int)` to match PostgreSQL keyword

---

## [1.2.2] - 2026-03-20

### ✨ New Features
- **DbColumn Attribute**: Added `[DbColumn("name")]` attribute to specify explicit database column names for entity properties, overriding the default snake_case conversion
- **Insert from Object**: Added `Insert<T>(object)` and `InsertAsync<T>(object)` overloads that accept anonymous objects, classes, or records and map their properties to the entity type with type validation
- **Update from Object**: Added `Update<T>(object)` and `UpdateAsync<T>(object)` overloads that accept anonymous objects with key properties for record lookup and partial update support — only the non-key properties present in the object are updated
- **Property Type Validation**: Both Insert and Update object overloads validate that matching property names have identical types, throwing `ArgumentException` with detailed mismatch information

## [1.2.1] - 2026-03-19

### 🚀 Enhanced Developer Experience
- **Comprehensive Documentation Integration**: All documentation files now included in NuGet package for offline access
- **MSBuild Integration**: Added `.targets` file that automatically makes documentation available to consuming projects
- **IntelliSense-Friendly Examples**: Created `WebVellaDatabaseExamples` class with structured code examples for better IDE support
- **Enhanced XML Documentation**: Significantly improved interface documentation with practical examples and feature highlights
- **Quick Reference Guide**: Added `docs/webvella.database.quick-ref.md` for rapid development reference

### 🤖 AI/Copilot Integration Improvements
- **Source Link Support**: Connected NuGet package directly to GitHub repository for enhanced tooling
- **AdditionalFiles Integration**: Documentation automatically available as analyzer additional files for AI tools
- **Rich Package Metadata**: Enhanced package description, tags, and documentation links
- **Welcome Messages**: Helpful build-time messages with quick-start examples for new users

### 📦 Package Enhancements
- **Improved Package Tags**: Better discoverability with comprehensive tags (postgres, postgresql, dapper, transactions, advisory-locks, rls, migrations, database, nested-transactions)
- **Documentation URLs**: Direct links to GitHub documentation in package metadata
- **Symbol Packages**: Enhanced debugging experience with source link and symbol packages (snupkg)

### 🔧 Technical Improvements
- **XML Documentation Generation**: Automatic generation of comprehensive XML documentation files
- **Enhanced Build Integration**: Better integration with consuming projects through MSBuild targets
- **Timezone Test Fix**: Resolved DateTime comparison issues in test suite by normalizing to UTC

### 📚 Documentation Updates
- **README Enhancements**: Added comprehensive RLS (Row Level Security) setup examples
- **Reddit Marketing Guide**: Created `docs/reddit.md` with community engagement templates
- **Enhanced Code Examples**: More comprehensive examples covering all major use cases

## [1.2.0] - 2026-03-15

### 🛡️ Row Level Security (RLS) Support
- **Automatic Session Context**: Implemented `IRlsContextProvider` for automatic PostgreSQL session variable injection
- **Multi-Tenant Ready**: Built-in support for tenant isolation using PostgreSQL RLS policies
- **RLS-Aware Caching**: Entity cache keys automatically include RLS context for proper tenant isolation
- **Migration RLS Bypass**: Migrations automatically bypass RLS to ensure unrestricted schema access
- **Flexible Configuration**: Support for custom prefixes, local vs global settings, and enable/disable options

### 🔧 Enhanced Service Registration
- **RLS Service Extensions**: New `AddWebVellaDatabaseWithRls<T>()` methods for easy RLS setup
- **Connection String Accessor**: Added `IDbConnectionStringAccessor` for migration service isolation
- **Factory Pattern Support**: Enhanced support for dependency injection patterns with RLS providers

### 🧪 Comprehensive Testing
- **RLS Test Coverage**: Extensive test suite covering RLS functionality, cache behavior, and migration bypass
- **Integration Tests**: Real database integration tests for RLS session variable verification
- **Migration RLS Tests**: Specific tests ensuring migrations work correctly with RLS enabled

### 📖 Documentation Improvements
- **RLS Setup Guides**: Comprehensive documentation for multi-tenant application setup
- **Security Best Practices**: Guidelines for implementing secure RLS policies
- **Migration Documentation**: Enhanced migration docs with RLS bypass explanation

## [1.1.0] - Previous Release

### ✨ Core Features
- **Nested Transaction Support**: Proper PostgreSQL savepoint management
- **Advisory Locks**: Distributed coordination with simple scope management  
- **Entity Caching**: Optional caching with automatic invalidation
- **Database Migrations**: Version-controlled schema changes
- **JSON Column Support**: Automatic serialization/deserialization
- **Composite Key Support**: Multi-column primary key operations
- **CRUD Operations**: Full suite of Create, Read, Update, Delete operations
- **Custom Queries**: Full Dapper query power with parameter binding

---

## Version Numbering
- **Major**: Breaking changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, improvements, backward compatible

## Links
- 📦 [NuGet Package](https://www.nuget.org/packages/WebVella.Database/)
- 📂 [GitHub Repository](https://github.com/WebVella/WebVella.Database)
- 📖 [Documentation](https://github.com/WebVella/WebVella.Database/blob/main/docs/webvella.database.docs.md)