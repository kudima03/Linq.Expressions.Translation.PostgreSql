namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The statistical aggregates of PostgreSqlFunctions give what PostgreSQL's
// definitions give, which ReferenceStatistics computes from them: over groups (also
// of one value, where the sample forms are NULL, and of values that do not vary,
// where CORR and the slope are NULL), over window frames and over queries, with
// NULL values skipped and filters applied.
[Collection(SharedDatabase.Name)]
public sealed class StatisticsSemanticsTests(Database database)
{
    [Fact]
    public void AggregatesOneColumnOfGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    StddevPop = PostgreSqlFunctions.StddevPop(
                        g,
                        o => (double?)o.Quantity
                    ),
                    StddevSamp = PostgreSqlFunctions.StddevSamp(
                        g,
                        o => (double?)o.Quantity
                    ),
                    VarPop = PostgreSqlFunctions.VarPop(g, o => (double?)o.Quantity),
                    VarSamp = PostgreSqlFunctions.VarSamp(g, o => (double?)o.Priority),
                    Exact = PostgreSqlFunctions.VarPop(g, o => (decimal?)o.Total),
                    Sample = PostgreSqlFunctions.VarSamp(g, o => o.Discount),
                })
        );
    }

    [Fact]
    public void AggregatesDecimalDeviations()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.IsPaid)
                .Select(g => new
                {
                    g.Key,
                    Population = PostgreSqlFunctions.StddevPop(
                        g,
                        o => (decimal?)o.Quantity
                    ),
                    Sample = PostgreSqlFunctions.StddevSamp(g, o => (decimal?)o.Total),
                })
        );
    }

    [Fact]
    public void AggregatesTwoColumnsOfGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.IsPaid)
                .Select(g => new
                {
                    g.Key,
                    Corr = PostgreSqlFunctions.Corr(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    CovarPop = PostgreSqlFunctions.CovarPop(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    CovarSamp = PostgreSqlFunctions.CovarSamp(
                        g,
                        o => o.Priority,
                        o => (double?)o.Id
                    ),
                    AvgX = PostgreSqlFunctions.RegrAvgX(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    AvgY = PostgreSqlFunctions.RegrAvgY(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    Count = PostgreSqlFunctions.RegrCount(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    Intercept = PostgreSqlFunctions.RegrIntercept(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    R2 = PostgreSqlFunctions.RegrR2(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    Slope = PostgreSqlFunctions.RegrSlope(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    Sxx = PostgreSqlFunctions.RegrSxx(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    Sxy = PostgreSqlFunctions.RegrSxy(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                    Syy = PostgreSqlFunctions.RegrSyy(
                        g,
                        o => o.Priority,
                        o => (double?)o.Quantity
                    ),
                })
        );
    }

    [Fact]
    public void HasNoLineThroughOnePointOrAConstantX()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Corr = PostgreSqlFunctions.Corr(
                        g,
                        o => (double?)o.Quantity,
                        o => (double?)1
                    ),
                    Slope = PostgreSqlFunctions.RegrSlope(
                        g,
                        o => (double?)o.Quantity,
                        o => (double?)o.CustomerId
                    ),
                    R2 = PostgreSqlFunctions.RegrR2(
                        g,
                        o => (double?)1,
                        o => (double?)o.Id
                    ),
                    CovarSamp = PostgreSqlFunctions.CovarSamp(
                        g,
                        o => (double?)o.Quantity,
                        o => (double?)o.Id
                    ),
                    Count = PostgreSqlFunctions.RegrCount(
                        g,
                        o => o.Priority,
                        o => (double?)o.Id
                    ),
                })
        );
    }

    [Fact]
    public void FiltersTheRowsOfAGroup()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId % 2)
                .Select(g => new
                {
                    g.Key,
                    Paid = PostgreSqlFunctions.VarPop(
                        g.Where(o => o.IsPaid),
                        o => (double?)o.Quantity
                    ),
                    None = PostgreSqlFunctions.VarPop(
                        g.Where(o => o.Id > 100),
                        o => (double?)o.Quantity
                    ),
                    Count = PostgreSqlFunctions.RegrCount(
                        g.Where(o => o.Quantity > 1),
                        o => (double?)o.Quantity,
                        o => (double?)o.Id
                    ),
                })
        );
    }

    [Fact]
    public void AggregatesTheRowsOfWindowFrames()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders,
                o => o.IsPaid,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-2, 0),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Deviation = PostgreSqlFunctions.StddevSamp(
                            w.Rows,
                            x => (double?)x.Quantity
                        ),
                        Variance = PostgreSqlFunctions.VarPop(
                            w.Rows.Where(x => x.Priority != null),
                            x => (decimal?)x.Total
                        ),
                        Corr = PostgreSqlFunctions.Corr(
                            w.Rows,
                            x => (double?)x.Quantity,
                            x => (double?)x.Id
                        ),
                        Count = PostgreSqlFunctions.RegrCount(
                            w.Rows,
                            x => x.Priority,
                            x => (double?)x.Id
                        ),
                    }
            )
        );
    }

    [Fact]
    public void AggregatesTheRowsOfAQuery()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Variance = PostgreSqlFunctions.VarPop(
                    Orders.Where(o => o.CustomerId == c.Id),
                    o => (double?)o.Quantity
                ),
                Slope = PostgreSqlFunctions.RegrSlope(
                    Orders,
                    o => (double?)o.Quantity,
                    o => (double?)o.Id
                ),
            })
        );
    }
}
