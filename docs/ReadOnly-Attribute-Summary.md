# ReadOnly Attribute Implementation Summary

## Overview
Added new `[ReadOnly]` attribute for properties that should be read from the database but not written during INSERT or UPDATE operations.

## Changes Made

### 1. ✅ New Attribute Added
**File**: `src/WebVella.Database/EntityAttributes.cs`

```csharp
/// <summary>
/// Marks a property as read-only. The property will be included in SELECT operations
/// but excluded from INSERT and UPDATE operations.
/// </summary>
/// <remarks>
/// Use this attribute for computed columns, database-generated values, or any property
/// that should only be read from the database and never written.
/// This is equivalent to <c>[Write(false)]</c> but provides clearer intent.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ReadOnlyAttribute : Attribute
{
}
```

### 2. ✅ Entity Metadata Updated
**File**: `src/WebVella.Database/EntityMetadata.cs`

- Updated `GetWritableProperties()` to exclude properties marked with `[ReadOnly]`
- Properties with `[ReadOnly]` are still included in SELECT operations
- Properties with `[ReadOnly]` are excluded from INSERT and UPDATE operations

### 3. ✅ Comprehensive Tests Added
**File**: `tests/WebVella.Database.Tests/ReadOnlyAttributeTests.cs`

**4 tests created:**
- `ReadOnlyProperty_ShouldBeReadFromDatabase` - Verifies reading works
- `InsertAsync_ShouldNotIncludeReadOnlyProperty` - Verifies INSERT excludes readonly
- `UpdateAsync_ShouldNotUpdateReadOnlyProperty` - Verifies UPDATE excludes readonly
- `MultipleReadOnlyProperties_ShouldAllBeExcludedFromWrites` - Multiple readonly properties

### 4. ✅ Documentation Updated
- **README.md**: Added `[ReadOnly]` to User entity example
- **docs/webvella.database.docs.md**: Added `[ReadOnly]` to Entity Attributes table
- **src/WebVella.Database/WebVellaDatabaseExamples.cs**: Added `[ReadOnly]` to User example

## Use Cases

### 1. Database-Generated Timestamps
```csharp
[Table("orders")]
public class Order
{
    [Key]
    public Guid Id { get; set; }
    
    public decimal Total { get; set; }
    
    [ReadOnly]
    public DateTime CreatedOn { get; set; }  // DEFAULT CURRENT_TIMESTAMP
    
    [ReadOnly]
    public DateTime UpdatedOn { get; set; }  // Trigger-maintained
}
```

### 2. Computed Columns
```csharp
[Table("products")]
public class Product
{
    [Key]
    public Guid Id { get; set; }
    
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    
    [ReadOnly]
    public decimal TotalValue { get; set; }  // GENERATED ALWAYS AS (price * quantity)
}
```

### 3. Version Columns
```csharp
[Table("documents")]
public class Document
{
    [Key]
    public Guid Id { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    [ReadOnly]
    public int Version { get; set; }  // Incremented by trigger
}
```

## Benefits

1. **Clear Intent**: More explicit than `[Write(false)]` for database-generated values
2. **Type Safety**: Prevents accidental writes to computed/generated columns
3. **Documentation**: Self-documenting code showing which properties are database-managed
4. **Flexibility**: Can be used with timestamps, computed columns, triggers, etc.

## Comparison with Other Attributes

| Attribute | SELECT | INSERT | UPDATE |
|-----------|--------|--------|--------|
| None | ✅ | ✅ | ✅ |
| `[External]` | ❌ | ❌ | ❌ |
| `[ReadOnly]` | ✅ | ❌ | ❌ |
| `[Write(false)]` | ✅ | ❌ | ❌ |
| `[Key]` | ✅ | ❌* | ✅ |

*Auto-generated keys are excluded from INSERT VALUES but returned via RETURNING clause

## Test Results
- ✅ 4/4 ReadOnlyAttributeTests passing
- ✅ 62/62 existing tests still passing
- ✅ Build successful with 0 warnings

## Status
✅ **COMPLETE** - ReadOnly attribute fully implemented, tested, and documented!
