namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Values the translation reads through, run against PostgreSQL: an aggregate of a
// value of the outer row alone, the aggregates of groups read by a window function
// over the groups, and distinct Utc timestamps in their order.
[Collection(SharedDatabase.Name)]
public sealed class ComposedValueSemanticsTests(Database database)
{
    [Fact]
    public void AggregatesValuesOfTheOuterRow()
    {
        database.AssertSameRows(
            Orders
                .Where(o => Customers.Any(c => c.Id == o.CustomerId))
                .Select(o => new
                {
                    o.Id,
                    Tagged = Customers
                        .Where(c => c.Id == o.CustomerId)
                        .Max(c => o.Tags.Contains("gift") ? 1 : 0),
                    Listed = Customers
                        .Where(c => c.Id == o.CustomerId)
                        .Max(c =>
                            Orders.Where(x => x.IsPaid).Select(x => x.Id).Contains(o.Id)
                                ? 1
                                : 0
                        ),
                    Moment = Customers
                        .Where(c => c.Id == o.CustomerId)
                        .Max(c => new DateTime(
                            2024,
                            1,
                            1,
                            o.Quantity % 24,
                            0,
                            0,
                            DateTimeKind.Utc
                        )),
                })
        );
    }

    [Fact]
    public void ReadsAggregatesOfGroupsInWindowFunctions()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        First = w.Lag(
                            x => x.OrderBy(o => o.Id).Select(o => o.Number).First(),
                            1,
                            null
                        ),
                        Joined = w.Lag(
                            x =>
                                string.Join(
                                    ",",
                                    x.OrderBy(o => o.Id).Select(o => o.Number)
                                ),
                            1,
                            null
                        ),
                        Distinct = w.Lag(
                            x =>
                                string.Join(
                                    ",",
                                    x.OrderBy(o => o.Id)
                                        .Where(o => o.IsPaid)
                                        .Select(o => o.Note)
                                        .Distinct()
                                ),
                            1,
                            null
                        ),
                    }
            )
        );
    }

    [Fact]
    public void OrdersDistinctUtcTimestamps()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => new DateTime(
                    2024,
                    1,
                    1,
                    o.Quantity % 24,
                    0,
                    0,
                    DateTimeKind.Utc
                ))
                .Select(o => new DateTime(
                    2024,
                    1,
                    1,
                    o.Quantity % 24,
                    0,
                    0,
                    DateTimeKind.Utc
                ))
                .Distinct()
        );
    }
}
