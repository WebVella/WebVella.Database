using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebVella.Database.Migrations;
using WebVella.Database.Security;
using Xunit;

namespace WebVella.Database.Tests;

public class RlsTests
{
	private static readonly string TestConnectionString = new ConfigurationBuilder()
		.SetBasePath(Directory.GetCurrentDirectory())
		.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
		.Build()
		.GetConnectionString("DefaultConnection")
		?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");

	#region <=== RlsContextProvider Tests ===>

	[Fact]
	public void NullRlsContextProvider_ShouldReturnNullValues()
	{
		var provider = NullRlsContextProvider.Instance;

		provider.TenantId.Should().BeNull();
		provider.UserId.Should().BeNull();
		provider.CustomClaims.Should().BeEmpty();
	}

	[Fact]
	public void NullRlsContextProvider_Instance_ShouldBeSingleton()
	{
		var instance1 = NullRlsContextProvider.Instance;
		var instance2 = NullRlsContextProvider.Instance;

		instance1.Should().BeSameAs(instance2);
	}

	#endregion

	#region <=== RlsOptions Tests ===>

	[Fact]
	public void RlsOptions_DefaultValues_ShouldBeCorrect()
	{
		var options = new RlsOptions();

		options.Prefix.Should().Be("app");
		options.UseLocalSettings.Should().BeTrue();
		options.Enabled.Should().BeTrue();
	}

	[Fact]
	public void RlsOptions_ShouldAllowCustomization()
	{
		var options = new RlsOptions
		{
			Prefix = "rls",
			UseLocalSettings = false,
			Enabled = false
		};

		options.Prefix.Should().Be("rls");
		options.UseLocalSettings.Should().BeFalse();
		options.Enabled.Should().BeFalse();
	}

	#endregion

	#region <=== DbService with RLS Tests ===>

	[Fact]
	public void DbService_WithRlsContextProvider_ShouldCreateInstance()
	{
		var rlsProvider = new TestRlsContextProvider(Guid.NewGuid(), Guid.NewGuid());

		var dbService = new DbService(TestConnectionString, NullDbEntityCache.Instance, rlsProvider, null);

		dbService.Should().NotBeNull();
	}

	[Fact]
	public void DbService_WithNullRlsContextProvider_ShouldCreateInstance()
	{
		var dbService = new DbService(TestConnectionString, NullDbEntityCache.Instance, null, null);

		dbService.Should().NotBeNull();
	}

	[Fact]
	public async Task DbService_WithRlsContext_ShouldSetSessionVariables()
	{
		var tenantId = Guid.NewGuid();
		var userId = Guid.NewGuid();
		var rlsProvider = new TestRlsContextProvider(tenantId, userId);
		var options = new RlsOptions { UseLocalSettings = false };

		var dbService = new DbService(TestConnectionString, NullDbEntityCache.Instance, rlsProvider, options);

		await using var scope = await dbService.CreateTransactionScopeAsync();
		var result = await dbService.ExecuteScalarAsync<string>(
			"SELECT current_setting('app.tenant_id', true)");

		result.Should().Be(tenantId.ToString());
	}

	[Fact]
	public async Task DbService_WithRlsContextAndCustomPrefix_ShouldSetSessionVariablesWithPrefix()
	{
		var tenantId = Guid.NewGuid();
		var rlsProvider = new TestRlsContextProvider(tenantId, null);
		var options = new RlsOptions { Prefix = "myapp", UseLocalSettings = false };

		var dbService = new DbService(TestConnectionString, NullDbEntityCache.Instance, rlsProvider, options);

		await using var scope = await dbService.CreateTransactionScopeAsync();
		var result = await dbService.ExecuteScalarAsync<string>(
			"SELECT current_setting('myapp.tenant_id', true)");

		result.Should().Be(tenantId.ToString());
	}

	[Fact]
	public async Task DbService_WithRlsContextAndCustomClaims_ShouldSetCustomClaimVariables()
	{
		var customClaims = new Dictionary<string, string>
		{
			["role"] = "admin",
			["department"] = "engineering"
		};
		var rlsProvider = new TestRlsContextProvider(null, null, customClaims);
		var options = new RlsOptions { UseLocalSettings = false };

		var dbService = new DbService(TestConnectionString, NullDbEntityCache.Instance, rlsProvider, options);

		await using var scope = await dbService.CreateTransactionScopeAsync();
		var role = await dbService.ExecuteScalarAsync<string>("SELECT current_setting('app.role', true)");
		var department = await dbService.ExecuteScalarAsync<string>(
			"SELECT current_setting('app.department', true)");

		role.Should().Be("admin");
		department.Should().Be("engineering");
	}

	[Fact]
	public async Task DbService_WithDisabledRls_ShouldNotSetSessionVariables()
	{
		var tenantId = Guid.NewGuid();
		var rlsProvider = new TestRlsContextProvider(tenantId, null);
		var options = new RlsOptions { Enabled = false };

		var dbService = new DbService(TestConnectionString, NullDbEntityCache.Instance, rlsProvider, options);

		var result = await dbService.ExecuteScalarAsync<string>(
			"SELECT current_setting('app.tenant_id', true)");

		result.Should().BeNullOrEmpty();
	}

	#endregion

	#region <=== Cache with RLS Tests ===>

	[Fact]
	public void Cache_WithDifferentTenants_ShouldGenerateDifferentKeys()
	{
		var cache = new DbEntityCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(
			new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

		var entityId = Guid.NewGuid();
		var tenant1Context = "t:11111111-1111-1111-1111-111111111111";
		var tenant2Context = "t:22222222-2222-2222-2222-222222222222";

		var key1 = cache.GenerateKey<TestEntity>(entityId, tenant1Context);
		var key2 = cache.GenerateKey<TestEntity>(entityId, tenant2Context);
		var keyNoContext = cache.GenerateKey<TestEntity>(entityId);

		key1.Should().NotBe(key2);
		key1.Should().NotBe(keyNoContext);
		key1.Should().Contain("Rls:t:11111111");
		key2.Should().Contain("Rls:t:22222222");
		keyNoContext.Should().NotContain("Rls:");
	}

	[Fact]
	public void Cache_CollectionKey_WithDifferentTenants_ShouldGenerateDifferentKeys()
	{
		var cache = new DbEntityCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(
			new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

		var tenant1Context = "t:11111111-1111-1111-1111-111111111111";
		var tenant2Context = "t:22222222-2222-2222-2222-222222222222";

		var key1 = cache.GenerateCollectionKey<TestEntity>(null, tenant1Context);
		var key2 = cache.GenerateCollectionKey<TestEntity>(null, tenant2Context);
		var keyNoContext = cache.GenerateCollectionKey<TestEntity>();

		key1.Should().NotBe(key2);
		key1.Should().NotBe(keyNoContext);
		key1.Should().Contain("Rls:t:11111111");
		key2.Should().Contain("Rls:t:22222222");
		keyNoContext.Should().NotContain("Rls:");
	}

	[Fact]
	public void Cache_WithTenantAndUser_ShouldIncludeBothInKey()
	{
		var cache = new DbEntityCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(
			new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

		var entityId = Guid.NewGuid();
		var context = "t:11111111-1111-1111-1111-111111111111|u:22222222-2222-2222-2222-222222222222";

		var key = cache.GenerateKey<TestEntity>(entityId, context);

		key.Should().Contain("Rls:t:11111111");
		key.Should().Contain("u:22222222");
	}

	[Fact]
	public void Cache_WithCustomClaims_ShouldIncludeClaimsInKey()
	{
		var cache = new DbEntityCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(
			new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

		var entityId = Guid.NewGuid();
		var context = "t:11111111-1111-1111-1111-111111111111|c:department:engineering,role:admin";

		var key = cache.GenerateKey<TestEntity>(entityId, context);

		key.Should().Contain("Rls:");
		key.Should().Contain("t:11111111");
		key.Should().Contain("c:department:engineering,role:admin");
	}

	[Fact]
	public void Cache_WithDifferentCustomClaims_ShouldGenerateDifferentKeys()
	{
		var cache = new DbEntityCache(new Microsoft.Extensions.Caching.Memory.MemoryCache(
			new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

		var entityId = Guid.NewGuid();
		var adminContext = "t:11111111-1111-1111-1111-111111111111|c:role:admin";
		var userContext = "t:11111111-1111-1111-1111-111111111111|c:role:user";

		var adminKey = cache.GenerateKey<TestEntity>(entityId, adminContext);
		var userKey = cache.GenerateKey<TestEntity>(entityId, userContext);

		adminKey.Should().NotBe(userKey);
		adminKey.Should().Contain("role:admin");
		userKey.Should().Contain("role:user");
	}

	[Table("test_entity")]
	private class TestEntity
	{
		[Key]
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	#endregion

	#region <=== Migration with RLS Tests ===>

	[Fact]
	public void AddWebVellaDatabaseWithRls_ShouldRegisterConnectionStringAccessor()
	{
		var services = new ServiceCollection();

		services.AddWebVellaDatabaseWithRls(
			TestConnectionString,
			rlsContextProviderFactory: _ => new TestRlsContextProvider(Guid.NewGuid(), null));

		var serviceProvider = services.BuildServiceProvider();
		var accessor = serviceProvider.GetService<IDbConnectionStringAccessor>();

		accessor.Should().NotBeNull();
		accessor!.ConnectionString.Should().Be(TestConnectionString);
	}

	[Fact]
	public void AddWebVellaDatabaseMigrations_WithRls_ShouldCreateMigrationService()
	{
		var services = new ServiceCollection();
		services.AddWebVellaDatabaseWithRls(
			TestConnectionString,
			rlsContextProviderFactory: _ => new TestRlsContextProvider(Guid.NewGuid(), null));
		services.AddWebVellaDatabaseMigrations();

		var serviceProvider = services.BuildServiceProvider();
		using var scope = serviceProvider.CreateScope();
		var migrationService = scope.ServiceProvider.GetService<IDbMigrationService>();

		migrationService.Should().NotBeNull();
	}

	[Fact]
	public async Task MigrationService_WithRlsEnabled_ShouldBypassRlsContext()
	{
		var tenantId = Guid.NewGuid();
		var userId = Guid.NewGuid();
		var tableName = $"_rls_test_migration_{Guid.NewGuid():N}";

		var services = new ServiceCollection();
		services.AddWebVellaDatabaseWithRls(
			TestConnectionString,
			rlsContextProviderFactory: _ => new TestRlsContextProvider(tenantId, userId));
		services.AddWebVellaDatabaseMigrations(new DbMigrationOptions
		{
			VersionTableName = tableName
		});

		var serviceProvider = services.BuildServiceProvider();

		try
		{
			using var scope = serviceProvider.CreateScope();
			var migrationService = scope.ServiceProvider.GetRequiredService<IDbMigrationService>();

			var version = await migrationService.GetCurrentDbVersionAsync();

			version.Should().Be(new Version(0, 0, 0, 0));
		}
		finally
		{
			using var cleanupScope = serviceProvider.CreateScope();
			var db = cleanupScope.ServiceProvider.GetRequiredService<IDbService>();
			await db.ExecuteAsync($"DROP TABLE IF EXISTS {tableName};");
		}
	}

	[Fact]
	public async Task MigrationService_WithRlsEnabled_ShouldNotSetSessionVariables()
	{
		var tenantId = Guid.NewGuid();
		var userId = Guid.NewGuid();
		var tableName = $"_rls_migration_check_{Guid.NewGuid():N}";

		var services = new ServiceCollection();
		services.AddWebVellaDatabaseWithRls(
			TestConnectionString,
			rlsContextProviderFactory: _ => new TestRlsContextProvider(tenantId, userId),
			enableCaching: false,
			rlsOptions: new RlsOptions { UseLocalSettings = false });
		services.AddWebVellaDatabaseMigrations(new DbMigrationOptions
		{
			VersionTableName = tableName
		});

		var serviceProvider = services.BuildServiceProvider();

		try
		{
			using var scope = serviceProvider.CreateScope();

			var regularDb = scope.ServiceProvider.GetRequiredService<IDbService>();
			var regularTenantId = await regularDb.ExecuteScalarAsync<string>(
				"SELECT current_setting('app.tenant_id', true)");
			regularTenantId.Should().Be(tenantId.ToString());

			var accessor = scope.ServiceProvider.GetRequiredService<IDbConnectionStringAccessor>();
			var migrationDb = new DbService(accessor.ConnectionString, NullDbEntityCache.Instance, null, null);
			var migrationTenantId = await migrationDb.ExecuteScalarAsync<string>(
				"SELECT current_setting('app.tenant_id', true)");
			migrationTenantId.Should().BeNullOrEmpty();
		}
		finally
		{
			using var cleanupScope = serviceProvider.CreateScope();
			var db = cleanupScope.ServiceProvider.GetRequiredService<IDbService>();
			await db.ExecuteAsync($"DROP TABLE IF EXISTS {tableName};");
		}
	}

	[Fact]
	public void AddWebVellaDatabaseWithRls_WithFactory_ShouldRegisterConnectionStringAccessor()
	{
		var services = new ServiceCollection();

		services.AddWebVellaDatabaseWithRls(
			TestConnectionString,
			rlsContextProviderFactory: _ => new TestRlsContextProvider(Guid.NewGuid(), null),
			enableCaching: false);

		var serviceProvider = services.BuildServiceProvider();
		using var scope = serviceProvider.CreateScope();
		var accessor = scope.ServiceProvider.GetService<IDbConnectionStringAccessor>();

		accessor.Should().NotBeNull();
		accessor!.ConnectionString.Should().Be(TestConnectionString);
	}

	[Fact]
	public void AddWebVellaDatabaseWithRls_WithConnectionStringFactory_ShouldRegisterConnectionStringAccessor()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IRlsContextProvider>(new TestRlsContextProvider(Guid.NewGuid(), null));

		services.AddWebVellaDatabase(_ => TestConnectionString, enableCaching: false);

		var serviceProvider = services.BuildServiceProvider();
		using var scope = serviceProvider.CreateScope();
		var accessor = scope.ServiceProvider.GetService<IDbConnectionStringAccessor>();

		accessor.Should().NotBeNull();
		accessor!.ConnectionString.Should().Be(TestConnectionString);
	}

	[Fact]
	public void AddWebVellaDatabaseWithRls_WithProviderFactory_ShouldRegisterConnectionStringAccessor()
	{
		var services = new ServiceCollection();

		services.AddWebVellaDatabaseWithRls(
			TestConnectionString,
			rlsContextProviderFactory: _ => new TestRlsContextProvider(Guid.NewGuid(), null),
			enableCaching: false);

		var serviceProvider = services.BuildServiceProvider();
		var accessor = serviceProvider.GetService<IDbConnectionStringAccessor>();

		accessor.Should().NotBeNull();
		accessor!.ConnectionString.Should().Be(TestConnectionString);
	}

	[Fact]
	public async Task MigrationService_WithRlsEnabled_ShouldExecuteMigrations()
	{
		var versionTable = $"_rls_mig_exec_{Guid.NewGuid():N}";
		var testTable = $"rls_test_table_{Guid.NewGuid():N}";

		var services = new ServiceCollection();
		services.AddWebVellaDatabaseWithRls(
			TestConnectionString,
			rlsContextProviderFactory: _ => new TestRlsContextProvider(Guid.NewGuid(), Guid.NewGuid()));

		var serviceProvider = services.BuildServiceProvider();

		try
		{
			using var scope = serviceProvider.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<IDbService>();

			var accessor = scope.ServiceProvider.GetRequiredService<IDbConnectionStringAccessor>();
			var migrationDb = new DbService(accessor.ConnectionString, NullDbEntityCache.Instance, null, null);
			var migrationService = new DbMigrationService(
				scope.ServiceProvider,
				migrationDb,
				new DbMigrationOptions { VersionTableName = versionTable });

			await migrationDb.ExecuteAsync($"CREATE TABLE IF NOT EXISTS {testTable} (id UUID PRIMARY KEY)");

			var tableExists = await db.ExecuteScalarAsync<bool>(
				"SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = @TableName)",
				new { TableName = testTable });

			tableExists.Should().BeTrue();
		}
		finally
		{
			using var cleanupScope = serviceProvider.CreateScope();
			var db = cleanupScope.ServiceProvider.GetRequiredService<IDbService>();
			await db.ExecuteAsync($"DROP TABLE IF EXISTS {testTable};");
			await db.ExecuteAsync($"DROP TABLE IF EXISTS {versionTable};");
		}
	}

	[Fact]
	public void DbConnectionStringAccessor_ShouldStoreConnectionString()
	{
		var connectionString = "Host=localhost;Database=test";
		var accessor = new DbConnectionStringAccessor(connectionString);

		accessor.ConnectionString.Should().Be(connectionString);
	}

	[Fact]
	public void DbConnectionStringAccessor_WithNullConnectionString_ShouldThrow()
	{
		var act = () => new DbConnectionStringAccessor(null!);

		act.Should().Throw<ArgumentNullException>();
	}

	#endregion

	#region <=== Test Helpers ===>

	private class TestRlsContextProvider : IRlsContextProvider
	{
		private readonly Dictionary<string, string> _customClaims;

		public TestRlsContextProvider(
			Guid? tenantId,
			Guid? userId,
			Dictionary<string, string>? customClaims = null)
		{
			TenantId = tenantId;
			UserId = userId;
			_customClaims = customClaims ?? [];
		}

		public Guid? TenantId { get; }
		public Guid? UserId { get; }
		public IReadOnlyDictionary<string, string> CustomClaims => _customClaims;
	}

	#endregion
}
