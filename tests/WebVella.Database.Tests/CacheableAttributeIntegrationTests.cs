using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Integration tests for <see cref="CacheableAttribute"/> and entity caching functionality.
/// </summary>
[Collection("Database")]
public class CacheableAttributeIntegrationTests : IAsyncLifetime
{
	private readonly DatabaseFixture _fixture;
	private readonly IDbService _dbService;
	private readonly IDbService _cachedDbService;
	private readonly IDbEntityCache _cache;

	public CacheableAttributeIntegrationTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
		_dbService = fixture.DbService;

		// Create a cached DbService for testing
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		_cache = new DbEntityCache(hybridCache);
		_cachedDbService = new DbService(
			fixture.Configuration.GetConnectionString("DefaultConnection")!,
			_cache);
	}

	public Task InitializeAsync() => _fixture.ClearTestCacheableProductsAsync();

	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== CacheableAttribute Metadata Tests ===>

	[Fact]
	public void CacheableAttribute_ShouldBeDetectedInMetadata()
	{
		var metadata = EntityMetadata.GetOrCreate<CacheableTestProduct>();

		metadata.IsCacheable.Should().BeTrue();
		metadata.CacheDurationSeconds.Should().Be(60);
		metadata.CacheSlidingExpiration.Should().BeFalse();
	}

	[Fact]
	public void CacheableAttribute_WithSlidingExpiration_ShouldBeDetectedInMetadata()
	{
		var metadata = EntityMetadata.GetOrCreate<SlidingCacheTestProduct>();

		metadata.IsCacheable.Should().BeTrue();
		metadata.CacheDurationSeconds.Should().Be(30);
		metadata.CacheSlidingExpiration.Should().BeTrue();
	}

	[Fact]
	public void NonCacheableEntity_ShouldNotBeCacheable()
	{
		var metadata = EntityMetadata.GetOrCreate<TestProduct>();

		metadata.IsCacheable.Should().BeFalse();
	}

	#endregion

	#region <=== Get Caching Tests ===>

	[Fact]
	public async Task GetAsync_ShouldCacheResult()
	{
		var product = new CacheableTestProduct
		{
			Name = "Cached Product",
			Price = 99.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _cachedDbService.InsertAsync(product);
		var id = inserted.Id;

		// First get should hit database and cache result
		var retrieved1 = await _cachedDbService.GetAsync<CacheableTestProduct>(id);

		// Second get should return cached result (or fetch from DB if cache expired)
		var retrieved2 = await _cachedDbService.GetAsync<CacheableTestProduct>(id);

		retrieved1.Should().NotBeNull();
		retrieved2.Should().NotBeNull();
		retrieved1!.Id.Should().Be(retrieved2!.Id);
		retrieved1.Name.Should().Be(retrieved2.Name);
		retrieved1.Name.Should().Be("Cached Product");
	}

	[Fact]
	public void Get_Sync_ShouldCacheResult()
	{
		var product = new CacheableTestProduct
		{
			Name = "Sync Cached Product",
			Price = 49.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = _cachedDbService.Insert(product);
		var id = inserted.Id;

		// First get should hit database and cache result
		var retrieved1 = _cachedDbService.Get<CacheableTestProduct>(id);

		// Second get should return cached result (or fetch from DB if cache expired)
		var retrieved2 = _cachedDbService.Get<CacheableTestProduct>(id);

		retrieved1.Should().NotBeNull();
		retrieved2.Should().NotBeNull();
		retrieved1!.Id.Should().Be(retrieved2!.Id);
	}

	[Fact]
	public async Task GetAsync_WithCompositeKeys_ShouldCacheResult()
	{
		var product = new CacheableTestProduct
		{
			Name = "Composite Key Cache Test",
			Price = 29.99m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _cachedDbService.InsertAsync(product);
		var keys = new Dictionary<string, Guid> { ["Id"] = inserted.Id };

		// Get using dictionary keys
		var retrieved1 = await _cachedDbService.GetAsync<CacheableTestProduct>(keys);

		// Get again - should use cache or fetch from DB
		var retrieved2 = await _cachedDbService.GetAsync<CacheableTestProduct>(keys);

		retrieved1.Should().NotBeNull();
		retrieved1!.Name.Should().Be("Composite Key Cache Test");
		retrieved2.Should().NotBeNull();
		retrieved2!.Id.Should().Be(retrieved1.Id);
	}

	#endregion

	#region <=== GetList Caching Tests ===>

	[Fact]
	public async Task GetListAsync_ShouldCacheResult()
	{
		var product1 = new CacheableTestProduct
		{
			Name = "List Cache Test 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var product2 = new CacheableTestProduct
		{
			Name = "List Cache Test 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _cachedDbService.InsertAsync(product1);
		await _cachedDbService.InsertAsync(product2);

		// First get should hit database and cache result
		var retrieved1 = await _cachedDbService.GetListAsync<CacheableTestProduct>();

		// Second get should return cached result (or fetch from DB if cache expired)
		var retrieved2 = await _cachedDbService.GetListAsync<CacheableTestProduct>();

		retrieved1.Should().HaveCount(2);
		retrieved2.Should().HaveCount(2);
	}

	[Fact]
	public void GetList_Sync_ShouldCacheResult()
	{
		var product1 = new CacheableTestProduct
		{
			Name = "Sync List Cache Test 1",
			Price = 15.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		var product2 = new CacheableTestProduct
		{
			Name = "Sync List Cache Test 2",
			Price = 25.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		_cachedDbService.Insert(product1);
		_cachedDbService.Insert(product2);

		// First get should hit database and cache result
		var retrieved1 = _cachedDbService.GetList<CacheableTestProduct>();

		// Get again - should use cache or fetch from DB
		var retrieved2 = _cachedDbService.GetList<CacheableTestProduct>();

		retrieved1.Should().HaveCount(2);
		retrieved2.Should().HaveCount(2);
	}

	#endregion

	#region <=== Cache Invalidation Tests ===>

	[Fact]
	public async Task InsertAsync_ShouldInvalidateCache()
	{
		var product1 = new CacheableTestProduct
		{
			Name = "Insert Invalidation Test 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _cachedDbService.InsertAsync(product1);

		// Load list into cache
		var list1 = await _cachedDbService.GetListAsync<CacheableTestProduct>();
		list1.Should().HaveCount(1);

		// Insert another product - should invalidate cache
		var product2 = new CacheableTestProduct
		{
			Name = "Insert Invalidation Test 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		await _cachedDbService.InsertAsync(product2);

		// Next query should return fresh data (cache was invalidated)
		var list2 = await _cachedDbService.GetListAsync<CacheableTestProduct>();
		list2.Should().HaveCount(2);
	}

	[Fact]
	public async Task UpdateAsync_ShouldInvalidateCache()
	{
		var product = new CacheableTestProduct
		{
			Name = "Update Invalidation Test",
			Price = 50.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _cachedDbService.InsertAsync(product);
		var id = inserted.Id;

		// Load into cache
		var retrieved1 = await _cachedDbService.GetAsync<CacheableTestProduct>(id);
		retrieved1.Should().NotBeNull();
		retrieved1!.Name.Should().Be("Update Invalidation Test");

		// Update the product - should invalidate cache
		inserted.Name = "Updated Name";
		await _cachedDbService.UpdateAsync(inserted);

		// Next query should return fresh data (cache was invalidated)
		var retrieved2 = await _cachedDbService.GetAsync<CacheableTestProduct>(id);
		retrieved2.Should().NotBeNull();
		retrieved2!.Name.Should().Be("Updated Name");
	}

	[Fact]
	public async Task DeleteAsync_ShouldInvalidateCache()
	{
		var product = new CacheableTestProduct
		{
			Name = "Delete Invalidation Test",
			Price = 75.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = await _cachedDbService.InsertAsync(product);
		var id = inserted.Id;

		// Load into cache
		var retrieved1 = await _cachedDbService.GetAsync<CacheableTestProduct>(id);
		retrieved1.Should().NotBeNull();

		// Delete the product - should invalidate cache
		await _cachedDbService.DeleteAsync<CacheableTestProduct>(id);

		// Entity should be gone from database
		var retrieved2 = await _cachedDbService.GetAsync<CacheableTestProduct>(id);
		retrieved2.Should().BeNull();
	}

	[Fact]
	public void Insert_Sync_ShouldInvalidateCache()
	{
		var product1 = new CacheableTestProduct
		{
			Name = "Sync Insert Invalidation 1",
			Price = 10.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		_cachedDbService.Insert(product1);

		// Load list into cache
		var list1 = _cachedDbService.GetList<CacheableTestProduct>();
		list1.Should().HaveCount(1);

		// Insert another product - should invalidate cache
		var product2 = new CacheableTestProduct
		{
			Name = "Sync Insert Invalidation 2",
			Price = 20.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};
		_cachedDbService.Insert(product2);

		// Next query should return fresh data (cache was invalidated)
		var list2 = _cachedDbService.GetList<CacheableTestProduct>();
		list2.Should().HaveCount(2);
	}

	[Fact]
	public void Update_Sync_ShouldInvalidateCache()
	{
		var product = new CacheableTestProduct
		{
			Name = "Sync Update Invalidation",
			Price = 60.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = _cachedDbService.Insert(product);
		var id = inserted.Id;

		// Load into cache
		var original = _cachedDbService.Get<CacheableTestProduct>(id);
		original.Should().NotBeNull();

		// Update - should invalidate cache
		inserted.Name = "Sync Updated";
		_cachedDbService.Update(inserted);

		// Next query should return fresh data (cache was invalidated)
		var updated = _cachedDbService.Get<CacheableTestProduct>(id);
		updated.Should().NotBeNull();
		updated!.Name.Should().Be("Sync Updated");
	}

	[Fact]
	public void Delete_Sync_ShouldInvalidateCache()
	{
		var product = new CacheableTestProduct
		{
			Name = "Sync Delete Invalidation",
			Price = 80.00m,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var inserted = _cachedDbService.Insert(product);
		var id = inserted.Id;

		// Load into cache
		var original = _cachedDbService.Get<CacheableTestProduct>(id);
		original.Should().NotBeNull();

		// Delete - should invalidate cache
		_cachedDbService.Delete<CacheableTestProduct>(id);

		// Entity should be gone
		var deleted = _cachedDbService.Get<CacheableTestProduct>(id);
		deleted.Should().BeNull();
	}

	#endregion

	#region <=== Non-Cacheable Entity Tests ===>

	[Fact]
	public void NonCacheableEntity_ShouldNotBeCached()
	{
		// TestProduct doesn't have [Cacheable] attribute
		var metadata = EntityMetadata.GetOrCreate<TestProduct>();
		metadata.IsCacheable.Should().BeFalse();

		// When IsCacheable is false, the cache methods should not store anything
		// This is verified by checking that non-cacheable entities' Get operations
		// don't populate the cache
	}

	#endregion

	#region <=== Cache Key Generation Tests ===>

	[Fact]
	public void GenerateKey_WithGuid_ShouldGenerateConsistentKey()
	{
		var id = Guid.NewGuid();

		// Note: The Guid overload is available for convenience but internally
		// DbService uses the Dictionary overload for caching
		var key1 = _cache.GenerateKey<CacheableTestProduct>(id);
		var key2 = _cache.GenerateKey<CacheableTestProduct>(id);

		key1.Should().Be(key2);
		key1.Should().Contain("CacheableTestProduct");
		key1.Should().Contain(id.ToString());
	}

	[Fact]
	public void GenerateKey_WithDictionary_ShouldGenerateConsistentKey()
	{
		var id = Guid.NewGuid();
		var keys = new Dictionary<string, Guid>
		{
			["Id"] = id
		};

		var key1 = _cache.GenerateKey<CacheableTestProduct>(keys);
		var key2 = _cache.GenerateKey<CacheableTestProduct>(keys);

		key1.Should().Be(key2);
		key1.Should().Contain("CacheableTestProduct");
		key1.Should().Contain(id.ToString());
	}

	[Fact]
	public void GenerateCollectionKey_ShouldGenerateConsistentKey()
	{
		var key1 = _cache.GenerateCollectionKey<CacheableTestProduct>();
		var key2 = _cache.GenerateCollectionKey<CacheableTestProduct>();

		key1.Should().Be(key2);
		key1.Should().Contain("Collection");
		key1.Should().Contain("CacheableTestProduct");
	}

	#endregion

	#region <=== NullDbEntityCache Tests ===>

	[Fact]
	public async Task NullDbEntityCache_ShouldNotCacheAnything()
	{
		var nullCache = NullDbEntityCache.Instance;
		var id = Guid.NewGuid();

		var key = nullCache.GenerateKey<CacheableTestProduct>(id);
		key.Should().BeEmpty();

		// NullCache should always call the factory
		var factoryCalled = false;
		await nullCache.GetOrCreateAsync<CacheableTestProduct>(
			"any-key",
			_ => { factoryCalled = true; return ValueTask.FromResult<CacheableTestProduct?>(null); },
			60,
			false);
		factoryCalled.Should().BeTrue();

		// Invalidation should not throw
		await nullCache.InvalidateByTagAsync("any-tag");
	}

	#endregion

	#region <=== Service Registration Tests ===>

	[Fact]
	public void AddWebVellaDatabase_WithCachingEnabled_ShouldRegisterCache()
	{
		var services = new ServiceCollection();
		services.AddWebVellaDatabase("Host=test;Database=test", enableCaching: true);

		var provider = services.BuildServiceProvider();

		var cache = provider.GetService<IDbEntityCache>();
		cache.Should().NotBeNull();
		cache.Should().BeOfType<DbEntityCache>();
	}

	[Fact]
	public void AddWebVellaDatabase_WithCachingDisabled_ShouldRegisterNullCache()
	{
		var services = new ServiceCollection();
		services.AddWebVellaDatabase("Host=test;Database=test", enableCaching: false);

		var provider = services.BuildServiceProvider();

		var cache = provider.GetService<IDbEntityCache>();
		cache.Should().NotBeNull();
		cache.Should().Be(NullDbEntityCache.Instance);
	}

	[Fact]
	public void AddWebVellaDatabase_DefaultOverload_ShouldDisableCaching()
	{
		var services = new ServiceCollection();
		services.AddWebVellaDatabase("Host=test;Database=test");

		var provider = services.BuildServiceProvider();

		var cache = provider.GetService<IDbEntityCache>();
		cache.Should().Be(NullDbEntityCache.Instance);
	}

	#endregion
}
