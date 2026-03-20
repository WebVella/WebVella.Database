# WebVella.Database Changelog

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