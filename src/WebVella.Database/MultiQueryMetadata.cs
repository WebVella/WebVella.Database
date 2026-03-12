namespace WebVella.Database;

/// <summary>
/// Represents metadata for a result set mapping in a multi-query container.
/// </summary>
internal sealed class ResultSetMapping
{
	/// <summary>
	/// Gets the result set index specified by the <see cref="ResultSetAttribute"/>.
	/// </summary>
	public int Index { get; }

	/// <summary>
	/// Gets the property that will receive the result set data.
	/// </summary>
	public PropertyInfo Property { get; }

	/// <summary>
	/// Gets the element type for collections, or the property type for single objects.
	/// </summary>
	public Type ElementType { get; }

	/// <summary>
	/// Gets a value indicating whether the property is a collection type.
	/// </summary>
	public bool IsCollection { get; }

	/// <summary>
	/// Gets the name of the foreign key property in the child entity. Null for non-child mappings.
	/// </summary>
	public string? ForeignKey { get; }

	/// <summary>
	/// Gets the name of the parent entity's key property. Defaults to "Id".
	/// </summary>
	public string ParentKey { get; }

	/// <summary>
	/// Gets the PropertyInfo for the foreign key in the child element type. Null if ForeignKey is not set.
	/// </summary>
	public PropertyInfo? ForeignKeyProperty { get; }

	public ResultSetMapping(
		int index,
		PropertyInfo property,
		Type elementType,
		bool isCollection,
		string? foreignKey = null,
		string parentKey = "Id")
	{
		Index = index;
		Property = property;
		ElementType = elementType;
		IsCollection = isCollection;
		ForeignKey = foreignKey;
		ParentKey = parentKey;

		if (!string.IsNullOrEmpty(foreignKey))
		{
			ForeignKeyProperty = elementType.GetProperty(foreignKey, BindingFlags.Public | BindingFlags.Instance);
		}
	}
}

/// <summary>
/// Represents cached metadata for a multi-query container type.
/// </summary>
internal sealed class MultiQueryMetadata
{
	/// <summary>
	/// Gets the list of result set mappings for the container type.
	/// </summary>
	public IReadOnlyList<ResultSetMapping> ResultSetMappings { get; }

	private MultiQueryMetadata(IReadOnlyList<ResultSetMapping> resultSetMappings)
	{
		ResultSetMappings = resultSetMappings;
	}

	#region <=== Static Cache ===>

	private static readonly ConcurrentDictionary<Type, MultiQueryMetadata> _cache = new();

	/// <summary>
	/// Gets or creates cached metadata for the specified multi-query container type.
	/// </summary>
	/// <typeparam name="T">The multi-query container type.</typeparam>
	/// <returns>The cached metadata for the type.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the type is not marked with <see cref="MultiQueryAttribute"/>.
	/// </exception>
	public static MultiQueryMetadata GetOrCreate<T>() where T : class
	{
		return _cache.GetOrAdd(typeof(T), CreateMetadata);
	}

	private static MultiQueryMetadata CreateMetadata(Type type)
	{
		var multiQueryAttr = type.GetCustomAttribute<MultiQueryAttribute>();
		if (multiQueryAttr == null)
		{
			throw new InvalidOperationException(
				$"Type '{type.Name}' must be marked with [MultiQuery] attribute to use QueryMultiple.");
		}

		var mappings = new List<ResultSetMapping>();
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (var prop in properties)
		{
			var resultSetAttr = prop.GetCustomAttribute<ResultSetAttribute>();
			if (resultSetAttr == null)
				continue;

			var (elementType, isCollection) = GetElementTypeAndCollectionInfo(prop.PropertyType);

			mappings.Add(new ResultSetMapping(
				resultSetAttr.Index,
				prop,
				elementType,
				isCollection,
				resultSetAttr.ForeignKey,
				resultSetAttr.ParentKey));
		}

		if (mappings.Count == 0)
		{
			throw new InvalidOperationException(
				$"Type '{type.Name}' must have at least one property marked with [ResultSet] attribute.");
		}

		var duplicateIndices = mappings
			.GroupBy(m => m.Index)
			.Where(g => g.Count() > 1)
			.Select(g => g.Key)
			.ToList();

		if (duplicateIndices.Count > 0)
		{
			throw new InvalidOperationException(
				$"Type '{type.Name}' has duplicate [ResultSet] indices: {string.Join(", ", duplicateIndices)}.");
		}

		return new MultiQueryMetadata(mappings.OrderBy(m => m.Index).ToList());
	}

	private static (Type elementType, bool isCollection) GetElementTypeAndCollectionInfo(Type propertyType)
	{
		if (propertyType.IsGenericType)
		{
			var genericDef = propertyType.GetGenericTypeDefinition();

			if (genericDef == typeof(List<>) ||
				genericDef == typeof(IList<>) ||
				genericDef == typeof(ICollection<>) ||
				genericDef == typeof(IEnumerable<>))
			{
				return (propertyType.GetGenericArguments()[0], true);
			}
		}

		if (propertyType.IsArray)
		{
			return (propertyType.GetElementType()!, true);
		}

		return (propertyType, false);
	}

	#endregion
}

/// <summary>
/// Represents cached metadata for a multi-query list entity type (for QueryMultipleList).
/// </summary>
internal sealed class MultiQueryListMetadata
{
	/// <summary>
	/// Gets the list of result set mappings for child collections.
	/// </summary>
	public IReadOnlyList<ResultSetMapping> ChildMappings { get; }

	/// <summary>
	/// Gets the parent key property info for mapping children.
	/// </summary>
	public PropertyInfo ParentKeyProperty { get; }

	private MultiQueryListMetadata(IReadOnlyList<ResultSetMapping> childMappings, PropertyInfo parentKeyProperty)
	{
		ChildMappings = childMappings;
		ParentKeyProperty = parentKeyProperty;
	}

	#region <=== Static Cache ===>

	private static readonly ConcurrentDictionary<Type, MultiQueryListMetadata> _cache = new();

	/// <summary>
	/// Gets or creates cached metadata for the specified entity type with child mappings.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <returns>The cached metadata for the type.</returns>
	public static MultiQueryListMetadata GetOrCreate<T>() where T : class
	{
		return _cache.GetOrAdd(typeof(T), CreateMetadata);
	}

	private static MultiQueryListMetadata CreateMetadata(Type type)
	{
		var mappings = new List<ResultSetMapping>();
		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		PropertyInfo? parentKeyProperty = null;
		string? firstParentKey = null;

		foreach (var prop in properties)
		{
			var resultSetAttr = prop.GetCustomAttribute<ResultSetAttribute>();
			if (resultSetAttr == null)
				continue;

			if (string.IsNullOrEmpty(resultSetAttr.ForeignKey))
			{
				throw new InvalidOperationException(
					$"Property '{prop.Name}' in type '{type.Name}' must specify ForeignKey " +
					"in [ResultSet] attribute for QueryMultipleList.");
			}

			var (elementType, isCollection) = GetElementTypeAndCollectionInfo(prop.PropertyType);

			if (!isCollection)
			{
				throw new InvalidOperationException(
					$"Property '{prop.Name}' in type '{type.Name}' must be a collection type " +
					"for QueryMultipleList child mappings.");
			}

			mappings.Add(new ResultSetMapping(
				resultSetAttr.Index,
				prop,
				elementType,
				isCollection,
				resultSetAttr.ForeignKey,
				resultSetAttr.ParentKey));

			if (firstParentKey == null)
			{
				firstParentKey = resultSetAttr.ParentKey;
				parentKeyProperty = type.GetProperty(
					firstParentKey,
					BindingFlags.Public | BindingFlags.Instance);

				if (parentKeyProperty == null)
				{
					throw new InvalidOperationException(
						$"Parent key property '{firstParentKey}' not found in type '{type.Name}'.");
				}
			}
		}

		if (mappings.Count == 0)
		{
			throw new InvalidOperationException(
				$"Type '{type.Name}' must have at least one property marked with " +
				"[ResultSet(ForeignKey = \"...\")] attribute for QueryMultipleList.");
		}

		return new MultiQueryListMetadata(mappings.OrderBy(m => m.Index).ToList(), parentKeyProperty!);
	}

	private static (Type elementType, bool isCollection) GetElementTypeAndCollectionInfo(Type propertyType)
	{
		if (propertyType.IsGenericType)
		{
			var genericDef = propertyType.GetGenericTypeDefinition();

			if (genericDef == typeof(List<>) ||
				genericDef == typeof(IList<>) ||
				genericDef == typeof(ICollection<>) ||
				genericDef == typeof(IEnumerable<>))
			{
				return (propertyType.GetGenericArguments()[0], true);
			}
		}

		if (propertyType.IsArray)
		{
			return (propertyType.GetElementType()!, true);
		}

		return (propertyType, false);
	}

	#endregion
}

/// <summary>
/// Extension methods for multi-query operations.
/// </summary>
internal static class MultiQueryExtensions
{
	/// <summary>
	/// Converts an enumerable to a typed List.
	/// </summary>
	public static object ToListOfType(this IEnumerable<dynamic> source, Type elementType)
	{
		var listType = typeof(List<>).MakeGenericType(elementType);
		var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

		foreach (var item in source)
		{
			list.Add(item);
		}

		return list;
	}

	/// <summary>
	/// Creates an empty list of the specified element type.
	/// </summary>
	public static object CreateEmptyList(Type elementType)
	{
		var listType = typeof(List<>).MakeGenericType(elementType);
		return Activator.CreateInstance(listType)!;
	}
}
