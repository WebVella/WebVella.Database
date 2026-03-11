using Xunit;

namespace WebVella.Database.Tests.Fixtures;

/// <summary>
/// Collection definition for database integration tests.
/// Tests in this collection share the same <see cref="DatabaseFixture"/> instance.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
