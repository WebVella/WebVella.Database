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
/// Marks a class as a container for multiple result sets.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class MultiQueryAttribute : Attribute { }


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
/// Maps a property to a specific result set index in a multi-query execution.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ResultSetAttribute : Attribute
{
	/// <summary>
	/// Gets the result set index (0-based).
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Gets the name of the foreign key property in the child entity that references the parent's key.
	/// Used by QueryMultipleList to map child entities to their parent.
	/// </summary>
	public string? ForeignKey { get; set; }

	/// <summary>
	/// Gets the name of the parent entity's key property that the foreign key references.
	/// Defaults to "Id" if not specified.
	/// </summary>
	public string ParentKey { get; set; } = "Id";

	/// <summary>
	/// Initializes a new instance of the <see cref="ResultSetAttribute"/> class.
	/// </summary>
	/// <param name="index">The result set index (0-based).</param>
	public ResultSetAttribute(int index) => Index = index;
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

/// <summary>
/// Specifies an explicit database column name for a property.
/// When applied, the specified name is used instead of the
/// auto-generated snake_case conversion.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DbColumnAttribute : Attribute
{
	/// <summary>
	/// Gets the database column name.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DbColumnAttribute"/> class.
	/// </summary>
	/// <param name="name">The database column name.</param>
	public DbColumnAttribute(string name)
	{
		Name = name;
	}
}
