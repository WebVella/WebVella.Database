using WebVella.Database;

namespace WebVella.Database.Tests.Models;

/// <summary>
/// Represents the status of a product.
/// </summary>
public enum ProductStatus
{
	Draft = 0,
	Active = 1,
	Discontinued = 2,
	OutOfStock = 3
}

/// <summary>
/// Represents metadata stored as JSON in the database.
/// </summary>
public class ProductMetadata
{
	public string? Manufacturer { get; set; }
	public string? CountryOfOrigin { get; set; }
	public List<string> Tags { get; set; } = [];
	public Dictionary<string, string> Attributes { get; set; } = [];
}

/// <summary>
/// Sample entity for integration testing.
/// Uses custom attributes for table and key mapping.
/// Column names are automatically converted to snake_case for PostgreSQL.
/// </summary>
[Table("test_products")]
public class TestProduct
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public decimal Price { get; set; }
	public int Quantity { get; set; }
	public bool IsActive { get; set; }
	public ProductStatus Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public DateOnly ReleaseDate { get; set; }
	public DateOnly? DiscontinuedDate { get; set; }
	public DateTimeOffset PublishedAt { get; set; }
	public DateTimeOffset? LastReviewedAt { get; set; }

	/// <summary>
	/// Product metadata stored as JSON in the database.
	/// </summary>
	[JsonColumn]
	public ProductMetadata? Metadata { get; set; }

	/// <summary>
	/// An external property representing related category data.
	/// Excluded from INSERT/UPDATE/SELECT operations.
	/// </summary>
	[External]
	public string? CategoryName { get; set; }

	/// <summary>
	/// An external property representing a list of related tags.
	/// Excluded from INSERT/UPDATE/SELECT operations.
	/// </summary>
	[External]
	public List<string>? RelatedTags { get; set; }

	/// <summary>
	/// A computed/read-only property that should not be written to the database.
	/// </summary>
	[Write(false)]
	public string DisplayName => FormattableString.Invariant($"{Name} - ${Price:F2}");
}
