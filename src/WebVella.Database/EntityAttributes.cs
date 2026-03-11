namespace WebVella.Database;

/// <summary>
/// Specifies the table name for an entity.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class TableAttribute : Attribute
{
	public string Name { get; }

	public TableAttribute(string name)
	{
		Name = name;
	}
}

/// <summary>
/// Indicates that entities of this class should be cached.
/// Cache is automatically invalidated when Insert, Update, or Delete operations are performed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CacheableAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the cache duration in seconds. Default is 300 seconds (5 minutes).
	/// </summary>
	public int DurationSeconds { get; set; } = 300;

	/// <summary>
	/// Gets or sets whether to use sliding expiration. Default is false (absolute expiration).
	/// </summary>
	public bool SlidingExpiration { get; set; } = false;

	/// <summary>
	/// Initializes a new instance of the <see cref="CacheableAttribute"/> class with default settings.
	/// </summary>
	public CacheableAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CacheableAttribute"/> class with specified duration.
	/// </summary>
	/// <param name="durationSeconds">Cache duration in seconds.</param>
	public CacheableAttribute(int durationSeconds)
	{
		DurationSeconds = durationSeconds;
	}
}

/// <summary>
/// Marks a property as an auto-generated primary key (UUID with gen_random_uuid()).
/// This property will be excluded from INSERT and populated via RETURNING.
/// Only Guid type is supported. Multiple [Key] properties can be used for composite keys.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class KeyAttribute : Attribute
{
}

/// <summary>
/// Marks a property as an explicit primary key (not auto-generated).
/// This property will be included in INSERT statements.
/// Use this when you want to provide your own key value.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExplicitKeyAttribute : Attribute
{
}

/// <summary>
/// Marks a property as external. It will be excluded from INSERT and UPDATE.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExternalAttribute : Attribute
{
}

/// <summary>
/// Controls whether a property should be written to the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class WriteAttribute : Attribute
{
	public bool Write { get; }

	public WriteAttribute(bool write)
	{
		Write = write;
	}
}

/// <summary>
/// Marks a property as being stored in a JSON column (json, jsonb, or text containing JSON).
/// The property value will be serialized to JSON when writing and deserialized when reading.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class JsonColumnAttribute : Attribute
{
}
