using Microsoft.Extensions.Caching.Memory;

namespace WebVella.Database;

/// <summary>
/// Provides entity caching functionality for DbService.
/// </summary>
public interface IDbEntityCache
{
	/// <summary>
	/// Gets an entity from cache by its key.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="key">The cache key.</param>
	/// <param name="entity">The cached entity if found.</param>
	/// <returns>True if the entity was found in cache; otherwise, false.</returns>
	bool TryGet<T>(string key, out T? entity) where T : class;

	/// <summary>
	/// Sets an entity in cache.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="key">The cache key.</param>
	/// <param name="entity">The entity to cache.</param>
	/// <param name="durationSeconds">Cache duration in seconds.</param>
	/// <param name="slidingExpiration">Whether to use sliding expiration.</param>
	void Set<T>(string key, T? entity, int durationSeconds, bool slidingExpiration) where T : class;

	/// <summary>
	/// Sets a collection of entities in cache.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="key">The cache key.</param>
	/// <param name="entities">The entities to cache.</param>
	/// <param name="durationSeconds">Cache duration in seconds.</param>
	/// <param name="slidingExpiration">Whether to use sliding expiration.</param>
	void SetCollection<T>(string key, IEnumerable<T> entities, int durationSeconds, bool slidingExpiration)
		where T : class;

	/// <summary>
	/// Gets a collection of entities from cache.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="key">The cache key.</param>
	/// <param name="entities">The cached entities if found.</param>
	/// <returns>True if the collection was found in cache; otherwise, false.</returns>
	bool TryGetCollection<T>(string key, out IEnumerable<T>? entities) where T : class;

	/// <summary>
	/// Invalidates all cached entries for the specified entity type.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	void Invalidate<T>() where T : class;

	/// <summary>
	/// Invalidates all cached entries for the specified entity type.
	/// </summary>
	/// <param name="entityType">The entity type.</param>
	void Invalidate(Type entityType);

	/// <summary>
	/// Generates a cache key for a single entity by its ID.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="id">The entity ID.</param>
	/// <returns>The cache key.</returns>
	string GenerateKey<T>(Guid id) where T : class;

	/// <summary>
	/// Generates a cache key for a single entity by composite keys.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="keys">The composite keys.</param>
	/// <returns>The cache key.</returns>
	string GenerateKey<T>(Dictionary<string, Guid> keys) where T : class;

	/// <summary>
	/// Generates a cache key for a collection query.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="suffix">Optional suffix for the key.</param>
	/// <returns>The cache key.</returns>
	string GenerateCollectionKey<T>(string? suffix = null) where T : class;
}

/// <summary>
/// Default implementation of <see cref="IDbEntityCache"/> using <see cref="IMemoryCache"/>.
/// </summary>
public class DbEntityCache : IDbEntityCache
{
	private readonly IMemoryCache _cache;
	private readonly ConcurrentDictionary<Type, HashSet<string>> _typeKeyTracker = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="DbEntityCache"/> class.
	/// </summary>
	/// <param name="cache">The memory cache instance.</param>
	public DbEntityCache(IMemoryCache cache)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
	}

	/// <inheritdoc/>
	public bool TryGet<T>(string key, out T? entity) where T : class
	{
		return _cache.TryGetValue(key, out entity);
	}

	/// <inheritdoc/>
	public void Set<T>(string key, T? entity, int durationSeconds, bool slidingExpiration) where T : class
	{
		var options = CreateCacheOptions(durationSeconds, slidingExpiration);
		_cache.Set(key, entity, options);
		TrackKey<T>(key);
	}

	/// <inheritdoc/>
	public void SetCollection<T>(string key, IEnumerable<T> entities, int durationSeconds, bool slidingExpiration)
		where T : class
	{
		var options = CreateCacheOptions(durationSeconds, slidingExpiration);
		var list = entities.ToList();
		_cache.Set(key, list, options);
		TrackKey<T>(key);
	}

	/// <inheritdoc/>
	public bool TryGetCollection<T>(string key, out IEnumerable<T>? entities) where T : class
	{
		if (_cache.TryGetValue(key, out List<T>? list))
		{
			entities = list;
			return true;
		}

		entities = null;
		return false;
	}

	/// <inheritdoc/>
	public void Invalidate<T>() where T : class
	{
		Invalidate(typeof(T));
	}

	/// <inheritdoc/>
	public void Invalidate(Type entityType)
	{
		if (_typeKeyTracker.TryGetValue(entityType, out var keys))
		{
			foreach (var key in keys.ToList())
			{
				_cache.Remove(key);
			}
			keys.Clear();
		}
	}

	/// <inheritdoc/>
	public string GenerateKey<T>(Guid id) where T : class
	{
		return $"Entity:{typeof(T).FullName}:Id:{id}";
	}

	/// <inheritdoc/>
	public string GenerateKey<T>(Dictionary<string, Guid> keys) where T : class
	{
		var sortedKeys = keys.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}");
		return $"Entity:{typeof(T).FullName}:Keys:{string.Join("&", sortedKeys)}";
	}

	/// <inheritdoc/>
	public string GenerateCollectionKey<T>(string? suffix = null) where T : class
	{
		return suffix == null
			? $"Collection:{typeof(T).FullName}:All"
			: $"Collection:{typeof(T).FullName}:{suffix}";
	}

	private static MemoryCacheEntryOptions CreateCacheOptions(int durationSeconds, bool slidingExpiration)
	{
		var options = new MemoryCacheEntryOptions();

		if (slidingExpiration)
		{
			options.SlidingExpiration = TimeSpan.FromSeconds(durationSeconds);
		}
		else
		{
			options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(durationSeconds);
		}

		return options;
	}

	private void TrackKey<T>(string key) where T : class
	{
		var keys = _typeKeyTracker.GetOrAdd(typeof(T), _ => []);
		lock (keys)
		{
			keys.Add(key);
		}
	}
}

/// <summary>
/// A no-op implementation of <see cref="IDbEntityCache"/> that disables caching.
/// </summary>
public class NullDbEntityCache : IDbEntityCache
{
	/// <summary>
	/// Gets the singleton instance of the null cache.
	/// </summary>
	public static NullDbEntityCache Instance { get; } = new();

	private NullDbEntityCache() { }

	/// <inheritdoc/>
	public bool TryGet<T>(string key, out T? entity) where T : class
	{
		entity = null;
		return false;
	}

	/// <inheritdoc/>
	public void Set<T>(string key, T? entity, int durationSeconds, bool slidingExpiration) where T : class { }

	/// <inheritdoc/>
	public void SetCollection<T>(string key, IEnumerable<T> entities, int durationSeconds, bool slidingExpiration)
		where T : class { }

	/// <inheritdoc/>
	public bool TryGetCollection<T>(string key, out IEnumerable<T>? entities) where T : class
	{
		entities = null;
		return false;
	}

	/// <inheritdoc/>
	public void Invalidate<T>() where T : class { }

	/// <inheritdoc/>
	public void Invalidate(Type entityType) { }

	/// <inheritdoc/>
	public string GenerateKey<T>(Guid id) where T : class => string.Empty;

	/// <inheritdoc/>
	public string GenerateKey<T>(Dictionary<string, Guid> keys) where T : class => string.Empty;

	/// <inheritdoc/>
	public string GenerateCollectionKey<T>(string? suffix = null) where T : class => string.Empty;
}
