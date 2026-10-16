#pragma warning disable IDE0120 // the LINQ calls are the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record AdditionalGroupingSemanticsTests
{
    [Fact]
    public void SumsConditionalQuantity()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Units = g.Sum(o => o.IsPaid ? o.Quantity : 0),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COALESCE(SUM(CASE WHEN t0."IsPaid" THEN t0."Quantity" ELSE 0 END), 0) AS integer) AS "Units"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsQuantityAfterWhereAndSelect()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Units = g.Where(o => o.IsPaid).Select(o => o.Quantity).Sum(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COALESCE(SUM(t0."Quantity") FILTER (WHERE t0."IsPaid"), 0) AS integer) AS "Units"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsQuantityFilteredAfterProjection()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Units = g.Select(o => o.Quantity).Where(q => q > 1).Sum(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COALESCE(SUM(t0."Quantity") FILTER (WHERE t0."Quantity" > 1), 0) AS integer) AS "Units"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LongCountsMatchingPredicate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Paid = g.LongCount(o => o.IsPaid) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COUNT(*) FILTER (WHERE t0."IsPaid") AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersGroupsByAny()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Any(o => o.IsPaid))
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING BOOL_OR(t0."IsPaid")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersGroupsByAll()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.All(o => o.IsPaid))
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING BOOL_AND(t0."IsPaid")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FindsMaximumOfElementSelector()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Quantity)
            .Select(g => new { CustomerId = g.Key, Peak = g.Max() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", MAX(t0."Quantity") AS "Peak"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesElementSelector()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Quantity)
            .Select(g => new { CustomerId = g.Key, Mean = g.Average() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                AVG(CAST(t0."Quantity" AS double precision)) AS "Mean"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsPaidAndUnpaidCounts()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Total = g.Count(o => o.IsPaid) + g.Count(o => !o.IsPaid),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(*) FILTER (WHERE t0."IsPaid") AS integer) + CAST(COUNT(*) FILTER (WHERE NOT t0."IsPaid") AS integer) AS "Total"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDistinctValuesMatchingBothFilters()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Statuses = g.Where(o => o.IsPaid)
                    .Select(o => o.Status)
                    .Distinct()
                    .Where(s => s != OrderStatus.Cancelled)
                    .Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(DISTINCT t0."Status") FILTER (WHERE (t0."Status" <> 3) AND t0."IsPaid") AS integer) AS "Statuses"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsAnyOfNoFilteredRowAsFalse()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                HasPaidLarge = g.Where(o => o.Total > 100).Any(o => o.IsPaid),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COALESCE(BOOL_OR(t0."IsPaid") FILTER (WHERE t0."Total" > 100), FALSE) AS "HasPaidLarge"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsAllOfNoFilteredRowAsTrue()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                AllPaidLarge = g.Where(o => o.Total > 100).All(o => o.IsPaid),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COALESCE(BOOL_AND(t0."IsPaid") FILTER (WHERE t0."Total" > 100), TRUE) AS "AllPaidLarge"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
