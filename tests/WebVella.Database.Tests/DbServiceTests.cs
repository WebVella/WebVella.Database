using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace WebVella.Database.Tests;

public class DbServiceTests
{
	private static readonly string TestConnectionString = new ConfigurationBuilder()
		.SetBasePath(Directory.GetCurrentDirectory())
		.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
		.Build()
		.GetConnectionString("DefaultConnection")
		?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");

	#region <=== Constructor Tests ===>

	[Fact]
	public void Constructor_WithNullConnectionString_ShouldThrowArgumentNullException()
	{
		var action = () => new DbService(null!);
		action.Should().Throw<ArgumentNullException>()
			.WithParameterName("connectionString");
	}

	[Fact]
	public void Constructor_WithValidConnectionString_ShouldCreateInstance()
	{
		var dbService = new DbService(TestConnectionString);
		dbService.Should().NotBeNull();
	}

	#endregion

	#region <=== CreateTransactionScope Tests ===>

	[Fact]
	public void CreateTransactionScope_WithLongLockKey_ShouldReturnTransactionScope()
	{
		var dbService = new DbService(TestConnectionString);

		var transactionScope = dbService.CreateTransactionScope(123L);

		transactionScope.Should().NotBeNull();
		transactionScope.Should().BeAssignableTo<IDbTransactionScope>();
	}

	[Fact]
	public void CreateTransactionScope_WithNullLockKey_ShouldReturnTransactionScope()
	{
		var dbService = new DbService(TestConnectionString);

		var transactionScope = dbService.CreateTransactionScope(null);

		transactionScope.Should().NotBeNull();
		transactionScope.Should().BeAssignableTo<IDbTransactionScope>();
	}

	[Fact]
	public void CreateTransactionScope_WithZeroLockKey_ShouldReturnTransactionScope()
	{
		var dbService = new DbService(TestConnectionString);

		var transactionScope = dbService.CreateTransactionScope(0L);

		transactionScope.Should().NotBeNull();
		transactionScope.Should().BeAssignableTo<IDbTransactionScope>();
	}

	[Fact]
	public void CreateTransactionScope_WithNegativeLockKey_ShouldReturnTransactionScope()
	{
		var dbService = new DbService(TestConnectionString);

		var transactionScope = dbService.CreateTransactionScope(-123L);

		transactionScope.Should().NotBeNull();
		transactionScope.Should().BeAssignableTo<IDbTransactionScope>();
	}

	[Fact]
	public void CreateTransactionScope_WithoutLockKey_ShouldReturnTransactionScope()
	{
		var dbService = new DbService(TestConnectionString);

		var transactionScope = dbService.CreateTransactionScope();

		transactionScope.Should().NotBeNull();
		transactionScope.Should().BeAssignableTo<IDbTransactionScope>();
	}

	#endregion

	#region <=== CreateConnection Tests ===>

	[Fact]
	public void CreateConnection_ShouldReturnConnection()
	{
		var dbService = new DbService(TestConnectionString);

		var connection = dbService.CreateConnection();

		connection.Should().NotBeNull();
		connection.Should().BeAssignableTo<IDbConnection>();
	}

	#endregion
}
