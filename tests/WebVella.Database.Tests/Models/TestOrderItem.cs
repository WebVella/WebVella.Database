using WebVella.Database;

namespace WebVella.Database.Tests.Models;

/// <summary>
/// Sample entity with composite primary key for integration testing.
/// Uses two [Key] attributes to define a composite key (OrderId + ProductId).
/// </summary>
[Table("test_order_items")]
public class TestOrderItem
{
	[Key]
	public Guid OrderId { get; set; }

	[Key]
	public Guid ProductId { get; set; }

	public int Quantity { get; set; }

	public decimal UnitPrice { get; set; }

	public decimal TotalPrice { get; set; }

	public DateTime CreatedAt { get; set; }
}
