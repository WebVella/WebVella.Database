using FluentAssertions;
using WebVella.Database.Tests.Fixtures;
using WebVella.Database.Tests.Models;
using Xunit;

namespace WebVella.Database.Tests;

[Collection("Database")]
public class DbServiceAsyncDatabaseTests : IAsyncLifetime
{
	private readonly DatabaseFixture _fixture;
	private readonly IDbService _dbService;

	public DbServiceAsyncDatabaseTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
		_dbService = fixture.DbService;
	}

	public Task InitializeAsync() => _fixture.ClearTestProductsAsync();

	public Task DisposeAsync() => Task.CompletedTask;

	#region <=== Async Connection CRUD Operations ===>

	[Fact]
	public async Task AsyncConnection_CRUDOperations_ShouldSucceed()
	{
		var createdAt = DateTime.UtcNow;
		var product = new TestProduct
		{
			Name = "AsyncName",
			Description = "Async test product",
			Price = 10.00m,
			Quantity = 5,
			IsActive = true,
			CreatedAt = createdAt
		};

		// Insert and verify all properties
		var inserted = await _dbService.InsertAsync(product);
		var id = inserted.Id;

		id.Should().NotBe(Guid.Empty);
		inserted.Name.Should().Be("AsyncName");
		inserted.Description.Should().Be("Async test product");
		inserted.Price.Should().Be(10.00m);
		inserted.Quantity.Should().Be(5);
		inserted.IsActive.Should().BeTrue();
		inserted.CreatedAt.ToUniversalTime().Should().BeCloseTo(createdAt.ToUniversalTime(), TimeSpan.FromSeconds(1));

		// Retrieve and verify all properties
		var retrieved = await _dbService.GetAsync<TestProduct>(id);
		retrieved.Should().NotBeNull();
		retrieved!.Id.Should().Be(id);
		retrieved.Name.Should().Be("AsyncName");
		retrieved.Description.Should().Be("Async test product");
		retrieved.Price.Should().Be(10.00m);
		retrieved.Quantity.Should().Be(5);
		retrieved.IsActive.Should().BeTrue();
		retrieved.CreatedAt.ToUniversalTime().Should().BeCloseTo(createdAt.ToUniversalTime(), TimeSpan.FromSeconds(1));

		// Update name only and verify other properties remain unchanged
		retrieved.Name = "AsyncUpdated";
		var updateResult = await _dbService.UpdateAsync(retrieved);
		updateResult.Should().BeTrue();

		var updated = await _dbService.GetAsync<TestProduct>(id);
		updated.Should().NotBeNull();
		updated!.Id.Should().Be(id);
		updated.Name.Should().Be("AsyncUpdated");
		updated.Description.Should().Be("Async test product"); // Should remain unchanged
		updated.Price.Should().Be(10.00m); // Should remain unchanged
		updated.Quantity.Should().Be(5); // Should remain unchanged
		updated.IsActive.Should().BeTrue(); // Should remain unchanged
		updated.CreatedAt.ToUniversalTime().Should().BeCloseTo(createdAt.ToUniversalTime(), TimeSpan.FromSeconds(1)); // Should remain unchanged

		// Delete and verify removal
		var deleteResult = await _dbService.DeleteAsync<TestProduct>(id);
		deleteResult.Should().BeTrue();

		var deleted = await _dbService.GetAsync<TestProduct>(id);
		deleted.Should().BeNull();
	}

	#endregion

	#region <=== Async Transaction Scope Tests ===>

	[Fact]
	public async Task AsyncTransactionScope_WithCompleteAsync_ShouldCommitRecords()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			var product2 = new TestProduct
			{
				Name = "Test2",
				Price = 20.00m,
				Quantity = 2,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);
			await _dbService.InsertAsync(product2);

			await scope.CompleteAsync();
		}

		var records = await _dbService.GetListAsync<TestProduct>();
		records.Count().Should().Be(2);
	}

	[Fact]
	public async Task AsyncTransactionScope_WithoutComplete_ShouldRollbackRecords()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			var product2 = new TestProduct
			{
				Name = "Test2",
				Price = 20.00m,
				Quantity = 2,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);
			await _dbService.InsertAsync(product2);

			var withinScopeRecords = await _dbService.GetListAsync<TestProduct>();
			withinScopeRecords.Count().Should().Be(2);
		}

		var records = await _dbService.GetListAsync<TestProduct>();
		records.Count().Should().Be(0);
	}

	[Fact]
	public async Task AsyncTransactionScope_NestedWithMixedComplete_ShouldRollbackAndCommitCorrectly()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			var product2 = new TestProduct
			{
				Name = "Test2",
				Price = 20.00m,
				Quantity = 2,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);
			await _dbService.InsertAsync(product2);

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);

			await using (var nestedScope = await _dbService.CreateTransactionScopeAsync())
			{
				(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);

				var product3 = new TestProduct
				{
					Name = "Test3",
					Price = 30.00m,
					Quantity = 3,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				var product4 = new TestProduct
				{
					Name = "Test4",
					Price = 40.00m,
					Quantity = 4,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product3);
				await _dbService.InsertAsync(product4);

				(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(4);

				await nestedScope.CompleteAsync();
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(4);

			await using (var nestedScope = await _dbService.CreateTransactionScopeAsync())
			{
				var product5 = new TestProduct
				{
					Name = "Test5",
					Price = 50.00m,
					Quantity = 5,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				var product6 = new TestProduct
				{
					Name = "Test6",
					Price = 60.00m,
					Quantity = 6,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product5);
				await _dbService.InsertAsync(product6);

				(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(6);
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(4);
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(0);
	}

	[Fact]
	public async Task NestedAsyncTransactionScope_InnerCompleted_OuterNotCompleted_ShouldRollbackAll()
	{
		await using (var outerScope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);

			await using (var innerScope = await _dbService.CreateTransactionScopeAsync())
			{
				var product2 = new TestProduct
				{
					Name = "Inner1",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
				await innerScope.CompleteAsync();
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(0);
	}

	[Fact]
	public async Task NestedAsyncTransactionScope_AllCompleted_ShouldCommitAll()
	{
		await using (var outerScope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);

			await using (var innerScope = await _dbService.CreateTransactionScopeAsync())
			{
				var product2 = new TestProduct
				{
					Name = "Inner1",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
				await innerScope.CompleteAsync();
			}

			await outerScope.CompleteAsync();
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);
	}

	#endregion

	#region <=== Mixed Sync/Async Transaction Scope Tests ===>

	[Fact]
	public async Task NestedMixedTransactionScope_SyncOuterAsyncInner_BothCompleted()
	{
		using (var outerScope = _dbService.CreateTransactionScope())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);

			await using (var innerScope = await _dbService.CreateTransactionScopeAsync())
			{
				var product2 = new TestProduct
				{
					Name = "Inner1",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
				await innerScope.CompleteAsync();
			}

			outerScope.Complete();
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);
	}

	[Fact]
	public async Task NestedMixedTransactionScope_AsyncOuterSyncInner_BothCompleted()
	{
		await using (var outerScope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);

			using (var innerScope = _dbService.CreateTransactionScope())
			{
				var product2 = new TestProduct
				{
					Name = "Inner1",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
				innerScope.Complete();
			}

			await outerScope.CompleteAsync();
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);
	}

	[Fact]
	public async Task NestedMixedTransactionScope_SyncOuterAsyncInner_InnerRolledBack()
	{
		using (var outerScope = _dbService.CreateTransactionScope())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);

			await using (var innerScope = await _dbService.CreateTransactionScopeAsync())
			{
				var product2 = new TestProduct
				{
					Name = "Inner1",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			outerScope.Complete();
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);
	}

	[Fact]
	public async Task NestedMixedTransactionScope_AsyncOuterSyncInner_InnerRolledBack()
	{
		await using (var outerScope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);

			using (var innerScope = _dbService.CreateTransactionScope())
			{
				var product2 = new TestProduct
				{
					Name = "Inner1",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			await outerScope.CompleteAsync();
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);
	}

	#endregion

	#region <=== Deeply Nested Transaction Tests ===>

	[Fact]
	public async Task AsyncTransactionScope_DeeplyNestedWithMixedComplete_ShouldPartiallyCommit()
	{
		await using (var level1 = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Level1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);
			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			await using (var level2 = await _dbService.CreateTransactionScopeAsync())
			{
				var product2 = new TestProduct
				{
					Name = "Level2",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
				(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);

				await using (var level3 = await _dbService.CreateTransactionScopeAsync())
				{
					var product3 = new TestProduct
					{
						Name = "Level3",
						Price = 30.00m,
						Quantity = 3,
						IsActive = true,
						CreatedAt = DateTime.UtcNow
					};

					await _dbService.InsertAsync(product3);
					(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(3);

					await level3.CompleteAsync();
				}

				(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(3);
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			await level1.CompleteAsync();
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);
	}

	#endregion

	#region <=== Advisory Lock Scope Tests ===>

	[Fact]
	public async Task AsyncAdvisoryLockScope_ConcurrentOperations_ShouldSerializeAccess()
	{
		const long LOCK_KEY = 2025;
		Guid productId = Guid.NewGuid();
		const int tasksCount = 10;

		var initialProduct = new TestProduct
		{
			Id = productId,
			Name = "Account",
			Description = "0",
			Price = 0m,
			Quantity = 0,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(initialProduct);

		Task[] tasks = new Task[tasksCount];
		for (int i = 0; i < tasksCount; i++)
		{
			tasks[i] = Task.Run(async () =>
			{
				await using (var scope = await _dbService.CreateAdvisoryLockScopeAsync(LOCK_KEY))
				{
					var product = await _dbService.GetAsync<TestProduct>(productId);

					await Task.Delay(100);

					int newQuantity = product!.Quantity + 1;
					product.Quantity = newQuantity;

					await _dbService.UpdateAsync(product, ["Quantity"]);

					await scope.CompleteAsync();
				}
			});
		}

		await Task.WhenAll(tasks);

		var finalProduct = await _dbService.GetAsync<TestProduct>(productId);
		finalProduct!.Quantity.Should().Be(tasksCount);
	}

	[Fact]
	public async Task AsyncTransactionScope_ConcurrentWithAdvisoryKey_ShouldSerializeAccess()
	{
		const long LOCK_KEY = 2026;
		Guid productId = Guid.NewGuid();
		const int tasksCount = 10;

		var initialProduct = new TestProduct
		{
			Id = productId,
			Name = "Account",
			Description = "0",
			Price = 0m,
			Quantity = 0,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(initialProduct);

		Task[] tasks = new Task[tasksCount];
		for (int i = 0; i < tasksCount; i++)
		{
			tasks[i] = Task.Run(async () =>
			{
				await using (var scope = await _dbService.CreateTransactionScopeAsync(LOCK_KEY))
				{
					var product = await _dbService.GetAsync<TestProduct>(productId);

					await Task.Delay(100);

					int newQuantity = product!.Quantity + 1;
					product.Quantity = newQuantity;

					await _dbService.UpdateAsync(product, ["Quantity"]);

					await scope.CompleteAsync();
				}
			});
		}

		await Task.WhenAll(tasks);

		var finalProduct = await _dbService.GetAsync<TestProduct>(productId);
		finalProduct!.Quantity.Should().Be(tasksCount);
	}

	[Fact]
	public async Task AsyncTransactionScope_ConcurrentWithoutAdvisoryKey_ShouldAllowRaceCondition()
	{
		Guid productId = Guid.NewGuid();
		const int tasksCount = 10;

		var initialProduct = new TestProduct
		{
			Id = productId,
			Name = "Account",
			Description = "0",
			Price = 0m,
			Quantity = 0,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		await _dbService.InsertAsync(initialProduct);

		Task[] tasks = new Task[tasksCount];
		for (int i = 0; i < tasksCount; i++)
		{
			tasks[i] = Task.Run(async () =>
			{
				await using (var scope = await _dbService.CreateTransactionScopeAsync())
				{
					var product = await _dbService.GetAsync<TestProduct>(productId);

					await Task.Delay(100);

					int newQuantity = product!.Quantity + 1;
					product.Quantity = newQuantity;

					await _dbService.UpdateAsync(product, ["Quantity"]);

					await scope.CompleteAsync();
				}
			});
		}

		await Task.WhenAll(tasks);

		var finalProduct = await _dbService.GetAsync<TestProduct>(productId);
		finalProduct!.Quantity.Should().Be(1);
	}

	#endregion

	#region <=== Explicit Rollback Tests ===>

	[Fact]
	public async Task TransactionScope_ExplicitRollback_ShouldRollbackRecords()
	{
		using (var scope = _dbService.CreateTransactionScope())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);

			var withinScopeRecords = await _dbService.GetListAsync<TestProduct>();
			withinScopeRecords.Count().Should().Be(1);

			scope.Rollback();
		}

		var records = await _dbService.GetListAsync<TestProduct>();
		records.Count().Should().Be(0);
	}

	[Fact]
	public async Task AsyncTransactionScope_ExplicitRollbackAsync_ShouldRollbackRecords()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);

			var withinScopeRecords = await _dbService.GetListAsync<TestProduct>();
			withinScopeRecords.Count().Should().Be(1);

			await scope.RollbackAsync();
		}

		var records = await _dbService.GetListAsync<TestProduct>();
		records.Count().Should().Be(0);
	}

	[Fact]
	public async Task NestedTransactionScope_InnerExplicitRollback_OuterCompleted_ShouldPartiallyCommit()
	{
		await using (var outerScope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);

			await using (var innerScope = await _dbService.CreateTransactionScopeAsync())
			{
				var product2 = new TestProduct
				{
					Name = "Inner1",
					Price = 20.00m,
					Quantity = 2,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				await _dbService.InsertAsync(product2);
				(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);

				await innerScope.RollbackAsync();
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			await outerScope.CompleteAsync();
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);
	}

	[Fact]
	public async Task TransactionScope_RollbackCalledTwice_ShouldThrowInvalidOperationException()
	{
		using (var scope = _dbService.CreateTransactionScope())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			scope.Rollback();

			Action act = () => scope.Rollback();
			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*already completed*");
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(0);
	}

	[Fact]
	public async Task TransactionScope_RollbackAsyncCalledTwice_ShouldThrowInvalidOperationException()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			await scope.RollbackAsync();

			Func<Task> act = () => scope.RollbackAsync();
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("*already completed*");
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(0);
	}

	[Fact]
	public async Task TransactionScope_CompleteAfterRollback_ShouldThrowInvalidOperationException()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			await scope.RollbackAsync();

			Func<Task> act = () => scope.CompleteAsync();
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("*already completed*");
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(0);
	}

	[Fact]
	public async Task TransactionScope_RollbackAfterComplete_ShouldThrowInvalidOperationException()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			await scope.CompleteAsync();

			Func<Task> act = () => scope.RollbackAsync();
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("*already completed*");
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);
	}

	[Fact]
	public async Task NestedTransactionScope_InnerDatabaseErrorCaughtAndRolledBack_OuterCompleted_ShouldCommit()
	{
		var productId = Guid.NewGuid();

		await using (var outerScope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Id = productId,
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);
			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			try
			{
				await using (var innerScope = await _dbService.CreateTransactionScopeAsync())
				{
					var duplicateProduct = new TestProduct
					{
						Id = productId,
						Name = "Duplicate",
						Price = 20.00m,
						Quantity = 2,
						IsActive = true,
						CreatedAt = DateTime.UtcNow
					};

					await _dbService.InsertAsync(duplicateProduct);

					await innerScope.CompleteAsync();
				}
			}
			catch (Npgsql.PostgresException)
			{
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			var product3 = new TestProduct
			{
				Name = "Outer2",
				Price = 30.00m,
				Quantity = 3,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product3);
			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);

			await outerScope.CompleteAsync();
		}

		var finalProducts = await _dbService.GetListAsync<TestProduct>();
		finalProducts.Count().Should().Be(2);
		finalProducts.Should().Contain(p => p.Name == "Outer1");
		finalProducts.Should().Contain(p => p.Name == "Outer2");
		finalProducts.Should().NotContain(p => p.Name == "Duplicate");
	}

	[Fact]
	public async Task NestedTransactionScope_InnerInvalidSqlErrorCaughtAndRolledBack_OuterCompleted_ShouldCommit()
	{
		await using (var outerScope = await _dbService.CreateTransactionScopeAsync())
		{
			var product1 = new TestProduct
			{
				Name = "Outer1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product1);
			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			try
			{
				await using (var innerScope = await _dbService.CreateTransactionScopeAsync())
				{
					var product2 = new TestProduct
					{
						Name = "Inner1",
						Price = 20.00m,
						Quantity = 2,
						IsActive = true,
						CreatedAt = DateTime.UtcNow
					};

					await _dbService.InsertAsync(product2);
					(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);

					await _dbService.ExecuteAsync("SELECT * FROM non_existent_table_xyz");

					await innerScope.CompleteAsync();
				}
			}
			catch (Npgsql.PostgresException)
			{
			}

			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);

			var product3 = new TestProduct
			{
				Name = "Outer2",
				Price = 30.00m,
				Quantity = 3,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product3);
			(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(2);

			await outerScope.CompleteAsync();
		}

		var finalProducts = await _dbService.GetListAsync<TestProduct>();
		finalProducts.Count().Should().Be(2);
		finalProducts.Should().Contain(p => p.Name == "Outer1");
		finalProducts.Should().Contain(p => p.Name == "Outer2");
		finalProducts.Should().NotContain(p => p.Name == "Inner1");
	}

	#endregion

	#region <=== Complete Called Twice Tests ===>

	[Fact]
	public async Task TransactionScope_CompleteCalledTwice_ShouldThrowInvalidOperationException()
	{
		using (var scope = _dbService.CreateTransactionScope())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			scope.Complete();

			Action act = () => scope.Complete();
			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*already completed*");
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);
	}

	[Fact]
	public async Task TransactionScope_CompleteAsyncCalledTwice_ShouldThrowInvalidOperationException()
	{
		await using (var scope = await _dbService.CreateTransactionScopeAsync())
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			await scope.CompleteAsync();

			Func<Task> act = () => scope.CompleteAsync();
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("*already completed*");
		}

		(await _dbService.GetListAsync<TestProduct>()).Count().Should().Be(1);
	}

	[Fact]
	public async Task AdvisoryLockScope_CompleteCalledTwice_ShouldThrowInvalidOperationException()
	{
		const long LOCK_KEY = 2025;

		using (var scope = _dbService.CreateAdvisoryLockScope(LOCK_KEY))
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			scope.Complete();

			Action act = () => scope.Complete();
			act.Should().Throw<InvalidOperationException>()
				.WithMessage("*already completed*");
		}
	}

	[Fact]
	public async Task AdvisoryLockScope_CompleteAsyncCalledTwice_ShouldThrowInvalidOperationException()
	{
		const long LOCK_KEY = 2025;

		await using (var scope = await _dbService.CreateAdvisoryLockScopeAsync(LOCK_KEY))
		{
			var product = new TestProduct
			{
				Name = "Test1",
				Price = 10.00m,
				Quantity = 1,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			await _dbService.InsertAsync(product);
			await scope.CompleteAsync();

			Func<Task> act = () => scope.CompleteAsync();
			await act.Should().ThrowAsync<InvalidOperationException>()
				.WithMessage("*already completed*");
		}
	}

	#endregion
}
