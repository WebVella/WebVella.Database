using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace WebVella.Database;

/// <summary>
/// Represents cached metadata for an entity type, including table name, properties, and pre-built SQL fragments.
/// </summary>
internal sealed class EntityMetadata
{
	/// <summary>
	/// Gets the table name for the entity.
	/// </summary>
	public string TableName { get; }

	/// <summary>
	/// Gets the list of key properties for the entity.
	/// </summary>
	public IReadOnlyList<PropertyInfo> KeyProperties { get; }

	/// <summary>
	/// Gets the list of all readable properties for SELECT operations.
	/// </summary>
	public IReadOnlyList<PropertyInfo> AllProperties { get; }

	/// <summary>
	/// Gets the list of writable properties (excluding keys) for INSERT/UPDATE operations.
	/// </summary>
	public IReadOnlyList<PropertyInfo> WritableProperties { get; }

	/// <summary>
	/// Gets the list of all insert properties (keys + writable properties).
	/// </summary>
	public IReadOnlyList<PropertyInfo> InsertProperties { get; }

	/// <summary>
	/// Gets a dictionary mapping property names (case-insensitive) to PropertyInfo.
	/// </summary>
	public IReadOnlyDictionary<string, PropertyInfo> WritablePropertiesByName { get; }

	/// <summary>
	/// Gets the set of property names that are marked as JSON columns.
	/// </summary>
	public IReadOnlySet<string> JsonColumnProperties { get; }

	/// <summary>
	/// Gets the pre-built SELECT columns clause (e.g., "column_name AS \"PropertyName\"").
	/// </summary>
	public string SelectColumns { get; }

	/// <summary>
	/// Gets the pre-built INSERT columns clause.
	/// </summary>
	public string InsertColumns { get; }

	/// <summary>
	/// Gets the pre-built INSERT parameters clause (e.g., "@Property1, @Property2").
	/// </summary>
	public string InsertParameters { get; }

	/// <summary>
	/// Gets the pre-built RETURNING columns clause for INSERT.
	/// </summary>
	public string ReturningColumns { get; }

	/// <summary>
	/// Gets the pre-built WHERE clause for key-based operations.
	/// </summary>
	public string KeyWhereClause { get; }

	/// <summary>
	/// Gets the pre-built SET clause for UPDATE (all writable properties).
	/// </summary>
	public string UpdateSetClause { get; }

	/// <summary>
	/// Gets the snake_case name of the first key property (for single-key entities).
	/// </summary>
	public string FirstKeyColumnName { get; }

	/// <summary>
	/// Gets the name of the first key property (for single-key entities).
	/// </summary>
	public string FirstKeyPropertyName { get; }

	/// <summary>
	/// Gets a value indicating whether the entity has a single key property.
	/// </summary>
	public bool HasSingleKey { get; }

	/// <summary>
	/// Gets a dictionary mapping key property names to their snake_case column names.
	/// </summary>
	public IReadOnlyDictionary<string, string> KeyPropertyColumnNames { get; }

	/// <summary>
	/// Gets a dictionary mapping all selectable property names (case-insensitive) to their
	/// database column names. Includes key and non-key properties; excludes [External]
	/// and [Write(false)] properties.
	/// </summary>
	public IReadOnlyDictionary<string, string> PropertyColumnNames { get; }

	/// <summary>
	/// Gets a value indicating whether the entity is cacheable.
	/// </summary>
	public bool IsCacheable { get; }

	/// <summary>
	/// Gets the cache duration in seconds. Only applicable if <see cref="IsCacheable"/> is true.
	/// </summary>
	public int CacheDurationSeconds { get; }

	/// <summary>
	/// Gets a value indicating whether to use sliding expiration. Only applicable if <see cref="IsCacheable"/> is true.
	/// </summary>
	public bool CacheSlidingExpiration { get; }

	private EntityMetadata(
		string tableName,
		IReadOnlyList<PropertyInfo> keyProperties,
		IReadOnlyList<PropertyInfo> allProperties,
		IReadOnlyList<PropertyInfo> writableProperties,
		IReadOnlySet<string> jsonColumnProperties,
		bool isCacheable,
		int cacheDurationSeconds,
		bool cacheSlidingExpiration)
	{
		TableName = tableName;
		KeyProperties = keyProperties;
		AllProperties = allProperties;
		WritableProperties = writableProperties;
		JsonColumnProperties = jsonColumnProperties;
		IsCacheable = isCacheable;
		CacheDurationSeconds = cacheDurationSeconds;
		CacheSlidingExpiration = cacheSlidingExpiration;
		InsertProperties = keyProperties.Concat(writableProperties).ToList();
		HasSingleKey = keyProperties.Count == 1;
		FirstKeyPropertyName = keyProperties[0].Name;
		FirstKeyColumnName = GetColumnName(keyProperties[0]);

		WritablePropertiesByName = writableProperties
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		KeyPropertyColumnNames = keyProperties
			.ToDictionary(p => p.Name, p => GetColumnName(p));

		PropertyColumnNames = allProperties
			.ToDictionary(p => p.Name, p => GetColumnName(p), StringComparer.OrdinalIgnoreCase);

		SelectColumns = string.Join(
			", ",
			allProperties.Select(p => $"{GetColumnName(p)} AS \"{p.Name}\""));

		InsertColumns = string.Join(", ", InsertProperties.Select(p => GetColumnName(p)));
		InsertParameters = string.Join(", ", InsertProperties.Select(p => "@" + p.Name));
		ReturningColumns = string.Join(", ", keyProperties.Select(p => GetColumnName(p)));

		KeyWhereClause = string.Join(
			" AND ",
			keyProperties.Select(p => $"{GetColumnName(p)} = @{p.Name}"));

		UpdateSetClause = string.Join(
			", ",
			writableProperties.Select(p => $"{GetColumnName(p)} = @{p.Name}"));
	}

	/// <summary>
	/// Determines whether the specified property is a JSON column.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <returns><c>true</c> if the property is marked with [JsonColumn]; otherwise, <c>false</c>.</returns>
	public bool IsJsonColumn(string propertyName) => JsonColumnProperties.Contains(propertyName);

	/// <summary>
	/// Builds the SET clause for UPDATE with only the specified properties.
	/// </summary>
	public string BuildUpdateSetClause(IEnumerable<PropertyInfo> properties)
	{
		return string.Join(", ", properties.Select(p => $"{GetColumnName(p)} = @{p.Name}"));
	}

	/// <summary>
	/// Returns the database column name for the specified property name.
	/// Throws <see cref="ArgumentException"/> if the property is not found.
	/// </summary>
	/// <param name="propertyName">The C# property name (case-insensitive).</param>
	internal string GetColumnName(string propertyName)
	{
		if (PropertyColumnNames.TryGetValue(propertyName, out var col))
			return col;
		throw new ArgumentException(
			$"Property '{propertyName}' was not found on '{GetType().Name}' " +
			"or is excluded from database queries ([External] / [Write(false)]).",
			nameof(propertyName));
	}

	internal static string GetColumnName(PropertyInfo property)
	{
		var dbColumnAttr = property.GetCustomAttribute<DbColumnAttribute>();
		return dbColumnAttr?.Name ?? ToSnakeCase(property.Name);
	}

	private static string ToSnakeCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;

		var sb = new StringBuilder();
		sb.Append(char.ToLowerInvariant(input[0]));

		for (int i = 1; i < input.Length; i++)
		{
			char c = input[i];
			if (char.IsUpper(c))
			{
				sb.Append('_');
				sb.Append(char.ToLowerInvariant(c));
			}
			else
			{
				sb.Append(c);
			}
		}

		return sb.ToString();
	}

	#region <=== Static Cache ===>

	private static readonly ConcurrentDictionary<Type, EntityMetadata> _cache = new();

	/// <summary>
	/// Gets or creates the metadata for the specified entity type.
	/// </summary>
	public static EntityMetadata GetOrCreate<T>() where T : class
	{
		return _cache.GetOrAdd(typeof(T), static type => CreateMetadata(type));
	}

	/// <summary>
	/// Gets or creates the metadata for the specified entity type.
	/// </summary>
	public static EntityMetadata GetOrCreate(Type type)
	{
		return _cache.GetOrAdd(type, static t => CreateMetadata(t));
	}

	private static EntityMetadata CreateMetadata(Type type)
	{
		var tableName = GetTableName(type);
		var keyProperties = GetKeyProperties(type);
		var allProperties = GetAllProperties(type);
		var writableProperties = GetWritableProperties(type);
		var jsonColumnProperties = GetJsonColumnProperties(type);
		var (isCacheable, cacheDurationSeconds, cacheSlidingExpiration) = GetCacheSettings(type);

		return new EntityMetadata(
			tableName,
			keyProperties,
			allProperties,
			writableProperties,
			jsonColumnProperties,
			isCacheable,
			cacheDurationSeconds,
			cacheSlidingExpiration);
	}

	private static string GetTableName(Type type)
	{
		var tableAttr = type.GetCustomAttribute<TableAttribute>();
		return tableAttr?.Name ?? ToSnakeCase(type.Name);
	}

	private static List<PropertyInfo> GetKeyProperties(Type type)
	{
		var keyProperties = type.GetProperties()
			.Where(p => p.GetCustomAttribute<KeyAttribute>() != null
					 || p.GetCustomAttribute<ExplicitKeyAttribute>() != null)
			.ToList();

		if (keyProperties.Count == 0)
		{
			var idProperty = type.GetProperty("Id") ?? type.GetProperty("id");
			if (idProperty != null)
			{
				keyProperties.Add(idProperty);
			}
		}

		if (keyProperties.Count == 0)
		{
			throw new InvalidOperationException(
				$"No key property found for type {type.Name}. " +
				"Add [Key] or [ExplicitKey] attribute or create an 'Id' property.");
		}

		foreach (var keyProp in keyProperties)
		{
			if (keyProp.PropertyType != typeof(Guid))
			{
				throw new InvalidOperationException(
					$"Key property '{keyProp.Name}' on type {type.Name} must be of type Guid. " +
					$"Found: {keyProp.PropertyType.Name}");
			}
		}

		return keyProperties;
	}

	private static List<PropertyInfo> GetAllProperties(Type type)
	{
		return type.GetProperties()
			.Where(p => p.CanRead)
			.Where(p => p.GetCustomAttribute<ExternalAttribute>() == null)
			.Where(p =>
			{
				var writeAttr = p.GetCustomAttribute<WriteAttribute>();
				return writeAttr == null || writeAttr.Write;
			})
			.ToList();
	}

	private static List<PropertyInfo> GetWritableProperties(Type type)
	{
		return type.GetProperties()
			.Where(p => p.CanRead && p.CanWrite)
			.Where(p => p.GetCustomAttribute<ExternalAttribute>() == null)
			.Where(p =>
			{
				var writeAttr = p.GetCustomAttribute<WriteAttribute>();
				return writeAttr == null || writeAttr.Write;
			})
			.Where(p => p.GetCustomAttribute<KeyAttribute>() == null
					 && p.GetCustomAttribute<ExplicitKeyAttribute>() == null)
			.ToList();
	}

	private static HashSet<string> GetJsonColumnProperties(Type type)
	{
		return type.GetProperties()
			.Where(p => p.GetCustomAttribute<JsonColumnAttribute>() != null)
			.Select(p => p.Name)
			.ToHashSet();
	}

	private static (bool IsCacheable, int DurationSeconds, bool SlidingExpiration) GetCacheSettings(Type type)
	{
		var cacheAttr = type.GetCustomAttribute<CacheableAttribute>();
		if (cacheAttr == null)
		{
			return (false, 0, false);
		}

		return (true, cacheAttr.DurationSeconds, cacheAttr.SlidingExpiration);
	}

	#endregion
}
