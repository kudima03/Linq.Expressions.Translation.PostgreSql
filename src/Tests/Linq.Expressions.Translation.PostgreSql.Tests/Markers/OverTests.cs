namespace Linq.Expressions.Translation.PostgreSql.Tests.Markers;

// PostgreSqlWindows.Over is the projection of a SELECT whose window functions share
// one window: its PARTITION BY, ORDER BY and frame. Windows are computed after WHERE,
// joins and GROUP BY and before DISTINCT, ORDER BY and paging, so a paged, distinct
// or windowed source is wrapped, as is the result for a filter, a grouping, a join
// or an aggregate.
public sealed record OverTests
{
    [Fact]
    public void ComputesAWindowOverEveryRow()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                null,
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", ROW_NUMBER() OVER () AS "Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PartitionsByOneKey()
    {
        Expression query = Trees
            .Over(Orders, o => o.CustomerId, null, null, (o, w) => w.Rows.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (PARTITION BY t0."CustomerId") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PartitionsByTheMembersOfATupleOrAnObject()
    {
        Expression tuple = Trees
            .Over(
                Orders,
                o => new ValueTuple<int, bool>(o.CustomerId, o.IsPaid),
                null,
                null,
                (o, w) => w.Rows.Count()
            )
            .Expression;
        Expression anonymous = Trees
            .Over(
                Orders,
                o => new { o.CustomerId, Day = o.CreatedAt.Date },
                null,
                null,
                (o, w) => w.Rows.Count()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (PARTITION BY t0."CustomerId", t0."IsPaid") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(tuple).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (PARTITION BY t0."CustomerId", DATE_TRUNC('day', t0."CreatedAt")) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(anonymous).TextValue
        );
    }

    [Fact]
    public void DropsConstantPartitionKeys()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => new { One = 1, o.CustomerId },
                null,
                null,
                (o, w) => w.Rows.Count()
            )
            .Expression;
        Expression constant = Trees
            .Over(Orders, o => 1, null, null, (o, w) => w.Rows.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER (PARTITION BY t0."CustomerId") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) OVER () AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(constant).TextValue
        );
    }

    [Fact]
    public void OrdersByKeysInBothDirections()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows =>
                    rows.OrderByDescending(o => o.Total)
                        .ThenBy(o => o.Priority)
                        .ThenByDescending(o => o.Discount)
                        .ThenBy(o => o.Id),
                null,
                (o, w) => w.RowNumber()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROW_NUMBER() OVER (ORDER BY t0."Total" DESC, t0."Priority" NULLS FIRST, t0."Discount" DESC NULLS LAST, t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByATupleKeyAndByCodePoints()
    {
        Expression query = Trees
            .Over(
                Customers,
                null,
                rows =>
                    rows.OrderBy(c => new ValueTuple<string, int>(c.Region, c.Id))
                        .ThenByDescending(c => c.Name, StringComparer.Ordinal),
                null,
                (c, w) => w.RowNumber()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROW_NUMBER() OVER (ORDER BY t0."Region", t0."Id", t0."Name" COLLATE "C" DESC)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersScalarRowsByThemselves()
    {
        Expression ascending = Trees
            .Over(
                Orders.Select(o => o.Priority),
                null,
                rows => rows.Order(),
                null,
                (p, w) => w.Rank()
            )
            .Expression;
        Expression descending = Trees
            .Over(
                Customers.Select(c => c.Name),
                null,
                rows => rows.OrderDescending(StringComparer.Ordinal),
                null,
                (n, w) => w.Rank()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT RANK() OVER (ORDER BY t0."Priority" NULLS FIRST)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(ascending).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT RANK() OVER (ORDER BY t0."Name" COLLATE "C" DESC)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(descending).TextValue
        );
    }

    [Fact]
    public void DropsConstantOrderKeys()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => 0).ThenBy(o => o.Id),
                null,
                (o, w) => w.RowNumber()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROW_NUMBER() OVER (ORDER BY t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SharesTheWindowBetweenFunctions()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderBy(o => o.CreatedAt),
                PostgreSqlFrame.Rows(-1, 1),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Number = w.RowNumber(),
                        Moving = w.Rows.Average(x => x.Total),
                        Previous = w.Lag(x => x.Number),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."CreatedAt" ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING) AS "Number", AVG(t0."Total") OVER (PARTITION BY t0."CustomerId" ORDER BY t0."CreatedAt" ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING) AS "Moving", LAG(t0."Number") OVER (PARTITION BY t0."CustomerId" ORDER BY t0."CreatedAt" ROWS BETWEEN 1 PRECEDING AND 1 FOLLOWING) AS "Previous"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsTheCurrentRowBesideTheWindow()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                null,
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Share = o.Total / w.Rows.Sum(x => x.Total),
                        Above = o.Quantity > w.Rows.Average(x => x.Quantity),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Total" / COALESCE(SUM(t0."Total") OVER (PARTITION BY t0."CustomerId"), 0) AS "Share", CAST(t0."Quantity" AS double precision) > AVG(CAST(t0."Quantity" AS double precision)) OVER (PARTITION BY t0."CustomerId") AS "Above"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsAWindowAggregateOverEveryRowBesideTheWindow()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                null,
                null,
                (o, w) =>
                    new
                    {
                        o.Id,
                        Customer = w.Rows.Sum(x => x.Total),
                        All = Orders.Sum(x => x.Total),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(SUM(t0."Total") OVER (PARTITION BY t0."CustomerId"), 0) AS "Customer", COALESCE(SUM(t0."Total") OVER (), 0) AS "All"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExtendsAFilteredAndOrderedSource()
    {
        Expression query = Trees
            .Over(
                Orders.Where(o => o.IsPaid).OrderBy(o => o.Id),
                null,
                rows => rows.OrderByDescending(o => o.Total),
                null,
                (o, w) => new { o.Id, Rank = w.Rank() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", RANK() OVER (ORDER BY t0."Total" DESC) AS "Rank"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                ORDER BY t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsAPagedSource()
    {
        Expression query = Trees
            .Over(
                Orders.OrderBy(o => o.Id).Skip(2).Take(5),
                null,
                rows => rows.OrderBy(o => o.Total),
                null,
                (o, w) => new { o.Id, Rank = w.Rank() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", RANK() OVER (ORDER BY t1."Total") AS "Rank"
                FROM (
                    SELECT t0."Id", t0."CustomerId", t0."Number", t0."Note", t0."Total", t0."Discount", t0."Quantity", t0."Priority", t0."Points", t0."Weight", t0."IsPaid", t0."IsGift", t0."Status", t0."Options", t0."CreatedAt", t0."PaidAt", t0."ShippedOn", t0."DeliveryTime", t0."TrackingId", t0."Tags"
                    FROM "Order" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                    OFFSET 2
                ) AS t1
                ORDER BY t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsADistinctSource()
    {
        Expression query = Trees
            .Over(
                Orders.Select(o => o.CustomerId).Distinct(),
                null,
                rows => rows.Order(),
                null,
                (id, w) => new { Id = id, Number = w.RowNumber() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."CustomerId" AS "Id", ROW_NUMBER() OVER (ORDER BY t1."CustomerId") AS "Number"
                FROM (SELECT DISTINCT t0."CustomerId" FROM "Order" AS t0) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsAWindowedSource()
    {
        Expression query = Trees
            .Over(
                Trees.Over(
                    Orders,
                    o => o.CustomerId,
                    rows => rows.OrderBy(o => o.Id),
                    null,
                    (o, w) => new { o.Id, Number = w.RowNumber() }
                ),
                x => x.Number,
                null,
                null,
                (x, w) => new { x.Id, Count = w.Rows.Count() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", CAST(COUNT(*) OVER (PARTITION BY t1."Number") AS integer) AS "Count"
                FROM (
                    SELECT t0."Id", ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id") AS "Number"
                    FROM "Order" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsTheWindowForAFilter()
    {
        Expression query = Trees
            .Over(
                Orders,
                o => o.CustomerId,
                rows => rows.OrderByDescending(o => o.Total),
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
            .Where(x => x.Number <= 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Number"
                FROM (
                    SELECT t0."Id", ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Total" DESC) AS "Number"
                    FROM "Order" AS t0
                ) AS t1
                WHERE t1."Number" <= 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExtendsTheWindowWithAProjectionOrderAndPaging()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Total),
                null,
                (o, w) => new { o.Id, Rank = w.Rank() }
            )
            .Select(x => new { x.Id, Next = x.Rank + 1 })
            .OrderByDescending(x => x.Next)
            .Take(3)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", RANK() OVER (ORDER BY t0."Total") + 1 AS "Next"
                FROM "Order" AS t0
                ORDER BY RANK() OVER (ORDER BY t0."Total") + 1 DESC
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExtendsTheWindowWithDistinct()
    {
        Expression query = Trees
            .Over(Orders, o => o.CustomerId, null, null, (o, w) => w.Rows.Count())
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT CAST(COUNT(*) OVER (PARTITION BY t0."CustomerId") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsTheWindowForAGroupingAJoinAndAnAggregate()
    {
        IQueryable<OrderRank> ranked = Trees.Over(
            Orders,
            o => o.CustomerId,
            rows => rows.OrderBy(o => o.Id),
            null,
            (o, w) => new OrderRank(o.Id, o.CustomerId, w.RowNumber())
        );
        Expression grouped = ranked
            .GroupBy(x => x.Rank)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;
        Expression joined = ranked
            .Join(
                Customers,
                x => x.CustomerId,
                c => c.Id,
                (x, c) => new { c.Name, x.Rank }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Rank" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM (
                    SELECT t0."Id", t0."CustomerId", ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id") AS "Rank"
                    FROM "Order" AS t0
                ) AS t1
                GROUP BY t1."Rank"
                """
            ),
            new PostgreSqlQuery(grouped).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name", t1."Rank"
                FROM (
                    SELECT t0."Id", t0."CustomerId", ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id") AS "Rank"
                    FROM "Order" AS t0
                ) AS t1
                INNER JOIN "Customer" AS t2 ON t1."CustomerId" = t2."Id"
                """
            ),
            new PostgreSqlQuery(joined).TextValue
        );
        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t1."Rank")
                FROM (
                    SELECT t0."Id", t0."CustomerId", ROW_NUMBER() OVER (PARTITION BY t0."CustomerId" ORDER BY t0."Id") AS "Rank"
                    FROM "Order" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(ranked.Executed(q => q.Max(x => x.Rank))).TextValue
        );
    }

    [Fact]
    public void ComputesTheWindowOverJoinedRows()
    {
        Expression query = Trees
            .Over(
                Orders.Join(
                    Customers,
                    o => o.CustomerId,
                    c => c.Id,
                    (o, c) =>
                        new
                        {
                            o.Id,
                            o.Total,
                            c.Region,
                        }
                ),
                x => x.Region,
                rows => rows.OrderBy(x => x.Total),
                null,
                (x, w) => new { x.Id, Running = w.Rows.Sum(r => r.Total) }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(SUM(t0."Total") OVER (PARTITION BY t1."Region" ORDER BY t0."Total"), 0) AS "Running"
                FROM "Order" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComputesTheWindowOverTheGroupsOfItsSelect()
    {
        Expression query = Trees
            .Over(
                Orders.GroupBy(o => o.CustomerId),
                null,
                rows => rows.OrderByDescending(g => g.Sum(o => o.Total)),
                null,
                (g, w) =>
                    new
                    {
                        g.Key,
                        Rank = w.Rank(),
                        Running = w.Rows.Sum(x => x.Count()),
                    }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId" AS "Key", RANK() OVER (ORDER BY COALESCE(SUM(t0."Total"), 0) DESC) AS "Rank", CAST(COALESCE(SUM(CAST(COUNT(*) AS integer)) OVER (ORDER BY COALESCE(SUM(t0."Total"), 0) DESC), 0) AS integer) AS "Running"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsAProjectionOfGroups()
    {
        Expression query = Trees
            .Over(
                Orders
                    .GroupBy(o => o.CustomerId)
                    .Select(g => new { g.Key, Total = g.Sum(o => o.Total) }),
                null,
                rows => rows.OrderByDescending(g => g.Total),
                null,
                (g, w) => new { g.Key, Rank = w.DenseRank() }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Key", DENSE_RANK() OVER (ORDER BY t1."Total" DESC) AS "Rank"
                FROM (
                    SELECT t0."CustomerId" AS "Key", COALESCE(SUM(t0."Total"), 0) AS "Total"
                    FROM "Order" AS t0
                    GROUP BY t0."CustomerId"
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComputesAWindowInACorrelatedSubquery()
    {
        Expression query = Customers
            .Select(c => new
            {
                c.Id,
                Latest = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderBy(o => o.CreatedAt),
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
                        SELECT ROW_NUMBER() OVER (ORDER BY t1."CreatedAt") AS "Value"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                ) AS "Latest"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersOnAWindowInASubquery()
    {
        Expression query = Customers
            .Where(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Over(
                        null,
                        rows => rows.OrderByDescending(o => o.Total),
                        null,
                        (o, w) => new { o.IsPaid, Number = w.RowNumber() }
                    )
                    .Any(x => x.Number == 1 && x.IsPaid)
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t1."IsPaid", ROW_NUMBER() OVER (ORDER BY t1."Total" DESC) AS "Number"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                    WHERE (t2."Number" = 1) AND t2."IsPaid"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesWindowedQueries()
    {
        Expression query = Trees
            .Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                null,
                (o, w) => w.RowNumber()
            )
            .Concat(
                Trees.Over(
                    Customers,
                    null,
                    rows => rows.OrderBy(c => c.Id),
                    null,
                    (c, w) => w.RowNumber()
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROW_NUMBER() OVER (ORDER BY t0."Id")
                FROM "Order" AS t0
                UNION ALL
                SELECT ROW_NUMBER() OVER (ORDER BY t1."Id")
                FROM "Customer" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public sealed record OrderRank(int Id, int CustomerId, long Rank);
}
