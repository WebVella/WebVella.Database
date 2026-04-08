using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Tests for QueryMultipleList with JSON columns.
/// </summary>
public class QueryMultipleListJsonColumnTests : IAsyncLifetime
{
	private readonly IDbService _db;
	private readonly string _orderTableName;
	private readonly string _orderItemTableName;

	public QueryMultipleListJsonColumnTests()
	{
		var connectionString = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
			.Build()
			.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("Connection string not found");

		var services = new ServiceCollection();
		services.AddWebVellaDatabase(connectionString, enableCaching: false);
		var serviceProvider = services.BuildServiceProvider();
		_db = serviceProvider.GetRequiredService<IDbService>();
		_orderTableName = $"orders_json_{Guid.NewGuid():N}";
		_orderItemTableName = $"order_items_json_{Guid.NewGuid():N}";
	}

	public async Task InitializeAsync()
	{
		// Create test tables
		await _db.ExecuteAsync($@"
			CREATE TABLE {_orderTableName} (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				order_number VARCHAR(50) NOT NULL,
				customer_data JSONB,
				metadata JSONB
			)");

		await _db.ExecuteAsync($@"
			CREATE TABLE {_orderItemTableName} (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				order_id UUID NOT NULL,
				product_name VARCHAR(100) NOT NULL,
				product_details JSONB,
				FOREIGN KEY (order_id) REFERENCES {_orderTableName}(id)
			)");

		// Insert test data
		await _db.ExecuteAsync($@"
			INSERT INTO {_orderTableName} (id, order_number, customer_data, metadata) VALUES
			(@Id1, 'ORD-001', @CustomerData1::jsonb, @Metadata1::jsonb),
			(@Id2, 'ORD-002', @CustomerData2::jsonb, @Metadata2::jsonb)
		", new
		{
			Id1 = new Guid("11111111-1111-1111-1111-111111111111"),
			CustomerData1 = "{\"name\":\"John Doe\",\"email\":\"john@example.com\"}",
			Metadata1 = "{\"source\":\"web\",\"priority\":\"high\"}",
			Id2 = new Guid("22222222-2222-2222-2222-222222222222"),
			CustomerData2 = "{\"name\":\"Jane Smith\",\"email\":\"jane@example.com\"}",
			Metadata2 = "{\"source\":\"mobile\",\"priority\":\"normal\"}"
		});

		await _db.ExecuteAsync($@"
			INSERT INTO {_orderItemTableName} (order_id, product_name, product_details) VALUES
			(@OrderId1, 'Product A', @Details1::jsonb),
			(@OrderId1, 'Product B', @Details2::jsonb),
			(@OrderId2, 'Product C', @Details3::jsonb)
		", new
		{
			OrderId1 = new Guid("11111111-1111-1111-1111-111111111111"),
			Details1 = "{\"sku\":\"SKU-A\",\"price\":99.99}",
			Details2 = "{\"sku\":\"SKU-B\",\"price\":149.99}",
			OrderId2 = new Guid("22222222-2222-2222-2222-222222222222"),
			Details3 = "{\"sku\":\"SKU-C\",\"price\":199.99}"
		});
	}

	public async Task DisposeAsync()
	{
		await _db.ExecuteAsync($"DROP TABLE IF EXISTS {_orderItemTableName}");
		await _db.ExecuteAsync($"DROP TABLE IF EXISTS {_orderTableName}");
	}

	[Fact]
	public async Task QueryMultipleListAsync_ShouldDeserializeJsonColumnsInParentAndChildren()
	{
		var sql = $@"
			SELECT id AS ""Id"", order_number AS ""OrderNumber"", customer_data AS ""CustomerData"", metadata AS ""Metadata""
			FROM {_orderTableName}
			ORDER BY order_number;
			
			SELECT id AS ""Id"", order_id AS ""OrderId"", product_name AS ""ProductName"", product_details AS ""ProductDetails""
			FROM {_orderItemTableName}
			ORDER BY product_name;
		";

		var orders = await _db.QueryMultipleListAsync<OrderWithJson>(sql);

		orders.Should().HaveCount(2);

		// Verify first order
		var order1 = orders[0];
		order1.OrderNumber.Should().Be("ORD-001");
		order1.CustomerData.Should().NotBeNull();
		order1.CustomerData!.Name.Should().Be("John Doe");
		order1.CustomerData.Email.Should().Be("john@example.com");
		order1.Metadata.Should().NotBeNull();
		order1.Metadata!["source"].ToString().Should().Be("web");
		order1.Metadata["priority"].ToString().Should().Be("high");

		order1.Items.Should().HaveCount(2);
		order1.Items[0].ProductName.Should().Be("Product A");
		order1.Items[0].ProductDetails.Should().NotBeNull();
		order1.Items[0].ProductDetails!.Sku.Should().Be("SKU-A");
		order1.Items[0].ProductDetails.Price.Should().Be(99.99m);

		// Verify second order
		var order2 = orders[1];
		order2.OrderNumber.Should().Be("ORD-002");
		order2.CustomerData.Should().NotBeNull();
		order2.CustomerData!.Name.Should().Be("Jane Smith");
		order2.CustomerData.Email.Should().Be("jane@example.com");

		order2.Items.Should().HaveCount(1);
		order2.Items[0].ProductName.Should().Be("Product C");
		order2.Items[0].ProductDetails.Should().NotBeNull();
		order2.Items[0].ProductDetails!.Price.Should().Be(199.99m);
	}

	[Fact]
	public async Task QueryMultipleList_ShouldDeserializeJsonColumnsInParentAndChildren()
	{
		var sql = $@"
			SELECT id AS ""Id"", order_number AS ""OrderNumber"", customer_data AS ""CustomerData"", metadata AS ""Metadata""
			FROM {_orderTableName}
			ORDER BY order_number;
			
			SELECT id AS ""Id"", order_id AS ""OrderId"", product_name AS ""ProductName"", product_details AS ""ProductDetails""
			FROM {_orderItemTableName}
			ORDER BY product_name;
		";

		var orders = _db.QueryMultipleList<OrderWithJson>(sql);

		orders.Should().HaveCount(2);

		// Verify JSON deserialization works
		var order1 = orders[0];
		order1.CustomerData.Should().NotBeNull();
		order1.CustomerData!.Name.Should().Be("John Doe");
		order1.Items[0].ProductDetails.Should().NotBeNull();
		order1.Items[0].ProductDetails!.Sku.Should().Be("SKU-A");
	}

	[Fact]
	public async Task QueryMultipleListAsync_WithNullJsonColumns_ShouldHandleGracefully()
	{
		// Insert order with null JSON columns
		var orderId = Guid.NewGuid();
		await _db.ExecuteAsync($@"
			INSERT INTO {_orderTableName} (id, order_number, customer_data, metadata)
			VALUES (@Id, 'ORD-NULL', NULL, NULL)
		", new { Id = orderId });

		await _db.ExecuteAsync($@"
			INSERT INTO {_orderItemTableName} (order_id, product_name, product_details)
			VALUES (@OrderId, 'Product NULL', NULL)
		", new { OrderId = orderId });

		var sql = $@"
			SELECT id AS ""Id"", order_number AS ""OrderNumber"", customer_data AS ""CustomerData"", metadata AS ""Metadata""
			FROM {_orderTableName}
			WHERE id = @OrderId;
			
			SELECT id AS ""Id"", order_id AS ""OrderId"", product_name AS ""ProductName"", product_details AS ""ProductDetails""
			FROM {_orderItemTableName}
			WHERE order_id = @OrderId;
		";

		var orders = await _db.QueryMultipleListAsync<OrderWithJson>(sql, new { OrderId = orderId });

		orders.Should().HaveCount(1);
		orders[0].CustomerData.Should().BeNull();
		orders[0].Metadata.Should().BeNull();
		orders[0].Items[0].ProductDetails.Should().BeNull();
	}

	#region <=== Test Models ===>

	private class OrderWithJson
	{
		public Guid Id { get; set; }
		public string OrderNumber { get; set; } = string.Empty;

		[JsonColumn]
		public CustomerData? CustomerData { get; set; }

		[JsonColumn]
		public Dictionary<string, object>? Metadata { get; set; }

		[ResultSet(0, ForeignKey = nameof(OrderItemWithJson.OrderId), ParentKey = nameof(Id))]
		public List<OrderItemWithJson> Items { get; set; } = [];
	}

	private class OrderItemWithJson
	{
		public Guid Id { get; set; }
		public Guid OrderId { get; set; }
		public string ProductName { get; set; } = string.Empty;

		[JsonColumn]
		public ProductDetails? ProductDetails { get; set; }
	}

	private class CustomerData
	{
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}

	private class ProductDetails
	{
		public string Sku { get; set; } = string.Empty;
		public decimal Price { get; set; }
	}

	#endregion
}
