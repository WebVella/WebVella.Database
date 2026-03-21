namespace WebVella.Database;

/// <summary>
/// Translates a LINQ <see cref="Expression{TDelegate}"/> predicate into a parameterized
/// PostgreSQL WHERE clause fragment.
/// </summary>
/// <remarks>
/// Supported expression patterns:
/// <list type="bullet">
/// <item>Equality / inequality: <c>e.Status == x</c>, <c>e.Name != null</c></item>
/// <item>Comparisons: <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c></item>
/// <item>Logical: <c>&amp;&amp;</c>, <c>||</c>, <c>!</c></item>
/// <item>Null checks: <c>e.Prop == null</c> → <c>IS NULL</c></item>
/// <item>Boolean member: <c>e.IsActive</c> → <c>is_active = true</c></item>
/// <item>String methods: <c>.Contains()</c>, <c>.StartsWith()</c>, <c>.EndsWith()</c></item>
/// <item>Case-insensitive: <c>e.Name.ILikeContains("x")</c> → <c>name ILIKE '%x%'</c></item>
/// <item>Case-folding: <c>e.Name.ToLower() == "x"</c> → <c>LOWER(name) = @p</c></item>
/// <item>Case-folding: <c>e.Name.ToUpper() == "X"</c> → <c>UPPER(name) = @p</c></item>
/// <item>Collection membership: <c>list.Contains(e.Id)</c> → <c>id = ANY(@p)</c></item>
/// <item>Captured variables and closures</item>
/// </list>
/// Enum values are automatically converted to their underlying <c>int</c> value.
/// LIKE patterns escape <c>%</c>, <c>_</c>, and <c>\</c> in the literal portion.
/// </remarks>
/// <typeparam name="T">The entity type being queried.</typeparam>
internal sealed class DbExpressionTranslator<T> where T : class
{
	private readonly EntityMetadata _metadata;
	private readonly string? _tableAlias;
	private readonly Dictionary<string, object?> _parameters = [];
	private int _paramCount;

	internal DbExpressionTranslator(
		EntityMetadata metadata, string? tableAlias = null)
	{
		_metadata = metadata;
		_tableAlias = tableAlias;
	}

	private string Col(string propertyName)
	{
		var col = _metadata.GetColumnName(propertyName);
		return _tableAlias is not null
			? $"{_tableAlias}.{col}" : col;
	}

	/// <summary>
	/// Translates the body of the given predicate expression into a SQL fragment and
	/// accumulates the produced parameters into the shared parameter bag.
	/// </summary>
	internal string Translate(Expression<Func<T, bool>> expression)
		=> VisitBoolean(expression.Body);

	/// <summary>
	/// Returns all accumulated query parameters as a Dapper
	/// <see cref="DynamicParameters"/> instance.
	/// </summary>
	internal DynamicParameters GetParameters()
	{
		var dp = new DynamicParameters();
		foreach (var (key, value) in _parameters)
			dp.Add(key, value);
		return dp;
	}

	#region <=== Visitors ===>

	/// <summary>
	/// Entry point for any expression that must produce a boolean predicate.
	/// Handles bare boolean members (<c>e.IsActive</c>) and their negation
	/// (<c>!e.IsActive</c>) before falling through to the general visitor.
	/// </summary>
	private string VisitBoolean(Expression expr)
	{
		// e.IsActive → "is_active = @p0" (true)
		if (expr is MemberExpression boolMem
			&& boolMem.Expression is ParameterExpression
			&& IsBoolean(boolMem.Type))
		{
			return $"{Col(boolMem.Member.Name)} = {Param(true)}";
		}

		// !e.IsActive → "is_active = @p0" (false)
		if (expr is UnaryExpression { NodeType: ExpressionType.Not } notExpr
			&& notExpr.Operand is MemberExpression boolMem2
			&& boolMem2.Expression is ParameterExpression
			&& IsBoolean(boolMem2.Type))
		{
			return $"{Col(boolMem2.Member.Name)} = {Param(false)}";
		}

		return Visit(expr);
	}

	private string Visit(Expression expr) => expr switch
	{
		BinaryExpression bin    => VisitBinary(bin),
		MemberExpression mem    => VisitMember(mem),
		UnaryExpression  un     => VisitUnary(un),
		MethodCallExpression mc => VisitMethod(mc),
		ConstantExpression con  => Param(Normalize(con.Value)),
		_ => throw new NotSupportedException(
			$"Expression node type '{expr.NodeType}' is not supported in query predicates.")
	};

	private string VisitBinary(BinaryExpression expr)
	{
		// Null comparisons → IS NULL / IS NOT NULL
		if (IsNullExpr(expr.Right))
		{
			return expr.NodeType == ExpressionType.Equal
				? $"{Visit(expr.Left)} IS NULL"
				: $"{Visit(expr.Left)} IS NOT NULL";
		}

		if (IsNullExpr(expr.Left))
		{
			return expr.NodeType == ExpressionType.Equal
				? $"{Visit(expr.Right)} IS NULL"
				: $"{Visit(expr.Right)} IS NOT NULL";
		}

		// bool member == true/false literal: e.IsActive == false
		if (expr.NodeType == ExpressionType.Equal
			&& expr.Left is MemberExpression bm
			&& bm.Expression is ParameterExpression
			&& IsBoolean(bm.Type)
			&& expr.Right is ConstantExpression bc)
		{
			return $"{Col(bm.Member.Name)} = {Param(bc.Value)}";
		}

		return expr.NodeType switch
		{
			ExpressionType.AndAlso =>
				$"({VisitBoolean(expr.Left)} AND {VisitBoolean(expr.Right)})",
			ExpressionType.OrElse =>
				$"({VisitBoolean(expr.Left)} OR {VisitBoolean(expr.Right)})",
			ExpressionType.Equal =>
				$"{Visit(expr.Left)} = {Visit(expr.Right)}",
			ExpressionType.NotEqual =>
				$"{Visit(expr.Left)} != {Visit(expr.Right)}",
			ExpressionType.LessThan =>
				$"{Visit(expr.Left)} < {Visit(expr.Right)}",
			ExpressionType.LessThanOrEqual =>
				$"{Visit(expr.Left)} <= {Visit(expr.Right)}",
			ExpressionType.GreaterThan =>
				$"{Visit(expr.Left)} > {Visit(expr.Right)}",
			ExpressionType.GreaterThanOrEqual =>
				$"{Visit(expr.Left)} >= {Visit(expr.Right)}",
			_ => throw new NotSupportedException(
				$"Binary operator '{expr.NodeType}' is not supported.")
		};
	}

	private string VisitMember(MemberExpression expr)
	{
		// Property on the entity parameter → return column name
		if (expr.Expression is ParameterExpression)
			return Col(expr.Member.Name);

		// Captured local variable / closure field → evaluate and parameterise
		return Param(Normalize(Expression.Lambda(expr).Compile().DynamicInvoke()));
	}

	private string VisitUnary(UnaryExpression expr)
	{
		// (T)x casts produced by the compiler for enum/value comparisons
		if (expr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
			return Visit(expr.Operand);

		if (expr.NodeType == ExpressionType.Not)
			return $"NOT ({VisitBoolean(expr.Operand)})";

		throw new NotSupportedException(
			$"Unary operator '{expr.NodeType}' is not supported.");
	}

	private string VisitMethod(MethodCallExpression expr)
	{
		// Instance string methods on an entity property:
		// e.Name.ToLower() / e.Name.ToLowerInvariant() → LOWER(col)
		// e.Name.Contains("x") / .StartsWith("x") / .EndsWith("x")
		if (expr.Object is MemberExpression { Expression: ParameterExpression } strProp)
		{
			var col = Col(strProp.Member.Name);

			if (expr.Method.Name is "ToLower" or "ToLowerInvariant")
				return $"LOWER({col})";

			if (expr.Method.Name is "ToUpper" or "ToUpperInvariant")
				return $"UPPER({col})";

			var arg = Evaluate(expr.Arguments[0])?.ToString() ?? string.Empty;

			return expr.Method.Name switch
			{
				"Contains"   => $"{col} LIKE {Param($"%{EscapeLike(arg)}%")}",
				"StartsWith" => $"{col} LIKE {Param($"{EscapeLike(arg)}%")}",
				"EndsWith"   => $"{col} LIKE {Param($"%{EscapeLike(arg)}")}",
				_ => throw new NotSupportedException(
					$"String method '{expr.Method.Name}' is not supported.")
			};
		}

		// Collection membership → PostgreSQL ANY:
		// Enumerable.Contains(list, e.Prop)  or  list.Contains(e.Prop)
		if (expr.Method.Name == "Contains")
		{
			// Static: Enumerable.Contains(source, e.Prop)
			if (expr.Method.IsStatic && expr.Arguments.Count == 2
				&& expr.Arguments[1] is MemberExpression
					{ Expression: ParameterExpression } sProp)
			{
				var col = Col(sProp.Member.Name);
				var collection = Evaluate(expr.Arguments[0]);
				var arr = ToTypedArray(collection);
				if (arr.Length == 0) return "1 = 0";
				return $"{col} = ANY({Param(arr)})";
			}

			// Instance: list.Contains(e.Prop)
			if (!expr.Method.IsStatic && expr.Arguments.Count == 1
				&& expr.Arguments[0] is MemberExpression
					{ Expression: ParameterExpression } iProp
				&& expr.Object != null)
			{
				var col = Col(iProp.Member.Name);
				var collection = Evaluate(expr.Object);
				var arr = ToTypedArray(collection);
					if (arr.Length == 0) return "1 = 0";
						return $"{col} = ANY({Param(arr)})";
					}
				}

				// ILike extension methods: e.Name.ILikeContains("x") / .ILikeStartsWith("x") / .ILikeEndsWith("x")
				if (expr.Method.DeclaringType == typeof(DbStringExtensions)
					&& expr.Method.IsStatic && expr.Arguments.Count == 2
					&& expr.Arguments[0] is MemberExpression
						{ Expression: ParameterExpression } ilikeProp)
				{
					var col = Col(ilikeProp.Member.Name);
					var arg = Evaluate(expr.Arguments[1])?.ToString() ?? string.Empty;

					return expr.Method.Name switch
					{
						"ILikeContains"   => $"{col} ILIKE {Param($"%{EscapeLike(arg)}%")}",
						"ILikeStartsWith" => $"{col} ILIKE {Param($"{EscapeLike(arg)}%")}",
						"ILikeEndsWith"   => $"{col} ILIKE {Param($"%{EscapeLike(arg)}")}",
						_ => throw new NotSupportedException(
							$"DbStringExtensions method '{expr.Method.Name}' is not supported.")
					};
				}

				throw new NotSupportedException(
					$"Method '{expr.Method.DeclaringType?.Name}.{expr.Method.Name}' is not " +
					"supported in query predicates.");
	}

	#endregion

	#region <=== Helpers ===>

	private string Param(object? value)
	{
		var name = $"p{_paramCount++}";
		_parameters[name] = value;
		return "@" + name;
	}

	private object? Evaluate(Expression expr)
		=> Normalize(Expression.Lambda(expr).Compile().DynamicInvoke());

	/// <summary>
	/// Converts enum values to their underlying <c>int</c> so Npgsql stores them
	/// as integers rather than attempting to map to a PostgreSQL enum type.
	/// </summary>
	private static object? Normalize(object? value)
		=> value is not null && value.GetType().IsEnum
			? Convert.ToInt32(value)
			: value;

	private static bool IsBoolean(Type t)
		=> t == typeof(bool) || t == typeof(bool?);

	private static bool IsNullExpr(Expression expr)
		=> expr is ConstantExpression { Value: null }
		or UnaryExpression
		{
			NodeType: ExpressionType.Convert,
			Operand: ConstantExpression { Value: null }
		};

	/// <summary>
	/// Escapes PostgreSQL LIKE special characters in a literal string.
	/// </summary>
	private static string EscapeLike(string value)
		=> value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

	/// <summary>
	/// Converts a collection to a typed array suitable for the PostgreSQL
	/// <c>= ANY(@param)</c> operator.
	/// </summary>
	private static Array ToTypedArray(object? collection)
	{
		if (collection is null)
			return Array.Empty<object>();

		if (collection is System.Collections.IEnumerable items)
		{
			var list = items.Cast<object?>().Select(Normalize).ToList();

			if (list.Count == 0)
				return Array.Empty<object>();

			var elementType = list.FirstOrDefault(x => x != null)?.GetType()
				?? typeof(object);

			var array = Array.CreateInstance(elementType, list.Count);
			for (var i = 0; i < list.Count; i++)
				array.SetValue(list[i], i);
			return array;
		}

		throw new ArgumentException(
			$"Expected a collection but got '{collection.GetType().Name}'.",
			nameof(collection));
	}

	#endregion
}
