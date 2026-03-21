using FluentAssertions;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Integration tests for <see cref="DbQuery{T}"/> — the fluent expression-based
/// query builder exposed via <see cref="IDbService.Query{T}()"/>.
/// </summary>
[Collection("Database")]
public class DbQueryIntegrationTests : IAsyncLifetime
{
	private readonly DatabaseFixture _fixture;
	private readonly IDbService _db;

	public DbQueryIntegrationTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
		_db = fixture.DbService;
	}

	public Task InitializeAsync() => _fixture.ClearTestProductsAsync();
	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== Helpers ===>

	private TestProduct MakeProduct(
		string name,
		decimal price = 10m,
		int quantity = 5,
		bool isActive = true,
		ProductStatus status = ProductStatus.Active,
		string? description = null)
		=> new TestProduct
		{
			Name = name,
			Price = price,
			Quantity = quantity,
			IsActive = isActive,
			Status = status,
			Description = description,
			CreatedAt = DateTime.UtcNow,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = DateTimeOffset.UtcNow
		};

	#endregion

	#region <=== ToListAsync — no filter ===>

	[Fact]
	public async Task ToListAsync_NoConditions_ShouldReturnAllEntities()
	{
		await _db.InsertAsync(MakeProduct("A"));
		await _db.InsertAsync(MakeProduct("B"));

		var result = await _db.Query<TestProduct>().ToListAsync();

		result.Should().HaveCount(2);
	}

	[Fact]
	public async Task ToListAsync_EmptyTable_ShouldReturnEmptyCollection()
	{
		var result = await _db.Query<TestProduct>().ToListAsync();

		result.Should().BeEmpty();
	}

	#endregion

	#region <=== Where — equality ===>

	[Fact]
	public async Task ToListAsync_WhereEquality_ShouldFilterCorrectly()
	{
		await _db.InsertAsync(MakeProduct("Active 1", isActive: true));
		await _db.InsertAsync(MakeProduct("Active 2", isActive: true));
		await _db.InsertAsync(MakeProduct("Inactive", isActive: false));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.IsActive == true)
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().OnlyContain(p => p.IsActive);
	}

	[Fact]
	public async Task ToListAsync_WhereNotEqual_ShouldFilterCorrectly()
	{
		await _db.InsertAsync(MakeProduct("A", price: 10m));
		await _db.InsertAsync(MakeProduct("B", price: 20m));
		await _db.InsertAsync(MakeProduct("C", price: 10m));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Price != 10m)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("B");
	}

	[Fact]
	public async Task ToListAsync_WhereBooleanShorthand_ShouldMapToTrue()
	{
		await _db.InsertAsync(MakeProduct("Active",   isActive: true));
		await _db.InsertAsync(MakeProduct("Inactive", isActive: false));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.IsActive)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Active");
	}

	[Fact]
	public async Task ToListAsync_WhereNegatedBoolean_ShouldMapToFalse()
	{
		await _db.InsertAsync(MakeProduct("Active",   isActive: true));
		await _db.InsertAsync(MakeProduct("Inactive", isActive: false));

		var result = await _db.Query<TestProduct>()
			.Where(p => !p.IsActive)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Inactive");
	}

	[Fact]
	public async Task ToListAsync_WhereEnumEquality_ShouldFilterByEnumValue()
	{
		await _db.InsertAsync(MakeProduct("Active",   status: ProductStatus.Active));
		await _db.InsertAsync(MakeProduct("Draft",    status: ProductStatus.Draft));
		await _db.InsertAsync(MakeProduct("Disc",     status: ProductStatus.Discontinued));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Status == ProductStatus.Active)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Active");
	}

	[Fact]
	public async Task ToListAsync_WhereCapturedLocalVariable_ShouldWork()
	{
		await _db.InsertAsync(MakeProduct("Widget", price: 50m));
		await _db.InsertAsync(MakeProduct("Gadget", price: 150m));

		var threshold = 100m;

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Price < threshold)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Widget");
	}

	#endregion

	#region <=== Where — comparisons ===>

	[Fact]
	public async Task ToListAsync_WhereGreaterThan_ShouldFilterCorrectly()
	{
		await _db.InsertAsync(MakeProduct("Cheap",     price: 5m));
		await _db.InsertAsync(MakeProduct("Mid",       price: 50m));
		await _db.InsertAsync(MakeProduct("Expensive", price: 500m));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Price > 10m)
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().Contain(p => p.Name == "Mid");
		result.Should().Contain(p => p.Name == "Expensive");
	}

	[Fact]
	public async Task ToListAsync_WhereLessThanOrEqual_ShouldFilterCorrectly()
	{
		await _db.InsertAsync(MakeProduct("A", quantity: 1));
		await _db.InsertAsync(MakeProduct("B", quantity: 5));
		await _db.InsertAsync(MakeProduct("C", quantity: 10));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Quantity <= 5)
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().NotContain(p => p.Name == "C");
	}

	[Fact]
	public async Task ToListAsync_WhereGreaterThanOrEqual_ShouldFilterCorrectly()
	{
		await _db.InsertAsync(MakeProduct("A", quantity: 1));
		await _db.InsertAsync(MakeProduct("B", quantity: 5));
		await _db.InsertAsync(MakeProduct("C", quantity: 10));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Quantity >= 5)
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().NotContain(p => p.Name == "A");
	}

	#endregion

	#region <=== Where — logical operators ===>

	[Fact]
	public async Task ToListAsync_WhereAndAlso_ShouldRequireBothConditions()
	{
		await _db.InsertAsync(MakeProduct("Active+Cheap",    price: 5m,   isActive: true));
		await _db.InsertAsync(MakeProduct("Active+Expensive",price: 500m, isActive: true));
		await _db.InsertAsync(MakeProduct("Inactive+Cheap",  price: 5m,   isActive: false));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.IsActive && p.Price < 10m)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Active+Cheap");
	}

	[Fact]
	public async Task ToListAsync_WhereOrElse_ShouldAcceptEitherCondition()
	{
		await _db.InsertAsync(MakeProduct("Draft", status: ProductStatus.Draft));
		await _db.InsertAsync(MakeProduct("Active", status: ProductStatus.Active));
		await _db.InsertAsync(MakeProduct("Disc", status: ProductStatus.Discontinued));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Status == ProductStatus.Draft
				     || p.Status == ProductStatus.Active)
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().NotContain(p => p.Status == ProductStatus.Discontinued);
	}

	[Fact]
	public async Task ToListAsync_MultipleWhereCalls_ShouldCombineWithAnd()
	{
		await _db.InsertAsync(MakeProduct("Passes",    price: 50m, quantity: 10, isActive: true));
		await _db.InsertAsync(MakeProduct("WrongPrice",price: 5m,  quantity: 10, isActive: true));
		await _db.InsertAsync(MakeProduct("WrongQty",  price: 50m, quantity: 1,  isActive: true));
		await _db.InsertAsync(MakeProduct("Inactive",  price: 50m, quantity: 10, isActive: false));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.IsActive)
			.Where(p => p.Price > 20m)
			.Where(p => p.Quantity >= 5)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Passes");
	}

	#endregion

	#region <=== Where — null checks ===>

	[Fact]
	public async Task ToListAsync_WhereEqualsNull_ShouldProduceIsNull()
	{
		await _db.InsertAsync(MakeProduct("HasDesc",  description: "some desc"));
		await _db.InsertAsync(MakeProduct("NoDesc",   description: null));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Description == null)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("NoDesc");
	}

	[Fact]
	public async Task ToListAsync_WhereNotEqualsNull_ShouldProduceIsNotNull()
	{
		await _db.InsertAsync(MakeProduct("HasDesc", description: "hello"));
		await _db.InsertAsync(MakeProduct("NoDesc",  description: null));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Description != null)
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("HasDesc");
	}

	#endregion

	#region <=== Where — string methods ===>

	[Fact]
	public async Task ToListAsync_WhereStringContains_ShouldUseLikePattern()
	{
		await _db.InsertAsync(MakeProduct("Apple iPhone"));
		await _db.InsertAsync(MakeProduct("Apple iPad"));
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.Contains("Apple"))
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().OnlyContain(p => p.Name.Contains("Apple"));
	}

	[Fact]
	public async Task ToListAsync_WhereStringStartsWith_ShouldMatchPrefix()
	{
		await _db.InsertAsync(MakeProduct("Apple iPhone"));
		await _db.InsertAsync(MakeProduct("Apple iPad"));
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.StartsWith("Samsung"))
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Samsung Galaxy");
	}

	[Fact]
	public async Task ToListAsync_WhereStringEndsWith_ShouldMatchSuffix()
	{
		await _db.InsertAsync(MakeProduct("Apple iPhone 15"));
		await _db.InsertAsync(MakeProduct("Samsung Galaxy S15"));
		await _db.InsertAsync(MakeProduct("Google Pixel"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.EndsWith("S15"))
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Samsung Galaxy S15");
	}

	[Fact]
	public async Task ToListAsync_WhereStringContains_WithLikeSpecialChars_ShouldEscape()
	{
		await _db.InsertAsync(MakeProduct("50% Off Deal"));
		await _db.InsertAsync(MakeProduct("Normal Deal"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.Contains("50%"))
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("50% Off Deal");
	}

	#endregion

	#region <=== Where — ILIKE ===>

	[Fact]
	public async Task ToListAsync_WhereILikeContains_ShouldMatchCaseInsensitive()
	{
		await _db.InsertAsync(MakeProduct("Apple iPhone"));
		await _db.InsertAsync(MakeProduct("APPLE Watch"));
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.ILikeContains("apple"))
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().NotContain(p => p.Name == "Samsung Galaxy");
	}

	[Fact]
	public async Task ToListAsync_WhereILikeStartsWith_ShouldMatchCaseInsensitivePrefix()
	{
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));
		await _db.InsertAsync(MakeProduct("samsung galaxy s21"));
		await _db.InsertAsync(MakeProduct("Apple iPhone"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.ILikeStartsWith("Samsung"))
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().NotContain(p => p.Name == "Apple iPhone");
	}

	[Fact]
	public async Task ToListAsync_WhereILikeEndsWith_ShouldMatchCaseInsensitiveSuffix()
	{
		await _db.InsertAsync(MakeProduct("Galaxy S24"));
		await _db.InsertAsync(MakeProduct("redmi note s24"));
		await _db.InsertAsync(MakeProduct("Apple iPhone"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.ILikeEndsWith("s24"))
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().NotContain(p => p.Name == "Apple iPhone");
	}

	#endregion

	#region <=== Where — case folding ===>

	[Fact]
	public async Task ToListAsync_WhereToLower_ShouldMatchByNormalizedCase()
	{
		await _db.InsertAsync(MakeProduct("Apple iPhone"));
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.ToLower() == "apple iphone")
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Apple iPhone");
	}

	[Fact]
	public async Task ToListAsync_WhereToLowerInvariant_ShouldMatchByNormalizedCase()
	{
		await _db.InsertAsync(MakeProduct("Apple iPhone"));
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.ToLowerInvariant() == "apple iphone")
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Apple iPhone");
	}

	[Fact]
	public async Task ToListAsync_WhereToUpper_ShouldMatchByNormalizedCase()
	{
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));
		await _db.InsertAsync(MakeProduct("Apple iPhone"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.ToUpper() == "SAMSUNG GALAXY")
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Samsung Galaxy");
	}

	[Fact]
	public async Task ToListAsync_WhereToUpperInvariant_ShouldMatchByNormalizedCase()
	{
		await _db.InsertAsync(MakeProduct("Samsung Galaxy"));
		await _db.InsertAsync(MakeProduct("Apple iPhone"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Name.ToUpperInvariant() == "SAMSUNG GALAXY")
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Samsung Galaxy");
	}

	#endregion

	#region <=== Where — collection Contains ===>

	[Fact]
	public async Task ToListAsync_WhereInstanceContains_ShouldUseAny()
	{
		var p1 = await _db.InsertAsync(MakeProduct("One"));
		var p2 = await _db.InsertAsync(MakeProduct("Two"));
		await _db.InsertAsync(MakeProduct("Three"));

		var ids = new List<Guid> { p1.Id, p2.Id };

		var result = await _db.Query<TestProduct>()
			.Where(p => ids.Contains(p.Id))
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Select(p => p.Id).Should().BeEquivalentTo(ids);
	}

	[Fact]
	public async Task ToListAsync_WhereStaticEnumerableContains_ShouldUseAny()
	{
		var p1 = await _db.InsertAsync(MakeProduct("A"));
		await _db.InsertAsync(MakeProduct("B"));

		var ids = new[] { p1.Id };

		var result = await _db.Query<TestProduct>()
			.Where(p => Enumerable.Contains(ids, p.Id))
			.ToListAsync();

		result.Should().HaveCount(1);
		result.Single().Id.Should().Be(p1.Id);
	}

	[Fact]
	public async Task ToListAsync_WhereContainsEmptyCollection_ShouldReturnEmpty()
	{
		await _db.InsertAsync(MakeProduct("A"));
		await _db.InsertAsync(MakeProduct("B"));

		var ids = new List<Guid>();

		var result = await _db.Query<TestProduct>()
			.Where(p => ids.Contains(p.Id))
			.ToListAsync();

		result.Should().BeEmpty();
	}

	#endregion

	#region <=== OrderBy ===>

	[Fact]
	public async Task ToListAsync_OrderByAscending_ShouldReturnSortedResults()
	{
		await _db.InsertAsync(MakeProduct("C", price: 30m));
		await _db.InsertAsync(MakeProduct("A", price: 10m));
		await _db.InsertAsync(MakeProduct("B", price: 20m));

		var result = (await _db.Query<TestProduct>()
			.OrderBy(p => p.Price)
			.ToListAsync()).ToList();

		result.Should().HaveCount(3);
		result[0].Name.Should().Be("A");
		result[1].Name.Should().Be("B");
		result[2].Name.Should().Be("C");
	}

	[Fact]
	public async Task ToListAsync_OrderByDescending_ShouldReturnReverseSortedResults()
	{
		await _db.InsertAsync(MakeProduct("C", price: 30m));
		await _db.InsertAsync(MakeProduct("A", price: 10m));
		await _db.InsertAsync(MakeProduct("B", price: 20m));

		var result = (await _db.Query<TestProduct>()
			.OrderByDescending(p => p.Price)
			.ToListAsync()).ToList();

		result.Should().HaveCount(3);
		result[0].Name.Should().Be("C");
		result[1].Name.Should().Be("B");
		result[2].Name.Should().Be("A");
	}

	[Fact]
	public async Task ToListAsync_ThenBy_ShouldSortBySecondaryColumn()
	{
		await _db.InsertAsync(MakeProduct("B", price: 10m, quantity: 2));
		await _db.InsertAsync(MakeProduct("A", price: 10m, quantity: 1));
		await _db.InsertAsync(MakeProduct("C", price: 20m, quantity: 3));

		var result = (await _db.Query<TestProduct>()
			.OrderBy(p => p.Price)
			.ThenBy(p => p.Quantity)
			.ToListAsync()).ToList();

		result[0].Name.Should().Be("A");
		result[1].Name.Should().Be("B");
		result[2].Name.Should().Be("C");
	}

	[Fact]
	public async Task ToListAsync_ThenByDescending_ShouldSortBySecondaryColumnDesc()
	{
		await _db.InsertAsync(MakeProduct("Y", price: 10m, quantity: 1));
		await _db.InsertAsync(MakeProduct("Z", price: 10m, quantity: 2));
		await _db.InsertAsync(MakeProduct("X", price: 20m, quantity: 5));

		var result = (await _db.Query<TestProduct>()
			.OrderBy(p => p.Price)
			.ThenByDescending(p => p.Quantity)
			.ToListAsync()).ToList();

		result[0].Name.Should().Be("Z");
		result[1].Name.Should().Be("Y");
		result[2].Name.Should().Be("X");
	}

	#endregion

	#region <=== Limit / Offset ===>

	[Fact]
	public async Task ToListAsync_WithLimit_ShouldLimitResults()
	{
		for (var i = 1; i <= 5; i++)
			await _db.InsertAsync(MakeProduct($"Product {i}", price: i * 10m));

		var result = await _db.Query<TestProduct>()
			.OrderBy(p => p.Price)
			.Limit(3)
			.ToListAsync();

		result.Should().HaveCount(3);
	}

	[Fact]
	public async Task ToListAsync_WithOffset_ShouldOffsetResults()
	{
		for (var i = 1; i <= 5; i++)
			await _db.InsertAsync(MakeProduct($"Product {i}", price: i * 10m));

		var result = await _db.Query<TestProduct>()
			.OrderBy(p => p.Price)
			.Offset(3)
			.ToListAsync();

		result.Should().HaveCount(2);
	}

	[Fact]
	public async Task ToListAsync_WithLimitAndOffset_ShouldPaginate()
	{
		for (var i = 1; i <= 10; i++)
			await _db.InsertAsync(MakeProduct($"Product {i:D2}", price: i * 10m));

		var page2 = (await _db.Query<TestProduct>()
			.OrderBy(p => p.Price)
			.Offset(3)
			.Limit(3)
			.ToListAsync()).ToList();

		page2.Should().HaveCount(3);
		page2[0].Name.Should().Be("Product 04");
		page2[1].Name.Should().Be("Product 05");
		page2[2].Name.Should().Be("Product 06");
	}

	#endregion

	#region <=== FirstOrDefaultAsync ===>

	[Fact]
	public async Task FirstOrDefaultAsync_ShouldReturnFirstMatchingEntity()
	{
		await _db.InsertAsync(MakeProduct("C", price: 30m));
		await _db.InsertAsync(MakeProduct("A", price: 10m));
		await _db.InsertAsync(MakeProduct("B", price: 20m));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Price >= 20m)
			.OrderBy(p => p.Price)
			.FirstOrDefaultAsync();

		result.Should().NotBeNull();
		result!.Name.Should().Be("B");
	}

	[Fact]
	public async Task FirstOrDefaultAsync_WithNoMatch_ShouldReturnNull()
	{
		await _db.InsertAsync(MakeProduct("A", price: 10m));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Price > 1000m)
			.FirstOrDefaultAsync();

		result.Should().BeNull();
	}

	#endregion

	#region <=== CountAsync ===>

	[Fact]
	public async Task CountAsync_WithNoFilter_ShouldReturnTotalCount()
	{
		await _db.InsertAsync(MakeProduct("A"));
		await _db.InsertAsync(MakeProduct("B"));
		await _db.InsertAsync(MakeProduct("C"));

		var count = await _db.Query<TestProduct>().CountAsync();

		count.Should().Be(3);
	}

	[Fact]
	public async Task CountAsync_WithWhere_ShouldReturnFilteredCount()
	{
		await _db.InsertAsync(MakeProduct("Active1", isActive: true));
		await _db.InsertAsync(MakeProduct("Active2", isActive: true));
		await _db.InsertAsync(MakeProduct("Inactive", isActive: false));

		var count = await _db.Query<TestProduct>()
			.Where(p => p.IsActive)
			.CountAsync();

		count.Should().Be(2);
	}

	[Fact]
	public async Task CountAsync_EmptyTable_ShouldReturnZero()
	{
		var count = await _db.Query<TestProduct>().CountAsync();

		count.Should().Be(0);
	}

	#endregion

	#region <=== ExistsAsync ===>

	[Fact]
	public async Task ExistsAsync_WhenMatchingRowExists_ShouldReturnTrue()
	{
		await _db.InsertAsync(MakeProduct("Widget", price: 99m));

		var exists = await _db.Query<TestProduct>()
			.Where(p => p.Price > 50m)
			.ExistsAsync();

		exists.Should().BeTrue();
	}

	[Fact]
	public async Task ExistsAsync_WhenNoMatchingRow_ShouldReturnFalse()
	{
		await _db.InsertAsync(MakeProduct("Widget", price: 10m));

		var exists = await _db.Query<TestProduct>()
			.Where(p => p.Price > 1000m)
			.ExistsAsync();

		exists.Should().BeFalse();
	}

	[Fact]
	public async Task ExistsAsync_EmptyTable_ShouldReturnFalse()
	{
		var exists = await _db.Query<TestProduct>().ExistsAsync();

		exists.Should().BeFalse();
	}

	#endregion

	#region <=== Sync Terminal Methods ===>

	[Fact]
	public void ToList_WithWhere_ShouldFilterCorrectly()
	{
		_db.Insert(MakeProduct("Active",   isActive: true));
		_db.Insert(MakeProduct("Inactive", isActive: false));

		var result = _db.Query<TestProduct>()
			.Where(p => p.IsActive)
			.ToList();

		result.Should().HaveCount(1);
		result.Single().Name.Should().Be("Active");
	}

	[Fact]
	public void FirstOrDefault_ShouldReturnFirstOrNull()
	{
		_db.Insert(MakeProduct("Only", price: 5m));

		var found = _db.Query<TestProduct>()
			.Where(p => p.Price == 5m)
			.FirstOrDefault();

		var missing = _db.Query<TestProduct>()
			.Where(p => p.Price == 999m)
			.FirstOrDefault();

		found.Should().NotBeNull();
		missing.Should().BeNull();
	}

	[Fact]
	public void Count_ShouldReturnCorrectCount()
	{
		_db.Insert(MakeProduct("A"));
		_db.Insert(MakeProduct("B"));

		var count = _db.Query<TestProduct>().Count();

		count.Should().Be(2);
	}

	[Fact]
	public void Exists_ShouldReturnTrueWhenRowExists()
	{
		_db.Insert(MakeProduct("Widget"));

		var exists  = _db.Query<TestProduct>().Where(p => p.Name == "Widget").Exists();
		var missing = _db.Query<TestProduct>().Where(p => p.Name == "NoSuchName").Exists();

		exists.Should().BeTrue();
		missing.Should().BeFalse();
	}

	#endregion

	#region <=== Combined scenarios ===>

	[Fact]
	public async Task FullQuery_WhereOrderByLimit_ShouldProduceCorrectResults()
	{
		// Models the target use-case: SELECT * FROM emails WHERE status = @s
		//                             ORDER BY created_on DESC LIMIT @n
		await _db.InsertAsync(MakeProduct("Active1",  isActive: true,  price: 30m));
		await _db.InsertAsync(MakeProduct("Active2",  isActive: true,  price: 10m));
		await _db.InsertAsync(MakeProduct("Active3",  isActive: true,  price: 20m));
		await _db.InsertAsync(MakeProduct("Inactive", isActive: false, price: 40m));

		var result = (await _db.Query<TestProduct>()
			.Where(p => p.IsActive)
			.OrderByDescending(p => p.Price)
			.Limit(2)
			.ToListAsync()).ToList();

		result.Should().HaveCount(2);
		result[0].Name.Should().Be("Active1"); // price 30
		result[1].Name.Should().Be("Active3"); // price 20
	}

	[Fact]
	public async Task Query_WhereOrderByOffsetLimit_ShouldPaginateFilteredResults()
	{
		for (var i = 1; i <= 8; i++)
			await _db.InsertAsync(MakeProduct($"P{i:D2}", price: i * 5m,
				isActive: i % 2 == 0));

		// Active products: P02(10), P04(20), P06(30), P08(40) — offset 1, limit 2
		var result = (await _db.Query<TestProduct>()
			.Where(p => p.IsActive)
			.OrderBy(p => p.Price)
			.Offset(1)
			.Limit(2)
			.ToListAsync()).ToList();

		result.Should().HaveCount(2);
		result[0].Name.Should().Be("P04");
		result[1].Name.Should().Be("P06");
	}

	[Fact]
	public async Task Query_ComplexWhere_AndOrCombined_ShouldWork()
	{
		await _db.InsertAsync(MakeProduct("Cheap+Active",
			price: 5m, isActive: true,  status: ProductStatus.Active));
		await _db.InsertAsync(MakeProduct("Expensive+Active",
			price: 500m, isActive: true,  status: ProductStatus.Active));
		await _db.InsertAsync(MakeProduct("Draft+Active",
			price: 5m, isActive: true,  status: ProductStatus.Draft));
		await _db.InsertAsync(MakeProduct("Inactive",
			price: 5m, isActive: false, status: ProductStatus.Active));

		// (status == Active || status == Draft) && isActive && price < 100
		var result = await _db.Query<TestProduct>()
			.Where(p => (p.Status == ProductStatus.Active
					  || p.Status == ProductStatus.Draft)
					 && p.IsActive
					 && p.Price < 100m)
			.ToListAsync();

		result.Should().HaveCount(2);
		result.Should().Contain(p => p.Name == "Cheap+Active");
		result.Should().Contain(p => p.Name == "Draft+Active");
	}

	[Fact]
	public async Task Query_GetById_ShouldReturnSingleEntity()
	{
		var inserted = await _db.InsertAsync(MakeProduct("Unique"));
		await _db.InsertAsync(MakeProduct("Other"));

		var result = await _db.Query<TestProduct>()
			.Where(p => p.Id == inserted.Id)
			.FirstOrDefaultAsync();

		result.Should().NotBeNull();
		result!.Id.Should().Be(inserted.Id);
		result.Name.Should().Be("Unique");
	}

	#endregion

	#region <=== Argument validation ===>

	[Fact]
	public void Limit_NegativeCount_ShouldThrow()
	{
		var act = () => _db.Query<TestProduct>().Limit(-1);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Offset_NegativeCount_ShouldThrow()
	{
		var act = () => _db.Query<TestProduct>().Offset(-1);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void Where_NullPredicate_ShouldThrow()
	{
		var act = () => _db.Query<TestProduct>()
			.Where(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task Where_UnknownProperty_ShouldThrow()
	{
		// OrderBy with a property that is [External] or otherwise unmapped
		// must throw ArgumentException at execution time.
		var act = async () => await _db.Query<TestProduct>()
			.Where(p => p.CategoryName == "x") // [External]
			.ToListAsync();

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*CategoryName*");
	}

	#endregion
}
