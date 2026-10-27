namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

[Collection(SharedDatabase.Name)]
public sealed class MappingTests(Database database)
{
    [Fact]
    public void ReadsTableNamedByAttributes()
    {
        database.AssertSameRows(MappedTables.OrderEvents.Where(e => e.OrderId != null));
    }
}
