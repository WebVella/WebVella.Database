using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Integration tests specifically for HybridCache functionality to ensure proper migration from IMemoryCache.
/// </summary>
public class HybridCacheIntegrationTests
{
	#region <=== HybridCache Registration Tests ===>

	[Fact]
	public void DbEntityCache_ShouldUseHybridCache()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();

		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		hybridCache.Should().NotBeNull();

		var cache = new DbEntityCache(hybridCache);
		cache.Should().NotBeNull();
		cache.Should().BeOfType<DbEntityCache>();
	}

	[Fact]
	public void AddWebVellaDatabase_WithCaching_ShouldRegisterHybridCache()
	{
		var services = new ServiceCollection();
		services.AddWebVellaDatabase("Host=localhost;Database=test", enableCaching: true);

		var serviceProvider = services.BuildServiceProvider();
		
		var hybridCache = serviceProvider.GetService<HybridCache>();
		hybridCache.Should().NotBeNull("HybridCache should be registered when caching is enabled");

		var dbCache = serviceProvider.GetService<IDbEntityCache>();
		dbCache.Should().NotBeNull();
		dbCache.Should().BeOfType<DbEntityCache>();
	}

	#endregion

	#region <=== Async Operations Tests ===>

	[Fact]
	public async Task GetOrCreateAsync_ShouldBeFullyAsync()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var factoryCalled = false;
		var key = $"test-key-{Guid.NewGuid()}";

		var result = await cache.GetOrCreateAsync<TestCacheEntity>(
			key,
			async ct =>
			{
				factoryCalled = true;
				await Task.Delay(10, ct);
				return new TestCacheEntity { Id = Guid.NewGuid(), Name = "Async Test" };
			},
			durationSeconds: 60,
			slidingExpiration: false);

		factoryCalled.Should().BeTrue("Factory should be called for cache miss");
		result.Should().NotBeNull();
		result!.Name.Should().Be("Async Test");
	}

	[Fact]
	public async Task GetOrCreateCollectionAsync_ShouldBeFullyAsync()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var factoryCalled = false;
		var key = $"test-collection-{Guid.NewGuid()}";

		var result = await cache.GetOrCreateCollectionAsync<TestCacheEntity>(
			key,
			async ct =>
			{
				factoryCalled = true;
				await Task.Delay(10, ct);
				return new List<TestCacheEntity>
				{
					new() { Id = Guid.NewGuid(), Name = "Item 1" },
					new() { Id = Guid.NewGuid(), Name = "Item 2" }
				};
			},
			durationSeconds: 60,
			slidingExpiration: false);

		factoryCalled.Should().BeTrue("Factory should be called for cache miss");
		result.Should().NotBeNull();
		result.Should().HaveCount(2);
	}

	#endregion

	#region <=== Tag-Based Invalidation Tests ===>

	[Fact]
	public async Task InvalidateByTagAsync_ShouldInvalidateCacheEntriesWithTag()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var tag = $"table:test_entities_{Guid.NewGuid()}";
		var key1 = $"entity-1-{Guid.NewGuid()}";
		var key2 = $"entity-2-{Guid.NewGuid()}";

		var factory1Calls = 0;
		var factory2Calls = 0;

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key1,
			_ => { factory1Calls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Entity 1" }); },
			60,
			false,
			new[] { tag });

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key2,
			_ => { factory2Calls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Entity 2" }); },
			60,
			false,
			new[] { tag });

		factory1Calls.Should().Be(1, "First call should invoke factory");
		factory2Calls.Should().Be(1, "First call should invoke factory");

		await cache.InvalidateByTagAsync(tag);

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key1,
			_ => { factory1Calls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Entity 1 Refreshed" }); },
			60,
			false,
			new[] { tag });

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key2,
			_ => { factory2Calls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Entity 2 Refreshed" }); },
			60,
			false,
			new[] { tag });

		factory1Calls.Should().Be(2, "After invalidation, factory should be called again");
		factory2Calls.Should().Be(2, "After invalidation, factory should be called again");
	}

	[Fact]
	public async Task InvalidateByTagAsync_ShouldOnlyInvalidateEntriesWithSpecificTag()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var tag1 = $"table:users_{Guid.NewGuid()}";
		var tag2 = $"table:products_{Guid.NewGuid()}";
		var key1 = $"user-{Guid.NewGuid()}";
		var key2 = $"product-{Guid.NewGuid()}";

		var userFactoryCalls = 0;
		var productFactoryCalls = 0;

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key1,
			_ => { userFactoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "User" }); },
			60,
			false,
			new[] { tag1 });

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key2,
			_ => { productFactoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Product" }); },
			60,
			false,
			new[] { tag2 });

		userFactoryCalls.Should().Be(1);
		productFactoryCalls.Should().Be(1);

		await cache.InvalidateByTagAsync(tag1);

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key1,
			_ => { userFactoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "User Refreshed" }); },
			60,
			false,
			new[] { tag1 });

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key2,
			_ => { productFactoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Product" }); },
			60,
			false,
			new[] { tag2 });

		userFactoryCalls.Should().Be(2, "User cache should be invalidated");
		productFactoryCalls.Should().Be(1, "Product cache should NOT be invalidated");
	}

	[Fact]
	public async Task MultipleTagsPerEntry_ShouldAllowInvalidationByAnyTag()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var tag1 = $"table:orders_{Guid.NewGuid()}";
		var tag2 = $"table:users_{Guid.NewGuid()}";
		var key = $"order-with-user-{Guid.NewGuid()}";

		var factoryCalls = 0;

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key,
			_ => { factoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Order with User" }); },
			60,
			false,
			new[] { tag1, tag2 });

		factoryCalls.Should().Be(1);

		await cache.InvalidateByTagAsync(tag2);

		await cache.GetOrCreateAsync<TestCacheEntity>(
			key,
			_ => { factoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Order with User Refreshed" }); },
			60,
			false,
			new[] { tag1, tag2 });

		factoryCalls.Should().Be(2, "Invalidating by tag2 should clear the entry tagged with both tag1 and tag2");
	}

	#endregion

	#region <=== Cache Options Tests ===>

	[Fact]
	public async Task CacheOptions_ShouldRespectExpiration()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var key = $"expiring-entry-{Guid.NewGuid()}";
		var factoryCalls = 0;

		var result1 = await cache.GetOrCreateAsync<TestCacheEntity>(
			key,
			_ => { factoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Expiring" }); },
			durationSeconds: 1,
			slidingExpiration: false);

		factoryCalls.Should().Be(1);
		result1.Should().NotBeNull();

		await Task.Delay(1500);

		var result2 = await cache.GetOrCreateAsync<TestCacheEntity>(
			key,
			_ => { factoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = "Expiring Refreshed" }); },
			durationSeconds: 1,
			slidingExpiration: false);

		factoryCalls.Should().Be(2, "After expiration, factory should be called again");
	}

	#endregion

	#region <=== NullDbEntityCache Tests ===>

	[Fact]
	public async Task NullDbEntityCache_GetOrCreateAsync_ShouldAlwaysCallFactory()
	{
		var cache = NullDbEntityCache.Instance;
		var key = $"null-cache-{Guid.NewGuid()}";

		var factoryCalls = 0;

		for (int i = 0; i < 5; i++)
		{
			await cache.GetOrCreateAsync<TestCacheEntity>(
				key,
				_ => { factoryCalls++; return ValueTask.FromResult<TestCacheEntity?>(new TestCacheEntity { Id = Guid.NewGuid(), Name = $"Call {i}" }); },
				60,
				false);
		}

		factoryCalls.Should().Be(5, "NullCache should always call factory");
	}

	[Fact]
	public async Task NullDbEntityCache_GetOrCreateCollectionAsync_ShouldAlwaysCallFactory()
	{
		var cache = NullDbEntityCache.Instance;
		var key = $"null-cache-collection-{Guid.NewGuid()}";

		var factoryCalls = 0;

		for (int i = 0; i < 3; i++)
		{
			await cache.GetOrCreateCollectionAsync<TestCacheEntity>(
				key,
				_ =>
				{
					factoryCalls++;
					return ValueTask.FromResult<IEnumerable<TestCacheEntity>>(new List<TestCacheEntity>
					{
						new() { Id = Guid.NewGuid(), Name = $"Item {i}" }
					});
				},
				60,
				false);
		}

		factoryCalls.Should().Be(3, "NullCache should always call factory");
	}

	[Fact]
	public async Task NullDbEntityCache_InvalidateByTagAsync_ShouldNotThrow()
	{
		var cache = NullDbEntityCache.Instance;

		var act = async () => await cache.InvalidateByTagAsync("any-tag");

		await act.Should().NotThrowAsync("NullCache invalidation should be a no-op");
	}

	#endregion

	#region <=== Cache Key Generation Tests ===>

	[Fact]
	public void GenerateKey_WithGuid_ShouldCreateConsistentKey()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var id = Guid.NewGuid();
		var key1 = cache.GenerateKey<TestCacheEntity>(id);
		var key2 = cache.GenerateKey<TestCacheEntity>(id);

		key1.Should().Be(key2, "Same ID should generate same key");
		key1.Should().Contain(id.ToString());
		key1.Should().Contain("TestCacheEntity");
	}

	[Fact]
	public void GenerateKey_WithDictionary_ShouldCreateConsistentKey()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var keys = new Dictionary<string, Guid>
		{
			["TenantId"] = Guid.NewGuid(),
			["UserId"] = Guid.NewGuid()
		};

		var key1 = cache.GenerateKey<TestCacheEntity>(keys);
		var key2 = cache.GenerateKey<TestCacheEntity>(keys);

		key1.Should().Be(key2, "Same keys dictionary should generate same key");
		key1.Should().Contain("TenantId");
		key1.Should().Contain("UserId");
	}

	[Fact]
	public void GenerateCollectionKey_ShouldCreateConsistentKey()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var key1 = cache.GenerateCollectionKey<TestCacheEntity>("active");
		var key2 = cache.GenerateCollectionKey<TestCacheEntity>("active");

		key1.Should().Be(key2, "Same suffix should generate same key");
		key1.Should().Contain("Collection");
		key1.Should().Contain("active");
	}

	#endregion

	#region <=== Cancellation Token Tests ===>

	[Fact]
	public async Task GetOrCreateAsync_ShouldRespectCancellationToken()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var cts = new CancellationTokenSource();
		var key = $"cancellable-{Guid.NewGuid()}";

		cts.Cancel();

		var act = async () => await cache.GetOrCreateAsync<TestCacheEntity>(
			key,
			async ct =>
			{
				await Task.Delay(1000, ct);
				return new TestCacheEntity { Id = Guid.NewGuid(), Name = "Should not complete" };
			},
			60,
			false,
			cancellationToken: cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task InvalidateByTagAsync_ShouldAcceptCancellationToken()
	{
		var services = new ServiceCollection();
		services.AddHybridCache();
		var serviceProvider = services.BuildServiceProvider();
		var hybridCache = serviceProvider.GetRequiredService<HybridCache>();
		var cache = new DbEntityCache(hybridCache);

		var cts = new CancellationTokenSource();
		var tag = $"table:test_{Guid.NewGuid()}";

		var act = async () => await cache.InvalidateByTagAsync(tag, cts.Token);

		await act.Should().NotThrowAsync("InvalidateByTagAsync should accept cancellation token even if not used");
	}

	#endregion

	#region <=== Test Helper Classes ===>

	private class TestCacheEntity
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	#endregion
}
