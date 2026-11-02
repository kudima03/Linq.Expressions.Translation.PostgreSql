#pragma warning disable CA1861 // the one-element array is the root under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// A one-row root of aggregates joined to a table repeats the totals on every row, on
// either side of the join, as LINQ to Objects does.
[Collection(SharedDatabase.Name)]
public sealed class OneRowJoinSemanticsTests(Database database)
{
    private static IQueryable<int> Totals =>
        new[] { 0 }.AsQueryable().Select(_ => Orders.Count());

    private static IQueryable<decimal> Revenue =>
        new[] { 0 }.AsQueryable().Select(_ => Orders.Sum(o => o.Total));

    [Fact]
    public void RepeatsTotalsOnEveryRow()
    {
        IQueryable<int> totals = Totals;

        database.AssertSameRows(
            Customers.SelectMany(c => totals, (c, n) => new { c.Id, Orders = n })
        );
    }

    [Fact]
    public void RepeatsEveryRowOnTotals()
    {
        database.AssertSameRows(
            Totals.SelectMany(n => Customers, (n, c) => new { c.Id, Orders = n })
        );
    }

    [Fact]
    public void DividesByTotalOfEveryRow()
    {
        IQueryable<decimal> revenue = Revenue;

        database.AssertSameRows(
            Orders.SelectMany(o => revenue, (o, r) => new { o.Id, Share = o.Total / r })
        );
    }

    [Fact]
    public void MatchesTotalsOnKey()
    {
        IQueryable<int> totals = Totals;

        database.AssertSameRows(
            Customers
                .GroupJoin(totals, c => c.Id, n => n, (c, ns) => new { c, ns })
                .SelectMany(
                    x => x.ns.DefaultIfEmpty(),
                    (x, n) => new { x.c.Id, Orders = n }
                )
        );
        database.AssertSameRows(
            Totals.Join(Customers, n => n, c => c.Id, (n, c) => new { c.Id, Orders = n })
        );
    }
}
