#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// PostgreSqlWindows.Over composed with other operators and with subqueries: the
// window of a source that a SELECT cannot extend is computed over a derived table of
// it, the window of a subquery over the rows of that subquery, and the arguments of
// the window functions are the expressions of the current row, captured values
// inlined. What cannot be computed is refused with NotSupportedException.
public sealed record WindowCompositionTests
{
    [Fact]
    public void ReadsACapturedAndAnInlineFrameInASubquery()
    {
        PostgreSqlFrame frame = PostgreSqlFrame.Rows(-1, 1);
        Expression query = Customers
            .Select(c => new
            {
                c.Id,
                Widest = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        frame,
                        (o, w) => (int?)w.Rows.Count()
                    )
                    .Max(),
                Inline = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        PostgreSqlFrame.Rows(0, 0),
                        (o, w) => (int?)w.Rows.Count()
                    )
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT CAST(COUNT(*) OVER (ORDER BY t1."Id" ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING) AS integer) AS "Value"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) AS "Widest", (
                    SELECT MAX(t4."Value")
                    FROM (
                        SELECT CAST(COUNT(*) OVER (ORDER BY t3."Id" ROWS BETWEEN CURRENT ROW AND CURRENT ROW) AS integer) AS "Value"
                        FROM "Order" AS t3
                        WHERE t3."CustomerId" = t0."Id"
                    ) AS t4
                ) AS "Inline"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PartitionsByAComparisonWithTheOuterRow()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Id,
                Count = Orders
                    .Over(
                        o => o.CustomerId == c.Id,
                        rows => rows.OrderBy(o => o.Id),
                        null,
                        (o, w) => (long?)w.RowNumber()
                    )
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT ROW_NUMBER() OVER (PARTITION BY t1."CustomerId" = t0."Id" ORDER BY t1."Id") AS "Value"
                        FROM "Order" AS t1
                    ) AS t2
                ) AS "Count"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PartitionsAndOrdersBySubqueries()
    {
        Expression query = Trees
            .Over(
                Orders,
                o =>
                    Customers
                        .Where(c => c.Id == o.CustomerId)
                        .Select(c => c.Region)
                        .FirstOrDefault(),
                rows =>
                    rows.OrderBy(o => Customers.Count(c => c.Id == o.CustomerId))
                        .ThenBy(o => o.Id),
                null,
                (o, w) => w.RowNumber()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROW_NUMBER() OVER (PARTITION BY (
                    SELECT t1."Region"
                    FROM "Customer" AS t1
                    WHERE t1."Id" = t0."CustomerId"
                    LIMIT 1
                ) ORDER BY (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Customer" AS t2
                    WHERE t2."Id" = t0."CustomerId"
                ), t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PartitionsByEveryColumnOfAnEntity()
    {
        Expression query = Trees
            .Over(Customers, c => c, null, null, (c, w) => w.Rows.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (PARTITION BY t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive") AS integer)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComputesTheWindowOverInMemoryValues()
    {
        Expression scalars = Trees
            .Over(
                new[] { 5, 3 }.AsQueryable(),
                null,
                rows => rows.Order(),
                null,
                (v, w) => new { v, N = w.RowNumber() }
            )
            .Expression;
        Expression tuples = Trees
            .Over(
                new (int, string?)[] { (1, "a"), (2, null) }.AsQueryable(),
                r => r.Item2,
                rows => rows.OrderByDescending(r => r.Item1),
                null,
                (r, w) => new { r.Item1, Previous = w.Lag(x => x.Item2, 1, "it's") }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value" AS "v", ROW_NUMBER() OVER (ORDER BY t0."Value") AS "N"
                FROM (VALUES (5), (3)) AS t0 ("Value")
                """
            ),
            new PostgreSqlQuery(scalars).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item1", LAG(t0."Item2", 1, 'it''s') OVER (PARTITION BY t0."Item2" ORDER BY t0."Item1" DESC) AS "Previous"
                FROM (VALUES (1, CAST('a' AS text)), (2, NULL)) AS t0 ("Item1", "Item2")
                """
            ),
            new PostgreSqlQuery(tuples).TextValue
        );
    }

    [Fact]
    public void RefusesAWindowOverAnInMemorySourceWithoutRows()
    {
        Expression one = Trees
            .Over(
                new[] { 5 }.AsQueryable(),
                null,
                null,
                null,
                (v, w) => new { v, N = w.RowNumber() }
            )
            .Expression;
        Expression none = Trees
            .Over(
                Array.Empty<int>().AsQueryable(),
                null,
                rows => rows.Order(),
                null,
                (v, w) => new { v, N = w.RowNumber() }
            )
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(one).TextValue
        );
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(none).TextValue
        );
    }

    [Fact]
    public void WrapsASetOperation()
    {
        Expression query = Trees
            .Over(
                Orders.Select(o => o.CustomerId).Concat(Customers.Select(c => c.Id)),
                null,
                rows => rows.Order(),
                null,
                (id, w) => new { Id = id, Rank = w.Rank() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."CustomerId" AS "Id", RANK() OVER (ORDER BY t2."CustomerId") AS "Rank"
                FROM (
                    SELECT t0."CustomerId"
                    FROM "Order" AS t0
                    UNION ALL
                    SELECT t1."Id"
                    FROM "Customer" AS t1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RanksTheGroupsLeftByHaving()
    {
        Expression query = Trees
            .Over(
                Orders.GroupBy(o => o.CustomerId).Where(g => g.Count() > 1),
                null,
                rows => rows.OrderByDescending(g => g.Count()),
                null,
                (g, w) => new { g.Key, Rank = w.Rank() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", RANK() OVER (ORDER BY CAST(COUNT(*) AS integer) DESC) AS "Rank"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) AS integer) > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsAGroupAggregateInAnotherRow()
    {
        Expression query = Trees
            .Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderBy(g => g.Key),
                null,
                (g, w) => w.FirstValue(x => x.Count())
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FIRST_VALUE(CAST(COUNT(*) AS integer)) OVER (ORDER BY t0."CustomerId")
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsAPagedWindowedSourceAndKeepsItsOrder()
    {
        Expression query = Trees
            .Over(
                Trees
                    .Over(
                        Orders,
                        null,
                        rows => rows.OrderBy(o => o.Total),
                        null,
                        (o, w) => new { o.Id, Number = w.RowNumber() }
                    )
                    .OrderBy(x => x.Number)
                    .Skip(1),
                null,
                rows => rows.OrderByDescending(x => x.Id),
                null,
                (x, w) =>
                    new
                    {
                        x.Id,
                        x.Number,
                        Second = w.RowNumber(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Number", ROW_NUMBER() OVER (ORDER BY t1."Id" DESC) AS "Second"
                FROM (
                    SELECT t0."Id", ROW_NUMBER() OVER (ORDER BY t0."Total") AS "Number"
                    FROM "Order" AS t0
                    ORDER BY ROW_NUMBER() OVER (ORDER BY t0."Total")
                    OFFSET 1
                ) AS t1
                ORDER BY t1."Number"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsAWindowWithoutFunctionsForAFilter()
    {
        Expression query = Trees
            .Over(Orders, null, null, null, (o, w) => o.Id)
            .Where(id => id > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id"
                FROM (SELECT t0."Id" FROM "Order" AS t0) AS t1
                WHERE t1."Id" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesTheWindowInADerivedTable()
    {
        IQueryable<long> numbers = Trees.Over(
            Orders,
            o => o.CustomerId,
            rows => rows.OrderBy(o => o.Id),
            null,
            (o, w) => w.RowNumber()
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(t1."Value"), 0) AS bigint)
                FROM (
                    SELECT ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id") AS "Value"
                    FROM "Order" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(numbers.Executed(q => q.Sum())).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT DISTINCT ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id") AS "Value"
                    FROM "Order" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(numbers.Distinct().Executed(q => q.Count())).TextValue
        );
    }

    [Fact]
    public void ComputesAWindowOfASubqueryInTheSelector()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Inner = Orders
                            .Where(x => x.CustomerId == o.CustomerId)
                            .Over(
                                null,
                                rows => rows.OrderBy(x => x.Id),
                                null,
                                (x, v) => (long?)v.RowNumber()
                            )
                            .Max(),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id") AS "Number", (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY t1."Id") AS "Value"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."CustomerId"
                    ) AS t2
                ) AS "Inner"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsAWindowAggregateBesideAWindowOfASubquery()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Id,
                Count = Orders.Count(),
                Last = Orders
                    .Over(
                        null,
                        rows => rows.OrderBy(x => x.Id),
                        null,
                        (x, w) => (long?)w.RowNumber()
                    )
                    .Max(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", CAST(COUNT(*) OVER () AS integer) AS "Count", (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT ROW_NUMBER() OVER (ORDER BY t1."Id") AS "Value"
                        FROM "Order" AS t1
                    ) AS t2
                ) AS "Last"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsWindowAggregatesOfTheSelectorForAFilter()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                null,
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        A = Orders.Count(),
                        B = Orders.Where(x => x.IsPaid).Sum(x => x.Total),
                    }
            )
            .Where(x => x.A > 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."A", t1."B"
                FROM (
                    SELECT t0."Id", CAST(COUNT(*) OVER () AS integer) AS "A", COALESCE(SUM(t0."Total") FILTER (WHERE t0."IsPaid") OVER (), 0) AS "B"
                    FROM "Order" AS t0
                ) AS t1
                WHERE t1."A" > 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComposesWindowFunctionsInConditionalsAndNullableComparisons()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        Label = w.RowNumber() == 1 ? "first" : "other",
                        Higher = o.Priority > w.Lag(x => x.Priority, 1, null),
                        Different = w.Lag(x => x.Priority, 1, null) != o.Priority,
                        Fallback = w.Lag(x => x.Priority, 1, null) ?? -1,
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN ROW_NUMBER() OVER (ORDER BY t0."Id") = 1 THEN 'first' ELSE 'other' END AS "Label", COALESCE(t0."Priority" > LAG(t0."Priority", 1) OVER (ORDER BY t0."Id"), FALSE) AS "Higher", LAG(t0."Priority", 1) OVER (ORDER BY t0."Id") IS DISTINCT FROM t0."Priority" AS "Different", COALESCE(LAG(t0."Priority", 1) OVER (ORDER BY t0."Id"), -1) AS "Fallback"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InlinesCapturedArgumentsOfWindowFunctions()
    {
        int buckets = 4;
        int offset = 2;
        string fallback = "it's";
        decimal factor = 1.5m;
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) =>
                    new
                    {
                        Bucket = w.Ntile(buckets),
                        Back = w.Lag(x => x.Id, offset, -offset),
                        Note = w.Lead(x => x.Note, offset, fallback),
                        Scaled = w.Lag(x => x.Total * factor, 1, 0m),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NTILE(4) OVER (ORDER BY t0."Id") AS "Bucket", LAG(t0."Id", 2, -2) OVER (ORDER BY t0."Id") AS "Back", LEAD(t0."Note", 2, 'it''s') OVER (ORDER BY t0."Id") AS "Note", LAG(t0."Total" * 1.5, 1, 0) OVER (ORDER BY t0."Id") AS "Scaled"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsAnArrayInAnotherRow()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => w.Lag(x => x.Tags, 1, null)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LAG(t0."Tags", 1) OVER (ORDER BY t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Func<Expression>> Refused =>
        [
            // A window function of no Over.
            () =>
                Orders.Select(o => PostgreSqlWindows.RowNumber<Order>(null!)).Expression,
            // An entity or a projection read in another row.
            () =>
                Trees
                    .Over(
                        Orders,
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        null,
                        (o, w) => w.Lag(x => x, 1, null)
                    )
                    .Expression,
            () =>
                Trees
                    .Over(
                        Orders,
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        null,
                        (o, w) => w.FirstValue(x => new { x.Id, x.Total })
                    )
                    .Expression,
            // The window of the outer Over in the selector of an inner one.
            () =>
                Trees
                    .Over(
                        Orders,
                        null,
                        rows => rows.OrderBy(o => o.Id),
                        null,
                        (o, w) =>
                            Orders.Over(null, null, null, (x, v) => w.RowNumber()).Max()
                    )
                    .Expression,
            // A comparer other than StringComparer.Ordinal, and an order of something
            // else than the rows.
            () =>
                Trees
                    .Over(
                        Orders,
                        null,
                        rows =>
                            rows.OrderBy(o => o.Id)
                                .ThenBy(o => o.Total, Comparer<decimal>.Default),
                        null,
                        (o, w) => w.RowNumber()
                    )
                    .Expression,
            () =>
                Trees
                    .Over(
                        Orders,
                        null,
                        rows => rows.Reverse().OrderBy(o => o.Id),
                        null,
                        (o, w) => w.RowNumber()
                    )
                    .Expression,
            // A window over the elements of a group.
            () =>
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g =>
                        g.AsQueryable()
                            .Over(
                                null,
                                rows => rows.OrderBy(o => o.Id),
                                null,
                                (o, w) => w.RowNumber()
                            )
                            .Max()
                    )
                    .Expression,
        ];

    [Theory]
    [MemberData(nameof(Refused))]
    public void RefusesWhatCannotBeComputed(Func<Expression> query)
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );
    }
}
