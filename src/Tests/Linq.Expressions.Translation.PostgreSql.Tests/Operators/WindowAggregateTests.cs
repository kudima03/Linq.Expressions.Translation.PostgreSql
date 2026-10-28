namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// An aggregate over the rows a projection reads is a window aggregate over them,
// computed in the pass that reads the rows, not a scalar subquery that reads the
// sources again.
public sealed record WindowAggregateTests
{
    [Fact]
    public void CountsTheRowsProjectedAsWindow()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, long>(o.Id, Orders.LongCount()))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", COUNT(*) OVER () AS "item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsTheFilteredRowsProjectedAsWindow()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .Select(o => new ValueTuple<int, int>(
                o.Id,
                Orders.Where(x => x.IsPaid).Count()
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", CAST(COUNT(*) OVER () AS integer) AS "item2"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsAndAveragesTheRowsProjectedAsWindows()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Total,
                Sum = Orders.Sum(x => x.Total),
                Mean = Orders.Average(x => x.Quantity),
                Least = Orders.Min(x => x.Priority),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total", COALESCE(SUM(t0."Total") OVER (), 0) AS "Sum", AVG(CAST(t0."Quantity" AS double precision)) OVER () AS "Mean", MIN(t0."Priority") OVER () AS "Least"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersTheWindowWithThePredicate()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, int, decimal>(
                o.Id,
                Orders.Count(x => x.IsPaid),
                Orders.Where(x => x.IsPaid).Where(x => x.Quantity > 1).Sum(x => x.Total)
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", CAST(COUNT(*) FILTER (WHERE t0."IsPaid") OVER () AS integer) AS "item2", COALESCE(SUM(t0."Total") FILTER (WHERE t0."IsPaid" AND (t0."Quantity" > 1)) OVER (), 0) AS "item3"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void BoundsTheSelectedValuesAsWindow()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, decimal>(
                o.Id,
                Orders.Select(x => x.Total).Max()
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", MAX(t0."Total") OVER () AS "item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QuantifiesTheRowsProjectedAsWindows()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, bool, bool, bool>(
                o.Id,
                Orders.Any(x => x.IsPaid),
                Orders.All(x => x.IsPaid),
                Orders.Any()
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", COALESCE(BOOL_OR(t0."IsPaid") OVER (), FALSE) AS "item2", COALESCE(BOOL_AND(t0."IsPaid") OVER (), TRUE) AS "item3", TRUE AS "item4"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsTheOrderBesideTheWindow()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Select(o => new ValueTuple<int, long>(o.Id, Orders.LongCount()))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", COUNT(*) OVER () AS "item2"
                FROM "Order" AS t0
                ORDER BY t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsTheJoinedRowsProjectedAsWindow()
    {
        IQueryable<CustomerSummary> joined = Customers.Join(
            Orders,
            c => c.Id,
            o => o.CustomerId,
            (c, o) => new CustomerSummary(c.Id, c.Name)
        );
        Expression query = joined
            .Select(j => new ValueTuple<string, long>(j.Name, joined.LongCount()))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name" AS "item1", COUNT(*) OVER () AS "item2"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsOtherRowsAsSubquery()
    {
        Expression query = Orders
            .Where(o => o.IsPaid)
            .Select(o => new ValueTuple<int, long>(o.Id, Orders.LongCount()))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", (
                    SELECT COUNT(*)
                    FROM "Order" AS t1
                ) AS "item2"
                FROM "Order" AS t0
                WHERE t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsCorrelatedAggregateAsSubquery()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, int>(o.Id, Orders.Count(x => x.Id > o.Id)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                    WHERE t1."Id" > t0."Id"
                ) AS "item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsPagedRowsAsSubquery()
    {
        Expression query = Orders
            .OrderBy(o => o.Id)
            .Take(5)
            .Select(o => new ValueTuple<int, long>(
                o.Id,
                Orders.OrderBy(x => x.Id).Take(5).LongCount()
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", (
                    SELECT COUNT(*)
                    FROM (
                        SELECT t1."Id", t1."CustomerId", t1."Number", t1."Note", t1."Total", t1."Discount", t1."Quantity", t1."Priority", t1."Points", t1."Weight", t1."IsPaid", t1."IsGift", t1."Status", t1."Options", t1."CreatedAt", t1."PaidAt", t1."ShippedOn", t1."DeliveryTime", t1."TrackingId", t1."Tags"
                        FROM "Order" AS t1
                        ORDER BY t1."Id"
                        LIMIT 5
                    ) AS t2
                ) AS "item2"
                FROM "Order" AS t0
                ORDER BY t0."Id"
                LIMIT 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsDistinctValuesAsSubquery()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, int>(
                o.Id,
                Orders.Select(x => x.Priority).Distinct().Count()
            ))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (
                        SELECT DISTINCT t1."Priority"
                        FROM "Order" AS t1
                    ) AS t2
                ) AS "item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsTheWindowedProjectionForAFilter()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Count = Orders.LongCount() })
            .Where(x => x.Count > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Count"
                FROM (
                    SELECT t0."Id", COUNT(*) OVER () AS "Count"
                    FROM "Order" AS t0
                ) AS t1
                WHERE t1."Count" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsTheWindowedProjectionForAnAggregate()
    {
        Expression query = Orders
            .Select(o => o.Total / Orders.Sum(x => x.Total))
            .Executed(q => q.Max());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t1."Value")
                FROM (
                    SELECT t0."Total" / COALESCE(SUM(t0."Total") OVER (), 0) AS "Value"
                    FROM "Order" AS t0
                ) AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesAndOrdersByTheWindowedProjection()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Share = o.Total / Orders.Sum(x => x.Total) })
            .OrderByDescending(x => x.Share)
            .Skip(1)
            .Take(2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Total" / COALESCE(SUM(t0."Total") OVER (), 0) AS "Share"
                FROM "Order" AS t0
                ORDER BY t0."Total" / COALESCE(SUM(t0."Total") OVER (), 0) DESC
                LIMIT 2
                OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesRequiredValuesWithEquals()
    {
        Expression query = Orders
            .Where(o => o.Priority!.Value == o.Quantity && (int)o.Priority == 2)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" = t0."Quantity") AND (t0."Priority" = 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsOnRequiredTupleKeysWithEquals()
    {
        Expression query = Orders
            .Join(
                Orders,
                a => new ValueTuple<int>(a.Priority!.Value),
                b => new ValueTuple<int>((int)b.Priority!),
                (a, b) => new ValueTuple<int, int>(a.Id, b.Id)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", t1."Id" AS "item2"
                FROM "Order" AS t0
                INNER JOIN "Order" AS t1 ON t0."Priority" = t1."Priority"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
