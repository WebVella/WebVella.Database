using FluentAssertions;
using Xunit;
using WebVella.Database.Tests.Models;

namespace WebVella.Database.Tests;

/// <summary>
/// Unit tests for <see cref="DbColumnAttribute"/> and its effect on
/// <see cref="EntityMetadata"/> SQL generation.
/// </summary>
public class DbColumnAttributeTests
{
	#region <=== Attribute Property Tests ===>

	[Fact]
	public void DbColumnAttribute_ShouldStoreProvidedName()
	{
		var attr = new DbColumnAttribute("custom_column");

		attr.Name.Should().Be("custom_column");
	}

	[Theory]
	[InlineData("id")]
	[InlineData("my_custom_col")]
	[InlineData("UPPER_CASE")]
	public void DbColumnAttribute_ShouldAcceptVariousNames(string name)
	{
		var attr = new DbColumnAttribute(name);

		attr.Name.Should().Be(name);
	}

	#endregion

	#region <=== SelectColumns Tests ===>

	[Fact]
	public void SelectColumns_ShouldUseDbColumnName_WhenAttributeIsPresent()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.SelectColumns.Should().Contain("entity_id AS \"Id\"");
		metadata.SelectColumns.Should().Contain("full_name AS \"DisplayName\"");
		metadata.SelectColumns.Should().Contain("email_address AS \"Email\"");
	}

	[Fact]
	public void SelectColumns_ShouldFallBackToSnakeCase_WhenAttributeIsMissing()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.SelectColumns.Should().Contain("description AS \"Description\"");
	}

	[Fact]
	public void SelectColumns_ShouldUseSnakeCase_WhenNoDbColumnAttributes()
	{
		var metadata = EntityMetadata.GetOrCreate<TestNoDbColumnEntity>();

		metadata.SelectColumns.Should().Contain("id AS \"Id\"");
		metadata.SelectColumns.Should().Contain("display_name AS \"DisplayName\"");
	}

	#endregion

	#region <=== InsertColumns Tests ===>

	[Fact]
	public void InsertColumns_ShouldUseDbColumnName_WhenAttributeIsPresent()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.InsertColumns.Should().Contain("full_name");
		metadata.InsertColumns.Should().Contain("email_address");
	}

	[Fact]
	public void InsertColumns_ShouldNotContainSnakeCaseForMappedProperties()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.InsertColumns.Should().NotContain("display_name");
		metadata.InsertColumns.Should().NotContain(", email,");
		metadata.InsertColumns.Should().NotEndWith(", email");
	}

	[Fact]
	public void InsertColumns_ShouldFallBackToSnakeCase_ForUnmappedProperties()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.InsertColumns.Should().Contain("description");
	}

	#endregion

	#region <=== InsertParameters Tests ===>

	[Fact]
	public void InsertParameters_ShouldUsePropertyNames_RegardlessOfDbColumn()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.InsertParameters.Should().Contain("@DisplayName");
		metadata.InsertParameters.Should().Contain("@Email");
		metadata.InsertParameters.Should().Contain("@Description");
	}

	#endregion

	#region <=== KeyWhereClause Tests ===>

	[Fact]
	public void KeyWhereClause_ShouldUseDbColumnName_WhenKeyHasAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.KeyWhereClause.Should().Be("entity_id = @Id");
	}

	[Fact]
	public void KeyWhereClause_ShouldUseSnakeCase_WhenKeyHasNoAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestNoDbColumnEntity>();

		metadata.KeyWhereClause.Should().Be("id = @Id");
	}

	#endregion

	#region <=== UpdateSetClause Tests ===>

	[Fact]
	public void UpdateSetClause_ShouldUseDbColumnName_WhenAttributeIsPresent()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.UpdateSetClause.Should().Contain("full_name = @DisplayName");
		metadata.UpdateSetClause.Should().Contain("email_address = @Email");
	}

	[Fact]
	public void UpdateSetClause_ShouldFallBackToSnakeCase_ForUnmappedProperties()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.UpdateSetClause.Should().Contain("description = @Description");
	}

	#endregion

	#region <=== ReturningColumns Tests ===>

	[Fact]
	public void ReturningColumns_ShouldUseDbColumnName_WhenKeyHasAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.ReturningColumns.Should().Be("entity_id");
	}

	[Fact]
	public void ReturningColumns_ShouldUseSnakeCase_WhenKeyHasNoAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestNoDbColumnEntity>();

		metadata.ReturningColumns.Should().Be("id");
	}

	#endregion

	#region <=== FirstKeyColumnName Tests ===>

	[Fact]
	public void FirstKeyColumnName_ShouldUseDbColumnName_WhenKeyHasAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.FirstKeyColumnName.Should().Be("entity_id");
	}

	[Fact]
	public void FirstKeyColumnName_ShouldUseSnakeCase_WhenKeyHasNoAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestNoDbColumnEntity>();

		metadata.FirstKeyColumnName.Should().Be("id");
	}

	#endregion

	#region <=== KeyPropertyColumnNames Tests ===>

	[Fact]
	public void KeyPropertyColumnNames_ShouldUseDbColumnName_WhenKeyHasAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();

		metadata.KeyPropertyColumnNames.Should().ContainKey("Id");
		metadata.KeyPropertyColumnNames["Id"].Should().Be("entity_id");
	}

	#endregion

	#region <=== ExplicitKey With DbColumn Tests ===>

	[Fact]
	public void ExplicitKey_WithDbColumn_ShouldUseCustomColumnName()
	{
		var metadata = EntityMetadata.GetOrCreate<TestExplicitKeyDbColumnEntity>();

		metadata.FirstKeyColumnName.Should().Be("custom_id");
		metadata.KeyWhereClause.Should().Be("custom_id = @Id");
		metadata.InsertColumns.Should().Contain("custom_id");
	}

	[Fact]
	public void ExplicitKey_WithDbColumn_ShouldUseCustomColumnInUpdateSet()
	{
		var metadata = EntityMetadata.GetOrCreate<TestExplicitKeyDbColumnEntity>();

		metadata.UpdateSetClause.Should().Contain("col_value = @Value");
	}

	#endregion

	#region <=== BuildUpdateSetClause Tests ===>

	[Fact]
	public void BuildUpdateSetClause_ShouldUseDbColumnName_ForSelectedProperties()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();
		var emailProperty = typeof(TestDbColumnEntity).GetProperty("Email")!;

		var result = metadata.BuildUpdateSetClause([emailProperty]);

		result.Should().Be("email_address = @Email");
	}

	[Fact]
	public void BuildUpdateSetClause_ShouldFallBackToSnakeCase_WhenNoAttribute()
	{
		var metadata = EntityMetadata.GetOrCreate<TestDbColumnEntity>();
		var descriptionProperty = typeof(TestDbColumnEntity).GetProperty("Description")!;

		var result = metadata.BuildUpdateSetClause([descriptionProperty]);

		result.Should().Be("description = @Description");
	}

	#endregion
}
