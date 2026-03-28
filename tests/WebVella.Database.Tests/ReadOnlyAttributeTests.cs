using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebVella.Database.Tests;

/// <summary>
/// Tests for the [ReadOnly] attribute functionality.
/// </summary>
public class ReadOnlyAttributeTests : IAsyncLifetime
{
	private readonly IDbService _db;
	private readonly string _tableName;

	public ReadOnlyAttributeTests()
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
		_tableName = $"readonly_test_{Guid.NewGuid():N}";
	}

	public async Task InitializeAsync()
	{
		await _db.ExecuteAsync($@"
			CREATE TABLE {_tableName} (
				id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
				name VARCHAR(100) NOT NULL,
				description TEXT,
				created_on TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
				updated_on TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
				version INTEGER NOT NULL DEFAULT 1
			)");
	}

	public async Task DisposeAsync()
	{
		await _db.ExecuteAsync($"DROP TABLE IF EXISTS {_tableName}");
	}

	#region <=== ReadOnly Attribute Tests ===>

	[Fact]
	public async Task ReadOnlyProperty_ShouldBeReadFromDatabase()
	{
		var id = Guid.NewGuid();
		await _db.ExecuteAsync(
			$"INSERT INTO {_tableName} (id, name) VALUES (@Id, @Name)",
			new { Id = id, Name = "Test Entity" });

		var result = (await _db.QueryAsync<ReadOnlyTestEntity>(
			$"SELECT id, name, created_on FROM {_tableName} WHERE id = @Id",
			new { Id = id })).FirstOrDefault();

		result.Should().NotBeNull();
		result!.Id.Should().Be(id);
		result.Name.Should().Be("Test Entity");
		result.CreatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(3),
			"CreatedOn should be set by database default");
	}

	[Fact]
	public async Task InsertAsync_ShouldNotIncludeReadOnlyProperty()
	{
		var id = Guid.NewGuid();
		var specificCreatedOn = DateTime.UtcNow.AddDays(-10);

		var sql = $"INSERT INTO {_tableName} (id, name) VALUES (@Id, @Name) RETURNING id, name, created_on";
		var result = (await _db.QueryAsync<ReadOnlyTestEntity>(
			sql,
			new { Id = id, Name = "Test", CreatedOn = specificCreatedOn })).FirstOrDefault();

		result.Should().NotBeNull();
		result!.CreatedOn.Should().NotBe(specificCreatedOn,
			"CreatedOn should use database default, not the provided value");
		result.CreatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(3));
	}

	[Fact]
	public async Task UpdateAsync_ShouldNotUpdateReadOnlyProperty()
	{
		var id = Guid.NewGuid();
		await _db.ExecuteAsync(
			$"INSERT INTO {_tableName} (id, name) VALUES (@Id, @Name)",
			new { Id = id, Name = "Original" });

		var original = (await _db.QueryAsync<ReadOnlyTestEntity>(
			$"SELECT id, name, created_on FROM {_tableName} WHERE id = @Id",
			new { Id = id })).FirstOrDefault();

		original.Should().NotBeNull();
		var originalCreatedOn = original!.CreatedOn;

		await Task.Delay(100);

		var updateSql = $"UPDATE {_tableName} SET name = @Name WHERE id = @Id";
		await _db.ExecuteAsync(updateSql, new { Id = id, Name = "Updated", CreatedOn = DateTime.UtcNow });

		var retrieved = (await _db.QueryAsync<ReadOnlyTestEntity>(
			$"SELECT id, name, created_on FROM {_tableName} WHERE id = @Id",
			new { Id = id })).FirstOrDefault();

		retrieved.Should().NotBeNull();
		retrieved!.Name.Should().Be("Updated");
		retrieved.CreatedOn.Should().Be(originalCreatedOn,
			"CreatedOn should not be updated even if value is provided");
	}

	[Fact]
	public async Task MultipleReadOnlyProperties_ShouldAllBeExcludedFromWrites()
	{
		var id = Guid.NewGuid();
		await _db.ExecuteAsync(
			$"INSERT INTO {_tableName} (id, name) VALUES (@Id, @Name)",
			new { Id = id, Name = "Test" });

		await _db.ExecuteAsync(
			$"UPDATE {_tableName} SET version = 2, updated_on = CURRENT_TIMESTAMP WHERE id = @Id",
			new { Id = id });

		var result = (await _db.QueryAsync<MultiReadOnlyEntity>(
			$"SELECT id, name, created_on, updated_on, version FROM {_tableName} WHERE id = @Id",
			new { Id = id })).FirstOrDefault();

		result.Should().NotBeNull();
		result!.Version.Should().Be(2);
		result.Name.Should().Be("Test");
	}

	#endregion

	#region <=== Test Entity Classes ===>

	private class ReadOnlyTestEntity
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		[ReadOnly]
		public DateTime CreatedOn { get; set; }
	}

	private class MultiReadOnlyEntity
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		[ReadOnly]
		public DateTime CreatedOn { get; set; }

		[ReadOnly]
		public DateTime UpdatedOn { get; set; }

		[ReadOnly]
		public int Version { get; set; }
	}

	#endregion
}
