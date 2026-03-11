namespace WebVella.Database;

#region <=== DateOnly Type Handlers ===>

/// <summary>
/// Dapper type handler for <see cref="DateOnly"/>.
/// Handles conversion between .NET DateOnly and PostgreSQL DATE type.
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
	/// <inheritdoc/>
	public override DateOnly Parse(object value) => value switch
	{
		DateOnly d => d,
		DateTime dt => DateOnly.FromDateTime(dt),
		_ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateOnly")
	};

	/// <inheritdoc/>
	public override void SetValue(IDbDataParameter parameter, DateOnly value)
	{
		parameter.DbType = DbType.Date;
		parameter.Value = value;
	}
}

/// <summary>
/// Dapper type handler for nullable <see cref="DateOnly"/>.
/// </summary>
public class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
	/// <inheritdoc/>
	public override DateOnly? Parse(object value) => value switch
	{
		null or DBNull => null,
		DateOnly d => d,
		DateTime dt => DateOnly.FromDateTime(dt),
		_ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateOnly?")
	};

	/// <inheritdoc/>
	public override void SetValue(IDbDataParameter parameter, DateOnly? value)
	{
		parameter.DbType = DbType.Date;
		parameter.Value = value ?? (object)DBNull.Value;
	}
}

#endregion

#region <=== DateTimeOffset Type Handlers ===>

/// <summary>
/// Dapper type handler for <see cref="DateTimeOffset"/>.
/// Handles conversion between .NET DateTimeOffset and PostgreSQL TIMESTAMPTZ type.
/// Values are converted to UTC for storage.
/// </summary>
public class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
	/// <inheritdoc/>
	public override DateTimeOffset Parse(object value) => value switch
	{
		DateTimeOffset dto => dto,
		DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
		_ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset")
	};

	/// <inheritdoc/>
	public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
	{
		parameter.DbType = DbType.DateTimeOffset;
		parameter.Value = value.UtcDateTime;
	}
}

/// <summary>
/// Dapper type handler for nullable <see cref="DateTimeOffset"/>.
/// </summary>
public class NullableDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset?>
{
	/// <inheritdoc/>
	public override DateTimeOffset? Parse(object value) => value switch
	{
		null or DBNull => null,
		DateTimeOffset dto => dto,
		DateTime dt => new DateTimeOffset(dt, TimeSpan.Zero),
		_ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateTimeOffset?")
	};

	/// <inheritdoc/>
	public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
	{
		parameter.DbType = DbType.DateTimeOffset;
		parameter.Value = value?.UtcDateTime ?? (object)DBNull.Value;
	}
}

#endregion
