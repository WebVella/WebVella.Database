using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebVella.Database.Tests;

public class JsonColumnDictionaryStringTests : IAsyncLifetime
{
	private readonly IDbService _db;
	private readonly string _tableName = "test_customfields_json";
	private readonly string _valuesTableName = "test_customfield_values_json";

	public JsonColumnDictionaryStringTests()
	{
		var connectionString = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
			.Build()
			.GetConnectionString("DefaultConnection")
			?? throw new InvalidOperationException("Connection string not found");

		var services = new ServiceCollection();
		services.AddWebVellaDatabase(connectionString, enableCaching: false);
		var serviceProvider = services.BuildServiceProvider();
		_db = serviceProvider.GetRequiredService<IDbService>();
	}

	public async Task InitializeAsync()
	{
		await _db.ExecuteAsync($@"
			CREATE TABLE IF NOT EXISTS {_tableName} (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				name VARCHAR(100) NOT NULL,
				custom_fields_json JSONB NOT NULL DEFAULT '{{}}'
			)");

		await _db.ExecuteAsync($@"
			CREATE TABLE IF NOT EXISTS {_valuesTableName} (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				entity_id UUID NOT NULL,
				field_name VARCHAR(100) NOT NULL,
				field_metadata JSONB NOT NULL DEFAULT '{{}}',
				FOREIGN KEY (entity_id) REFERENCES {_tableName}(id) ON DELETE CASCADE
			)");
	}

	public async Task DisposeAsync()
	{
		await _db.ExecuteAsync($"DROP TABLE IF EXISTS {_valuesTableName}");
		await _db.ExecuteAsync($"DROP TABLE IF EXISTS {_tableName}");
	}

	[Fact]
	public async Task JsonColumn_DictionaryStringString_ShouldSerializeAndDeserializeCorrectly()
	{
		var testEntity = new TestEntityWithCustomFields
		{
			Id = Guid.NewGuid(),
			Name = "Test Entity",
			CustomFields = new Dictionary<string, string>
			{
				{ "cf_location", "София" },
				{ "cf_department", "Разработка" },
				{ "cf_budget", "50000" },
				{ "cf_notes", "Екип от София, България!" },
				{ "cf_quote", "\"Excellence is not a skill, it's an attitude\"" },
				{ "cf_multiline", "Line1\nLine2\tTabbed" }
			}
		};

		await _db.InsertAsync(testEntity);

		var retrievedEntity = await _db.GetAsync<TestEntityWithCustomFields>(testEntity.Id);

		retrievedEntity.Should().NotBeNull();
		retrievedEntity!.Id.Should().Be(testEntity.Id);
		retrievedEntity.Name.Should().Be(testEntity.Name);
		retrievedEntity.CustomFields.Should().NotBeNull();
		retrievedEntity.CustomFields.Should().HaveCount(6);
		retrievedEntity.CustomFields["cf_location"].Should().Be("София");
		retrievedEntity.CustomFields["cf_department"].Should().Be("Разработка");
		retrievedEntity.CustomFields["cf_budget"].Should().Be("50000");
		retrievedEntity.CustomFields["cf_notes"].Should().Be("Екип от София, България!");
		retrievedEntity.CustomFields["cf_quote"]
			.Should().Be("\"Excellence is not a skill, it's an attitude\"");
		retrievedEntity.CustomFields["cf_multiline"].Should().Be("Line1\nLine2\tTabbed");

		testEntity.CustomFields["cf_location"] = "Пловдив";
		testEntity.CustomFields["cf_status"] = "Активен";

		await _db.UpdateAsync(testEntity, [nameof(TestEntityWithCustomFields.CustomFields)]);

		var updatedEntity = await _db.GetAsync<TestEntityWithCustomFields>(testEntity.Id);

		updatedEntity.Should().NotBeNull();
		updatedEntity!.CustomFields.Should().HaveCount(7);
		updatedEntity.CustomFields["cf_location"].Should().Be("Пловдив");
		updatedEntity.CustomFields["cf_status"].Should().Be("Активен");

		var entity1 = new TestEntityWithFieldValues
		{
			Id = Guid.NewGuid(),
			Name = "Parent Entity 1",
			CustomFields = new Dictionary<string, string>
			{
				{ "parent_type", "Организация" },
				{ "parent_status", "Активен" }
			}
		};

		var entity2 = new TestEntityWithFieldValues
		{
			Id = Guid.NewGuid(),
			Name = "Parent Entity 2",
			CustomFields = new Dictionary<string, string>
			{
				{ "parent_type", "Отдел" },
				{ "parent_status", "В процес" }
			}
		};

		await _db.InsertAsync(entity1);
		await _db.InsertAsync(entity2);

		await _db.ExecuteAsync($@"
			INSERT INTO {_valuesTableName} (id, entity_id, field_name, field_metadata)
			VALUES
				(@Id1, @EntityId1, @FieldName1, @Metadata1::jsonb),
				(@Id2, @EntityId1, @FieldName2, @Metadata2::jsonb),
				(@Id3, @EntityId2, @FieldName3, @Metadata3::jsonb)",
			new
			{
				Id1 = Guid.NewGuid(),
				EntityId1 = entity1.Id,
				FieldName1 = "email",
				Metadata1 = "{\"validation\":\"email\",\"required\":\"true\",\"label\":\"Имейл\"}",
				Id2 = Guid.NewGuid(),
				FieldName2 = "phone",
				Metadata2 = "{\"validation\":\"phone\",\"required\":\"false\",\"label\":\"Телефон\"}",
				Id3 = Guid.NewGuid(),
				EntityId2 = entity2.Id,
				FieldName3 = "address",
				Metadata3 = "{\"validation\":\"text\",\"maxLength\":\"500\",\"label\":\"Адрес\"}"
			});

		var sql = $@"
			SELECT id, name, custom_fields_json
			FROM {_tableName}
			ORDER BY name;

			SELECT id, entity_id, field_name, field_metadata
			FROM {_valuesTableName}
			ORDER BY field_name;
		";

		var entitiesWithValues = await _db.QueryMultipleListAsync<TestEntityWithFieldValues>(sql);

		entitiesWithValues.Should().HaveCount(3);

		var parent1 = entitiesWithValues.FirstOrDefault(e => e.Id == entity1.Id);
		parent1.Should().NotBeNull();
		parent1!.CustomFields.Should().NotBeNull();
		parent1.CustomFields.Should().HaveCount(2);
		parent1.CustomFields["parent_type"].Should().Be("Организация");
		parent1.CustomFields["parent_status"].Should().Be("Активен");
		parent1.FieldValues.Should().HaveCount(2);

		var emailField = parent1.FieldValues.FirstOrDefault(f => f.FieldName == "email");
		emailField.Should().NotBeNull();
		emailField!.FieldMetadata.Should().NotBeNull();
		emailField.FieldMetadata.Should().HaveCount(3);
		emailField.FieldMetadata["validation"].Should().Be("email");
		emailField.FieldMetadata["required"].Should().Be("true");
		emailField.FieldMetadata["label"].Should().Be("Имейл");

		var phoneField = parent1.FieldValues.FirstOrDefault(f => f.FieldName == "phone");
		phoneField.Should().NotBeNull();
		phoneField!.FieldMetadata.Should().NotBeNull();
		phoneField.FieldMetadata["validation"].Should().Be("phone");
		phoneField.FieldMetadata["required"].Should().Be("false");
		phoneField.FieldMetadata["label"].Should().Be("Телефон");

		var parent2 = entitiesWithValues.FirstOrDefault(e => e.Id == entity2.Id);
		parent2.Should().NotBeNull();
		parent2!.CustomFields.Should().NotBeNull();
		parent2.CustomFields["parent_type"].Should().Be("Отдел");
		parent2.CustomFields["parent_status"].Should().Be("В процес");
		parent2.FieldValues.Should().HaveCount(1);

		var addressField = parent2.FieldValues.FirstOrDefault(f => f.FieldName == "address");
		addressField.Should().NotBeNull();
		addressField!.FieldMetadata.Should().NotBeNull();
		addressField.FieldMetadata["validation"].Should().Be("text");
		addressField.FieldMetadata["maxLength"].Should().Be("500");
		addressField.FieldMetadata["label"].Should().Be("Адрес");
	}

	[Table("test_customfields_json")]
	private class TestEntityWithCustomFields
	{
		[Key]
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;

		[JsonColumn]
		[DbColumn("custom_fields_json")]
		public Dictionary<string, string> CustomFields { get; set; } = new();
	}

	[Table("test_customfields_json")]
	private class TestEntityWithFieldValues
	{
		[Key]
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;

		[JsonColumn]
		[DbColumn("custom_fields_json")]
		public Dictionary<string, string> CustomFields { get; set; } = new();

		[External]
		[ResultSet(0, ForeignKey = nameof(CustomFieldValue.EntityId), ParentKey = nameof(Id))]
		public List<CustomFieldValue> FieldValues { get; set; } = [];
	}

	[Table("test_customfield_values_json")]
	private class CustomFieldValue
	{
		[Key]
		public Guid Id { get; set; }
		public Guid EntityId { get; set; }
		public string FieldName { get; set; } = string.Empty;

		[JsonColumn]
		[DbColumn("field_metadata")]
		public Dictionary<string, string> FieldMetadata { get; set; } = new();
	}
}
