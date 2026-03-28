using Microsoft.Extensions.Caching.Hybrid;

namespace WebVella.Database;

/// <summary>
/// Provides entity caching functionality for DbService using <see cref="HybridCache"/>
/// with tag-based invalidation.
/// </summary>
public interface IDbEntityCache
{
	/// <summary>
	/// Gets an entity from cache, or creates and caches it using the factory on a miss.
	/// Tags enable targeted invalidation across multiple tables for future join-based queries.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="key">The cache key.</param>
	/// <param name="factory">The async factory invoked on a cache miss.</param>
	/// <param name="durationSeconds">Cache duration in seconds.</param>
	/// <param name="slidingExpiration">Reserved for future use; currently maps to absolute expiration.</param>
	/// <param name="tags">Tags for targeted invalidation (e.g., table names).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cached or freshly produced entity, or null if the factory returns null.</returns>
	ValueTask<T?> GetOrCreateAsync<T>(
		string key,
		Func<CancellationToken, ValueTask<T?>> factory,
		int durationSeconds,
		bool slidingExpiration,
		IReadOnlyCollection<string>? tags = null,
		CancellationToken cancellationToken = default) where T : class;

	/// <summary>
	/// Gets a collection from cache, or creates and caches it using the factory on a miss.
	/// Tags enable targeted invalidation across multiple tables for future join-based queries.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="key">The cache key.</param>
	/// <param name="factory">The async factory invoked on a cache miss.</param>
	/// <param name="durationSeconds">Cache duration in seconds.</param>
	/// <param name="slidingExpiration">Reserved for future use; currently maps to absolute expiration.</param>
	/// <param name="tags">Tags for targeted invalidation (e.g., table names).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cached or freshly produced collection.</returns>
	ValueTask<IEnumerable<T>> GetOrCreateCollectionAsync<T>(
		string key,
		Func<CancellationToken, ValueTask<IEnumerable<T>>> factory,
		int durationSeconds,
		bool slidingExpiration,
		IReadOnlyCollection<string>? tags = null,
		CancellationToken cancellationToken = default) where T : class;

	/// <summary>
	/// Removes all cache entries associated with the specified tag.
	/// Call when a table is mutated (Insert, Update, Delete) to evict all dependent cached results.
	/// </summary>
	/// <param name="tag">The tag to evict (e.g., "table:users").</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	ValueTask InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);

	/// <summary>
	/// Generates a cache key for a single entity by its ID.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="id">The entity ID.</param>
	/// <param name="rlsContext">Optional RLS context to include in the cache key.</param>
	/// <returns>The cache key.</returns>
	string GenerateKey<T>(Guid id, string? rlsContext = null) where T : class;

	/// <summary>
	/// Generates a cache key for a single entity by composite keys.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">The composite keys.</param>
	/// <param name="rlsContext">Optional RLS context to include in the cache key.</param>
	/// <returns>The cache key.</returns>
	string GenerateKey<T>(Dictionary<string, Guid> keys, string? rlsContext = null) where T : class;

	/// <summary>
	/// Generates a cache key for a collection query.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="suffix">Optional suffix for the key.</param>
	/// <param name="rlsContext">Optional RLS context to include in the cache key.</param>
	/// <returns>The cache key.</returns>
	string GenerateCollectionKey<T>(string? suffix = null, string? rlsContext = null) where T : class;
}

/// <summary>
/// Default implementation of <see cref="IDbEntityCache"/> using <see cref="HybridCache"/>.
/// Entries are tagged with table names to support cross-table invalidation.
/// </summary>
public class DbEntityCache : IDbEntityCache
{
	private readonly HybridCache _cache;

	/// <summary>
	/// Initializes a new instance of the <see cref="DbEntityCache"/> class.
	/// </summary>
	/// <param name="cache">The <see cref="HybridCache"/> instance provided by DI.</param>
	public DbEntityCache(HybridCache cache)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
	}

	/// <inheritdoc/>
	public ValueTask<T?> GetOrCreateAsync<T>(
		string key,
		Func<CancellationToken, ValueTask<T?>> factory,
		int durationSeconds,
		bool slidingExpiration,
		IReadOnlyCollection<string>? tags = null,
		CancellationToken cancellationToken = default) where T : class
		=> _cache.GetOrCreateAsync(key, factory, BuildOptions(durationSeconds), tags, cancellationToken);

	/// <inheritdoc/>
	public async ValueTask<IEnumerable<T>> GetOrCreateCollectionAsync<T>(
		string key,
		Func<CancellationToken, ValueTask<IEnumerable<T>>> factory,
		int durationSeconds,
		bool slidingExpiration,
		IReadOnlyCollection<string>? tags = null,
		CancellationToken cancellationToken = default) where T : class
	{
		var result = await _cache.GetOrCreateAsync<List<T>>(
			key,
			async ct => (await factory(ct)).ToList(),
			BuildOptions(durationSeconds),
			tags,
			cancellationToken);
		return result ?? [];
	}

	/// <inheritdoc/>
	public ValueTask InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
		=> _cache.RemoveByTagAsync(tag, cancellationToken);

	/// <inheritdoc/>
	public string GenerateKey<T>(Guid id, string? rlsContext = null) where T : class
	{
		var baseKey = $"Entity:{typeof(T).FullName}:Id:{id}";
		return string.IsNullOrEmpty(rlsContext) ? baseKey : $"{baseKey}:Rls:{rlsContext}";
	}

	/// <inheritdoc/>
	public string GenerateKey<T>(Dictionary<string, Guid> keys, string? rlsContext = null) where T : class
	{
		var sortedKeys = keys.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}");
		var baseKey = $"Entity:{typeof(T).FullName}:Keys:{string.Join("&", sortedKeys)}";
		return string.IsNullOrEmpty(rlsContext) ? baseKey : $"{baseKey}:Rls:{rlsContext}";
	}

	/// <inheritdoc/>
	public string GenerateCollectionKey<T>(string? suffix = null, string? rlsContext = null) where T : class
	{
		var baseKey = suffix == null
			? $"Collection:{typeof(T).FullName}:All"
			: $"Collection:{typeof(T).FullName}:{suffix}";
		return string.IsNullOrEmpty(rlsContext) ? baseKey : $"{baseKey}:Rls:{rlsContext}";
	}

	private static HybridCacheEntryOptions BuildOptions(int durationSeconds) =>
		new()
		{
			Expiration = TimeSpan.FromSeconds(durationSeconds),
			LocalCacheExpiration = TimeSpan.FromSeconds(durationSeconds)
		};
}

/// <summary>
/// A no-op implementation of <see cref="IDbEntityCache"/> that disables caching.
/// All get-or-create calls pass through directly to the factory.
/// </summary>
public class NullDbEntityCache : IDbEntityCache
{
	/// <summary>
	/// Gets the singleton instance of the null cache.
	/// </summary>
	public static NullDbEntityCache Instance { get; } = new();

	private NullDbEntityCache() { }

	/// <inheritdoc/>
	public ValueTask<T?> GetOrCreateAsync<T>(
		string key,
		Func<CancellationToken, ValueTask<T?>> factory,
		int durationSeconds,
		bool slidingExpiration,
		IReadOnlyCollection<string>? tags = null,
		CancellationToken cancellationToken = default) where T : class
		=> factory(cancellationToken);

	/// <inheritdoc/>
	public ValueTask<IEnumerable<T>> GetOrCreateCollectionAsync<T>(
		string key,
		Func<CancellationToken, ValueTask<IEnumerable<T>>> factory,
		int durationSeconds,
		bool slidingExpiration,
		IReadOnlyCollection<string>? tags = null,
		CancellationToken cancellationToken = default) where T : class
		=> factory(cancellationToken);

	/// <inheritdoc/>
	public ValueTask InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
		=> ValueTask.CompletedTask;

	/// <inheritdoc/>
	public string GenerateKey<T>(Guid id, string? rlsContext = null) where T : class => string.Empty;

	/// <inheritdoc/>
	public string GenerateKey<T>(Dictionary<string, Guid> keys, string? rlsContext = null)
		where T : class => string.Empty;

	/// <inheritdoc/>
	public string GenerateCollectionKey<T>(string? suffix = null, string? rlsContext = null)
		where T : class => string.Empty;
}
