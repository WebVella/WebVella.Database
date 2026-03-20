using FluentAssertions;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Integration tests for <see cref="DbColumnAttribute"/> verifying that custom column name
/// mapping works correctly with a real PostgreSQL database.
/// </summary>
[Collection("Database")]
public class DbColumnAttributeIntegrationTests : IAsyncLifetime
{
	private readonly DatabaseFixture _fixture;
	private readonly IDbService _dbService;

	public DbColumnAttributeIntegrationTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
		_dbService = fixture.DbService;
	}

	public Task InitializeAsync() => _fixture.ClearTestDbColumnEntitiesAsync();

	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== InsertAsync Tests ===>

	[Fact]
	public async Task InsertAsync_WithDbColumn_ShouldInsertAndReturnGeneratedId()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "John Doe",
			Email = "john@example.com",
			Description = "A test entity"
		};

		var inserted = await _dbService.InsertAsync(entity);

		inserted.Id.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public async Task InsertAsync_WithDbColumn_ShouldMapColumnsCorrectly()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "Jane Smith",
			Email = "jane@example.com",
			Description = "Integration test"
		};

		var inserted = await _dbService.InsertAsync(entity);
		var id = inserted.Id;

		var fullName = await _dbService.ExecuteScalarAsync<string>(
			"SELECT full_name FROM test_db_column_entities WHERE entity_id = @Id",
			new { Id = id });
		var emailAddress = await _dbService.ExecuteScalarAsync<string>(
			"SELECT email_address FROM test_db_column_entities WHERE entity_id = @Id",
			new { Id = id });
		var description = await _dbService.ExecuteScalarAsync<string>(
			"SELECT description FROM test_db_column_entities WHERE entity_id = @Id",
			new { Id = id });

		fullName.Should().Be("Jane Smith");
		emailAddress.Should().Be("jane@example.com");
		description.Should().Be("Integration test");
	}

	[Fact]
	public async Task InsertAsync_WithDbColumn_MultipleInserts_ShouldGenerateUniqueIds()
	{
		var entities = Enumerable.Range(1, 3)
			.Select(i => new TestDbColumnEntity
			{
				DisplayName = $"User {i}",
				Email = $"user{i}@example.com",
				Description = $"Description {i}"
			})
			.ToList();

		var ids = new List<Guid>();
		foreach (var entity in entities)
		{
			var inserted = await _dbService.InsertAsync(entity);
			ids.Add(inserted.Id);
		}

		ids.Should().HaveCount(3);
		ids.Should().OnlyHaveUniqueItems();
		ids.Should().NotContain(Guid.Empty);
	}

	#endregion

	#region <=== GetAsync Tests ===>

	[Fact]
	public async Task GetAsync_WithDbColumn_ShouldReturnInsertedEntity()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "Get Test",
			Email = "get@example.com",
			Description = "Get test description"
		};
		var inserted = await _dbService.InsertAsync(entity);
		var id = inserted.Id;

		var retrieved = await _dbService.GetAsync<TestDbColumnEntity>(id);

		retrieved.Should().NotBeNull();
		retrieved!.Id.Should().Be(id);
		retrieved.DisplayName.Should().Be("Get Test");
		retrieved.Email.Should().Be("get@example.com");
		retrieved.Description.Should().Be("Get test description");
	}

	[Fact]
	public async Task GetAsync_WithDbColumn_NonExistentId_ShouldReturnNull()
	{
		var retrieved = await _dbService.GetAsync<TestDbColumnEntity>(Guid.NewGuid());

		retrieved.Should().BeNull();
	}

	#endregion

	#region <=== GetListAsync Tests ===>

	[Fact]
	public async Task GetListAsync_WithDbColumn_ShouldReturnAllEntities()
	{
		await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "User 1",
			Email = "user1@example.com",
			Description = "First"
		});
		await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "User 2",
			Email = "user2@example.com",
			Description = "Second"
		});

		var all = await _dbService.GetListAsync<TestDbColumnEntity>();

		all.Should().HaveCount(2);
		all.Select(e => e.DisplayName).Should().Contain("User 1", "User 2");
	}

	[Fact]
	public async Task GetListAsync_WithDbColumn_ByIds_ShouldReturnOnlyMatching()
	{
		var inserted1 = await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Match 1",
			Email = "match1@example.com",
			Description = "Matching"
		});
		var inserted2 = await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Match 2",
			Email = "match2@example.com",
			Description = "Matching"
		});
		await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "No Match",
			Email = "nomatch@example.com",
			Description = "Not matching"
		});

		var ids = new List<Guid> { inserted1.Id, inserted2.Id };
		var results = await _dbService.GetListAsync<TestDbColumnEntity>(ids);

		results.Should().HaveCount(2);
		results.Select(e => e.DisplayName).Should()
			.Contain("Match 1")
			.And.Contain("Match 2")
			.And.NotContain("No Match");
	}

	#endregion

	#region <=== UpdateAsync Tests ===>

	[Fact]
	public async Task UpdateAsync_WithDbColumn_ShouldUpdateAllWritableFields()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "Original Name",
			Email = "original@example.com",
			Description = "Original"
		};
		var inserted = await _dbService.InsertAsync(entity);
		entity.Id = inserted.Id;

		entity.DisplayName = "Updated Name";
		entity.Email = "updated@example.com";
		entity.Description = "Updated";

		var updated = await _dbService.UpdateAsync(entity);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestDbColumnEntity>(inserted.Id);
		retrieved.Should().NotBeNull();
		retrieved!.DisplayName.Should().Be("Updated Name");
		retrieved.Email.Should().Be("updated@example.com");
		retrieved.Description.Should().Be("Updated");
	}

	[Fact]
	public async Task UpdateAsync_WithDbColumn_NonExistentId_ShouldReturnFalse()
	{
		var entity = new TestDbColumnEntity
		{
			Id = Guid.NewGuid(),
			DisplayName = "Non Existent",
			Email = "none@example.com",
			Description = "Does not exist"
		};

		var updated = await _dbService.UpdateAsync(entity);

		updated.Should().BeFalse();
	}

	[Fact]
	public async Task UpdateAsync_WithDbColumn_SpecificProperties_ShouldUpdateOnlyThose()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "Original",
			Email = "original@example.com",
			Description = "Original description"
		};
		var inserted = await _dbService.InsertAsync(entity);
		entity.Id = inserted.Id;

		entity.DisplayName = "Updated";
		entity.Email = "updated@example.com";
		entity.Description = "Updated description";

		var updated = await _dbService.UpdateAsync(entity, ["DisplayName"]);

		updated.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestDbColumnEntity>(inserted.Id);
		retrieved!.DisplayName.Should().Be("Updated");
		retrieved.Email.Should().Be("original@example.com");
		retrieved.Description.Should().Be("Original description");
	}

	[Fact]
	public async Task UpdateAsync_WithDbColumn_ShouldOnlyAffectTargetEntity()
	{
		var entity1 = await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Entity 1",
			Email = "e1@example.com",
			Description = "First"
		});
		var entity2 = await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Entity 2",
			Email = "e2@example.com",
			Description = "Second"
		});

		var toUpdate = new TestDbColumnEntity
		{
			Id = entity1.Id,
			DisplayName = "Entity 1 Updated",
			Email = "e1-updated@example.com",
			Description = "First updated"
		};
		await _dbService.UpdateAsync(toUpdate);

		var retrieved1 = await _dbService.GetAsync<TestDbColumnEntity>(entity1.Id);
		var retrieved2 = await _dbService.GetAsync<TestDbColumnEntity>(entity2.Id);

		retrieved1!.DisplayName.Should().Be("Entity 1 Updated");
		retrieved2!.DisplayName.Should().Be("Entity 2");
	}

	#endregion

	#region <=== DeleteAsync Tests ===>

	[Fact]
	public async Task DeleteAsync_WithDbColumn_ShouldDeleteEntity()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "To Delete",
			Email = "delete@example.com",
			Description = "Will be deleted"
		};
		var inserted = await _dbService.InsertAsync(entity);
		entity.Id = inserted.Id;

		var deleted = await _dbService.DeleteAsync(entity);

		deleted.Should().BeTrue();

		var retrieved = await _dbService.GetAsync<TestDbColumnEntity>(inserted.Id);
		retrieved.Should().BeNull();
	}

	[Fact]
	public async Task DeleteAsync_WithDbColumn_NonExistentId_ShouldReturnFalse()
	{
		var entity = new TestDbColumnEntity
		{
			Id = Guid.NewGuid(),
			DisplayName = "Non Existent",
			Email = "none@example.com",
			Description = "Does not exist"
		};

		var deleted = await _dbService.DeleteAsync(entity);

		deleted.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteAsync_WithDbColumn_ShouldOnlyDeleteTargetEntity()
	{
		var entity1 = await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Keep Me",
			Email = "keep@example.com",
			Description = "Should remain"
		});
		var entity2 = await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Delete Me",
			Email = "delete@example.com",
			Description = "Should be deleted"
		});

		var toDelete = new TestDbColumnEntity { Id = entity2.Id };
		await _dbService.DeleteAsync(toDelete);

		var retrieved1 = await _dbService.GetAsync<TestDbColumnEntity>(entity1.Id);
		var retrieved2 = await _dbService.GetAsync<TestDbColumnEntity>(entity2.Id);

		retrieved1.Should().NotBeNull();
		retrieved2.Should().BeNull();
	}

	#endregion

	#region <=== QueryAsync Tests ===>

	[Fact]
	public async Task QueryAsync_WithDbColumn_ShouldMapCustomColumnsCorrectly()
	{
		await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Query User 1",
			Email = "q1@example.com",
			Description = "First"
		});
		await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Query User 2",
			Email = "q2@example.com",
			Description = "Second"
		});

		var results = await _dbService.QueryAsync<TestDbColumnEntity>(
			"SELECT entity_id AS \"Id\", full_name AS \"DisplayName\", " +
			"email_address AS \"Email\", description AS \"Description\" " +
			"FROM test_db_column_entities WHERE full_name LIKE @Pattern",
			new { Pattern = "Query%" });

		results.Should().HaveCount(2);
		results.Should().OnlyContain(e => e.DisplayName.StartsWith("Query"));
	}

	[Fact]
	public async Task QueryAsync_WithDbColumn_FilterByCustomColumn_ShouldWork()
	{
		await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Alice",
			Email = "alice@example.com",
			Description = "Active user"
		});
		await _dbService.InsertAsync(new TestDbColumnEntity
		{
			DisplayName = "Bob",
			Email = "bob@example.com",
			Description = "Inactive user"
		});

		var results = await _dbService.QueryAsync<TestDbColumnEntity>(
			"SELECT entity_id AS \"Id\", full_name AS \"DisplayName\", " +
			"email_address AS \"Email\", description AS \"Description\" " +
			"FROM test_db_column_entities WHERE email_address = @Email",
			new { Email = "alice@example.com" });

		results.Should().HaveCount(1);
		results.First().DisplayName.Should().Be("Alice");
	}

	#endregion

	#region <=== Mixed DbColumn And SnakeCase Tests ===>

	[Fact]
	public async Task InsertAndGet_WithMixedDbColumnAndSnakeCase_ShouldWork()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "Mixed Test",
			Email = "mixed@example.com",
			Description = "This uses default snake_case"
		};

		var inserted = await _dbService.InsertAsync(entity);

		var retrieved = await _dbService.GetAsync<TestDbColumnEntity>(inserted.Id);

		retrieved.Should().NotBeNull();
		retrieved!.DisplayName.Should().Be("Mixed Test");
		retrieved.Email.Should().Be("mixed@example.com");
		retrieved.Description.Should().Be("This uses default snake_case");
	}

	[Fact]
	public async Task FullCrudCycle_WithDbColumn_ShouldWorkEndToEnd()
	{
		var entity = new TestDbColumnEntity
		{
			DisplayName = "CRUD Test",
			Email = "crud@example.com",
			Description = "Full cycle"
		};
		var inserted = await _dbService.InsertAsync(entity);
		inserted.Id.Should().NotBe(Guid.Empty);

		var retrieved = await _dbService.GetAsync<TestDbColumnEntity>(inserted.Id);
		retrieved.Should().NotBeNull();
		retrieved!.DisplayName.Should().Be("CRUD Test");

		retrieved.DisplayName = "CRUD Updated";
		retrieved.Email = "crud-updated@example.com";
		var updated = await _dbService.UpdateAsync(retrieved);
		updated.Should().BeTrue();

		var afterUpdate = await _dbService.GetAsync<TestDbColumnEntity>(inserted.Id);
		afterUpdate!.DisplayName.Should().Be("CRUD Updated");
		afterUpdate.Email.Should().Be("crud-updated@example.com");
		afterUpdate.Description.Should().Be("Full cycle");

		var deleted = await _dbService.DeleteAsync(afterUpdate);
		deleted.Should().BeTrue();

		var afterDelete = await _dbService.GetAsync<TestDbColumnEntity>(inserted.Id);
		afterDelete.Should().BeNull();
	}

	#endregion
}
