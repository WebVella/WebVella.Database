using FluentAssertions;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

[Collection("Database")]
public class QueryMultipleListTests : IAsyncLifetime
{
	private readonly DatabaseFixture _fixture;
	private readonly IDbService _dbService;

	public QueryMultipleListTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
		_dbService = fixture.DbService;
	}

	public async Task InitializeAsync()
	{
		await _fixture.ClearTestOrdersAsync();
		await _fixture.ClearTestProductsAsync();
	}

	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== QueryMultipleList Tests ===>

	[Fact]
	public async Task QueryMultipleList_WithChildCollections_ShouldMapCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
			       (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
			       (@OrderId1, 'Product 2', 1, 50.00),
			       (@OrderId2, 'Product 3', 4, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_notes (order_id, text, created_at)
			VALUES (@OrderId1, 'Note 1 for Order 1', NOW()),
			       (@OrderId2, 'Note 1 for Order 2', NOW()),
			       (@OrderId2, 'Note 2 for Order 2', NOW());
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName", total_amount AS "TotalAmount", 
			       created_at AS "CreatedAt"
			FROM test_orders ORDER BY customer_name;
			
			SELECT id AS "Id", order_id AS "OrderId", product_name AS "ProductName", 
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines;
			
			SELECT id AS "Id", order_id AS "OrderId", text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes;
			""";

		var orders = _dbService.QueryMultipleList<TestOrder>(sql);

		orders.Should().HaveCount(2);

		var orderA = orders.First(o => o.CustomerName == "Customer A");
		orderA.Lines.Should().HaveCount(2);
		orderA.Lines.Should().Contain(l => l.ProductName == "Product 1");
		orderA.Lines.Should().Contain(l => l.ProductName == "Product 2");
		orderA.Notes.Should().HaveCount(1);
		orderA.Notes.First().Text.Should().Be("Note 1 for Order 1");

		var orderB = orders.First(o => o.CustomerName == "Customer B");
		orderB.Lines.Should().HaveCount(1);
		orderB.Lines.First().ProductName.Should().Be("Product 3");
		orderB.Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task QueryMultipleListAsync_WithChildCollections_ShouldMapCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
			       (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
			       (@OrderId1, 'Product 2', 1, 50.00),
			       (@OrderId2, 'Product 3', 4, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_notes (order_id, text, created_at)
			VALUES (@OrderId1, 'Note 1 for Order 1', NOW()),
			       (@OrderId2, 'Note 1 for Order 2', NOW()),
			       (@OrderId2, 'Note 2 for Order 2', NOW());
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName", total_amount AS "TotalAmount", 
			       created_at AS "CreatedAt"
			FROM test_orders ORDER BY customer_name;
			
			SELECT id AS "Id", order_id AS "OrderId", product_name AS "ProductName", 
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines;
			
			SELECT id AS "Id", order_id AS "OrderId", text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes;
			""";

		var orders = await _dbService.QueryMultipleListAsync<TestOrder>(sql);

		orders.Should().HaveCount(2);

		var orderA = orders.First(o => o.CustomerName == "Customer A");
		orderA.Lines.Should().HaveCount(2);
		orderA.Notes.Should().HaveCount(1);

		var orderB = orders.First(o => o.CustomerName == "Customer B");
		orderB.Lines.Should().HaveCount(1);
		orderB.Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task QueryMultipleList_WithNoChildren_ShouldReturnEmptyCollections()
	{
		var orderId = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id, 'Customer Without Items', 0.00, NOW());
			""",
			new { Id = orderId });

		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName", total_amount AS "TotalAmount", 
			       created_at AS "CreatedAt"
			FROM test_orders;
			
			SELECT id AS "Id", order_id AS "OrderId", product_name AS "ProductName", 
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines WHERE order_id = @OrderId;
			
			SELECT id AS "Id", order_id AS "OrderId", text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes WHERE order_id = @OrderId;
			""";

		var orders = await _dbService.QueryMultipleListAsync<TestOrder>(sql, new { OrderId = orderId });

		orders.Should().HaveCount(1);
		orders.First().Lines.Should().BeEmpty();
		orders.First().Notes.Should().BeEmpty();
	}

	[Fact]
	public async Task QueryMultipleList_WithNoParents_ShouldReturnEmptyList()
	{
		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName", total_amount AS "TotalAmount", 
			       created_at AS "CreatedAt"
			FROM test_orders WHERE 1 = 0;
			
			SELECT id AS "Id", order_id AS "OrderId", product_name AS "ProductName", 
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines WHERE 1 = 0;
			
			SELECT id AS "Id", order_id AS "OrderId", text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes WHERE 1 = 0;
			""";

		var orders = await _dbService.QueryMultipleListAsync<TestOrder>(sql);

		orders.Should().BeEmpty();
	}

	[Fact]
	public async Task QueryMultipleListAsync_WithParameters_ShouldFilterCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
			       (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
			       (@OrderId2, 'Product 2', 1, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName", total_amount AS "TotalAmount", 
			       created_at AS "CreatedAt"
			FROM test_orders WHERE customer_name = @CustomerName;
			
			SELECT id AS "Id", order_id AS "OrderId", product_name AS "ProductName", 
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines WHERE order_id IN (
			    SELECT id FROM test_orders WHERE customer_name = @CustomerName
			);
			
			SELECT id AS "Id", order_id AS "OrderId", text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes WHERE order_id IN (
			    SELECT id FROM test_orders WHERE customer_name = @CustomerName
			);
			""";

		var orders = await _dbService.QueryMultipleListAsync<TestOrder>(
			sql,
			new { CustomerName = "Customer A" });

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer A");
		orders.First().Lines.Should().HaveCount(1);
		orders.First().Lines.First().ProductName.Should().Be("Product 1");
	}

	#endregion

	#region <=== QueryMultiple Container Tests ===>

	[Fact]
	public async Task QueryMultiple_WithContainerClass_ShouldMapCorrectly()
	{
		var product1 = new TestProduct
		{
			Name = "Featured Product",
			Description = "This is featured",
			Price = 99.99m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var product2 = new TestProduct
		{
			Name = "Recent Product 1",
			Price = 49.99m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var product3 = new TestProduct
		{
			Name = "Recent Product 2",
			Price = 29.99m,
			Quantity = 3,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(product1);
		await _dbService.InsertAsync(product2);
		await _dbService.InsertAsync(product3);

		var orderId = Guid.NewGuid();
		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id, 'Test Customer', 100.00, NOW());
			""",
			new { Id = orderId });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId, 'Line Item 1', 2, 25.00),
			       (@OrderId, 'Line Item 2', 1, 50.00);
			""",
			new { OrderId = orderId });

		var sql = """
			SELECT id AS "Id", name AS "Name", description AS "Description", price AS "Price",
			       quantity AS "Quantity", is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate", discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt", last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products WHERE name = 'Featured Product' LIMIT 1;
			
			SELECT id AS "Id", name AS "Name", description AS "Description", price AS "Price",
			       quantity AS "Quantity", is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate", discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt", last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products WHERE name LIKE 'Recent%' ORDER BY name;
			
			SELECT id AS "Id", order_id AS "OrderId", product_name AS "ProductName",
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines;
			""";

		var dashboard = await _dbService.QueryMultipleAsync<TestDashboard>(sql);

		dashboard.Should().NotBeNull();
		dashboard.FeaturedProduct.Should().NotBeNull();
		dashboard.FeaturedProduct!.Name.Should().Be("Featured Product");
		dashboard.RecentProducts.Should().HaveCount(2);
		dashboard.RecentOrderLines.Should().HaveCount(2);
	}

	[Fact]
	public async Task QueryMultipleAsync_WithEmptyResultSets_ShouldHandleGracefully()
	{
		var sql = """
			SELECT id AS "Id", name AS "Name", description AS "Description", price AS "Price",
			       quantity AS "Quantity", is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate", discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt", last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products WHERE 1 = 0 LIMIT 1;
			
			SELECT id AS "Id", name AS "Name", description AS "Description", price AS "Price",
			       quantity AS "Quantity", is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate", discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt", last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products WHERE 1 = 0;
			
			SELECT id AS "Id", order_id AS "OrderId", product_name AS "ProductName",
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines WHERE 1 = 0;
			""";

		var dashboard = await _dbService.QueryMultipleAsync<TestDashboard>(sql);

		dashboard.Should().NotBeNull();
		dashboard.FeaturedProduct.Should().BeNull();
		dashboard.RecentProducts.Should().BeEmpty();
		dashboard.RecentOrderLines.Should().BeEmpty();
	}

	#endregion

	#region <=== QueryWithJoin Tests ===>

	[Fact]
	public async Task QueryWithJoin_SingleChild_ShouldMapCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
				   (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
				   (@OrderId1, 'Product 2', 1, 50.00),
				   (@OrderId2, 'Product 3', 4, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName", 
				   o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
				   l.id AS "Id", l.order_id AS "OrderId", l.product_name AS "ProductName",
				   l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			ORDER BY o.customer_name, l.product_name
			""";

		var orders = _dbService.QueryWithJoin<TestOrder, TestOrderLine>(
			sql,
			parent => parent.Lines,
			parent => parent.Id,
			child => child.Id,
			splitOn: "Id");

		orders.Should().HaveCount(2);

		var orderA = orders.First(o => o.CustomerName == "Customer A");
		orderA.Lines.Should().HaveCount(2);
		orderA.Lines.Should().Contain(l => l.ProductName == "Product 1");
		orderA.Lines.Should().Contain(l => l.ProductName == "Product 2");

		var orderB = orders.First(o => o.CustomerName == "Customer B");
		orderB.Lines.Should().HaveCount(1);
		orderB.Lines.First().ProductName.Should().Be("Product 3");
	}

	[Fact]
	public async Task QueryWithJoinAsync_SingleChild_ShouldMapCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
				   (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
				   (@OrderId2, 'Product 2', 1, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName", 
				   o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
				   l.id AS "Id", l.order_id AS "OrderId", l.product_name AS "ProductName",
				   l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			ORDER BY o.customer_name
			""";

		var orders = await _dbService.QueryWithJoinAsync<TestOrder, TestOrderLine>(
			sql,
			parent => parent.Lines,
			parent => parent.Id,
			child => child.Id,
			splitOn: "Id");

		orders.Should().HaveCount(2);
		orders.First(o => o.CustomerName == "Customer A").Lines.Should().HaveCount(1);
		orders.First(o => o.CustomerName == "Customer B").Lines.Should().HaveCount(1);
	}

	[Fact]
	public async Task QueryWithJoin_WithNoChildren_ShouldReturnEmptyCollections()
	{
		var orderId = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id, 'Customer Without Items', 0.00, NOW());
			""",
			new { Id = orderId });

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName", 
				   o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
				   l.id AS "Id", l.order_id AS "OrderId", l.product_name AS "ProductName",
				   l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			""";

		var orders = await _dbService.QueryWithJoinAsync<TestOrder, TestOrderLine>(
			sql,
			parent => parent.Lines,
			parent => parent.Id,
			child => child.Id,
			splitOn: "Id");

		orders.Should().HaveCount(1);
		orders.First().Lines.Should().BeEmpty();
	}

	[Fact]
	public async Task QueryWithJoin_TwoChildCollections_ShouldMapCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
				   (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
				   (@OrderId1, 'Product 2', 1, 50.00),
				   (@OrderId2, 'Product 3', 4, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_notes (order_id, text, created_at)
			VALUES (@OrderId1, 'Note for A', NOW()),
				   (@OrderId2, 'Note 1 for B', NOW()),
				   (@OrderId2, 'Note 2 for B', NOW());
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName", 
				   o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
				   l.id AS "Id", l.order_id AS "OrderId", l.product_name AS "ProductName",
				   l.quantity AS "Quantity", l.unit_price AS "UnitPrice",
				   n.id AS "Id", n.order_id AS "OrderId", n.text AS "Text", n.created_at AS "CreatedAt"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			LEFT JOIN test_order_notes n ON n.order_id = o.id
			ORDER BY o.customer_name, l.product_name, n.text
			""";

		var orders = _dbService.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>(
			sql,
			parent => parent.Lines,
			parent => parent.Notes,
			parent => parent.Id,
			child1 => child1.Id,
			child2 => child2.Id,
			splitOn: "Id,Id");

		orders.Should().HaveCount(2);

		var orderA = orders.First(o => o.CustomerName == "Customer A");
		orderA.Lines.Should().HaveCount(2);
		orderA.Notes.Should().HaveCount(1);
		orderA.Notes.First().Text.Should().Be("Note for A");

		var orderB = orders.First(o => o.CustomerName == "Customer B");
		orderB.Lines.Should().HaveCount(1);
		orderB.Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task QueryWithJoinAsync_TwoChildCollections_ShouldMapCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
				   (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
				   (@OrderId2, 'Product 2', 1, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_notes (order_id, text, created_at)
			VALUES (@OrderId1, 'Note for A', NOW()),
				   (@OrderId2, 'Note for B', NOW());
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName", 
				   o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
				   l.id AS "Id", l.order_id AS "OrderId", l.product_name AS "ProductName",
				   l.quantity AS "Quantity", l.unit_price AS "UnitPrice",
				   n.id AS "Id", n.order_id AS "OrderId", n.text AS "Text", n.created_at AS "CreatedAt"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			LEFT JOIN test_order_notes n ON n.order_id = o.id
			ORDER BY o.customer_name
			""";

		var orders = await _dbService.QueryWithJoinAsync<TestOrder, TestOrderLine, TestOrderNote>(
			sql,
			parent => parent.Lines,
			parent => parent.Notes,
			parent => parent.Id,
			child1 => child1.Id,
			child2 => child2.Id,
			splitOn: "Id,Id");

		orders.Should().HaveCount(2);
		orders.All(o => o.Lines.Count == 1).Should().BeTrue();
		orders.All(o => o.Notes.Count == 1).Should().BeTrue();
	}

	[Fact]
	public async Task QueryWithJoin_WithParameters_ShouldFilterCorrectly()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
				   (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _dbService.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
				   (@OrderId2, 'Product 2', 1, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName", 
				   o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
				   l.id AS "Id", l.order_id AS "OrderId", l.product_name AS "ProductName",
				   l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			WHERE o.customer_name = @CustomerName
			""";

		var orders = await _dbService.QueryWithJoinAsync<TestOrder, TestOrderLine>(
			sql,
			parent => parent.Lines,
			parent => parent.Id,
			child => child.Id,
			splitOn: "Id",
			parameters: new { CustomerName = "Customer A" });

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer A");
		orders.First().Lines.Should().HaveCount(1);
		orders.First().Lines.First().ProductName.Should().Be("Product 1");
	}

	#endregion
}
