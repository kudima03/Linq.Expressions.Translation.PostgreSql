namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// Every test class of the collection shares one database, started once.
[CollectionDefinition(Name)]
public sealed class SharedDatabase : ICollectionFixture<Database>
{
    public const string Name = "PostgreSQL";
}
