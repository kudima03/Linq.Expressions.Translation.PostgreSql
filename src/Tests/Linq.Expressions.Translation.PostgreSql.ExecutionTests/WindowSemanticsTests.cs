namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// An aggregate over the rows a projection reads, a window aggregate in SQL, gives
// every row the aggregate of all of them, as LINQ to Objects does.
[Collection(SharedDatabase.Name)]
public sealed class WindowSemanticsTests(Database database)
{
    [Fact]
    public void AggregatesTheRowsProjected()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.IsPaid)
                .Select(o => new
                {
                    o.Id,
                    Count = Orders.Where(x => x.IsPaid).Count(),
                    Total = Orders.Where(x => x.IsPaid).Sum(x => x.Total),
                    Top = Orders.Where(x => x.IsPaid).Max(x => x.Priority),
                    Mean = Orders.Where(x => x.IsPaid).Average(x => x.Quantity),
                    Prioritized = Orders
                        .Where(x => x.IsPaid)
                        .Count(x => x.Priority != null),
                    Gifts = Orders.Where(x => x.IsPaid).Any(x => x.IsGift == true),
                    Noted = Orders.Where(x => x.IsPaid).All(x => x.Note != null),
                })
        );
    }

    [Fact]
    public void FiltersOrdersAndPagesByTheWindow()
    {
        database.AssertSameSequence(
            Orders
                .Select(o => new { o.Id, Share = o.Total / Orders.Sum(x => x.Total) })
                .Where(x => x.Share > 0)
                .OrderByDescending(x => x.Share)
                .ThenBy(x => x.Id)
                .Skip(1)
                .Take(2)
        );
    }

    [Fact]
    public void AggregatesTheWindowedProjection()
    {
        database.AssertSameResult(
            Orders.Select(o => o.Total / Orders.Sum(x => x.Total)),
            q => q.Max()
        );
    }

    [Fact]
    public void ComparesRequiredValuesAsTheirValues()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.Priority != null && o.Priority.Value == 2)
                .Select(o => o.Id)
        );
        database.AssertSameRows(
            Orders
                .Where(a => a.Priority != null)
                .Join(
                    Orders.Where(b => b.Priority != null),
                    a => new ValueTuple<int>(a.Priority!.Value),
                    b => new ValueTuple<int>((int)b.Priority!),
                    (a, b) => new { First = a.Id, Second = b.Id }
                )
        );
    }
}
