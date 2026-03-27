using FluentAssertions;
using WebVella.Database;
using WebVella.Database.Security;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Integration tests for <see cref="DbService"/> using a real PostgreSQL database.
/// </summary>
[Collection("Database")]
public class DbServiceIntegrationTests : IAsyncLifetime
{
	private readonly DatabaseFixture _fixture;
	private readonly IDbService _dbService;

	public DbServiceIntegrationTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
		_dbService = fixture.DbService;
	}

	public async Task InitializeAsync()
	{
		await _fixture.ClearTestProductsAsync();
		await _fixture.ClearTestRlsItemsAsync();
	}

	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== InsertAsync Tests ===>

	[Fact]
	public async Task InsertAsync_ShouldInsertEntityAndReturnId()
	{
		var product = new TestProduct
		{
			Name = "Test Product",
			Description = "A test product description",
			Price = 19.99m,
			Quantity = 100,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public async Task InsertAsync_WithNullDescription_ShouldInsertSuccessfully()
	{
		var product = new TestProduct
		{
			Name = "Product Without Description",
			Description = null,
			Price = 9.99m,
			Quantity = 50,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Description.Should().BeNull();
	}

	[Fact]
	public async Task InsertAsync_WithZeroValues_ShouldInsertSuccessfully()
	{
		var product = new TestProduct
		{
			Name = "Zero Values Product",
			Price = 0m,
			Quantity = 0,
			IsActive = false,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Price.Should().Be(0m);
		retrieved.Quantity.Should().Be(0);
		retrieved.IsActive.Should().BeFalse();
	}

	[Fact]
	public async Task InsertAsync_WithSpecialCharactersInName_ShouldInsertSuccessfully()
	{
		var product = new TestProduct
		{
			Name = "Test's \"Special\" <Product> & More",
			Description = "Description with 'quotes' and \"double quotes\"",
			Price = 19.99m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Test's \"Special\" <Product> & More");
	}

	[Fact]
	public async Task MultipleInserts_ShouldGenerateUniqueIds()
	{
		var products = Enumerable.Range(1, 5)
			.Select(i => new TestProduct
			{
				Name = $"Sequential Product {i}",
				Price = i * 10.00m,
				Quantity = i * 5,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			})
			.ToList();

		var ids = new List<Guid>();
		foreach (var product in products)
		{
			var inserted = await _dbService.InsertAsync(product);
			ids.Add(inserted.Id);
		}

		ids.Should().HaveCount(5);
		ids.Should().OnlyHaveUniqueItems();
		ids.Should().NotContain(Guid.Empty);
	}

	[Fact]
	public async Task InsertAsync_WithEmptyGuidKey_ShouldAutoGenerateNewGuid()
	{
		var product = new TestProduct
		{
			Id = Guid.Empty,
			Name = "Auto Generate Key Test",
			Price = 25.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);
		product.Id.Should().NotBe(Guid.Empty);
		product.Id.Should().Be(id);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Auto Generate Key Test");
	}

	[Fact]
	public async Task InsertAsync_WithPresetGuidKey_ShouldUseProvidedGuid()
	{
		var presetId = Guid.NewGuid();
		var product = new TestProduct
		{
			Id = presetId,
			Name = "Preset Key Test",
			Price = 30.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().Be(presetId);
		product.Id.Should().Be(presetId);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Id.Should().Be(presetId);
	}

	#endregion

	#region <=== GetAsync Tests ===>

	[Fact]
	public async Task GetAsync_ShouldReturnInsertedEntity()
	{
		var product = new TestProduct
		{
			Name = "Get Test Product",
			Description = "Description for get test",
			Price = 29.99m,
			Quantity = 50,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrievedProduct = await _dbService.GetAsync<TestProduct>(id);

		retrievedProduct.Should().NotBeNull();
		retrievedProduct!.Id.Should().Be(id);
		retrievedProduct.Name.Should().Be("Get Test Product");
		retrievedProduct.Price.Should().Be(29.99m);
	}

	[Fact]
	public async Task GetAsync_WithNonExistentId_ShouldReturnNull()
	{
		var product = await _dbService.GetAsync<TestProduct>(Guid.NewGuid());

		product.Should().BeNull();
	}

	[Fact]
	public async Task GetAsync_WithEmptyGuid_ShouldReturnNull()
	{
		var product = await _dbService.GetAsync<TestProduct>(Guid.Empty);

		product.Should().BeNull();
	}

	[Fact]
	public async Task GetAsync_ShouldReturnAllProperties()
	{
		var now = DateTime.Now;
		var product = new TestProduct
		{
			Name = "Full Property Test",
			Description = "Full description",
			Price = 99.99m,
			Quantity = 100,
			IsActive = true,
			CreatedAt = now,
			UpdatedAt = now
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Id.Should().Be(id);
		retrieved.Name.Should().Be("Full Property Test");
		retrieved.Description.Should().Be("Full description");
		retrieved.Price.Should().Be(99.99m);
		retrieved.Quantity.Should().Be(100);
		retrieved.IsActive.Should().BeTrue();
		retrieved.CreatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
		retrieved.UpdatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
	}

	#endregion

	#region <=== GetListAsync Tests ===>

	[Fact]
	public async Task GetListAsync_WithNullIds_ShouldReturnAllEntities()
	{
		var product1 = new TestProduct
		{
			Name = "All Product 1",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var product2 = new TestProduct
		{
			Name = "All Product 2",
			Price = 20.00m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(product1);
		await _dbService.InsertAsync(product2);

		var allProducts = await _dbService.GetListAsync<TestProduct>();

		allProducts.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetListAsync_WithNoEntities_ShouldReturnEmptyCollection()
	{
		var allProducts = await _dbService.GetListAsync<TestProduct>();

		allProducts.Should().BeEmpty();
	}

	[Fact]
	public async Task GetListAsync_WithSpecificIds_ShouldReturnOnlyMatchingEntities()
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

		var inserted1 = await _dbService.InsertAsync(product1);
		var inserted2 = await _dbService.InsertAsync(product2);
		await _dbService.InsertAsync(product3);

		var ids = new List<Guid> { inserted1.Id, inserted2.Id };
		var products = await _dbService.GetListAsync<TestProduct>(ids);

		products.Should().HaveCount(2);
		products.Select(p => p.Name).Should().Contain("Product 1", "Product 2");
		products.Select(p => p.Name).Should().NotContain("Product 3");
	}

	[Fact]
	public async Task GetListAsync_WithEmptyIdsList_ShouldReturnEmptyCollection()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Some Product",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var products = await _dbService.GetListAsync<TestProduct>(new List<Guid>());

		products.Should().BeEmpty();
	}

	[Fact]
	public async Task GetListAsync_WithNonExistentIds_ShouldReturnEmptyCollection()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Existing Product",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var nonExistentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
		var products = await _dbService.GetListAsync<TestProduct>(nonExistentIds);

		products.Should().BeEmpty();
	}

	[Fact]
	public async Task GetListAsync_WithMixedExistentAndNonExistentIds_ShouldReturnOnlyExisting()
	{
		var product = new TestProduct
		{
			Name = "Existing Product",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var existingId = inserted.Id;

		var mixedIds = new List<Guid> { existingId, Guid.NewGuid(), Guid.NewGuid() };
		var products = await _dbService.GetListAsync<TestProduct>(mixedIds);

		products.Should().HaveCount(1);
		products.First().Id.Should().Be(existingId);
	}

	[Fact]
	public async Task GetListAsync_ShouldReturnEntitiesInInsertionOrder()
	{
		var products = Enumerable.Range(1, 3)
			.Select(i => new TestProduct
			{
				Name = $"Ordered Product {i}",
				Price = i * 10.00m,
				Quantity = i,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			})
			.ToList();

		foreach (var product in products)
		{
			await _dbService.InsertAsync(product);
		}

		var allProducts = await _dbService.GetListAsync<TestProduct>();

		allProducts.Should().HaveCount(3);
		allProducts.Select(p => p.Name).Should().ContainInOrder(
			"Ordered Product 1", "Ordered Product 2", "Ordered Product 3");
	}

	#endregion

	#region <=== QueryAsync Tests ===>

	[Fact]
	public async Task QueryAsync_ShouldReturnMatchingEntities()
	{
		var product1 = new TestProduct
		{
			Name = "Query Product 1",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var product2 = new TestProduct
		{
			Name = "Query Product 2",
			Price = 20.00m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var product3 = new TestProduct
		{
			Name = "Query Product 3",
			Price = 30.00m,
			Quantity = 15,
			IsActive = false,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(product1);
		await _dbService.InsertAsync(product2);
		await _dbService.InsertAsync(product3);

		var activeProducts = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE is_active = @IsActive",
			new { IsActive = true });

		activeProducts.Should().HaveCount(2);
		activeProducts.Should().OnlyContain(p => p.IsActive);
	}

	[Fact]
	public async Task QueryAsync_WithNoMatches_ShouldReturnEmptyCollection()
	{
		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE price > @MinPrice",
			new { MinPrice = 1000000m });

		products.Should().BeEmpty();
	}

	[Fact]
	public async Task QueryAsync_WithMultipleParameters_ShouldFilterCorrectly()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Cheap Active",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Expensive Active",
			Price = 100.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Cheap Inactive",
			Price = 10.00m,
			IsActive = false,
			CreatedAt = DateTime.UtcNow
		});

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE is_active = @IsActive AND price < @MaxPrice",
			new { IsActive = true, MaxPrice = 50.00m });

		products.Should().HaveCount(1);
		products.First().Name.Should().Be("Cheap Active");
	}

	[Fact]
	public async Task QueryAsync_WithLikePattern_ShouldMatchPartialNames()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Apple iPhone",
			Price = 999.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Apple iPad",
			Price = 799.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Samsung Galaxy",
			Price = 899.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE name LIKE @Pattern",
			new { Pattern = "Apple%" });

		products.Should().HaveCount(2);
		products.Should().OnlyContain(p => p.Name.StartsWith("Apple"));
	}

	[Fact]
	public async Task QueryAsync_WithOrderBy_ShouldReturnOrderedResults()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "B Product",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "A Product",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "C Product",
			Price = 30.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products ORDER BY name ASC");

		products.Should().HaveCount(3);
		products.Select(p => p.Name).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task QueryAsync_WithNullParameter_ShouldWork()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "With Description",
			Description = "Some desc",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Without Description",
			Description = null,
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE description IS NULL");

		products.Should().HaveCount(1);
		products.First().Name.Should().Be("Without Description");
	}

	#endregion

	#region <=== UpdateAsync Tests ===>

	[Fact]
	public async Task UpdateAsync_ShouldUpdateEntity()
	{
		var product = new TestProduct
		{
			Name = "Original Name",
			Price = 15.00m,
			Quantity = 25,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Updated Name";
		product.Price = 25.00m;
		product.UpdatedAt = DateTime.UtcNow;
		var updated = await _dbService.UpdateAsync(product);

		updated.Should().BeTrue();

		var updatedProduct = await _dbService.GetAsync<TestProduct>(id);
		updatedProduct.Should().NotBeNull();
		updatedProduct!.Name.Should().Be("Updated Name");
		updatedProduct.Price.Should().Be(25.00m);
		updatedProduct.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateAsync_WithNonExistentId_ShouldReturnFalse()
	{
		var product = new TestProduct
		{
			Id = Guid.NewGuid(),
			Name = "Non-existent Product",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var updated = await _dbService.UpdateAsync(product);

		updated.Should().BeFalse();
	}

	[Fact]
	public async Task UpdateAsync_ShouldOnlyUpdateSpecifiedEntity()
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

		var inserted1 = await _dbService.InsertAsync(product1);
		var inserted2 = await _dbService.InsertAsync(product2);
		var id1 = inserted1.Id;
		var id2 = inserted2.Id;

		product1.Id = id1;
		product1.Name = "Updated Product 1";

		await _dbService.UpdateAsync(product1);

		var retrieved1 = await _dbService.GetAsync<TestProduct>(id1);
		var retrieved2 = await _dbService.GetAsync<TestProduct>(id2);

		retrieved1!.Name.Should().Be("Updated Product 1");
		retrieved2!.Name.Should().Be("Product 2");
	}

	[Fact]
	public async Task UpdateAsync_WithAllFieldsChanged_ShouldUpdateAllFields()
	{
		var product = new TestProduct
		{
			Name = "Original",
			Description = "Original Desc",
			Price = 10.00m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Updated";
		product.Description = "Updated Desc";
		product.Price = 99.99m;
		product.Quantity = 999;
		product.IsActive = false;
		product.UpdatedAt = DateTime.UtcNow;

		var updated = await _dbService.UpdateAsync(product);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Name.Should().Be("Updated");
		retrieved.Description.Should().Be("Updated Desc");
		retrieved.Price.Should().Be(99.99m);
		retrieved.Quantity.Should().Be(999);
		retrieved.IsActive.Should().BeFalse();
		retrieved.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateAsync_WithSpecificPropertyNames_ShouldUpdateOnlyThoseProperties()
	{
		var product = new TestProduct
		{
			Name = "Original Name",
			Description = "Original Description",
			Price = 50.00m,
			Quantity = 100,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Updated Name";
		product.Description = "Updated Description";
		product.Price = 999.99m;
		product.Quantity = 999;

		var updated = await _dbService.UpdateAsync(product, ["Name", "Price"]);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Name.Should().Be("Updated Name");
		retrieved.Description.Should().Be("Original Description");
		retrieved.Price.Should().Be(999.99m);
		retrieved.Quantity.Should().Be(100);
	}

	[Fact]
	public async Task UpdateAsync_WithSinglePropertyName_ShouldUpdateOnlyThatProperty()
	{
		var product = new TestProduct
		{
			Name = "Original",
			Price = 25.00m,
			Quantity = 50,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "New Name";
		product.Price = 999.99m;

		var updated = await _dbService.UpdateAsync(product, ["Name"]);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Name.Should().Be("New Name");
		retrieved.Price.Should().Be(25.00m);
	}

	[Fact]
	public async Task UpdateAsync_WithCaseInsensitivePropertyNames_ShouldWork()
	{
		var product = new TestProduct
		{
			Name = "Original",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Updated";
		product.Price = 99.99m;

		var updated = await _dbService.UpdateAsync(product, ["name", "PRICE"]);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Name.Should().Be("Updated");
		retrieved.Price.Should().Be(99.99m);
	}

	[Fact]
	public async Task UpdateAsync_WithInvalidPropertyName_ShouldThrowArgumentException()
	{
		var product = new TestProduct
		{
			Name = "Test",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		product.Id = inserted.Id;

		var act = async () => await _dbService.UpdateAsync(product, ["InvalidProperty"]);

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*Invalid property names*InvalidProperty*");
	}

	[Fact]
	public async Task UpdateAsync_WithNullPropertyNames_ShouldUpdateAllProperties()
	{
		var product = new TestProduct
		{
			Name = "Original",
			Description = "Original Desc",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Updated";
		product.Description = "Updated Desc";
		product.Price = 99.99m;

		var updated = await _dbService.UpdateAsync(product);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Name.Should().Be("Updated");
		retrieved.Description.Should().Be("Updated Desc");
		retrieved.Price.Should().Be(99.99m);
	}

	#endregion

	#region <=== DeleteAsync Tests ===>

	[Fact]
	public async Task DeleteAsync_ShouldDeleteEntity()
	{
		var product = new TestProduct
		{
			Name = "Product To Delete",
			Price = 5.00m,
			Quantity = 1,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		var deleted = await _dbService.DeleteAsync(product);

		deleted.Should().BeTrue();

		var deletedProduct = await _dbService.GetAsync<TestProduct>(id);
		deletedProduct.Should().BeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithNonExistentId_ShouldReturnFalse()
	{
		var product = new TestProduct
		{
			Id = Guid.NewGuid(),
			Name = "Non-existent Product",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var deleted = await _dbService.DeleteAsync(product);

		deleted.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteAsync_ShouldOnlyDeleteSpecifiedEntity()
	{
		var product1 = new TestProduct
		{
			Name = "Keep Me",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var product2 = new TestProduct
		{
			Name = "Delete Me",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted1 = await _dbService.InsertAsync(product1);
		var inserted2 = await _dbService.InsertAsync(product2);
		var id1 = inserted1.Id;
		var id2 = inserted2.Id;

		product2.Id = id2;

		await _dbService.DeleteAsync(product2);

		var retrieved1 = await _dbService.GetAsync<TestProduct>(id1);
		var retrieved2 = await _dbService.GetAsync<TestProduct>(id2);

		retrieved1.Should().NotBeNull();
		retrieved2.Should().BeNull();
	}

	[Fact]
	public async Task DeleteAsync_MultipleTimes_ShouldReturnFalseOnSecondAttempt()
	{
		var product = new TestProduct
		{
			Name = "Double Delete",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		var firstDelete = await _dbService.DeleteAsync(product);
		var secondDelete = await _dbService.DeleteAsync(product);

		firstDelete.Should().BeTrue();
		secondDelete.Should().BeFalse();
	}

	#endregion

	#region <=== ExecuteAsync Tests ===>

	[Fact]
	public async Task ExecuteAsync_ShouldExecuteCommandAndReturnAffectedRows()
	{
		var product1 = new TestProduct
		{
			Name = "Execute Product 1",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var product2 = new TestProduct
		{
			Name = "Execute Product 2",
			Price = 20.00m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(product1);
		await _dbService.InsertAsync(product2);

		var affectedRows = await _dbService.ExecuteAsync(
			"UPDATE test_products SET is_active = @IsActive WHERE price > @MinPrice",
			new { IsActive = false, MinPrice = 15.00m });

		affectedRows.Should().Be(1);

		var inactiveProducts = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE is_active = @IsActive",
			new { IsActive = false });
		inactiveProducts.Should().HaveCount(1);
		inactiveProducts.First().Name.Should().Be("Execute Product 2");
	}

	[Fact]
	public async Task ExecuteAsync_WithNoMatchingRows_ShouldReturnZero()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Test",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var affectedRows = await _dbService.ExecuteAsync(
			"UPDATE test_products SET is_active = @IsActive WHERE price > @MinPrice",
			new { IsActive = false, MinPrice = 1000.00m });

		affectedRows.Should().Be(0);
	}

	[Fact]
	public async Task ExecuteAsync_DeleteStatement_ShouldDeleteMatchingRows()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Cheap",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Expensive",
			Price = 100.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var affectedRows = await _dbService.ExecuteAsync(
			"DELETE FROM test_products WHERE price < @MaxPrice",
			new { MaxPrice = 50.00m });

		affectedRows.Should().Be(1);

		var remaining = await _dbService.GetListAsync<TestProduct>();
		remaining.Should().HaveCount(1);
		remaining.First().Name.Should().Be("Expensive");
	}

	[Fact]
	public async Task ExecuteAsync_WithoutParameters_ShouldWork()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Test 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Test 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var affectedRows = await _dbService.ExecuteAsync("DELETE FROM test_products");

		affectedRows.Should().Be(2);

		var remaining = await _dbService.GetListAsync<TestProduct>();
		remaining.Should().BeEmpty();
	}

	#endregion

	#region <=== ExecuteReader Tests ===>

	[Fact]
	public void ExecuteReader_ShouldReturnDataReader()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "Reader Product 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		_dbService.Insert(new TestProduct
		{
			Name = "Reader Product 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		using var reader = _dbService.ExecuteReader("SELECT name, price FROM test_products ORDER BY price");

		var names = new List<string>();
		var prices = new List<decimal>();

		while (reader.Read())
		{
			names.Add(reader.GetString(0));
			prices.Add(reader.GetDecimal(1));
		}

		names.Should().HaveCount(2);
		names[0].Should().Be("Reader Product 1");
		names[1].Should().Be("Reader Product 2");
		prices[0].Should().Be(10.00m);
		prices[1].Should().Be(20.00m);
	}

	[Fact]
	public void ExecuteReader_WithParameters_ShouldFilterResults()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "Cheap Item",
			Price = 5.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		_dbService.Insert(new TestProduct
		{
			Name = "Expensive Item",
			Price = 100.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		using var reader = _dbService.ExecuteReader(
			"SELECT name FROM test_products WHERE price > @MinPrice",
			new { MinPrice = 50.00m });

		var names = new List<string>();
		while (reader.Read())
		{
			names.Add(reader.GetString(0));
		}

		names.Should().HaveCount(1);
		names[0].Should().Be("Expensive Item");
	}

	[Fact]
	public void ExecuteReader_WithNoResults_ShouldReturnEmptyReader()
	{
		using var reader = _dbService.ExecuteReader("SELECT name FROM test_products WHERE 1 = 0");

		reader.Read().Should().BeFalse();
	}

	[Fact]
	public async Task ExecuteReaderAsync_ShouldReturnDataReader()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Async Reader Product 1",
			Price = 15.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Async Reader Product 2",
			Price = 25.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		await using var reader = await _dbService.ExecuteReaderAsync(
			"SELECT name, price FROM test_products ORDER BY price");

		var names = new List<string>();
		var prices = new List<decimal>();

		while (await reader.ReadAsync())
		{
			names.Add(reader.GetString(0));
			prices.Add(reader.GetDecimal(1));
		}

		names.Should().HaveCount(2);
		names[0].Should().Be("Async Reader Product 1");
		names[1].Should().Be("Async Reader Product 2");
		prices[0].Should().Be(15.00m);
		prices[1].Should().Be(25.00m);
	}

	[Fact]
	public async Task ExecuteReaderAsync_WithParameters_ShouldFilterResults()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Active Product",
			Price = 30.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Inactive Product",
			Price = 40.00m,
			IsActive = false,
			CreatedAt = DateTime.UtcNow
		});

		await using var reader = await _dbService.ExecuteReaderAsync(
			"SELECT name FROM test_products WHERE is_active = @IsActive",
			new { IsActive = true });

		var names = new List<string>();
		while (await reader.ReadAsync())
		{
			names.Add(reader.GetString(0));
		}

		names.Should().HaveCount(1);
		names[0].Should().Be("Active Product");
	}

	#endregion

	#region <=== ExecuteScalar Tests ===>

	[Fact]
	public void ExecuteScalar_ShouldReturnSingleValue()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "Scalar Product 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		_dbService.Insert(new TestProduct
		{
			Name = "Scalar Product 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var count = _dbService.ExecuteScalar<int>("SELECT COUNT(*) FROM test_products");

		count.Should().Be(2);
	}

	[Fact]
	public void ExecuteScalar_WithParameters_ShouldReturnFilteredValue()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "Low Price",
			Price = 5.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		_dbService.Insert(new TestProduct
		{
			Name = "High Price",
			Price = 100.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var sum = _dbService.ExecuteScalar<decimal>(
			"SELECT SUM(price) FROM test_products WHERE price > @MinPrice",
			new { MinPrice = 10.00m });

		sum.Should().Be(100.00m);
	}

	[Fact]
	public void ExecuteScalar_WithNoResults_ShouldReturnDefault()
	{
		var result = _dbService.ExecuteScalar<int?>(
			"SELECT quantity FROM test_products WHERE name = @Name",
			new { Name = "NonExistent" });

		result.Should().BeNull();
	}

	[Fact]
	public void ExecuteScalar_ShouldReturnStringValue()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "First Product",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var name = _dbService.ExecuteScalar<string>(
			"SELECT name FROM test_products ORDER BY created_at LIMIT 1");

		name.Should().Be("First Product");
	}

	[Fact]
	public async Task ExecuteScalarAsync_ShouldReturnSingleValue()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Async Scalar 1",
			Price = 15.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Async Scalar 2",
			Price = 25.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Async Scalar 3",
			Price = 35.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var count = await _dbService.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM test_products");

		count.Should().Be(3);
	}

	[Fact]
	public async Task ExecuteScalarAsync_WithParameters_ShouldReturnAggregateValue()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Product A",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Product B",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Product C",
			Price = 30.00m,
			IsActive = false,
			CreatedAt = DateTime.UtcNow
		});

		var avgPrice = await _dbService.ExecuteScalarAsync<decimal>(
			"SELECT AVG(price) FROM test_products WHERE is_active = @IsActive",
			new { IsActive = true });

		avgPrice.Should().Be(15.00m);
	}

	[Fact]
	public async Task ExecuteScalarAsync_ShouldReturnMaxValue()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Cheap",
			Price = 5.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Medium",
			Price = 50.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Expensive",
			Price = 500.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var maxPrice = await _dbService.ExecuteScalarAsync<decimal>("SELECT MAX(price) FROM test_products");

		maxPrice.Should().Be(500.00m);
	}

	#endregion

	#region <=== GetDataTable Tests ===>

	[Fact]
	public void GetDataTable_ShouldReturnDataTable()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "DataTable Product 1",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		_dbService.Insert(new TestProduct
		{
			Name = "DataTable Product 2",
			Price = 20.00m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var dataTable = _dbService.GetDataTable("SELECT name, price, quantity FROM test_products ORDER BY price");

		dataTable.Should().NotBeNull();
		dataTable.Rows.Count.Should().Be(2);
		dataTable.Columns.Count.Should().Be(3);
		dataTable.Columns[0].ColumnName.Should().Be("name");
		dataTable.Columns[1].ColumnName.Should().Be("price");
		dataTable.Columns[2].ColumnName.Should().Be("quantity");

		dataTable.Rows[0]["name"].Should().Be("DataTable Product 1");
		dataTable.Rows[0]["price"].Should().Be(10.00m);
		dataTable.Rows[0]["quantity"].Should().Be(5);

		dataTable.Rows[1]["name"].Should().Be("DataTable Product 2");
		dataTable.Rows[1]["price"].Should().Be(20.00m);
		dataTable.Rows[1]["quantity"].Should().Be(10);
	}

	[Fact]
	public void GetDataTable_WithParameters_ShouldFilterResults()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "Active DataTable Product",
			Price = 15.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		_dbService.Insert(new TestProduct
		{
			Name = "Inactive DataTable Product",
			Price = 25.00m,
			IsActive = false,
			CreatedAt = DateTime.UtcNow
		});

		var dataTable = _dbService.GetDataTable(
			"SELECT name, price FROM test_products WHERE is_active = @IsActive",
			new { IsActive = true });

		dataTable.Rows.Count.Should().Be(1);
		dataTable.Rows[0]["name"].Should().Be("Active DataTable Product");
		dataTable.Rows[0]["price"].Should().Be(15.00m);
	}

	[Fact]
	public void GetDataTable_WithNoResults_ShouldReturnEmptyDataTable()
	{
		var dataTable = _dbService.GetDataTable("SELECT name FROM test_products WHERE 1 = 0");

		dataTable.Should().NotBeNull();
		dataTable.Rows.Count.Should().Be(0);
	}

	[Fact]
	public void GetDataTable_WithAllColumns_ShouldReturnAllData()
	{
		_dbService.Insert(new TestProduct
		{
			Name = "Full Data Product",
			Description = "A test description",
			Price = 99.99m,
			Quantity = 50,
			IsActive = true,
			Status = ProductStatus.Active,
			CreatedAt = DateTime.UtcNow
		});

		var dataTable = _dbService.GetDataTable("SELECT * FROM test_products");

		dataTable.Rows.Count.Should().Be(1);
		dataTable.Columns.Should().Contain(c => c.ColumnName == "name");
		dataTable.Columns.Should().Contain(c => c.ColumnName == "price");
		dataTable.Columns.Should().Contain(c => c.ColumnName == "quantity");
		dataTable.Columns.Should().Contain(c => c.ColumnName == "is_active");
	}

	[Fact]
	public async Task GetDataTableAsync_ShouldReturnDataTable()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Async DataTable 1",
			Price = 30.00m,
			Quantity = 15,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Async DataTable 2",
			Price = 40.00m,
			Quantity = 20,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var dataTable = await _dbService.GetDataTableAsync(
			"SELECT name, price, quantity FROM test_products ORDER BY price");

		dataTable.Should().NotBeNull();
		dataTable.Rows.Count.Should().Be(2);
		dataTable.Columns.Count.Should().Be(3);

		dataTable.Rows[0]["name"].Should().Be("Async DataTable 1");
		dataTable.Rows[0]["price"].Should().Be(30.00m);
		dataTable.Rows[0]["quantity"].Should().Be(15);

		dataTable.Rows[1]["name"].Should().Be("Async DataTable 2");
		dataTable.Rows[1]["price"].Should().Be(40.00m);
		dataTable.Rows[1]["quantity"].Should().Be(20);
	}

	[Fact]
	public async Task GetDataTableAsync_WithParameters_ShouldFilterResults()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Cheap Async Product",
			Price = 5.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Expensive Async Product",
			Price = 500.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var dataTable = await _dbService.GetDataTableAsync(
			"SELECT name, price FROM test_products WHERE price > @MinPrice",
			new { MinPrice = 100.00m });

		dataTable.Rows.Count.Should().Be(1);
		dataTable.Rows[0]["name"].Should().Be("Expensive Async Product");
		dataTable.Rows[0]["price"].Should().Be(500.00m);
	}

	[Fact]
	public async Task GetDataTableAsync_WithNoResults_ShouldReturnEmptyDataTable()
	{
		var dataTable = await _dbService.GetDataTableAsync(
			"SELECT name FROM test_products WHERE name = @Name",
			new { Name = "NonExistentProduct" });

		dataTable.Should().NotBeNull();
		dataTable.Rows.Count.Should().Be(0);
	}

	[Fact]
	public async Task GetDataTableAsync_WithNullValues_ShouldHandleNullsCorrectly()
	{
		await _dbService.InsertAsync(new TestProduct
		{
			Name = "Product With Nulls",
			Description = null,
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var dataTable = await _dbService.GetDataTableAsync(
			"SELECT name, description FROM test_products");

		dataTable.Rows.Count.Should().Be(1);
		dataTable.Rows[0]["name"].Should().Be("Product With Nulls");
		dataTable.Rows[0]["description"].Should().Be(DBNull.Value);
	}

	#endregion

	#region <=== Transaction Tests ===>

	[Fact]
	public async Task CreateTransactionScopeAsync_ShouldCommitChanges()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product = new TestProduct
			{
				Name = "Transaction Product",
				Price = 50.00m,
				Quantity = 100,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			await scope.CompleteAsync();
		}

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE name = @Name",
			new { Name = "Transaction Product" });

		products.Should().HaveCount(1);
	}

	[Fact]
	public async Task CreateTransactionScopeAsync_WithStringLockKey_ShouldWork()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync("test-lock-key"))
		{
			var product = new TestProduct
			{
				Name = "String Lock Product",
				Price = 75.00m,
				Quantity = 200,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			await scope.CompleteAsync();
		}

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE name = @Name",
			new { Name = "String Lock Product" });

		products.Should().HaveCount(1);
	}

	#endregion

	#region <=== Edge Cases ===>

	[Fact]
	public async Task InsertAndUpdate_RapidSuccession_ShouldWorkCorrectly()
	{
		var product = new TestProduct
		{
			Name = "Rapid Test",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		for (int i = 1; i <= 5; i++)
		{
			product.Name = $"Rapid Test {i}";
			product.Price = i * 10.00m;
			await _dbService.UpdateAsync(product);
		}

		var final = await _dbService.GetAsync<TestProduct>(id);
		final!.Name.Should().Be("Rapid Test 5");
		final.Price.Should().Be(50.00m);
	}

	[Fact]
	public async Task InsertAsync_WithLargeDecimalPrecision_ShouldMaintainPrecision()
	{
		var product = new TestProduct
		{
			Name = "Precision Test",
			Price = 12345.67m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved!.Price.Should().Be(12345.67m);
	}

	[Fact]
	public async Task QueryAsync_WithEmptyTable_ShouldReturnEmptyCollection()
	{
		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products");

		products.Should().BeEmpty();
	}

	#endregion

	#region <=== Enum Property Tests ===>

	[Fact]
	public async Task InsertAsync_WithEnumProperty_ShouldPersistEnumValue()
	{
		var product = new TestProduct
		{
			Name = "Enum Test Product",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			Status = ProductStatus.Active,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Status.Should().Be(ProductStatus.Active);
	}

	[Fact]
	public async Task InsertAsync_WithDefaultEnumValue_ShouldPersistDraft()
	{
		var product = new TestProduct
		{
			Name = "Default Enum Product",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Status.Should().Be(ProductStatus.Draft);
	}

	[Fact]
	public async Task InsertAsync_WithAllEnumValues_ShouldPersistCorrectly()
	{
		var statuses = new[]
		{
			ProductStatus.Draft,
			ProductStatus.Active,
			ProductStatus.Discontinued,
			ProductStatus.OutOfStock
		};

		foreach (var status in statuses)
		{
			var product = new TestProduct
			{
				Name = $"Product with {status}",
				Price = 10.00m,
				Quantity = 5,
				IsActive = true,
				Status = status,
				CreatedAt = DateTime.UtcNow
			};

			var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

			var retrieved = await _dbService.GetAsync<TestProduct>(id);

			retrieved.Should().NotBeNull();
			retrieved!.Status.Should().Be(status);
		}
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateEnumProperty()
	{
		var product = new TestProduct
		{
			Name = "Enum Update Test",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			Status = ProductStatus.Draft,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Status = ProductStatus.Active;
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Status.Should().Be(ProductStatus.Active);

		product.Status = ProductStatus.Discontinued;
		await _dbService.UpdateAsync(product);

		retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Status.Should().Be(ProductStatus.Discontinued);
	}

	[Fact]
	public async Task UpdateAsync_WithPropertyNamesUpdateOnly_ShouldUpdateOnlyEnumProperty()
	{
		var product = new TestProduct
		{
			Name = "Partial Enum Update Test",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			Status = ProductStatus.Draft,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Changed Name";
		product.Status = ProductStatus.Active;
		await _dbService.UpdateAsync(product, ["Status"]);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.Name.Should().Be("Partial Enum Update Test");
		retrieved.Status.Should().Be(ProductStatus.Active);
	}

	[Fact]
	public async Task QueryAsync_ShouldFilterByEnumValue()
	{
		var activeProduct = new TestProduct
		{
			Name = "Active Product",
			Price = 10.00m,
			Status = ProductStatus.Active,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var discontinuedProduct = new TestProduct
		{
			Name = "Discontinued Product",
			Price = 20.00m,
			Status = ProductStatus.Discontinued,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(activeProduct);
		await _dbService.InsertAsync(discontinuedProduct);

		var activeProducts = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE status = @Status",
			new { Status = (int)ProductStatus.Active });

		activeProducts.Should().HaveCount(1);
		activeProducts.First().Name.Should().Be("Active Product");
	}

	#endregion

	#region <=== DateOnly Property Tests ===>

	[Fact]
	public async Task InsertAsync_WithDateOnlyProperty_ShouldPersistDateValue()
	{
		var releaseDate = new DateOnly(2024, 6, 15);
		var product = new TestProduct
		{
			Name = "DateOnly Test Product",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = releaseDate,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.ReleaseDate.Should().Be(releaseDate);
	}

	[Fact]
	public async Task InsertAsync_WithNullableDateOnly_ShouldPersistNull()
	{
		var product = new TestProduct
		{
			Name = "Nullable DateOnly Test",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			DiscontinuedDate = null,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.DiscontinuedDate.Should().BeNull();
	}

	[Fact]
	public async Task InsertAsync_WithNullableDateOnly_ShouldPersistValue()
	{
		var discontinuedDate = new DateOnly(2025, 12, 31);
		var product = new TestProduct
		{
			Name = "Nullable DateOnly With Value",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			DiscontinuedDate = discontinuedDate,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.DiscontinuedDate.Should().Be(discontinuedDate);
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateDateOnlyProperty()
	{
		var originalDate = new DateOnly(2024, 1, 1);
		var product = new TestProduct
		{
			Name = "DateOnly Update Test",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = originalDate,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		var newDate = new DateOnly(2024, 12, 25);
		product.ReleaseDate = newDate;
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.ReleaseDate.Should().Be(newDate);
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateNullableDateOnlyFromNullToValue()
	{
		var product = new TestProduct
		{
			Name = "Nullable DateOnly Update Test",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			DiscontinuedDate = null,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		var discontinuedDate = new DateOnly(2025, 6, 30);
		product.DiscontinuedDate = discontinuedDate;
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.DiscontinuedDate.Should().Be(discontinuedDate);
	}

	[Fact]
	public async Task QueryAsync_ShouldFilterByDateOnly()
	{
		var date1 = new DateOnly(2024, 1, 15);
		var date2 = new DateOnly(2024, 6, 15);

		var product1 = new TestProduct
		{
			Name = "January Product",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = date1,
			CreatedAt = DateTime.UtcNow
		};

		var product2 = new TestProduct
		{
			Name = "June Product",
			Price = 20.00m,
			IsActive = true,
			ReleaseDate = date2,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(product1);
		await _dbService.InsertAsync(product2);

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE release_date = @ReleaseDate",
			new { ReleaseDate = date1 });

		products.Should().HaveCount(1);
		products.First().Name.Should().Be("January Product");
	}

	#endregion

	#region <=== DateTimeOffset Property Tests ===>

	[Fact]
	public async Task InsertAsync_WithDateTimeOffsetProperty_ShouldPersistValue()
	{
		var publishedAt = new DateTimeOffset(2024, 6, 15, 14, 30, 0, TimeSpan.Zero);
		var product = new TestProduct
		{
			Name = "DateTimeOffset Test Product",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = publishedAt,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.PublishedAt.Should().BeCloseTo(publishedAt, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task InsertAsync_WithDateTimeOffsetUtc_ShouldPersistCorrectly()
	{
		var publishedAt = DateTimeOffset.UtcNow;
		var product = new TestProduct
		{
			Name = "DateTimeOffset UTC Test",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = publishedAt,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.PublishedAt.Should().BeCloseTo(publishedAt, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task InsertAsync_WithNullableDateTimeOffset_ShouldPersistNull()
	{
		var product = new TestProduct
		{
			Name = "Nullable DateTimeOffset Test",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = DateTimeOffset.UtcNow,
			LastReviewedAt = null,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.LastReviewedAt.Should().BeNull();
	}

	[Fact]
	public async Task InsertAsync_WithNullableDateTimeOffset_ShouldPersistValue()
	{
		var lastReviewedAt = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero);
		var product = new TestProduct
		{
			Name = "Nullable DateTimeOffset With Value",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = DateTimeOffset.UtcNow,
			LastReviewedAt = lastReviewedAt,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.LastReviewedAt.Should().BeCloseTo(lastReviewedAt, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateDateTimeOffsetProperty()
	{
		var originalPublishedAt = DateTimeOffset.UtcNow.AddDays(-30);
		var product = new TestProduct
		{
			Name = "DateTimeOffset Update Test",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = originalPublishedAt,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		var newPublishedAt = DateTimeOffset.UtcNow;
		product.PublishedAt = newPublishedAt;
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.PublishedAt.Should().BeCloseTo(newPublishedAt, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateNullableDateTimeOffsetFromNullToValue()
	{
		var product = new TestProduct
		{
			Name = "Nullable DateTimeOffset Update Test",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = DateTimeOffset.UtcNow,
			LastReviewedAt = null,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		var lastReviewedAt = DateTimeOffset.UtcNow;
		product.LastReviewedAt = lastReviewedAt;
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved!.LastReviewedAt.Should().BeCloseTo(lastReviewedAt, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task InsertAsync_WithDifferentTimeZones_ShouldStoreAsUtc()
	{
		var time1 = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
		var time2 = new DateTimeOffset(2024, 6, 15, 13, 0, 0, TimeSpan.Zero);

		var product1 = new TestProduct
		{
			Name = "Time1 Product",
			Price = 10.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = time1,
			CreatedAt = DateTime.UtcNow
		};

		var product2 = new TestProduct
		{
			Name = "Time2 Product",
			Price = 20.00m,
			IsActive = true,
			ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
			PublishedAt = time2,
			CreatedAt = DateTime.UtcNow
		};

		var inserted1 = await _dbService.InsertAsync(product1);
		var inserted2 = await _dbService.InsertAsync(product2);

		var retrieved1 = await _dbService.GetAsync<TestProduct>(inserted1.Id);
		var retrieved2 = await _dbService.GetAsync<TestProduct>(inserted2.Id);

		retrieved1!.PublishedAt.Should().BeCloseTo(time1, TimeSpan.FromSeconds(1));
		retrieved2!.PublishedAt.Should().BeCloseTo(time2, TimeSpan.FromSeconds(1));
		(retrieved2!.PublishedAt - retrieved1!.PublishedAt).Should().BeCloseTo(
			TimeSpan.FromHours(1), TimeSpan.FromSeconds(1));
	}

	#endregion

	#region <=== Write(false) Attribute Tests ===>

	[Fact]
	public async Task InsertAsync_WithWriteFalseProperty_ShouldNotIncludeInInsert()
	{
		var product = new TestProduct
		{
			Name = "Write Attribute Test",
			Price = 49.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Write Attribute Test");
		retrieved.Price.Should().Be(49.99m);
	}

	[Fact]
	public async Task UpdateAsync_WithWriteFalseProperty_ShouldNotIncludeInUpdate()
	{
		var product = new TestProduct
		{
			Name = "Original Name",
			Price = 25.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Updated Name";
		product.Price = 99.99m;
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Updated Name");
		retrieved.Price.Should().Be(99.99m);
	}

	[Fact]
	public async Task DisplayName_ShouldBeComputedCorrectly()
	{
		var product = new TestProduct
		{
			Name = "Widget",
			Price = 29.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		product.DisplayName.Should().Be("Widget - $29.99");

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.DisplayName.Should().Be("Widget - $29.99");
	}

	#endregion

	#region <=== JsonColumn Attribute Tests ===>

	[Fact]
	public async Task InsertAsync_WithJsonColumnProperty_ShouldPersistAsJson()
	{
		var metadata = new ProductMetadata
		{
			Manufacturer = "Acme Corp",
			CountryOfOrigin = "USA",
			Tags = ["electronics", "gadgets", "new"],
			Attributes = new Dictionary<string, string>
			{
				["color"] = "blue",
				["weight"] = "1.5kg"
			}
		};

		var product = new TestProduct
		{
			Name = "JSON Test Product",
			Price = 99.99m,
			IsActive = true,
			Metadata = metadata,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Metadata.Should().NotBeNull();
		retrieved.Metadata!.Manufacturer.Should().Be("Acme Corp");
		retrieved.Metadata.CountryOfOrigin.Should().Be("USA");
		retrieved.Metadata.Tags.Should().BeEquivalentTo(["electronics", "gadgets", "new"]);
		retrieved.Metadata.Attributes.Should().ContainKey("color").WhoseValue.Should().Be("blue");
		retrieved.Metadata.Attributes.Should().ContainKey("weight").WhoseValue.Should().Be("1.5kg");
	}

	[Fact]
	public async Task InsertAsync_WithNullJsonColumn_ShouldPersistNull()
	{
		var product = new TestProduct
		{
			Name = "Null JSON Test",
			Price = 49.99m,
			IsActive = true,
			Metadata = null,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Metadata.Should().BeNull();
	}

	[Fact]
	public async Task InsertAsync_WithEmptyJsonObject_ShouldPersistEmptyObject()
	{
		var product = new TestProduct
		{
			Name = "Empty JSON Test",
			Price = 29.99m,
			IsActive = true,
			Metadata = new ProductMetadata(),
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Metadata.Should().NotBeNull();
		retrieved.Metadata!.Manufacturer.Should().BeNull();
		retrieved.Metadata.CountryOfOrigin.Should().BeNull();
		retrieved.Metadata.Tags.Should().BeEmpty();
		retrieved.Metadata.Attributes.Should().BeEmpty();
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateJsonColumnProperty()
	{
		var product = new TestProduct
		{
			Name = "JSON Update Test",
			Price = 59.99m,
			IsActive = true,
			Metadata = new ProductMetadata
			{
				Manufacturer = "Original Manufacturer",
				Tags = ["original"]
			},
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Metadata = new ProductMetadata
		{
			Manufacturer = "Updated Manufacturer",
			CountryOfOrigin = "Canada",
			Tags = ["updated", "modified"],
			Attributes = new Dictionary<string, string> { ["size"] = "large" }
		};
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Metadata.Should().NotBeNull();
		retrieved.Metadata!.Manufacturer.Should().Be("Updated Manufacturer");
		retrieved.Metadata.CountryOfOrigin.Should().Be("Canada");
		retrieved.Metadata.Tags.Should().BeEquivalentTo(["updated", "modified"]);
		retrieved.Metadata.Attributes.Should().ContainKey("size").WhoseValue.Should().Be("large");
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateJsonColumnFromValueToNull()
	{
		var product = new TestProduct
		{
			Name = "JSON to Null Test",
			Price = 39.99m,
			IsActive = true,
			Metadata = new ProductMetadata { Manufacturer = "Some Manufacturer" },
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Metadata = null;
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Metadata.Should().BeNull();
	}

	[Fact]
	public async Task UpdateAsync_ShouldUpdateJsonColumnFromNullToValue()
	{
		var product = new TestProduct
		{
			Name = "Null to JSON Test",
			Price = 44.99m,
			IsActive = true,
			Metadata = null,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Metadata = new ProductMetadata
		{
			Manufacturer = "New Manufacturer",
			Tags = ["new"]
		};
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Metadata.Should().NotBeNull();
		retrieved.Metadata!.Manufacturer.Should().Be("New Manufacturer");
		retrieved.Metadata.Tags.Should().BeEquivalentTo(["new"]);
	}

	#endregion

	#region <=== External Attribute Tests ===>

	[Fact]
	public async Task InsertAsync_WithExternalProperty_ShouldNotIncludeInInsert()
	{
		var product = new TestProduct
		{
			Name = "External Attribute Test",
			Price = 59.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			CategoryName = "Electronics",
			RelatedTags = ["tag1", "tag2"]
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("External Attribute Test");
		retrieved.CategoryName.Should().BeNull();
		retrieved.RelatedTags.Should().BeNull();
	}

	[Fact]
	public async Task UpdateAsync_WithExternalProperty_ShouldNotIncludeInUpdate()
	{
		var product = new TestProduct
		{
			Name = "External Update Test",
			Price = 29.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Updated Name";
		product.CategoryName = "New Category";
		product.RelatedTags = ["new-tag"];
		await _dbService.UpdateAsync(product);

		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Updated Name");
		retrieved.CategoryName.Should().BeNull();
		retrieved.RelatedTags.Should().BeNull();
	}

	[Fact]
	public async Task GetAsync_WithExternalProperty_ShouldNotIncludeInSelect()
	{
		var product = new TestProduct
		{
			Name = "External Select Test",
			Price = 39.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestProduct>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("External Select Test");
		retrieved.CategoryName.Should().BeNull();
		retrieved.RelatedTags.Should().BeNull();
	}

	[Fact]
	public async Task QueryAsync_WithExternalProperty_ShouldNotAffectQuery()
	{
		var product = new TestProduct
		{
			Name = "External Query Test",
			Price = 49.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			CategoryName = "Test Category",
			RelatedTags = ["query-tag"]
		};

		await _dbService.InsertAsync(product);

		var products = await _dbService.QueryAsync<TestProduct>(
			"SELECT * FROM test_products WHERE name = @Name",
			new { Name = "External Query Test" });

		products.Should().HaveCount(1);
		var retrieved = products.First();
		retrieved.Name.Should().Be("External Query Test");
		retrieved.CategoryName.Should().BeNull();
		retrieved.RelatedTags.Should().BeNull();
	}

	[Fact]
	public async Task GetListAsync_WithExternalProperty_ShouldNotIncludeInSelect()
	{
		var product1 = new TestProduct
		{
			Name = "External List Test 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			CategoryName = "Category A"
		};
		var product2 = new TestProduct
		{
			Name = "External List Test 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			CategoryName = "Category B"
		};

		await _dbService.InsertAsync(product1);
		await _dbService.InsertAsync(product2);

		var products = await _dbService.GetListAsync<TestProduct>();

		products.Should().HaveCount(2);
		products.Should().OnlyContain(p => p.CategoryName == null);
		products.Should().OnlyContain(p => p.RelatedTags == null);
	}

	[Fact]
	public void Insert_Sync_WithExternalProperty_ShouldNotIncludeInInsert()
	{
		var product = new TestProduct
		{
			Name = "External Sync Insert Test",
			Price = 69.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			CategoryName = "Sync Category",
			RelatedTags = ["sync-tag"]
		};

		var inserted = _dbService.Insert(product);
		var id = inserted.Id;

		var retrieved = _dbService.Get<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("External Sync Insert Test");
		retrieved.CategoryName.Should().BeNull();
		retrieved.RelatedTags.Should().BeNull();
	}

	[Fact]
	public void Update_Sync_WithExternalProperty_ShouldNotIncludeInUpdate()
	{
		var product = new TestProduct
		{
			Name = "External Sync Update Test",
			Price = 79.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = _dbService.Insert(product);
		var id = inserted.Id;
		product.Id = id;

		product.Name = "Sync Updated Name";
		product.CategoryName = "Sync New Category";
		_dbService.Update(product);

		var retrieved = _dbService.Get<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Sync Updated Name");
		retrieved.CategoryName.Should().BeNull();
	}

	[Fact]
	public void Query_Sync_WithExternalProperty_ShouldNotAffectQuery()
	{
		var product = new TestProduct
		{
			Name = "External Sync Query Test",
			Price = 89.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			CategoryName = "Sync Query Category"
		};

		_dbService.Insert(product);

		var products = _dbService.Query<TestProduct>(
			"SELECT * FROM test_products WHERE name = @Name",
			new { Name = "External Sync Query Test" });

		products.Should().HaveCount(1);
		var retrieved = products.First();
		retrieved.Name.Should().Be("External Sync Query Test");
		retrieved.CategoryName.Should().BeNull();
	}

	#endregion

	#region <=== Insert From Object Tests ===>

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_ShouldInsertEntityWithMappedProperties()
	{
		var obj = new
		{
			Name = "Anonymous Product",
			Price = 49.99m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync<TestProduct>(obj);

		inserted.Id.Should().NotBe(Guid.Empty);
		inserted.Name.Should().Be("Anonymous Product");
		inserted.Price.Should().Be(49.99m);
		inserted.Quantity.Should().Be(10);
		inserted.IsActive.Should().BeTrue();
	}

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_ShouldPersistToDatabase()
	{
		var obj = new
		{
			Name = "Persisted Anonymous",
			Price = 29.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync<TestProduct>(obj);

		var retrieved = await _dbService.GetAsync<TestProduct>(inserted.Id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Persisted Anonymous");
		retrieved.Price.Should().Be(29.99m);
		retrieved.IsActive.Should().BeTrue();
	}

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_WithPartialProperties_ShouldUseDefaults()
	{
		var obj = new
		{
			Name = "Partial Props",
			Price = 10.00m,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync<TestProduct>(obj);

		var retrieved = await _dbService.GetAsync<TestProduct>(inserted.Id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Partial Props");
		retrieved.Price.Should().Be(10.00m);
		retrieved.Quantity.Should().Be(0);
		retrieved.IsActive.Should().BeFalse();
		retrieved.Description.Should().BeNull();
	}

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_WithExtraProperties_ShouldThrow()
	{
		var obj = new
		{
			Name = "Extra Props",
			Price = 15.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			NonExistentProperty = "should throw"
		};

		var act = async () => await _dbService.InsertAsync<TestProduct>(obj);

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*Unknown properties*NonExistentProperty*");
	}

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_WithTypeMismatch_ShouldThrow()
	{
		var obj = new
		{
			Name = 12345,
			Price = 10.00m,
			CreatedAt = DateTime.UtcNow
		};

		var act = async () => await _dbService.InsertAsync<TestProduct>(obj);

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*Type mismatch*Name*");
	}

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_WithNullArgument_ShouldThrow()
	{
		var act = async () => await _dbService.InsertAsync<TestProduct>((object)null!);

		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public void Insert_FromAnonymousObject_ShouldInsertEntityWithMappedProperties()
	{
		var obj = new
		{
			Name = "Sync Anonymous Product",
			Price = 39.99m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = _dbService.Insert<TestProduct>(obj);

		inserted.Id.Should().NotBe(Guid.Empty);
		inserted.Name.Should().Be("Sync Anonymous Product");
		inserted.Price.Should().Be(39.99m);

		var retrieved = _dbService.Get<TestProduct>(inserted.Id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Sync Anonymous Product");
	}

	[Fact]
	public void Insert_FromAnonymousObject_WithTypeMismatch_ShouldThrow()
	{
		var obj = new
		{
			Name = "Valid",
			Price = "not a decimal",
			CreatedAt = DateTime.UtcNow
		};

		var act = () => _dbService.Insert<TestProduct>(obj);

		act.Should().Throw<ArgumentException>()
			.WithMessage("*Type mismatch*Price*");
	}

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_WithNullableProperty_ShouldMapCorrectly()
	{
		var obj = new
		{
			Name = "Nullable Test",
			Price = 20.00m,
			IsActive = true,
			Description = (string?)null,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _dbService.InsertAsync<TestProduct>(obj);

		var retrieved = await _dbService.GetAsync<TestProduct>(inserted.Id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Nullable Test");
		retrieved.Description.Should().BeNull();
	}

	[Fact]
	public async Task InsertAsync_FromAnonymousObject_MultipleInserts_ShouldGenerateUniqueIds()
	{
		var ids = new List<Guid>();
		for (int i = 1; i <= 3; i++)
		{
			var obj = new
			{
				Name = $"Multi Insert {i}",
				Price = i * 10.00m,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			var inserted = await _dbService.InsertAsync<TestProduct>(obj);
			ids.Add(inserted.Id);
		}

		ids.Should().HaveCount(3);
		ids.Should().OnlyHaveUniqueItems();
		ids.Should().NotContain(Guid.Empty);
	}

	#endregion

	#region <=== Update From Object Tests ===>

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_ShouldUpdateOnlySpecifiedProperties()
	{
		var product = new TestProduct
		{
			Name = "Original",
			Description = "Original Desc",
			Price = 10.00m,
			Quantity = 50,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);

		var updateObj = new
		{
			Id = inserted.Id,
			Name = "Updated Name",
			Price = 99.99m
		};

		var updated = await _dbService.UpdateAsync<TestProduct>(updateObj);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(inserted.Id);
		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Updated Name");
		retrieved.Price.Should().Be(99.99m);
		retrieved.Description.Should().Be("Original Desc");
		retrieved.Quantity.Should().Be(50);
		retrieved.IsActive.Should().BeTrue();
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_WithSingleProperty_ShouldUpdateOnlyThat()
	{
		var product = new TestProduct
		{
			Name = "Original",
			Price = 25.00m,
			Quantity = 10,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var inserted = await _dbService.InsertAsync(product);

		var updateObj = new
		{
			Id = inserted.Id,
			Name = "Only Name Updated"
		};

		var updated = await _dbService.UpdateAsync<TestProduct>(updateObj);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(inserted.Id);
		retrieved!.Name.Should().Be("Only Name Updated");
		retrieved.Price.Should().Be(25.00m);
		retrieved.Quantity.Should().Be(10);
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_MissingKey_ShouldThrow()
	{
		var updateObj = new
		{
			Name = "No Key Provided"
		};

		var act = async () => await _dbService.UpdateAsync<TestProduct>(updateObj);

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*Missing required key properties*Id*");
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_WithTypeMismatch_ShouldThrow()
	{
		var product = await _dbService.InsertAsync(new TestProduct
		{
			Name = "Test",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var updateObj = new
		{
			Id = product.Id,
			Price = "not a decimal"
		};

		var act = async () => await _dbService.UpdateAsync<TestProduct>(updateObj);

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*Type mismatch*Price*");
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_WithNonExistentId_ShouldReturnFalse()
	{
		var updateObj = new
		{
			Id = Guid.NewGuid(),
			Name = "Non Existent"
		};

		var updated = await _dbService.UpdateAsync<TestProduct>(updateObj);

		updated.Should().BeFalse();
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_WithNull_ShouldThrow()
	{
		var act = async () => await _dbService.UpdateAsync<TestProduct>((object)null!);

		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_WithExtraProperties_ShouldThrow()
	{
		var product = await _dbService.InsertAsync(new TestProduct
		{
			Name = "Original",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var updateObj = new
		{
			Id = product.Id,
			Name = "Updated",
			NonExistentProperty = "should throw"
		};

		var act = async () => await _dbService.UpdateAsync<TestProduct>(updateObj);

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*Unknown properties*NonExistentProperty*");
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_ShouldOnlyAffectTargetEntity()
	{
		var product1 = await _dbService.InsertAsync(new TestProduct
		{
			Name = "Product 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});
		var product2 = await _dbService.InsertAsync(new TestProduct
		{
			Name = "Product 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var updateObj = new
		{
			Id = product1.Id,
			Name = "Product 1 Updated"
		};
		await _dbService.UpdateAsync<TestProduct>(updateObj);

		var retrieved1 = await _dbService.GetAsync<TestProduct>(product1.Id);
		var retrieved2 = await _dbService.GetAsync<TestProduct>(product2.Id);

		retrieved1!.Name.Should().Be("Product 1 Updated");
		retrieved2!.Name.Should().Be("Product 2");
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_WithNullableProperty_ShouldSetToNull()
	{
		var product = await _dbService.InsertAsync(new TestProduct
		{
			Name = "Has Description",
			Description = "Some description",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var updateObj = new
		{
			Id = product.Id,
			Description = (string?)null
		};

		var updated = await _dbService.UpdateAsync<TestProduct>(updateObj);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestProduct>(product.Id);
		retrieved!.Description.Should().BeNull();
		retrieved.Name.Should().Be("Has Description");
	}

	[Fact]
	public void Update_FromAnonymousObject_ShouldUpdateOnlySpecifiedProperties()
	{
		var product = _dbService.Insert(new TestProduct
		{
			Name = "Sync Original",
			Description = "Sync Desc",
			Price = 15.00m,
			Quantity = 30,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var updateObj = new
		{
			Id = product.Id,
			Name = "Sync Updated",
			Quantity = 99
		};

		var updated = _dbService.Update<TestProduct>(updateObj);

		updated.Should().BeTrue();

		var retrieved = _dbService.Get<TestProduct>(product.Id);
		retrieved!.Name.Should().Be("Sync Updated");
		retrieved.Quantity.Should().Be(99);
		retrieved.Description.Should().Be("Sync Desc");
		retrieved.Price.Should().Be(15.00m);
	}

	[Fact]
	public void Update_FromAnonymousObject_WithTypeMismatch_ShouldThrow()
	{
		var product = _dbService.Insert(new TestProduct
		{
			Name = "Test",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var updateObj = new
		{
			Id = product.Id,
			Name = 12345
		};

		var act = () => _dbService.Update<TestProduct>(updateObj);

		act.Should().Throw<ArgumentException>()
			.WithMessage("*Type mismatch*Name*");
	}

	[Fact]
	public async Task UpdateAsync_FromAnonymousObject_KeyOnly_ShouldReturnFalse()
	{
		var product = await _dbService.InsertAsync(new TestProduct
		{
			Name = "Key Only Test",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		});

		var updateObj = new
		{
			Id = product.Id
		};

		var updated = await _dbService.UpdateAsync<TestProduct>(updateObj);

		updated.Should().BeFalse();
	}

	#endregion

	#region <=== RLS Integration Tests ===>

	[Fact]
	public async Task RlsSelect_WithMultipleEntityIds_ShouldReturnOnlyOwnRows()
	{
		var user1Id = Guid.NewGuid();
		var user2Id = Guid.NewGuid();

		await _dbService.ExecuteAsync(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value)",
			new { OwnerId = user1Id, Name = "User1 Item 1", Value = 10 });
		await _dbService.ExecuteAsync(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value)",
			new { OwnerId = user1Id, Name = "User1 Item 2", Value = 20 });
		await _dbService.ExecuteAsync(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value)",
			new { OwnerId = user2Id, Name = "User2 Item 1", Value = 30 });

		var user1Service = CreateRlsDbService(user1Id);
		var user2Service = CreateRlsDbService(user2Id);

		var user1Items = await user1Service.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");
		var user2Items = await user2Service.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");

		user1Items.Should().HaveCount(2);
		user1Items.Should().OnlyContain(i => i.OwnerId == user1Id);

		user2Items.Should().HaveCount(1);
		user2Items.Should().OnlyContain(i => i.OwnerId == user2Id);
	}

	[Fact]
	public async Task RlsGetAsync_ShouldReturnNullForOtherUsersItems()
	{
		var user1Id = Guid.NewGuid();
		var user2Id = Guid.NewGuid();

		var item1Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user1Id, Name = "User1 Item", Value = 10 });
		var item2Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user2Id, Name = "User2 Item", Value = 20 });

		var user1Service = CreateRlsDbService(user1Id);
		var user2Service = CreateRlsDbService(user2Id);

		var user1OwnItem = await user1Service.GetAsync<TestRlsItem>(item1Id);
		var user1ForeignItem = await user1Service.GetAsync<TestRlsItem>(item2Id);
		var user2OwnItem = await user2Service.GetAsync<TestRlsItem>(item2Id);
		var user2ForeignItem = await user2Service.GetAsync<TestRlsItem>(item1Id);

		user1OwnItem.Should().NotBeNull();
		user1OwnItem!.OwnerId.Should().Be(user1Id);
		user1ForeignItem.Should().BeNull();

		user2OwnItem.Should().NotBeNull();
		user2OwnItem!.OwnerId.Should().Be(user2Id);
		user2ForeignItem.Should().BeNull();
	}

	[Fact]
	public async Task RlsInsertAsync_ShouldOnlyBeVisibleToInsertingUser()
	{
		var user1Id = Guid.NewGuid();
		var user2Id = Guid.NewGuid();

		var user1Service = CreateRlsDbService(user1Id);
		var user2Service = CreateRlsDbService(user2Id);

		var item = new TestRlsItem { OwnerId = user1Id, Name = "User1 New Item", Value = 100 };
		var inserted = await user1Service.InsertAsync(item);

		var user1Items = await user1Service.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");
		var user2Items = await user2Service.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");
		var allItems = await _dbService.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");

		inserted.Id.Should().NotBe(Guid.Empty);
		user1Items.Should().HaveCount(1).And.OnlyContain(i => i.OwnerId == user1Id);
		user2Items.Should().BeEmpty();
		allItems.Should().HaveCount(1);
	}

	[Fact]
	public async Task RlsUpdateAsync_ShouldOnlyUpdateOwnRows()
	{
		var user1Id = Guid.NewGuid();
		var user2Id = Guid.NewGuid();

		var item1Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user1Id, Name = "User1 Item", Value = 10 });
		var item2Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user2Id, Name = "User2 Item", Value = 20 });

		var user1Service = CreateRlsDbService(user1Id);

		var ownItem = new TestRlsItem { Id = item1Id, OwnerId = user1Id, Name = "User1 Updated", Value = 99 };
		var foreignItem = new TestRlsItem
		{
			Id = item2Id,
			OwnerId = user2Id,
			Name = "User2 Tampered",
			Value = 999
		};

		var ownUpdated = await user1Service.UpdateAsync(ownItem);
		var foreignUpdated = await user1Service.UpdateAsync(foreignItem);

		ownUpdated.Should().BeTrue();
		foreignUpdated.Should().BeFalse();

		var item1Result = await _dbService.QueryAsync<TestRlsItem>(
			"SELECT * FROM test_rls_items WHERE id = @Id", new { Id = item1Id });
		var item2Result = await _dbService.QueryAsync<TestRlsItem>(
			"SELECT * FROM test_rls_items WHERE id = @Id", new { Id = item2Id });

		item1Result.First().Name.Should().Be("User1 Updated");
		item2Result.First().Name.Should().Be("User2 Item");
	}

	[Fact]
	public async Task RlsDeleteAsync_ShouldOnlyDeleteOwnRows()
	{
		var user1Id = Guid.NewGuid();
		var user2Id = Guid.NewGuid();

		var item1Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user1Id, Name = "User1 Item", Value = 10 });
		var item2Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user2Id, Name = "User2 Item", Value = 20 });

		var user1Service = CreateRlsDbService(user1Id);

		var foreignItem = new TestRlsItem { Id = item2Id, OwnerId = user2Id, Name = "User2 Item" };
		var ownItem = new TestRlsItem { Id = item1Id, OwnerId = user1Id, Name = "User1 Item" };

		var ownDeleted = await user1Service.DeleteAsync(ownItem);
		var foreignDeleted = await user1Service.DeleteAsync(foreignItem);

		foreignDeleted.Should().BeFalse();
		ownDeleted.Should().BeTrue();

		var remainingItems = await _dbService.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");
		remainingItems.Should().HaveCount(1);
		remainingItems.First().OwnerId.Should().Be(user2Id);
	}

	[Fact]
	public async Task RlsGetListAsync_ShouldNotReturnOtherUsersItems()
	{
		var user1Id = Guid.NewGuid();
		var user2Id = Guid.NewGuid();

		var item1Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user1Id, Name = "User1 Item", Value = 10 });
		var item2Id = await _dbService.ExecuteScalarAsync<Guid>(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value) RETURNING id",
			new { OwnerId = user2Id, Name = "User2 Item", Value = 20 });

		var user1Service = CreateRlsDbService(user1Id);

		var ownList = await user1Service.GetListAsync<TestRlsItem>(new List<Guid> { item1Id });
		var foreignList = await user1Service.GetListAsync<TestRlsItem>(new List<Guid> { item2Id });
		var mixedList = await user1Service.GetListAsync<TestRlsItem>(
			new List<Guid> { item1Id, item2Id });

		ownList.Should().HaveCount(1).And.OnlyContain(i => i.OwnerId == user1Id);
		foreignList.Should().BeEmpty();
		mixedList.Should().HaveCount(1).And.OnlyContain(i => i.OwnerId == user1Id);
	}

	[Fact]
	public async Task RlsIsolation_ThreeEntityIds_ShouldBeCompletelyIsolated()
	{
		var user1Id = Guid.NewGuid();
		var user2Id = Guid.NewGuid();
		var user3Id = Guid.NewGuid();

		for (var i = 1; i <= 2; i++)
			await _dbService.ExecuteAsync(
				"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value)",
				new { OwnerId = user1Id, Name = $"User1 Item {i}", Value = i * 10 });

		for (var i = 1; i <= 3; i++)
			await _dbService.ExecuteAsync(
				"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value)",
				new { OwnerId = user2Id, Name = $"User2 Item {i}", Value = i * 10 });

		await _dbService.ExecuteAsync(
			"INSERT INTO test_rls_items (owner_id, name, value) VALUES (@OwnerId, @Name, @Value)",
			new { OwnerId = user3Id, Name = "User3 Item 1", Value = 10 });

		var user1Service = CreateRlsDbService(user1Id);
		var user2Service = CreateRlsDbService(user2Id);
		var user3Service = CreateRlsDbService(user3Id);

		var user1Items = await user1Service.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");
		var user2Items = await user2Service.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");
		var user3Items = await user3Service.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");
		var adminItems = await _dbService.QueryAsync<TestRlsItem>("SELECT * FROM test_rls_items");

		user1Items.Should().HaveCount(2).And.OnlyContain(i => i.OwnerId == user1Id);
		user2Items.Should().HaveCount(3).And.OnlyContain(i => i.OwnerId == user2Id);
		user3Items.Should().HaveCount(1).And.OnlyContain(i => i.OwnerId == user3Id);
		adminItems.Should().HaveCount(6);
	}

	#endregion

	#region <=== Test Helpers ===>

	private IDbService CreateRlsDbService(Guid userId) =>
		new DbService(_fixture.ConnectionString, NullDbEntityCache.Instance,
			new RlsContextProvider(userId), _fixture.RlsOptions);

	[Table("test_rls_items")]
	private sealed class TestRlsItem
	{
		[Key]
		public Guid Id { get; set; }
		public Guid OwnerId { get; set; }
		public string Name { get; set; } = string.Empty;
		public int Value { get; set; }
	}

	private sealed class AdminRlsContextProvider : IRlsContextProvider
	{
		public string? EntityId => string.Empty;
		public IReadOnlyDictionary<string, string> CustomClaims { get; } =
			new Dictionary<string, string>();
	}

	private sealed class RlsContextProvider : IRlsContextProvider
	{
		public RlsContextProvider(Guid entityId) => EntityId = entityId.ToString();
		public string? EntityId { get; }
		public IReadOnlyDictionary<string, string> CustomClaims { get; } =
			new Dictionary<string, string>();
	}

	#endregion
}
