using WebVella.Database;

namespace WebVella.Database.Tests.Models;

/// <summary>
/// Test entity with [DbColumn] attributes for verifying custom column name mapping.
/// </summary>
[Table("test_db_column_entities")]
public class TestDbColumnEntity
{
	[Key]
	[DbColumn("entity_id")]
	public Guid Id { get; set; }

	[DbColumn("full_name")]
	public string DisplayName { get; set; } = string.Empty;

	[DbColumn("email_address")]
	public string Email { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Test entity without any [DbColumn] attributes, for comparison tests.
/// </summary>
[Table("test_no_db_column_entities")]
public class TestNoDbColumnEntity
{
	[Key]
	public Guid Id { get; set; }

	public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Test entity with [DbColumn] on key property and [ExplicitKey].
/// </summary>
[Table("test_explicit_key_db_column")]
public class TestExplicitKeyDbColumnEntity
{
	[ExplicitKey]
	[DbColumn("custom_id")]
	public Guid Id { get; set; }

	[DbColumn("col_value")]
	public string Value { get; set; } = string.Empty;
}
