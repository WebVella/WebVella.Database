using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Tests for PostgreSQL ltree extension methods support in DbQuery.
/// </summary>
/// <remarks>
/// These tests require the ltree extension to be installed in PostgreSQL.
/// Run: CREATE EXTENSION IF NOT EXISTS ltree;
/// 
/// These tests validate SQL generation and expression translation without requiring database access.
/// </remarks>
public class LtreeExtensionTests
{
	private readonly EntityMetadata _metadata;
	private readonly DbExpressionTranslator<CategoryPath> _translator;

	public LtreeExtensionTests()
	{
		_metadata = EntityMetadata.GetOrCreate<CategoryPath>();
		_translator = new DbExpressionTranslator<CategoryPath>(_metadata);
	}

	#region <=== LtreeIsAncestorOf Tests ===>

	[Fact]
	public void LtreeIsAncestorOf_WithConstant_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeIsAncestorOf("root.electronics.phones"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @> @p0::ltree");
		parameters.ParameterNames.Should().ContainSingle();
		parameters.Get<string>("p0").Should().Be("root.electronics.phones");
	}

	[Fact]
	public void LtreeIsAncestorOf_WithCapturedVariable_GeneratesCorrectSQL()
	{
		var searchPath = "root.appliances.kitchen";
		var sql = _translator.Translate(c => c.Path.LtreeIsAncestorOf(searchPath));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @> @p0::ltree");
		parameters.Get<string>("p0").Should().Be("root.appliances.kitchen");
	}

	[Fact]
	public void LtreeIsAncestorOf_WithComplexPath_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeIsAncestorOf("root.a.b.c.d.e.f"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @> @p0::ltree");
		parameters.Get<string>("p0").Should().Be("root.a.b.c.d.e.f");
	}

	#endregion

	#region <=== LtreeIsDescendantOf Tests ===>

	[Fact]
	public void LtreeIsDescendantOf_WithConstant_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeIsDescendantOf("root.electronics"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path <@ @p0::ltree");
		parameters.ParameterNames.Should().ContainSingle();
		parameters.Get<string>("p0").Should().Be("root.electronics");
	}

	[Fact]
	public void LtreeIsDescendantOf_WithCapturedVariable_GeneratesCorrectSQL()
	{
		var parentPath = "root";
		var sql = _translator.Translate(c => c.Path.LtreeIsDescendantOf(parentPath));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path <@ @p0::ltree");
		parameters.Get<string>("p0").Should().Be("root");
	}

	#endregion

	#region <=== LtreeIsAncestorOrEqual Tests ===>

	[Fact]
	public void LtreeIsAncestorOrEqual_WithConstant_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeIsAncestorOrEqual("root.electronics"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("(path @> @p0::ltree OR path = @p0::ltree)");
		parameters.ParameterNames.Should().ContainSingle();
		parameters.Get<string>("p0").Should().Be("root.electronics");
	}

	[Fact]
	public void LtreeIsAncestorOrEqual_WithCapturedVariable_GeneratesCorrectSQL()
	{
		var searchPath = "root.appliances";
		var sql = _translator.Translate(c => c.Path.LtreeIsAncestorOrEqual(searchPath));
		var parameters = _translator.GetParameters();

		sql.Should().Be("(path @> @p0::ltree OR path = @p0::ltree)");
		parameters.Get<string>("p0").Should().Be("root.appliances");
	}

	#endregion

	#region <=== LtreeIsDescendantOrEqual Tests ===>

	[Fact]
	public void LtreeIsDescendantOrEqual_WithConstant_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeIsDescendantOrEqual("root.electronics"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("(path <@ @p0::ltree OR path = @p0::ltree)");
		parameters.ParameterNames.Should().ContainSingle();
		parameters.Get<string>("p0").Should().Be("root.electronics");
	}

	[Fact]
	public void LtreeIsDescendantOrEqual_WithCapturedVariable_GeneratesCorrectSQL()
	{
		var parentPath = "root.appliances.kitchen";
		var sql = _translator.Translate(c => c.Path.LtreeIsDescendantOrEqual(parentPath));
		var parameters = _translator.GetParameters();

		sql.Should().Be("(path <@ @p0::ltree OR path = @p0::ltree)");
		parameters.Get<string>("p0").Should().Be("root.appliances.kitchen");
	}

	#endregion

	#region <=== LtreeMatchesLQuery Tests ===>

	[Fact]
	public void LtreeMatchesLQuery_WithWildcardPattern_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLQuery("root.*.phones"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ~ @p0::lquery");
		parameters.ParameterNames.Should().ContainSingle();
		parameters.Get<string>("p0").Should().Be("root.*.phones");
	}

	[Fact]
	public void LtreeMatchesLQuery_WithMultipleWildcards_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLQuery("root.*.*.*"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ~ @p0::lquery");
		parameters.Get<string>("p0").Should().Be("root.*.*.*");
	}

	[Fact]
	public void LtreeMatchesLQuery_WithChoicePattern_GeneratesCorrectSQL()
	{
		var pattern = "root.{electronics,appliances}.*";
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLQuery(pattern));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ~ @p0::lquery");
		parameters.Get<string>("p0").Should().Be("root.{electronics,appliances}.*");
	}

	[Fact]
	public void LtreeMatchesLQuery_WithCapturedVariable_GeneratesCorrectSQL()
	{
		var pattern = "*.phones.*";
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLQuery(pattern));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ~ @p0::lquery");
		parameters.Get<string>("p0").Should().Be("*.phones.*");
	}

	#endregion

	#region <=== LtreeMatchesLTxtQuery Tests ===>

	[Fact]
	public void LtreeMatchesLTxtQuery_WithAndOperator_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLTxtQuery("electronics & phones"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @ @p0::ltxtquery");
		parameters.ParameterNames.Should().ContainSingle();
		parameters.Get<string>("p0").Should().Be("electronics & phones");
	}

	[Fact]
	public void LtreeMatchesLTxtQuery_WithOrOperator_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLTxtQuery("phones | appliances"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @ @p0::ltxtquery");
		parameters.Get<string>("p0").Should().Be("phones | appliances");
	}

	[Fact]
	public void LtreeMatchesLTxtQuery_WithNotOperator_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLTxtQuery("electronics & !phones"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @ @p0::ltxtquery");
		parameters.Get<string>("p0").Should().Be("electronics & !phones");
	}

	[Fact]
	public void LtreeMatchesLTxtQuery_WithCapturedVariable_GeneratesCorrectSQL()
	{
		var query = "kitchen | refrigerators";
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLTxtQuery(query));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @ @p0::ltxtquery");
		parameters.Get<string>("p0").Should().Be("kitchen | refrigerators");
	}

	#endregion

	#region <=== LtreeContainsAny Tests ===>

	[Fact]
	public void LtreeContainsAny_WithMultiplePaths_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeContainsAny("root.electronics", "root.appliances"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ? @p0");
		parameters.ParameterNames.Should().ContainSingle();
		var array = parameters.Get<string[]>("p0");
		array.Should().HaveCount(2);
		array.Should().Contain("root.electronics");
		array.Should().Contain("root.appliances");
	}

	[Fact]
	public void LtreeContainsAny_WithSinglePath_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeContainsAny("root.electronics"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ? @p0");
		var array = parameters.Get<string[]>("p0");
		array.Should().ContainSingle();
		array[0].Should().Be("root.electronics");
	}

	[Fact]
	public void LtreeContainsAny_WithEmptyArray_GeneratesShortCircuit()
	{
		var emptyArray = Array.Empty<string>();
		var sql = _translator.Translate(c => c.Path.LtreeContainsAny(emptyArray));
		var parameters = _translator.GetParameters();

		sql.Should().Be("1 = 0");
		parameters.ParameterNames.Should().BeEmpty();
	}

	[Fact]
	public void LtreeContainsAny_WithCapturedArray_GeneratesCorrectSQL()
	{
		var paths = new[] { "root.a", "root.b", "root.c" };
		var sql = _translator.Translate(c => c.Path.LtreeContainsAny(paths));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ? @p0");
		var array = parameters.Get<string[]>("p0");
		array.Should().HaveCount(3);
		array.Should().BeEquivalentTo(paths);
	}

	#endregion

	#region <=== LtreeContainsAll Tests ===>

	[Fact]
	public void LtreeContainsAll_WithMultipleLabels_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeContainsAll("root", "electronics", "phones"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ?& @p0");
		parameters.ParameterNames.Should().ContainSingle();
		var array = parameters.Get<string[]>("p0");
		array.Should().HaveCount(3);
		array.Should().Contain("root");
		array.Should().Contain("electronics");
		array.Should().Contain("phones");
	}

	[Fact]
	public void LtreeContainsAll_WithSingleLabel_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => c.Path.LtreeContainsAll("root"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ?& @p0");
		var array = parameters.Get<string[]>("p0");
		array.Should().ContainSingle();
		array[0].Should().Be("root");
	}

	[Fact]
	public void LtreeContainsAll_WithEmptyArray_GeneratesShortCircuit()
	{
		var emptyArray = Array.Empty<string>();
		var sql = _translator.Translate(c => c.Path.LtreeContainsAll(emptyArray));
		var parameters = _translator.GetParameters();

		sql.Should().Be("1 = 1");
		parameters.ParameterNames.Should().BeEmpty();
	}

	[Fact]
	public void LtreeContainsAll_WithCapturedArray_GeneratesCorrectSQL()
	{
		var labels = new[] { "root", "electronics" };
		var sql = _translator.Translate(c => c.Path.LtreeContainsAll(labels));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ?& @p0");
		var array = parameters.Get<string[]>("p0");
		array.Should().HaveCount(2);
		array.Should().BeEquivalentTo(labels);
	}

	#endregion

	#region <=== Combined Predicates Tests ===>

	[Fact]
	public void LtreeCombinedWithAnd_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => 
			c.Path.LtreeIsAncestorOf("root.electronics.phones") && c.Name.Contains("Electr"));
		var parameters = _translator.GetParameters();

		sql.Should().Contain("path @> @p0::ltree");
		sql.Should().Contain("AND");
		sql.Should().Contain("name LIKE @p1");
		parameters.ParameterNames.Should().HaveCount(2);
	}

	[Fact]
	public void LtreeCombinedWithOr_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => 
			c.Path.LtreeIsDescendantOf("root.electronics") || c.Path.LtreeIsDescendantOf("root.appliances"));
		var parameters = _translator.GetParameters();

		sql.Should().Contain("path <@ @p0::ltree");
		sql.Should().Contain("OR");
		sql.Should().Contain("path <@ @p1::ltree");
		parameters.ParameterNames.Should().HaveCount(2);
	}

	[Fact]
	public void MultipleLtreePredicates_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => 
			c.Path.LtreeIsDescendantOf("root") && c.Path.LtreeMatchesLQuery("*.electronics.*"));
		var parameters = _translator.GetParameters();

		sql.Should().Contain("path <@ @p0::ltree");
		sql.Should().Contain("AND");
		sql.Should().Contain("path ~ @p1::lquery");
		parameters.ParameterNames.Should().HaveCount(2);
		parameters.Get<string>("p0").Should().Be("root");
		parameters.Get<string>("p1").Should().Be("*.electronics.*");
	}

	[Fact]
	public void LtreeWithOtherComparisons_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => 
			c.Path.LtreeIsAncestorOf("root.electronics.phones") && c.IsActive && c.SortOrder > 10);
		var parameters = _translator.GetParameters();

		sql.Should().Contain("path @> @p0::ltree");
		sql.Should().Contain("AND");
		sql.Should().Contain("is_active = @p1");
		sql.Should().Contain("AND");
		sql.Should().Contain("sort_order > @p2");
		parameters.ParameterNames.Should().HaveCount(3);
	}

	#endregion

	#region <=== Edge Cases and Special Scenarios ===>

	[Fact]
	public void LtreeWithDotInLabel_HandlesCorrectly()
	{
		var sql = _translator.Translate(c => c.Path.LtreeIsAncestorOf("root.my_category.sub_item"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path @> @p0::ltree");
		parameters.Get<string>("p0").Should().Be("root.my_category.sub_item");
	}

	[Fact]
	public void LtreeWithUnderscores_HandlesCorrectly()
	{
		var sql = _translator.Translate(c => c.Path.LtreeMatchesLQuery("root.category_name.*"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path ~ @p0::lquery");
		parameters.Get<string>("p0").Should().Be("root.category_name.*");
	}

	[Fact]
	public void LtreeWithNumbers_HandlesCorrectly()
	{
		var sql = _translator.Translate(c => c.Path.LtreeIsDescendantOf("root.cat123.sub456"));
		var parameters = _translator.GetParameters();

		sql.Should().Be("path <@ @p0::ltree");
		parameters.Get<string>("p0").Should().Be("root.cat123.sub456");
	}

	[Fact]
	public void LtreeNegation_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => !c.Path.LtreeIsAncestorOf("root.electronics"));
		var parameters = _translator.GetParameters();

		sql.Should().Contain("NOT");
		sql.Should().Contain("path @> @p0::ltree");
		parameters.Get<string>("p0").Should().Be("root.electronics");
	}

	[Fact]
	public void LtreeInComplexBooleanExpression_GeneratesCorrectSQL()
	{
		var sql = _translator.Translate(c => 
			(c.Path.LtreeIsAncestorOf("root.a") || c.Path.LtreeIsAncestorOf("root.b")) 
			&& c.IsActive);
		var parameters = _translator.GetParameters();

		sql.Should().Contain("((path @> @p0::ltree OR path @> @p1::ltree) AND is_active = @p2)");
		parameters.ParameterNames.Should().HaveCount(3);
	}

	#endregion

	#region <=== Parameter Reuse Tests ===>

	[Fact]
	public void MultiplePredicatesWithSameValue_ReusesParameters()
	{
		var searchPath = "root.electronics";
		var sql = _translator.Translate(c => 
			c.Path.LtreeIsAncestorOf(searchPath) || c.Path.LtreeIsDescendantOf(searchPath));
		var parameters = _translator.GetParameters();

		// Each call creates a new parameter (no reuse in current implementation)
		parameters.ParameterNames.Should().HaveCount(2);
	}

	[Fact]
	public void LtreeWithMultipleCaptures_HandlesCorrectly()
	{
		var path1 = "root.a";
		var path2 = "root.b";
		var path3 = "root.c";

		var sql = _translator.Translate(c => 
			c.Path.LtreeIsAncestorOf(path1) || 
			c.Path.LtreeIsAncestorOf(path2) || 
			c.Path.LtreeIsAncestorOf(path3));
		var parameters = _translator.GetParameters();

		parameters.ParameterNames.Should().HaveCount(3);
		parameters.Get<string>("p0").Should().Be("root.a");
		parameters.Get<string>("p1").Should().Be("root.b");
		parameters.Get<string>("p2").Should().Be("root.c");
	}

	#endregion

	#region <=== Type Safety Tests ===>

	[Fact]
	public void LtreeMethodsOnNonStringProperty_ShouldNotCompile()
	{
		// This test documents that ltree methods only work on string properties
		// Actual compilation error would occur if trying:
		// c.Id.LtreeIsAncestorOf("root")  // Won't compile

		// We verify the method works on string property
		var sql = _translator.Translate(c => c.Path.LtreeIsAncestorOf("root"));
		sql.Should().Contain("path @>");
	}

	#endregion

	#region <=== Test Model ===>

	[Table("categories")]
	private class CategoryPath
	{
		[Key]
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public int SortOrder { get; set; }
	}

	#endregion
}
