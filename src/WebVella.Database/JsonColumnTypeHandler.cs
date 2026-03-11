using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Text.Json;
using Dapper;
using NpgsqlTypes;

namespace WebVella.Database;

/// <summary>
/// Dapper type handler for properties marked with [JsonColumn] attribute.
/// Serializes objects to JSON when writing and deserializes when reading.
/// Uses PostgreSQL's JSONB type for storage.
/// </summary>
/// <typeparam name="T">The type of the object to serialize/deserialize.</typeparam>
public class JsonColumnTypeHandler<T> : SqlMapper.TypeHandler<T>
{
	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	/// <inheritdoc/>
	public override T? Parse(object value)
	{
		if (value is null or DBNull)
			return default;

		var json = value.ToString();
		if (string.IsNullOrEmpty(json))
			return default;

		return JsonSerializer.Deserialize<T>(json, _jsonOptions);
	}

	/// <inheritdoc/>
	public override void SetValue(IDbDataParameter parameter, T? value)
	{
		if (value is null)
		{
			parameter.Value = DBNull.Value;
		}
		else
		{
			parameter.Value = JsonSerializer.Serialize(value, _jsonOptions);
		}

		// Set NpgsqlDbType to Jsonb for proper PostgreSQL JSONB handling
		if (parameter is Npgsql.NpgsqlParameter npgsqlParameter)
		{
			npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
		}
		else
		{
			parameter.DbType = DbType.String;
		}
	}
}

/// <summary>
/// Provides registration methods for JSON column type handlers.
/// </summary>
public static class JsonColumnTypeHandlerExtensions
{
	private static readonly ConcurrentDictionary<Type, bool> _registeredTypes = new();

	/// <summary>
	/// Registers a JSON column type handler for the specified type.
	/// Call this method during application startup for each type that uses [JsonColumn].
	/// </summary>
	/// <typeparam name="T">The type to register as a JSON column.</typeparam>
	public static void RegisterJsonColumnType<T>()
	{
		RegisterJsonColumnType(typeof(T));
	}

	/// <summary>
	/// Registers a JSON column type handler for the specified type.
	/// </summary>
	/// <param name="type">The type to register as a JSON column.</param>
	public static void RegisterJsonColumnType(Type type)
	{
		if (_registeredTypes.TryAdd(type, true))
		{
			var handlerType = typeof(JsonColumnTypeHandler<>).MakeGenericType(type);
			var handler = Activator.CreateInstance(handlerType)!;
			SqlMapper.AddTypeHandler(type, (SqlMapper.ITypeHandler)handler);
		}
	}

	/// <summary>
	/// Scans an entity type for properties marked with [JsonColumn] and registers type handlers for them.
	/// </summary>
	/// <typeparam name="TEntity">The entity type to scan.</typeparam>
	public static void RegisterJsonColumnsFromEntity<TEntity>()
	{
		RegisterJsonColumnsFromEntity(typeof(TEntity));
	}

	/// <summary>
	/// Scans an entity type for properties marked with [JsonColumn] and registers type handlers for them.
	/// </summary>
	/// <param name="entityType">The entity type to scan.</param>
	public static void RegisterJsonColumnsFromEntity(Type entityType)
	{
		var jsonProperties = entityType.GetProperties()
			.Where(p => p.GetCustomAttribute<JsonColumnAttribute>() != null);

		foreach (var property in jsonProperties)
		{
			var propertyType = property.PropertyType;

			// Handle nullable types - get the underlying type
			var typeToRegister = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

			RegisterJsonColumnType(typeToRegister);
		}
	}

	/// <summary>
	/// Scans all types in an assembly for properties marked with [JsonColumn] and registers type handlers.
	/// </summary>
	/// <param name="assembly">The assembly to scan.</param>
	public static void RegisterJsonColumnsFromAssembly(Assembly assembly)
	{
		var entityTypes = assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract);

		foreach (var entityType in entityTypes)
		{
			RegisterJsonColumnsFromEntity(entityType);
		}
	}

	/// <summary>
	/// Registers JSON column type handlers for common collection types.
	/// </summary>
	public static void RegisterCommonJsonColumnTypes()
	{
		RegisterJsonColumnType<Dictionary<string, object>>();
		RegisterJsonColumnType<Dictionary<string, string>>();
		RegisterJsonColumnType<List<string>>();
		RegisterJsonColumnType<List<int>>();
		RegisterJsonColumnType<string[]>();
		RegisterJsonColumnType<int[]>();
	}
}
