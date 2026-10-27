namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

public static class MappedTables
{
    public static IQueryable<OrderEvent> OrderEvents =>
        new CapturingQueryable<OrderEvent>();
}
