using WebVella.Database;

namespace WebVella.Database.Tests.Models;

/// <summary>
/// Sample cacheable entity for testing cache functionality.
/// Uses [Cacheable] attribute with 60-second duration.
/// </summary>
[Cacheable(60)]
[Table("test_cacheable_products")]
public class CacheableTestProduct
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal Price { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Sample cacheable entity with sliding expiration for testing.
/// </summary>
[Cacheable(30, SlidingExpiration = true)]
[Table("test_cacheable_products")]
public class SlidingCacheTestProduct
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal Price { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
}
