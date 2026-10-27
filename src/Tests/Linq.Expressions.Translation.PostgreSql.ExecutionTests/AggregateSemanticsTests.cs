namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

[Collection(SharedDatabase.Name)]
public sealed class AggregateSemanticsTests(Database database)
{
    [Fact]
    public void CountsAndSumsNoRowsAsZero()
    {
        IQueryable<Order> none = Orders.Where(o => o.Id < 0);

        database.AssertSameResult(none, q => q.Count());
        database.AssertSameResult(none, q => q.LongCount(o => o.IsPaid));
        database.AssertSameResult(none, q => q.Sum(o => o.Total));
        database.AssertSameResult(none, q => q.Sum(o => o.Priority));
    }

    [Fact]
    public void SumsAllNullsAsZero()
    {
        IQueryable<Order> unprioritized = Orders.Where(o => o.Priority == null);

        database.AssertSameResult(unprioritized, q => q.Sum(o => o.Priority));
        database.AssertSameResult(unprioritized, q => q.Count(o => o.Priority != null));
    }

    [Fact]
    public void AveragesAndBoundsNoValuesAsNull()
    {
        IQueryable<Order> unprioritized = Orders.Where(o => o.Priority == null);

        database.AssertSameResult(unprioritized, q => q.Average(o => o.Priority));
        database.AssertSameResult(unprioritized, q => q.Min(o => o.Priority));
        database.AssertSameResult(unprioritized, q => q.Max(o => o.Priority));
        database.AssertSameResult(
            Orders.Where(o => o.Id < 0),
            q => q.Max(o => o.Discount)
        );
    }

    [Fact]
    public void FailsToAverageOrBoundNoRows()
    {
        IQueryable<Order> none = Orders.Where(o => o.Id < 0);

        database.AssertSameResult(none, q => q.Average(o => o.Total));
        database.AssertSameResult(none, q => q.Min(o => o.Quantity));
        database.AssertSameResult(none, q => q.Max(o => o.CreatedAt));
    }

    [Fact]
    public void AggregatesDefaultValueOfNoRows()
    {
        IQueryable<int> none = Orders.Where(o => o.Id < 0).Select(o => o.Quantity);

        database.AssertSameResult(none, q => q.DefaultIfEmpty().Max());
        database.AssertSameResult(none, q => q.DefaultIfEmpty().Count());
        database.AssertSameResult(none, q => q.DefaultIfEmpty().Average());
    }

    [Fact]
    public void AggregatesEmptyAndNullOnlyGroups()
    {
        database.AssertSameSequence(
            Customers
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    Count = Orders.Count(o => o.CustomerId == c.Id),
                    Total = Orders.Where(o => o.CustomerId == c.Id).Sum(o => o.Total),
                    Priority = Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Sum(o => o.Priority),
                    Average = Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Average(o => o.Priority),
                    Largest = Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Max(o => (decimal?)o.Total),
                })
        );
    }

    [Fact]
    public void AggregatesGroupsOfNulls()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Priority = g.Sum(o => o.Priority),
                    Average = g.Average(o => o.Priority),
                    Discount = g.Max(o => o.Discount),
                    Prioritized = g.Count(o => o.Priority != null),
                })
        );
    }
}
