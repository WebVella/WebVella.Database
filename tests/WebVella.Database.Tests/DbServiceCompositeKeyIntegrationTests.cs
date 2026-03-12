using FluentAssertions;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Integration tests for <see cref="DbService"/> with composite key entities.
/// </summary>
[Collection("Database")]
public class DbServiceCompositeKeyIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly IDbService _dbService;

    public DbServiceCompositeKeyIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _dbService = fixture.DbService;
    }

    public Task InitializeAsync() => _fixture.ClearAllTestDataAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region <=== InsertAsync Tests ===>

        [Fact]
        public async Task InsertAsync_WithCompositeKey_ShouldReturnEntityWithBothKeys()
        {
            var orderItem = new TestOrderItem
            {
                Quantity = 5,
                UnitPrice = 10.00m,
                TotalPrice = 50.00m,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await _dbService.InsertAsync(orderItem);

            inserted.Should().NotBeNull();
            inserted.OrderId.Should().NotBe(Guid.Empty);
            inserted.ProductId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task InsertAsync_MultipleCompositeKeyEntities_ShouldGenerateUniqueKeys()
        {
            var items = Enumerable.Range(1, 3)
                .Select(i => new TestOrderItem
                {
                    Quantity = i,
                    UnitPrice = i * 10.00m,
                    TotalPrice = i * i * 10.00m,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            var insertedItems = new List<TestOrderItem>();
            foreach (var item in items)
            {
                var inserted = await _dbService.InsertAsync(item);
                insertedItems.Add(inserted);
            }

            insertedItems.Should().HaveCount(3);
            var allOrderIds = insertedItems.Select(i => i.OrderId).ToList();
            var allProductIds = insertedItems.Select(i => i.ProductId).ToList();

            allOrderIds.Should().OnlyHaveUniqueItems();
            allProductIds.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task InsertAsync_WithEmptyCompositeKeys_ShouldAutoGenerateNewGuids()
        {
            var orderItem = new TestOrderItem
            {
                OrderId = Guid.Empty,
                ProductId = Guid.Empty,
                Quantity = 7,
                UnitPrice = 15.00m,
                TotalPrice = 105.00m,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await _dbService.InsertAsync(orderItem);

            inserted.OrderId.Should().NotBe(Guid.Empty);
            inserted.ProductId.Should().NotBe(Guid.Empty);
            orderItem.OrderId.Should().NotBe(Guid.Empty);
            orderItem.ProductId.Should().NotBe(Guid.Empty);
            orderItem.OrderId.Should().Be(inserted.OrderId);
            orderItem.ProductId.Should().Be(inserted.ProductId);

            var keys = new Dictionary<string, Guid>
            {
                ["OrderId"] = inserted.OrderId,
                ["ProductId"] = inserted.ProductId
            };
            var retrieved = await _dbService.GetAsync<TestOrderItem>(keys);
            retrieved.Should().NotBeNull();
            retrieved!.Quantity.Should().Be(7);
        }

        [Fact]
        public async Task InsertAsync_WithPresetCompositeKeys_ShouldUseProvidedGuids()
        {
            var presetOrderId = Guid.NewGuid();
            var presetProductId = Guid.NewGuid();
            var orderItem = new TestOrderItem
            {
                OrderId = presetOrderId,
                ProductId = presetProductId,
                Quantity = 8,
                UnitPrice = 20.00m,
                TotalPrice = 160.00m,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await _dbService.InsertAsync(orderItem);

            inserted.OrderId.Should().Be(presetOrderId);
            inserted.ProductId.Should().Be(presetProductId);
            orderItem.OrderId.Should().Be(presetOrderId);
            orderItem.ProductId.Should().Be(presetProductId);

            var keys = new Dictionary<string, Guid>
            {
                ["OrderId"] = inserted.OrderId,
                ["ProductId"] = inserted.ProductId
            };
            var retrieved = await _dbService.GetAsync<TestOrderItem>(keys);
            retrieved.Should().NotBeNull();
            retrieved!.OrderId.Should().Be(presetOrderId);
            retrieved.ProductId.Should().Be(presetProductId);
        }

        [Fact]
        public async Task InsertAsync_WithPartiallyEmptyCompositeKeys_ShouldAutoGenerateOnlyEmptyOnes()
        {
            var presetOrderId = Guid.NewGuid();
            var orderItem = new TestOrderItem
            {
                OrderId = presetOrderId,
                ProductId = Guid.Empty,
                Quantity = 9,
                UnitPrice = 25.00m,
                TotalPrice = 225.00m,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await _dbService.InsertAsync(orderItem);

            inserted.OrderId.Should().Be(presetOrderId);
            inserted.ProductId.Should().NotBe(Guid.Empty);
            orderItem.OrderId.Should().Be(presetOrderId);
            orderItem.ProductId.Should().NotBe(Guid.Empty);

            var keys = new Dictionary<string, Guid>
            {
                ["OrderId"] = inserted.OrderId,
                ["ProductId"] = inserted.ProductId
            };
            var retrieved = await _dbService.GetAsync<TestOrderItem>(keys);
            retrieved.Should().NotBeNull();
            retrieved!.OrderId.Should().Be(presetOrderId);
        }

        #endregion

        #region <=== GetAsync Tests ===>

        [Fact]
        public async Task GetAsync_WithCompositeKeyDictionary_ShouldReturnEntity()
        {
            var orderItem = new TestOrderItem
            {
                Quantity = 3,
                UnitPrice = 25.00m,
                TotalPrice = 75.00m,
                CreatedAt = DateTime.UtcNow
            };
            var inserted = await _dbService.InsertAsync(orderItem);

            var keys = new Dictionary<string, Guid>
            {
                ["OrderId"] = inserted.OrderId,
                ["ProductId"] = inserted.ProductId
            };
            var retrieved = await _dbService.GetAsync<TestOrderItem>(keys);

            retrieved.Should().NotBeNull();
            retrieved!.OrderId.Should().Be(inserted.OrderId);
            retrieved.ProductId.Should().Be(inserted.ProductId);
            retrieved.Quantity.Should().Be(3);
            retrieved.UnitPrice.Should().Be(25.00m);
            retrieved.TotalPrice.Should().Be(75.00m);
        }

    [Fact]
    public async Task GetAsync_WithCompositeKeyDictionary_NonExistent_ShouldReturnNull()
    {
        var keys = new Dictionary<string, Guid>
        {
            ["OrderId"] = Guid.NewGuid(),
            ["ProductId"] = Guid.NewGuid()
        };

        var result = await _dbService.GetAsync<TestOrderItem>(keys);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithSingleGuidOverload_OnCompositeKeyEntity_ShouldThrowException()
    {
        var act = async () => await _dbService.GetAsync<TestOrderItem>(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*has 2 key properties*Use GetAsync(Dictionary<string, Guid>)*");
    }

    [Fact]
    public async Task GetAsync_WithMissingKeyInDictionary_ShouldThrowArgumentException()
    {
        var keys = new Dictionary<string, Guid>
        {
            ["OrderId"] = Guid.NewGuid()
        };

        var act = async () => await _dbService.GetAsync<TestOrderItem>(keys);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Missing key property 'ProductId'*");
    }

    [Fact]
    public async Task GetAsync_WithNullDictionary_ShouldThrowArgumentNullException()
    {
        var act = async () => await _dbService.GetAsync<TestOrderItem>((Dictionary<string, Guid>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

	#region <=== UpdateAsync Tests ===>

		[Fact]
		public async Task UpdateAsync_WithCompositeKey_ShouldUpdateEntity()
		{
			var orderItem = new TestOrderItem
			{
				Quantity = 2,
				UnitPrice = 15.00m,
				TotalPrice = 30.00m,
				CreatedAt = DateTime.UtcNow
			};
			var inserted = await _dbService.InsertAsync(orderItem);

			inserted.Quantity = 10;
			inserted.TotalPrice = 150.00m;
			var updated = await _dbService.UpdateAsync(inserted);

			updated.Should().BeTrue();

			var keys = new Dictionary<string, Guid>
			{
				["OrderId"] = inserted.OrderId,
				["ProductId"] = inserted.ProductId
			};
			var retrieved = await _dbService.GetAsync<TestOrderItem>(keys);
			retrieved.Should().NotBeNull();
			retrieved!.Quantity.Should().Be(10);
			retrieved.TotalPrice.Should().Be(150.00m);
		}

		[Fact]
		public async Task UpdateAsync_WithCompositeKey_ShouldOnlyUpdateMatchingEntity()
		{
			var item1 = new TestOrderItem
			{
				Quantity = 1,
				UnitPrice = 10.00m,
				TotalPrice = 10.00m,
				CreatedAt = DateTime.UtcNow
			};
			var item2 = new TestOrderItem
			{
				Quantity = 2,
				UnitPrice = 20.00m,
				TotalPrice = 40.00m,
				CreatedAt = DateTime.UtcNow
			};

			var inserted1 = await _dbService.InsertAsync(item1);
			var inserted2 = await _dbService.InsertAsync(item2);

			inserted1.Quantity = 99;
			await _dbService.UpdateAsync(inserted1);

			var keys1 = new Dictionary<string, Guid>
			{
				["OrderId"] = inserted1.OrderId,
				["ProductId"] = inserted1.ProductId
			};
			var keys2 = new Dictionary<string, Guid>
			{
				["OrderId"] = inserted2.OrderId,
				["ProductId"] = inserted2.ProductId
			};
			var retrieved1 = await _dbService.GetAsync<TestOrderItem>(keys1);
			var retrieved2 = await _dbService.GetAsync<TestOrderItem>(keys2);

			retrieved1!.Quantity.Should().Be(99);
			retrieved2!.Quantity.Should().Be(2);
		}

		#endregion

		#region <=== DeleteAsync Tests ===>

		[Fact]
		public async Task DeleteAsync_WithCompositeKeyDictionary_ShouldDeleteEntity()
		{
			var orderItem = new TestOrderItem
			{
				Quantity = 5,
				UnitPrice = 20.00m,
				TotalPrice = 100.00m,
				CreatedAt = DateTime.UtcNow
			};
			var inserted = await _dbService.InsertAsync(orderItem);

			var keys = new Dictionary<string, Guid>
			{
				["OrderId"] = inserted.OrderId,
				["ProductId"] = inserted.ProductId
			};
			var deleted = await _dbService.DeleteAsync<TestOrderItem>(keys);

			deleted.Should().BeTrue();

			var retrieved = await _dbService.GetAsync<TestOrderItem>(keys);
			retrieved.Should().BeNull();
		}

    [Fact]
    public async Task DeleteAsync_WithCompositeKeyDictionary_NonExistent_ShouldReturnFalse()
    {
        var keys = new Dictionary<string, Guid>
        {
            ["OrderId"] = Guid.NewGuid(),
            ["ProductId"] = Guid.NewGuid()
        };

        var deleted = await _dbService.DeleteAsync<TestOrderItem>(keys);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithSingleGuidOverload_OnCompositeKeyEntity_ShouldThrowException()
    {
        var act = async () => await _dbService.DeleteAsync<TestOrderItem>(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*has 2 key properties*Use DeleteAsync(Dictionary<string, Guid>)*");
    }

    [Fact]
    public async Task DeleteAsync_WithMissingKeyInDictionary_ShouldThrowArgumentException()
    {
        var keys = new Dictionary<string, Guid>
        {
            ["ProductId"] = Guid.NewGuid()
        };

        var act = async () => await _dbService.DeleteAsync<TestOrderItem>(keys);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Missing key property 'OrderId'*");
    }

    [Fact]
    public async Task DeleteAsync_WithNullDictionary_ShouldThrowArgumentNullException()
    {
        var act = async () => await _dbService.DeleteAsync<TestOrderItem>((Dictionary<string, Guid>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

	[Fact]
	public async Task DeleteAsync_WithCompositeKey_ShouldOnlyDeleteMatchingEntity()
	{
		var item1 = new TestOrderItem
		{
			Quantity = 1,
			UnitPrice = 10.00m,
			TotalPrice = 10.00m,
			CreatedAt = DateTime.UtcNow
		};
		var item2 = new TestOrderItem
		{
			Quantity = 2,
			UnitPrice = 20.00m,
			TotalPrice = 40.00m,
			CreatedAt = DateTime.UtcNow
		};

		var inserted1 = await _dbService.InsertAsync(item1);
		var inserted2 = await _dbService.InsertAsync(item2);

		var keys1 = new Dictionary<string, Guid>
		{
			["OrderId"] = inserted1.OrderId,
			["ProductId"] = inserted1.ProductId
		};
		var keys2 = new Dictionary<string, Guid>
		{
			["OrderId"] = inserted2.OrderId,
			["ProductId"] = inserted2.ProductId
		};
		var deleted = await _dbService.DeleteAsync<TestOrderItem>(keys1);

		deleted.Should().BeTrue();

		var retrieved1 = await _dbService.GetAsync<TestOrderItem>(keys1);
		var retrieved2 = await _dbService.GetAsync<TestOrderItem>(keys2);

		retrieved1.Should().BeNull();
		retrieved2.Should().NotBeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithEntityOverload_CompositeKey_ShouldDeleteEntity()
	{
		var orderItem = new TestOrderItem
		{
			Quantity = 3,
			UnitPrice = 15.00m,
			TotalPrice = 45.00m,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(orderItem);

		var deleted = await _dbService.DeleteAsync(inserted);

		deleted.Should().BeTrue();

		var keys = new Dictionary<string, Guid>
		{
			["OrderId"] = inserted.OrderId,
			["ProductId"] = inserted.ProductId
		};
		var retrieved = await _dbService.GetAsync<TestOrderItem>(keys);
		retrieved.Should().BeNull();
	}

	#endregion

    #region <=== GetListAsync Tests ===>

    [Fact]
    public async Task GetListAsync_WithNoParameters_ShouldReturnAllEntities()
    {
        var items = Enumerable.Range(1, 3)
            .Select(i => new TestOrderItem
            {
                Quantity = i,
                UnitPrice = i * 10.00m,
                TotalPrice = i * i * 10.00m,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        foreach (var item in items)
        {
            await _dbService.InsertAsync(item);
        }

        var allItems = await _dbService.GetListAsync<TestOrderItem>();

        allItems.Should().HaveCount(3);
        allItems.Should().OnlyContain(i => i.OrderId != Guid.Empty && i.ProductId != Guid.Empty);
    }

	[Fact]
	public async Task GetListAsync_WithSpecificCompositeKeys_ShouldReturnMatchingEntities()
	{
		var item1 = new TestOrderItem
		{
			Quantity = 1,
			UnitPrice = 10.00m,
			TotalPrice = 10.00m,
			CreatedAt = DateTime.UtcNow
		};
		var item2 = new TestOrderItem
		{
			Quantity = 2,
			UnitPrice = 20.00m,
			TotalPrice = 40.00m,
			CreatedAt = DateTime.UtcNow
		};
		var item3 = new TestOrderItem
		{
			Quantity = 3,
			UnitPrice = 30.00m,
			TotalPrice = 90.00m,
			CreatedAt = DateTime.UtcNow
		};

		var inserted1 = await _dbService.InsertAsync(item1);
		var inserted2 = await _dbService.InsertAsync(item2);
		await _dbService.InsertAsync(item3);

		var keysList = new List<Dictionary<string, Guid>>
		{
			new Dictionary<string, Guid>
			{
				["OrderId"] = inserted1.OrderId,
				["ProductId"] = inserted1.ProductId
			},
			new Dictionary<string, Guid>
			{
				["OrderId"] = inserted2.OrderId,
				["ProductId"] = inserted2.ProductId
			}
		};
		var items = await _dbService.GetListAsync<TestOrderItem>(keysList);

		items.Should().HaveCount(2);
		items.Select(i => i.Quantity).Should().Contain(new[] { 1, 2 });
		items.Select(i => i.Quantity).Should().NotContain(3);
	}

    [Fact]
    public async Task GetListAsync_WithEmptyKeysList_ShouldReturnEmptyCollection()
    {
        await _dbService.InsertAsync(new TestOrderItem
        {
            Quantity = 1,
            UnitPrice = 10.00m,
            TotalPrice = 10.00m,
            CreatedAt = DateTime.UtcNow
        });

        var items = await _dbService.GetListAsync<TestOrderItem>(
            new List<Dictionary<string, Guid>>());

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListAsync_WithNonExistentCompositeKeys_ShouldReturnEmptyCollection()
    {
        await _dbService.InsertAsync(new TestOrderItem
        {
            Quantity = 1,
            UnitPrice = 10.00m,
            TotalPrice = 10.00m,
            CreatedAt = DateTime.UtcNow
        });

        var nonExistentKeys = new List<Dictionary<string, Guid>>
        {
            new Dictionary<string, Guid>
            {
                ["OrderId"] = Guid.NewGuid(),
                ["ProductId"] = Guid.NewGuid()
            }
        };

        var items = await _dbService.GetListAsync<TestOrderItem>(nonExistentKeys);

        items.Should().BeEmpty();
    }

    #endregion

    #region <=== Single Key Entity Tests (Ensuring Guid Overloads Work) ===>

    [Fact]
    public async Task GetAsync_WithSingleGuidOverload_OnSingleKeyEntity_ShouldWork()
    {
        var product = new TestProduct
        {
            Name = "Single Key Test",
            Price = 99.99m,
            Quantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

        var retrieved = await _dbService.GetAsync<TestProduct>(id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(id);
        retrieved.Name.Should().Be("Single Key Test");
    }

    [Fact]
    public async Task DeleteAsync_WithSingleGuidOverload_OnSingleKeyEntity_ShouldWork()
    {
        var product = new TestProduct
        {
            Name = "Delete Single Key Test",
            Price = 49.99m,
            Quantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

        var deleted = await _dbService.DeleteAsync<TestProduct>(id);

        deleted.Should().BeTrue();

        var retrieved = await _dbService.GetAsync<TestProduct>(id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithSingleGuidOverload_NonExistent_ShouldReturnFalse()
    {
        var deleted = await _dbService.DeleteAsync<TestProduct>(Guid.NewGuid());

        deleted.Should().BeFalse();
    }

    #endregion

    #region <=== Synchronous Method Tests ===>

        [Fact]
        public void Insert_WithCompositeKey_ShouldReturnEntityWithBothKeys()
        {
            var orderItem = new TestOrderItem
            {
                Quantity = 5,
                UnitPrice = 10.00m,
                TotalPrice = 50.00m,
                CreatedAt = DateTime.UtcNow
            };

            var inserted = _dbService.Insert(orderItem);

            inserted.Should().NotBeNull();
            inserted.OrderId.Should().NotBe(Guid.Empty);
            inserted.ProductId.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void Get_WithCompositeKeyDictionary_ShouldReturnEntity()
        {
            var orderItem = new TestOrderItem
            {
                Quantity = 3,
                UnitPrice = 25.00m,
                TotalPrice = 75.00m,
                CreatedAt = DateTime.UtcNow
            };
            var inserted = _dbService.Insert(orderItem);

            var keys = new Dictionary<string, Guid>
            {
                ["OrderId"] = inserted.OrderId,
                ["ProductId"] = inserted.ProductId
            };
            var retrieved = _dbService.Get<TestOrderItem>(keys);

            retrieved.Should().NotBeNull();
            retrieved!.OrderId.Should().Be(inserted.OrderId);
            retrieved.ProductId.Should().Be(inserted.ProductId);
            retrieved.Quantity.Should().Be(3);
        }

    [Fact]
    public void Get_WithCompositeKeyDictionary_NonExistent_ShouldReturnNull()
    {
        var keys = new Dictionary<string, Guid>
        {
            ["OrderId"] = Guid.NewGuid(),
            ["ProductId"] = Guid.NewGuid()
        };

        var result = _dbService.Get<TestOrderItem>(keys);

        result.Should().BeNull();
    }

    [Fact]
    public void Update_WithCompositeKey_ShouldUpdateEntity()
    {
        var orderItem = new TestOrderItem
        {
            Quantity = 2,
            UnitPrice = 15.00m,
            TotalPrice = 30.00m,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = _dbService.Insert(orderItem);

        inserted.Quantity = 10;
        inserted.TotalPrice = 150.00m;
        var updated = _dbService.Update(inserted);

        updated.Should().BeTrue();

        var keys = new Dictionary<string, Guid>
        {
            ["OrderId"] = inserted.OrderId,
            ["ProductId"] = inserted.ProductId
        };
        var retrieved = _dbService.Get<TestOrderItem>(keys);
        retrieved.Should().NotBeNull();
        retrieved!.Quantity.Should().Be(10);
        retrieved.TotalPrice.Should().Be(150.00m);
    }

    [Fact]
    public void Delete_WithCompositeKeyDictionary_ShouldDeleteEntity()
    {
        var orderItem = new TestOrderItem
        {
            Quantity = 5,
            UnitPrice = 20.00m,
            TotalPrice = 100.00m,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = _dbService.Insert(orderItem);

        var keys = new Dictionary<string, Guid>
        {
            ["OrderId"] = inserted.OrderId,
            ["ProductId"] = inserted.ProductId
        };
        var deleted = _dbService.Delete<TestOrderItem>(keys);

        deleted.Should().BeTrue();

        var retrieved = _dbService.Get<TestOrderItem>(keys);
        retrieved.Should().BeNull();
    }

    [Fact]
    public void Delete_WithEntity_ShouldDeleteEntity()
    {
        var orderItem = new TestOrderItem
        {
            Quantity = 3,
            UnitPrice = 15.00m,
            TotalPrice = 45.00m,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = _dbService.Insert(orderItem);

        var deleted = _dbService.Delete(inserted);

        deleted.Should().BeTrue();

        var keys = new Dictionary<string, Guid>
        {
            ["OrderId"] = inserted.OrderId,
            ["ProductId"] = inserted.ProductId
        };
        var retrieved = _dbService.Get<TestOrderItem>(keys);
        retrieved.Should().BeNull();
    }

    [Fact]
    public void GetList_WithNoParameters_ShouldReturnAllEntities()
    {
        var items = Enumerable.Range(1, 3)
            .Select(i => new TestOrderItem
            {
                Quantity = i,
                UnitPrice = i * 10.00m,
                TotalPrice = i * i * 10.00m,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        foreach (var item in items)
        {
            _dbService.Insert(item);
        }

        var allItems = _dbService.GetList<TestOrderItem>();

        allItems.Should().HaveCount(3);
        allItems.Should().OnlyContain(i => i.OrderId != Guid.Empty && i.ProductId != Guid.Empty);
    }

    [Fact]
    public void GetList_WithSpecificCompositeKeys_ShouldReturnMatchingEntities()
    {
        var item1 = new TestOrderItem
        {
            Quantity = 1,
            UnitPrice = 10.00m,
            TotalPrice = 10.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item2 = new TestOrderItem
        {
            Quantity = 2,
            UnitPrice = 20.00m,
            TotalPrice = 40.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item3 = new TestOrderItem
        {
            Quantity = 3,
            UnitPrice = 30.00m,
            TotalPrice = 90.00m,
            CreatedAt = DateTime.UtcNow
        };

        var inserted1 = _dbService.Insert(item1);
        var inserted2 = _dbService.Insert(item2);
        _dbService.Insert(item3);

        var keysList = new List<Dictionary<string, Guid>>
        {
            new Dictionary<string, Guid>
            {
                ["OrderId"] = inserted1.OrderId,
                ["ProductId"] = inserted1.ProductId
            },
            new Dictionary<string, Guid>
            {
                ["OrderId"] = inserted2.OrderId,
                ["ProductId"] = inserted2.ProductId
            }
        };
        var items = _dbService.GetList<TestOrderItem>(keysList);

        items.Should().HaveCount(2);
        items.Select(i => i.Quantity).Should().Contain(new[] { 1, 2 });
        items.Select(i => i.Quantity).Should().NotContain(3);
    }

    [Fact]
    public void Query_ShouldReturnMatchingEntities()
    {
        var item1 = new TestOrderItem
        {
            Quantity = 5,
            UnitPrice = 10.00m,
            TotalPrice = 50.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item2 = new TestOrderItem
        {
            Quantity = 10,
            UnitPrice = 20.00m,
            TotalPrice = 200.00m,
            CreatedAt = DateTime.UtcNow
        };

        _dbService.Insert(item1);
        _dbService.Insert(item2);

        var items = _dbService.Query<TestOrderItem>(
            "SELECT * FROM test_order_items WHERE quantity > @Quantity",
            new { Quantity = 7 });

        items.Should().HaveCount(1);
        items.First().Quantity.Should().Be(10);
    }

    [Fact]
    public void Execute_ShouldExecuteCommandAndReturnAffectedRows()
    {
        var item1 = new TestOrderItem
        {
            Quantity = 5,
            UnitPrice = 10.00m,
            TotalPrice = 50.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item2 = new TestOrderItem
        {
            Quantity = 10,
            UnitPrice = 20.00m,
            TotalPrice = 200.00m,
            CreatedAt = DateTime.UtcNow
        };

        _dbService.Insert(item1);
        _dbService.Insert(item2);

        var affectedRows = _dbService.Execute(
            "UPDATE test_order_items SET quantity = @NewQuantity WHERE quantity > @MinQuantity",
            new { NewQuantity = 99, MinQuantity = 7 });

        affectedRows.Should().Be(1);

        var items = _dbService.Query<TestOrderItem>(
            "SELECT * FROM test_order_items WHERE quantity = @Quantity",
            new { Quantity = 99 });
        items.Should().HaveCount(1);
    }

    [Fact]
    public void Get_WithSingleGuid_OnSingleKeyEntity_ShouldWork()
    {
        var product = new TestProduct
        {
            Name = "Sync Get Test",
            Price = 99.99m,
            Quantity = 10,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = _dbService.Insert(product);
		var id = inserted.Id;

        var retrieved = _dbService.Get<TestProduct>(id);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(id);
        retrieved.Name.Should().Be("Sync Get Test");
    }

    [Fact]
    public void Delete_WithSingleGuid_OnSingleKeyEntity_ShouldWork()
    {
        var product = new TestProduct
        {
            Name = "Sync Delete Test",
            Price = 49.99m,
            Quantity = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = _dbService.Insert(product);
		var id = inserted.Id;

        var deleted = _dbService.Delete<TestProduct>(id);

        deleted.Should().BeTrue();

        var retrieved = _dbService.Get<TestProduct>(id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public void GetList_WithIds_OnSingleKeyEntity_ShouldReturnMatchingEntities()
    {
        var product1 = new TestProduct
        {
            Name = "Product 1",
            Price = 10.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var product2 = new TestProduct
        {
            Name = "Product 2",
            Price = 20.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var product3 = new TestProduct
        {
            Name = "Product 3",
            Price = 30.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var inserted1 = _dbService.Insert(product1);
        var inserted2 = _dbService.Insert(product2);
        _dbService.Insert(product3);

        var ids = new List<Guid> { inserted1.Id, inserted2.Id };
        var products = _dbService.GetList<TestProduct>(ids);

        products.Should().HaveCount(2);
        products.Select(p => p.Name).Should().Contain("Product 1", "Product 2");
        products.Select(p => p.Name).Should().NotContain("Product 3");
    }

    #endregion

    #region <=== Anonymous Object Key Tests ===>

    [Fact]
    public async Task GetAsync_WithAnonymousObject_ShouldReturnEntity()
    {
        var orderItem = new TestOrderItem
        {
            Quantity = 7,
            UnitPrice = 35.00m,
            TotalPrice = 245.00m,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = await _dbService.InsertAsync(orderItem);

        var retrieved = await _dbService.GetAsync<TestOrderItem>(
            new { OrderId = inserted.OrderId, ProductId = inserted.ProductId });

        retrieved.Should().NotBeNull();
        retrieved!.OrderId.Should().Be(inserted.OrderId);
        retrieved.ProductId.Should().Be(inserted.ProductId);
        retrieved.Quantity.Should().Be(7);
        retrieved.UnitPrice.Should().Be(35.00m);
    }

    [Fact]
    public async Task GetAsync_WithAnonymousObject_NonExistent_ShouldReturnNull()
    {
        var result = await _dbService.GetAsync<TestOrderItem>(
            new { OrderId = Guid.NewGuid(), ProductId = Guid.NewGuid() });

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithAnonymousObject_MissingProperty_ShouldThrowArgumentException()
    {
        var act = async () => await _dbService.GetAsync<TestOrderItem>(
            new { OrderId = Guid.NewGuid() });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Missing key property 'ProductId'*");
    }

    [Fact]
    public async Task GetAsync_WithAnonymousObject_InvalidPropertyType_ShouldThrowArgumentException()
    {
        var act = async () => await _dbService.GetAsync<TestOrderItem>(
            new { OrderId = Guid.NewGuid(), ProductId = "invalid-guid" });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Property 'ProductId' must be of type Guid*");
    }

    [Fact]
    public async Task GetAsync_WithAnonymousObject_EmptyObject_ShouldThrowArgumentException()
    {
        var act = async () => await _dbService.GetAsync<TestOrderItem>(new { });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must have at least one Guid property*");
    }

    [Fact]
    public async Task GetAsync_WithAnonymousObject_NullKeys_ShouldThrowArgumentNullException()
    {
        var act = async () => await _dbService.GetAsync<TestOrderItem>((object)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Get_WithAnonymousObject_ShouldReturnEntity()
    {
        var orderItem = new TestOrderItem
        {
            Quantity = 8,
            UnitPrice = 40.00m,
            TotalPrice = 320.00m,
            CreatedAt = DateTime.UtcNow
        };
        var inserted = _dbService.Insert(orderItem);

        var retrieved = _dbService.Get<TestOrderItem>(
            new { OrderId = inserted.OrderId, ProductId = inserted.ProductId });

        retrieved.Should().NotBeNull();
        retrieved!.OrderId.Should().Be(inserted.OrderId);
        retrieved.ProductId.Should().Be(inserted.ProductId);
        retrieved.Quantity.Should().Be(8);
    }

    [Fact]
    public void Get_WithAnonymousObject_NonExistent_ShouldReturnNull()
    {
        var result = _dbService.Get<TestOrderItem>(
            new { OrderId = Guid.NewGuid(), ProductId = Guid.NewGuid() });

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetListAsync_WithAnonymousObjects_ShouldReturnMatchingEntities()
    {
        var item1 = new TestOrderItem
        {
            Quantity = 11,
            UnitPrice = 10.00m,
            TotalPrice = 110.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item2 = new TestOrderItem
        {
            Quantity = 22,
            UnitPrice = 20.00m,
            TotalPrice = 440.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item3 = new TestOrderItem
        {
            Quantity = 33,
            UnitPrice = 30.00m,
            TotalPrice = 990.00m,
            CreatedAt = DateTime.UtcNow
        };

        var inserted1 = await _dbService.InsertAsync(item1);
        var inserted2 = await _dbService.InsertAsync(item2);
        await _dbService.InsertAsync(item3);

        var items = await _dbService.GetListAsync<TestOrderItem>(new[]
        {
            new { OrderId = inserted1.OrderId, ProductId = inserted1.ProductId },
            new { OrderId = inserted2.OrderId, ProductId = inserted2.ProductId }
        });

        items.Should().HaveCount(2);
        items.Select(i => i.Quantity).Should().Contain(new[] { 11, 22 });
        items.Select(i => i.Quantity).Should().NotContain(33);
    }

    [Fact]
    public async Task GetListAsync_WithAnonymousObjects_EmptyList_ShouldReturnEmpty()
    {
        await _dbService.InsertAsync(new TestOrderItem
        {
            Quantity = 1,
            UnitPrice = 10.00m,
            TotalPrice = 10.00m,
            CreatedAt = DateTime.UtcNow
        });

        var items = await _dbService.GetListAsync<TestOrderItem>(
            Array.Empty<object>());

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListAsync_WithAnonymousObjects_NonExistent_ShouldReturnEmpty()
    {
        await _dbService.InsertAsync(new TestOrderItem
        {
            Quantity = 1,
            UnitPrice = 10.00m,
            TotalPrice = 10.00m,
            CreatedAt = DateTime.UtcNow
        });

        var items = await _dbService.GetListAsync<TestOrderItem>(new[]
        {
            new { OrderId = Guid.NewGuid(), ProductId = Guid.NewGuid() }
        });

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListAsync_WithAnonymousObjects_MissingProperty_ShouldThrowArgumentException()
    {
        var act = async () => await _dbService.GetListAsync<TestOrderItem>(new[]
        {
            new { OrderId = Guid.NewGuid() }
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Missing key property 'ProductId'*");
    }

    [Fact]
    public void GetList_WithAnonymousObjects_ShouldReturnMatchingEntities()
    {
        var item1 = new TestOrderItem
        {
            Quantity = 44,
            UnitPrice = 10.00m,
            TotalPrice = 440.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item2 = new TestOrderItem
        {
            Quantity = 55,
            UnitPrice = 20.00m,
            TotalPrice = 1100.00m,
            CreatedAt = DateTime.UtcNow
        };
        var item3 = new TestOrderItem
        {
            Quantity = 66,
            UnitPrice = 30.00m,
            TotalPrice = 1980.00m,
            CreatedAt = DateTime.UtcNow
        };

        var inserted1 = _dbService.Insert(item1);
        var inserted2 = _dbService.Insert(item2);
        _dbService.Insert(item3);

        var items = _dbService.GetList<TestOrderItem>(new[]
        {
            new { OrderId = inserted1.OrderId, ProductId = inserted1.ProductId },
            new { OrderId = inserted2.OrderId, ProductId = inserted2.ProductId }
        });

        items.Should().HaveCount(2);
        items.Select(i => i.Quantity).Should().Contain(new[] { 44, 55 });
        items.Select(i => i.Quantity).Should().NotContain(66);
    }

    [Fact]
    public void GetList_WithAnonymousObjects_EmptyList_ShouldReturnEmpty()
    {
        _dbService.Insert(new TestOrderItem
        {
            Quantity = 1,
            UnitPrice = 10.00m,
            TotalPrice = 10.00m,
            CreatedAt = DateTime.UtcNow
        });

        var items = _dbService.GetList<TestOrderItem>(Array.Empty<object>());

        items.Should().BeEmpty();
    }

    #endregion
}
