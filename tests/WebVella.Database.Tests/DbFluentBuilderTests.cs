using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Unit tests for the fluent query builders: <see cref="DbJoinQuery{TParent, TChild}"/>,
/// <see cref="DbJoinQuery{TParent, TChild1, TChild2}"/>, <see cref="DbMultiQuery{T}"/>,
/// and <see cref="DbMultiQueryList{T}"/>. These tests validate builder configuration
/// and validation without requiring a database connection.
/// </summary>
public class DbFluentBuilderTests
{
	private static readonly string TestConnectionString =
		new Microsoft.Extensions.Configuration.ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
			.Build()
			.GetConnectionString("DefaultConnection")
		?? throw new InvalidOperationException(
			"Connection string 'DefaultConnection' not found in appsettings.json");

	private readonly IDbService _db = new DbService(TestConnectionString);

	#region <=== Factory Method Tests ===>

	[Fact]
	public void QueryWithJoin_SingleChild_ShouldReturnBuilder()
	{
		var builder = _db.QueryWithJoin<TestOrder, TestOrderLine>();

		builder.Should().NotBeNull();
		builder.Should().BeOfType<DbJoinQuery<TestOrder, TestOrderLine>>();
	}

	[Fact]
	public void QueryWithJoin_TwoChildren_ShouldReturnBuilder()
	{
		var builder = _db.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>();

		builder.Should().NotBeNull();
		builder.Should()
			.BeOfType<DbJoinQuery<TestOrder, TestOrderLine, TestOrderNote>>();
	}

	[Fact]
	public void QueryMultiple_ShouldReturnBuilder()
	{
		var builder = _db.QueryMultiple<TestDashboard>();

		builder.Should().NotBeNull();
		builder.Should().BeOfType<DbMultiQuery<TestDashboard>>();
	}

	[Fact]
	public void QueryMultipleList_ShouldReturnBuilder()
	{
		var builder = _db.QueryMultipleList<TestOrder>();

		builder.Should().NotBeNull();
		builder.Should().BeOfType<DbMultiQueryList<TestOrder>>();
	}

	#endregion

	#region <=== DbJoinQuery<TParent, TChild> Validation ===>

	[Fact]
	public void DbJoinQuery_SingleChild_WithoutChildSelector_ShouldThrow()
	{
		var builder = _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql("SELECT 1")
			.ParentKey(o => o.Id)
			.ChildKey(l => l.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*Child selector*");
	}

	[Fact]
	public void DbJoinQuery_SingleChild_WithoutParentKey_ShouldThrow()
	{
		var builder = _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql("SELECT 1")
			.ChildSelector(o => o.Lines)
			.ChildKey(l => l.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*Parent key*");
	}

	[Fact]
	public void DbJoinQuery_SingleChild_WithoutChildKey_ShouldThrow()
	{
		var builder = _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql("SELECT 1")
			.ChildSelector(o => o.Lines)
			.ParentKey(o => o.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*Child key*");
	}

	[Fact]
	public void DbJoinQuery_SingleChild_Sql_WithNull_ShouldThrow()
	{
		var action = () => _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql(null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DbJoinQuery_SingleChild_ChildSelector_WithNull_ShouldThrow()
	{
		var action = () => _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.ChildSelector(null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DbJoinQuery_SingleChild_ParentKey_WithNull_ShouldThrow()
	{
		var action = () => _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.ParentKey(null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DbJoinQuery_SingleChild_ChildKey_WithNull_ShouldThrow()
	{
		var action = () => _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.ChildKey(null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DbJoinQuery_SingleChild_SplitOn_WithNull_ShouldThrow()
	{
		var action = () => _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.SplitOn(null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DbJoinQuery_SingleChild_BuilderMethodsReturnSameInstance()
	{
		var builder = _db.QueryWithJoin<TestOrder, TestOrderLine>();

		var afterSql = builder.Sql("SELECT 1");
		var afterChild = afterSql.ChildSelector(o => o.Lines);
		var afterParent = afterChild.ParentKey(o => o.Id);
		var afterChildKey = afterParent.ChildKey(l => l.Id);
		var afterSplitOn = afterChildKey.SplitOn("Id");
		var afterParams = afterSplitOn.Parameters(new { Id = 1 });

		afterSql.Should().BeSameAs(builder);
		afterChild.Should().BeSameAs(builder);
		afterParent.Should().BeSameAs(builder);
		afterChildKey.Should().BeSameAs(builder);
		afterSplitOn.Should().BeSameAs(builder);
		afterParams.Should().BeSameAs(builder);
	}

	#endregion

	#region <=== DbJoinQuery<TParent, TChild1, TChild2> Validation ===>

	[Fact]
	public void DbJoinQuery_TwoChildren_WithoutChildSelector1_ShouldThrow()
	{
		var builder = _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.Sql("SELECT 1")
			.ChildSelector2(o => o.Notes)
			.ParentKey(o => o.Id)
			.ChildKey1(l => l.Id)
			.ChildKey2(n => n.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*First child selector*");
	}

	[Fact]
	public void DbJoinQuery_TwoChildren_WithoutChildSelector2_ShouldThrow()
	{
		var builder = _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.Sql("SELECT 1")
			.ChildSelector1(o => o.Lines)
			.ParentKey(o => o.Id)
			.ChildKey1(l => l.Id)
			.ChildKey2(n => n.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*Second child selector*");
	}

	[Fact]
	public void DbJoinQuery_TwoChildren_WithoutParentKey_ShouldThrow()
	{
		var builder = _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.Sql("SELECT 1")
			.ChildSelector1(o => o.Lines)
			.ChildSelector2(o => o.Notes)
			.ChildKey1(l => l.Id)
			.ChildKey2(n => n.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*Parent key*");
	}

	[Fact]
	public void DbJoinQuery_TwoChildren_WithoutChildKey1_ShouldThrow()
	{
		var builder = _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.Sql("SELECT 1")
			.ChildSelector1(o => o.Lines)
			.ChildSelector2(o => o.Notes)
			.ParentKey(o => o.Id)
			.ChildKey2(n => n.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*First child key*");
	}

	[Fact]
	public void DbJoinQuery_TwoChildren_WithoutChildKey2_ShouldThrow()
	{
		var builder = _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.Sql("SELECT 1")
			.ChildSelector1(o => o.Lines)
			.ChildSelector2(o => o.Notes)
			.ParentKey(o => o.Id)
			.ChildKey1(l => l.Id);

		var action = () => builder.ToList();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*Second child key*");
	}

	[Fact]
	public void DbJoinQuery_TwoChildren_BuilderMethodsReturnSameInstance()
	{
		var builder = _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>();

		var afterSql = builder.Sql("SELECT 1");
		var afterChild1 = afterSql.ChildSelector1(o => o.Lines);
		var afterChild2 = afterChild1.ChildSelector2(o => o.Notes);
		var afterParent = afterChild2.ParentKey(o => o.Id);
		var afterKey1 = afterParent.ChildKey1(l => l.Id);
		var afterKey2 = afterKey1.ChildKey2(n => n.Id);
		var afterSplitOn = afterKey2.SplitOn("Id,Id");
		var afterParams = afterSplitOn.Parameters(new { Id = 1 });

		afterSql.Should().BeSameAs(builder);
		afterChild1.Should().BeSameAs(builder);
		afterChild2.Should().BeSameAs(builder);
		afterParent.Should().BeSameAs(builder);
		afterKey1.Should().BeSameAs(builder);
		afterKey2.Should().BeSameAs(builder);
		afterSplitOn.Should().BeSameAs(builder);
		afterParams.Should().BeSameAs(builder);
	}

	#endregion

	#region <=== DbMultiQuery<T> Validation ===>

	[Fact]
	public void DbMultiQuery_Execute_WithoutSql_ShouldThrow()
	{
		var builder = _db.QueryMultiple<TestDashboard>();

		var action = () => builder.Execute();

		action.Should().Throw<InvalidOperationException>()
			.WithMessage("*SQL*");
	}

	[Fact]
	public async Task DbMultiQuery_ExecuteAsync_WithoutSql_ShouldThrow()
	{
		var builder = _db.QueryMultiple<TestDashboard>();

		var action = async () => await builder.ExecuteAsync();

		await action.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*SQL*");
	}

	[Fact]
	public void DbMultiQuery_Sql_WithNull_ShouldThrow()
	{
		var action = () => _db.QueryMultiple<TestDashboard>().Sql(null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DbMultiQuery_BuilderMethodsReturnSameInstance()
	{
		var builder = _db.QueryMultiple<TestDashboard>();

		var afterSql = builder.Sql("SELECT 1; SELECT 2;");
		var afterParams = afterSql.Parameters(new { Id = 1 });

		afterSql.Should().BeSameAs(builder);
		afterParams.Should().BeSameAs(builder);
	}

	#endregion

	#region <=== DbMultiQueryList<T> Validation ===>

	[Fact]
	public void DbMultiQueryList_Sql_WithNull_ShouldThrow()
	{
		var action = () => _db.QueryMultipleList<TestOrder>().Sql(null!);

		action.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DbMultiQueryList_BuilderMethodsReturnSameInstance()
	{
		var builder = _db.QueryMultipleList<TestOrder>();

		var afterSql = builder.Sql("SELECT 1; SELECT 2;");
		var afterParams = afterSql.Parameters(new { Id = 1 });

		afterSql.Should().BeSameAs(builder);
		afterParams.Should().BeSameAs(builder);
	}

	#endregion
}

/// <summary>
/// Integration tests for the fluent query builders. These tests verify that the
/// builders correctly delegate to the underlying <see cref="IDbService"/> methods
/// and produce the same results as calling those methods directly.
/// </summary>
[Collection("Database")]
public class DbFluentBuilderIntegrationTests : IAsyncLifetime
{
	private readonly DatabaseFixture _fixture;
	private readonly IDbService _db;

	public DbFluentBuilderIntegrationTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
		_db = fixture.DbService;
	}

	public async Task InitializeAsync()
	{
		await _fixture.ClearTestOrdersAsync();
		await _fixture.ClearTestProductsAsync();
	}

	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== Helpers ===>

	private async Task<(Guid Order1Id, Guid Order2Id)> SeedTwoOrdersWithLinesAndNotesAsync()
	{
		var order1Id = Guid.NewGuid();
		var order2Id = Guid.NewGuid();

		await _db.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id1, 'Customer A', 100.00, NOW()),
			       (@Id2, 'Customer B', 200.00, NOW());
			""",
			new { Id1 = order1Id, Id2 = order2Id });

		await _db.ExecuteAsync(
			"""
			INSERT INTO test_order_lines (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId1, 'Product 1', 2, 25.00),
			       (@OrderId1, 'Product 2', 1, 50.00),
			       (@OrderId2, 'Product 3', 4, 50.00);
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		await _db.ExecuteAsync(
			"""
			INSERT INTO test_order_notes (order_id, text, created_at)
			VALUES (@OrderId1, 'Note for A', NOW()),
			       (@OrderId2, 'Note 1 for B', NOW()),
			       (@OrderId2, 'Note 2 for B', NOW());
			""",
			new { OrderId1 = order1Id, OrderId2 = order2Id });

		return (order1Id, order2Id);
	}

	#endregion

	#region <=== DbJoinQuery — Single Child ===>

	[Fact]
	public async Task DbJoinQuery_SingleChild_ToList_ShouldMapCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName",
			       o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
			       l.id AS "Id", l.order_id AS "OrderId",
			       l.product_name AS "ProductName",
			       l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			ORDER BY o.customer_name, l.product_name
			""";

		var orders = _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql(sql)
			.ChildSelector(o => o.Lines)
			.ParentKey(o => o.Id)
			.ChildKey(l => l.Id)
			.SplitOn("Id")
			.ToList();

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
	public async Task DbJoinQuery_SingleChild_ToListAsync_ShouldMapCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName",
			       o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
			       l.id AS "Id", l.order_id AS "OrderId",
			       l.product_name AS "ProductName",
			       l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			ORDER BY o.customer_name
			""";

		var orders = await _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql(sql)
			.ChildSelector(o => o.Lines)
			.ParentKey(o => o.Id)
			.ChildKey(l => l.Id)
			.SplitOn("Id")
			.ToListAsync();

		orders.Should().HaveCount(2);
		orders.First(o => o.CustomerName == "Customer A")
			.Lines.Should().HaveCount(2);
		orders.First(o => o.CustomerName == "Customer B")
			.Lines.Should().HaveCount(1);
	}

	[Fact]
	public async Task DbJoinQuery_SingleChild_WithNoChildren_ShouldReturnEmptyCollections()
	{
		var orderId = Guid.NewGuid();
		await _db.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id, 'Lonely Customer', 0.00, NOW());
			""",
			new { Id = orderId });

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName",
			       o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
			       l.id AS "Id", l.order_id AS "OrderId",
			       l.product_name AS "ProductName",
			       l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			""";

		var orders = await _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql(sql)
			.ChildSelector(o => o.Lines)
			.ParentKey(o => o.Id)
			.ChildKey(l => l.Id)
			.SplitOn("Id")
			.ToListAsync();

		orders.Should().HaveCount(1);
		orders.First().Lines.Should().BeEmpty();
	}

	[Fact]
	public async Task DbJoinQuery_SingleChild_WithParameters_ShouldFilterCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName",
			       o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
			       l.id AS "Id", l.order_id AS "OrderId",
			       l.product_name AS "ProductName",
			       l.quantity AS "Quantity", l.unit_price AS "UnitPrice"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			WHERE o.customer_name = @CustomerName
			""";

		var orders = await _db.QueryWithJoin<TestOrder, TestOrderLine>()
			.Sql(sql)
			.ChildSelector(o => o.Lines)
			.ParentKey(o => o.Id)
			.ChildKey(l => l.Id)
			.SplitOn("Id")
			.Parameters(new { CustomerName = "Customer A" })
			.ToListAsync();

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer A");
		orders.First().Lines.Should().HaveCount(2);
	}

	#endregion

	#region <=== DbJoinQuery — Two Children ===>

	[Fact]
	public async Task DbJoinQuery_TwoChildren_ToList_ShouldMapCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName",
			       o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
			       l.id AS "Id", l.order_id AS "OrderId",
			       l.product_name AS "ProductName",
			       l.quantity AS "Quantity", l.unit_price AS "UnitPrice",
			       n.id AS "Id", n.order_id AS "OrderId",
			       n.text AS "Text", n.created_at AS "CreatedAt"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			LEFT JOIN test_order_notes n ON n.order_id = o.id
			ORDER BY o.customer_name, l.product_name, n.text
			""";

		var orders = _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.Sql(sql)
			.ChildSelector1(o => o.Lines)
			.ChildSelector2(o => o.Notes)
			.ParentKey(o => o.Id)
			.ChildKey1(l => l.Id)
			.ChildKey2(n => n.Id)
			.SplitOn("Id,Id")
			.ToList();

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
	public async Task DbJoinQuery_TwoChildren_ToListAsync_ShouldMapCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT o.id AS "Id", o.customer_name AS "CustomerName",
			       o.total_amount AS "TotalAmount", o.created_at AS "CreatedAt",
			       l.id AS "Id", l.order_id AS "OrderId",
			       l.product_name AS "ProductName",
			       l.quantity AS "Quantity", l.unit_price AS "UnitPrice",
			       n.id AS "Id", n.order_id AS "OrderId",
			       n.text AS "Text", n.created_at AS "CreatedAt"
			FROM test_orders o
			LEFT JOIN test_order_lines l ON l.order_id = o.id
			LEFT JOIN test_order_notes n ON n.order_id = o.id
			ORDER BY o.customer_name
			""";

		var orders = await _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.Sql(sql)
			.ChildSelector1(o => o.Lines)
			.ChildSelector2(o => o.Notes)
			.ParentKey(o => o.Id)
			.ChildKey1(l => l.Id)
			.ChildKey2(n => n.Id)
			.SplitOn("Id,Id")
			.ToListAsync();

		orders.Should().HaveCount(2);
		orders.All(o => o.Lines.Count >= 1).Should().BeTrue();
		orders.All(o => o.Notes.Count >= 1).Should().BeTrue();
	}

	#endregion

	#region <=== DbMultiQuery ===>

	[Fact]
	public async Task DbMultiQuery_Execute_ShouldMapCorrectly()
	{
		var product = new TestProduct
		{
			Name = "Featured Product",
			Description = "This is featured",
			Price = 99.99m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		await _db.InsertAsync(product);

		var product2 = new TestProduct
		{
			Name = "Recent Product",
			Price = 49.99m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		await _db.InsertAsync(product2);

		var orderId = Guid.NewGuid();
		await _db.ExecuteAsync(
			"""
			INSERT INTO test_orders (id, customer_name, total_amount, created_at)
			VALUES (@Id, 'Test Customer', 100.00, NOW());
			""",
			new { Id = orderId });

		await _db.ExecuteAsync(
			"""
			INSERT INTO test_order_lines
			    (order_id, product_name, quantity, unit_price)
			VALUES (@OrderId, 'Line Item 1', 2, 25.00);
			""",
			new { OrderId = orderId });

		var sql = """
			SELECT id AS "Id", name AS "Name", description AS "Description",
			       price AS "Price", quantity AS "Quantity",
			       is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate",
			       discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt",
			       last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products
			WHERE name = 'Featured Product' LIMIT 1;

			SELECT id AS "Id", name AS "Name", description AS "Description",
			       price AS "Price", quantity AS "Quantity",
			       is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate",
			       discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt",
			       last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products ORDER BY name;

			SELECT id AS "Id", order_id AS "OrderId",
			       product_name AS "ProductName",
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines;
			""";

		var dashboard = _db.QueryMultiple<TestDashboard>()
			.Sql(sql)
			.Execute();

		dashboard.Should().NotBeNull();
		dashboard.FeaturedProduct.Should().NotBeNull();
		dashboard.FeaturedProduct!.Name.Should().Be("Featured Product");
		dashboard.RecentProducts.Should().HaveCount(2);
		dashboard.RecentOrderLines.Should().HaveCount(1);
	}

	[Fact]
	public async Task DbMultiQuery_ExecuteAsync_ShouldMapCorrectly()
	{
		var product = new TestProduct
		{
			Name = "Async Product",
			Price = 10.00m,
			Quantity = 1,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		await _db.InsertAsync(product);

		var sql = """
			SELECT id AS "Id", name AS "Name", description AS "Description",
			       price AS "Price", quantity AS "Quantity",
			       is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate",
			       discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt",
			       last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products WHERE 1 = 0 LIMIT 1;

			SELECT id AS "Id", name AS "Name", description AS "Description",
			       price AS "Price", quantity AS "Quantity",
			       is_active AS "IsActive", status AS "Status",
			       created_at AS "CreatedAt", updated_at AS "UpdatedAt",
			       release_date AS "ReleaseDate",
			       discontinued_date AS "DiscontinuedDate",
			       published_at AS "PublishedAt",
			       last_reviewed_at AS "LastReviewedAt",
			       metadata AS "Metadata"
			FROM test_products;

			SELECT id AS "Id", order_id AS "OrderId",
			       product_name AS "ProductName",
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines WHERE 1 = 0;
			""";

		var dashboard = await _db.QueryMultiple<TestDashboard>()
			.Sql(sql)
			.ExecuteAsync();

		dashboard.Should().NotBeNull();
		dashboard.FeaturedProduct.Should().BeNull();
		dashboard.RecentProducts.Should().HaveCount(1);
		dashboard.RecentProducts.First().Name.Should().Be("Async Product");
		dashboard.RecentOrderLines.Should().BeEmpty();
	}

	#endregion

	#region <=== DbMultiQueryList ===>

	[Fact]
	public async Task DbMultiQueryList_ToList_ShouldMapCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName",
			       total_amount AS "TotalAmount", created_at AS "CreatedAt"
			FROM test_orders ORDER BY customer_name;

			SELECT id AS "Id", order_id AS "OrderId",
			       product_name AS "ProductName",
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines;

			SELECT id AS "Id", order_id AS "OrderId",
			       text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes;
			""";

		var orders = _db.QueryMultipleList<TestOrder>()
			.Sql(sql)
			.ToList();

		orders.Should().HaveCount(2);

		var orderA = orders.First(o => o.CustomerName == "Customer A");
		orderA.Lines.Should().HaveCount(2);
		orderA.Notes.Should().HaveCount(1);

		var orderB = orders.First(o => o.CustomerName == "Customer B");
		orderB.Lines.Should().HaveCount(1);
		orderB.Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbMultiQueryList_ToListAsync_ShouldMapCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName",
			       total_amount AS "TotalAmount", created_at AS "CreatedAt"
			FROM test_orders ORDER BY customer_name;

			SELECT id AS "Id", order_id AS "OrderId",
			       product_name AS "ProductName",
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines;

			SELECT id AS "Id", order_id AS "OrderId",
			       text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes;
			""";

		var orders = await _db.QueryMultipleList<TestOrder>()
			.Sql(sql)
			.ToListAsync();

		orders.Should().HaveCount(2);
		orders.First(o => o.CustomerName == "Customer A")
			.Lines.Should().HaveCount(2);
		orders.First(o => o.CustomerName == "Customer B")
			.Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbMultiQueryList_WithParameters_ShouldFilterCorrectly()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName",
			       total_amount AS "TotalAmount", created_at AS "CreatedAt"
			FROM test_orders WHERE customer_name = @CustomerName;

			SELECT id AS "Id", order_id AS "OrderId",
			       product_name AS "ProductName",
			       quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines WHERE order_id IN (
			    SELECT id FROM test_orders
			    WHERE customer_name = @CustomerName
			);

			SELECT id AS "Id", order_id AS "OrderId",
			       text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes WHERE order_id IN (
			    SELECT id FROM test_orders
			    WHERE customer_name = @CustomerName
			);
			""";

		var orders = await _db.QueryMultipleList<TestOrder>()
			.Sql(sql)
			.Parameters(new { CustomerName = "Customer A" })
			.ToListAsync();

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer A");
		orders.First().Lines.Should().HaveCount(2);
		orders.First().Notes.Should().HaveCount(1);
	}

	[Fact]
	public async Task DbMultiQueryList_WithNoParents_ShouldReturnEmptyList()
	{
		var sql = """
			SELECT id AS "Id", customer_name AS "CustomerName",
				   total_amount AS "TotalAmount", created_at AS "CreatedAt"
			FROM test_orders WHERE 1 = 0;

			SELECT id AS "Id", order_id AS "OrderId",
				   product_name AS "ProductName",
				   quantity AS "Quantity", unit_price AS "UnitPrice"
			FROM test_order_lines WHERE 1 = 0;

			SELECT id AS "Id", order_id AS "OrderId",
				   text AS "Text", created_at AS "CreatedAt"
			FROM test_order_notes WHERE 1 = 0;
			""";

		var orders = await _db.QueryMultipleList<TestOrder>()
			.Sql(sql)
			.ToListAsync();

		orders.Should().BeEmpty();
	}

	#endregion

	#region <=== SQL-free DbMultiQueryList ===>

	[Fact]
	public async Task DbMultiQueryList_SqlFree_NoFilters_ShouldReturnAll()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db.QueryMultipleList<TestOrder>()
			.ToListAsync();

		orders.Should().HaveCount(2);

		var orderA = orders.First(
			o => o.CustomerName == "Customer A");
		orderA.Lines.Should().HaveCount(2);
		orderA.Notes.Should().HaveCount(1);

		var orderB = orders.First(
			o => o.CustomerName == "Customer B");
		orderB.Lines.Should().HaveCount(1);
		orderB.Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbMultiQueryList_SqlFree_WithWhere_ShouldFilter()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db.QueryMultipleList<TestOrder>()
			.Where(o => o.CustomerName == "Customer A")
			.ToListAsync();

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer A");
		orders.First().Lines.Should().HaveCount(2);
		orders.First().Notes.Should().HaveCount(1);
	}

	[Fact]
	public async Task DbMultiQueryList_SqlFree_WithOrderByAndLimit(
		)
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db.QueryMultipleList<TestOrder>()
			.OrderByDescending(o => o.CustomerName)
			.Limit(1)
			.ToListAsync();

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer B");
		orders.First().Lines.Should().HaveCount(1);
		orders.First().Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbMultiQueryList_SqlFree_Sync_ShouldWork()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = _db.QueryMultipleList<TestOrder>()
			.Where(o => o.TotalAmount > 150m)
			.ToList();

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer B");
	}

	#endregion

	#region <=== SQL-free DbJoinQuery — Single Child ===>

	[Fact]
	public async Task DbJoinQuery_SqlFree_SingleChild_NoFilters()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db
			.QueryWithJoin<TestOrder, TestOrderLine>()
			.ChildSelector(o => o.Lines)
			.ToListAsync();

		orders.Should().HaveCount(2);
		orders.First(o => o.CustomerName == "Customer A")
			.Lines.Should().HaveCount(2);
		orders.First(o => o.CustomerName == "Customer B")
			.Lines.Should().HaveCount(1);
	}

	[Fact]
	public async Task DbJoinQuery_SqlFree_SingleChild_WithWhere()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db
			.QueryWithJoin<TestOrder, TestOrderLine>()
			.ChildSelector(o => o.Lines)
			.Where(o => o.CustomerName == "Customer A")
			.ToListAsync();

		orders.Should().HaveCount(1);
		orders.First().CustomerName.Should().Be("Customer A");
		orders.First().Lines.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbJoinQuery_SqlFree_SingleChild_AutoDerived()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db
			.QueryWithJoin<TestOrder, TestOrderLine>()
			.Where(o => o.TotalAmount >= 100m)
			.OrderBy(o => o.CustomerName)
			.ToListAsync();

		orders.Should().HaveCount(2);
		orders[0].CustomerName.Should().Be("Customer A");
		orders[0].Lines.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbJoinQuery_SqlFree_SingleChild_Sync()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = _db
			.QueryWithJoin<TestOrder, TestOrderLine>()
			.ChildSelector(o => o.Lines)
			.Where(o => o.CustomerName == "Customer B")
			.ToList();

		orders.Should().HaveCount(1);
		orders.First().Lines.Should().HaveCount(1);
	}

	#endregion

	#region <=== SQL-free DbJoinQuery — Two Children ===>

	[Fact]
	public async Task DbJoinQuery_SqlFree_TwoChildren_NoFilters()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.ChildSelector1(o => o.Lines)
			.ChildSelector2(o => o.Notes)
			.ToListAsync();

		orders.Should().HaveCount(2);

		var orderA = orders.First(
			o => o.CustomerName == "Customer A");
		orderA.Lines.Should().HaveCount(2);
		orderA.Notes.Should().HaveCount(1);

		var orderB = orders.First(
			o => o.CustomerName == "Customer B");
		orderB.Lines.Should().HaveCount(1);
		orderB.Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbJoinQuery_SqlFree_TwoChildren_WithWhere()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.ChildSelector1(o => o.Lines)
			.ChildSelector2(o => o.Notes)
			.Where(o => o.CustomerName == "Customer B")
			.ToListAsync();

		orders.Should().HaveCount(1);
		orders.First().Lines.Should().HaveCount(1);
		orders.First().Notes.Should().HaveCount(2);
	}

	[Fact]
	public async Task DbJoinQuery_SqlFree_TwoChildren_AutoDerived()
	{
		await SeedTwoOrdersWithLinesAndNotesAsync();

		var orders = await _db
			.QueryWithJoin<TestOrder, TestOrderLine, TestOrderNote>()
			.OrderBy(o => o.CustomerName)
			.ToListAsync();

		orders.Should().HaveCount(2);
		orders[0].CustomerName.Should().Be("Customer A");
		orders[0].Lines.Should().HaveCount(2);
		orders[0].Notes.Should().HaveCount(1);
	}

	#endregion
}
