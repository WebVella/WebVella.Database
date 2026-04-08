using System.Reflection;
using Dapper;

namespace WebVella.Database;

/// <summary>
/// Custom Dapper column mapper that recognizes [DbColumn] attributes.
/// Allows raw SQL queries to map columns without requiring aliases.
/// </summary>
internal class DapperColumnMapper : SqlMapper.ITypeMap
{
	private readonly Type _type;

	public DapperColumnMapper(Type type)
	{
		_type = type;
	}

	public ConstructorInfo? FindConstructor(string[] names, Type[] types)
	{
		return _type.GetConstructors().FirstOrDefault();
	}

	public ConstructorInfo? FindExplicitConstructor()
	{
		return _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
			.OrderByDescending(c => c.GetParameters().Length)
			.FirstOrDefault();
	}

	public SqlMapper.IMemberMap? GetConstructorParameter(
		ConstructorInfo constructor, string columnName)
	{
		var parameters = constructor.GetParameters();
		var parameter = parameters.FirstOrDefault(p =>
			string.Equals(p.Name, columnName, StringComparison.OrdinalIgnoreCase));

		return parameter != null ? new ParameterMemberMap(columnName, parameter) : null;
	}

	public SqlMapper.IMemberMap? GetMember(string columnName)
	{
		var properties = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		foreach (var property in properties)
		{
			var dbColumnAttr = property.GetCustomAttribute<DbColumnAttribute>();
			if (dbColumnAttr != null)
			{
				if (string.Equals(dbColumnAttr.Name, columnName, StringComparison.OrdinalIgnoreCase))
				{
					return new SimpleMemberMap(columnName, property);
				}
			}

			var snakeCaseName = ToSnakeCase(property.Name);
			if (string.Equals(snakeCaseName, columnName, StringComparison.OrdinalIgnoreCase))
			{
				return new SimpleMemberMap(columnName, property);
			}

			if (string.Equals(property.Name, columnName, StringComparison.OrdinalIgnoreCase))
			{
				return new SimpleMemberMap(columnName, property);
			}
		}

		return null;
	}

	private static string ToSnakeCase(string text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

		var result = new System.Text.StringBuilder();
		result.Append(char.ToLowerInvariant(text[0]));

		for (int i = 1; i < text.Length; i++)
		{
			if (char.IsUpper(text[i]))
			{
				result.Append('_');
				result.Append(char.ToLowerInvariant(text[i]));
			}
			else
			{
				result.Append(text[i]);
			}
		}

		return result.ToString();
	}

	private class SimpleMemberMap : SqlMapper.IMemberMap
	{
		private readonly string _columnName;
		private readonly PropertyInfo _property;

		public SimpleMemberMap(string columnName, PropertyInfo property)
		{
			_columnName = columnName;
			_property = property;
		}

		public string ColumnName => _columnName;
		public Type MemberType => _property.PropertyType;
		public PropertyInfo? Property => _property;
		public ParameterInfo? Parameter => null;
		public FieldInfo? Field => null;
	}

	private class ParameterMemberMap : SqlMapper.IMemberMap
	{
		private readonly string _columnName;
		private readonly ParameterInfo _parameter;

		public ParameterMemberMap(string columnName, ParameterInfo parameter)
		{
			_columnName = columnName;
			_parameter = parameter;
		}

		public string ColumnName => _columnName;
		public Type MemberType => _parameter.ParameterType;
		public PropertyInfo? Property => null;
		public ParameterInfo? Parameter => _parameter;
		public FieldInfo? Field => null;
	}
}

/// <summary>
/// Provides extension methods for registering Dapper column mappings.
/// </summary>
public static class DapperColumnMapperExtensions
{
	private static readonly HashSet<Type> _registeredTypes = new();
	private static readonly object _lock = new();

	/// <summary>
	/// Registers a custom Dapper type map for the specified entity type
	/// that recognizes [DbColumn] attributes.
	/// </summary>
	/// <typeparam name="T">The entity type to register.</typeparam>
	public static void RegisterDapperMapping<T>()
	{
		RegisterDapperMapping(typeof(T));
	}

	/// <summary>
	/// Registers a custom Dapper type map for the specified entity type
	/// that recognizes [DbColumn] attributes.
	/// </summary>
	/// <param name="type">The entity type to register.</param>
	public static void RegisterDapperMapping(Type type)
	{
		lock (_lock)
		{
			if (_registeredTypes.Add(type))
			{
				var mapper = new DapperColumnMapper(type);
				SqlMapper.SetTypeMap(type, mapper);
			}
		}
	}

	/// <summary>
	/// Scans an assembly for all entity types and registers Dapper mappings for them.
	/// </summary>
	/// <param name="assembly">The assembly to scan.</param>
	public static void RegisterDapperMappingsFromAssembly(Assembly assembly)
	{
		var entityTypes = assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && t.GetCustomAttribute<TableAttribute>() != null);

		foreach (var entityType in entityTypes)
		{
			RegisterDapperMapping(entityType);
		}
	}
}
