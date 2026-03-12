using WebVella.Database;

namespace WebVella.Database.Tests.Models;

/// <summary>
/// Sample entity for testing QueryMultipleList with parent-child relationships.
/// </summary>
[Table("test_orders")]
public class TestOrder
{
	[Key]
	public Guid Id { get; set; }

	public string CustomerName { get; set; } = string.Empty;

	public decimal TotalAmount { get; set; }

	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Order line items populated from a subsequent result set.
	/// </summary>
	[External]
	[ResultSet(1, ForeignKey = "OrderId")]
	public List<TestOrderLine> Lines { get; set; } = [];

	/// <summary>
	/// Order notes populated from a subsequent result set.
	/// </summary>
	[External]
	[ResultSet(2, ForeignKey = "OrderId")]
	public List<TestOrderNote> Notes { get; set; } = [];
}

/// <summary>
/// Order line item entity for testing QueryMultipleList.
/// </summary>
[Table("test_order_lines")]
public class TestOrderLine
{
	[Key]
	public Guid Id { get; set; }

	public Guid OrderId { get; set; }

	public string ProductName { get; set; } = string.Empty;

	public int Quantity { get; set; }

	public decimal UnitPrice { get; set; }
}

/// <summary>
/// Order note entity for testing QueryMultipleList.
/// </summary>
[Table("test_order_notes")]
public class TestOrderNote
{
	[Key]
	public Guid Id { get; set; }

	public Guid OrderId { get; set; }

	public string Text { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Container class for QueryMultiple tests using [MultiQuery] attribute.
/// </summary>
[MultiQuery]
public class TestDashboard
{
	[ResultSet(0)]
	public TestProduct? FeaturedProduct { get; set; }

	[ResultSet(1)]
	public List<TestProduct> RecentProducts { get; set; } = [];

	[ResultSet(2)]
	public List<TestOrderLine> RecentOrderLines { get; set; } = [];
}
