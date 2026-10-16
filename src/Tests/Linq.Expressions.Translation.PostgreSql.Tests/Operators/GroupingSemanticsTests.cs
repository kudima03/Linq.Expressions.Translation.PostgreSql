#pragma warning disable IDE0120 // the LINQ calls are the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record GroupingSemanticsTests
{
    [Fact]
    public void CountsRowsMatchingWhereAndPredicate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Paid = g.Where(o => o.IsPaid).Count(o => o.Quantity > 1),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(*) FILTER (WHERE (t0."Quantity" > 1) AND t0."IsPaid") AS integer) AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsDistinctValuesAfterWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Statuses = g.Where(o => o.IsPaid)
                    .Select(o => o.Status)
                    .Distinct()
                    .Count(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(DISTINCT t0."Status") FILTER (WHERE t0."IsPaid") AS integer) AS "Statuses"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FindsMinimumAfterWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Lowest = g.Where(o => o.IsPaid).Min(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                MIN(t0."Total") FILTER (WHERE t0."IsPaid") AS "Lowest"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsLongsAfterWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Points = g.Where(o => o.IsPaid).Sum(o => o.Points),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COALESCE(SUM(t0."Points") FILTER (WHERE t0."IsPaid"), 0) AS bigint) AS "Points"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AveragesIntegersAfterWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Mean = g.Where(o => o.IsPaid).Average(o => o.Quantity),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                AVG(CAST(t0."Quantity" AS double precision)) FILTER (WHERE t0."IsPaid") AS "Mean"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LongCountsRowsMatchingWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Paid = g.Where(o => o.IsPaid).LongCount(),
            })
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
    public void FiltersGroupsByCountPredicate()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count(o => o.IsPaid) > 1)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) FILTER (WHERE t0."IsPaid") AS integer) > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyMatchingPredicateAfterWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                HasLarge = g.Where(o => o.IsPaid).Any(o => o.Total > 100),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COALESCE(BOOL_OR(t0."Total" > 100) FILTER (WHERE t0."IsPaid"), FALSE) AS "HasLarge"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAllMatchingPredicateAfterWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                AllLarge = g.Where(o => o.IsPaid).All(o => o.Quantity > 0),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COALESCE(BOOL_AND(t0."Quantity" > 0) FILTER (WHERE t0."IsPaid"), TRUE) AS "AllLarge"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsRowsMatchingChainedWheres()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Paid = g.Where(o => o.IsPaid).Count(o => o.Quantity > 1),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COUNT(*) FILTER (WHERE (t0."Quantity" > 1) AND t0."IsPaid") AS integer) AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsRowsMatchingChainedWheres()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Paid = g.Where(o => o.IsPaid)
                    .Where(o => o.Quantity > 1)
                    .Sum(o => o.Total),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COALESCE(SUM(t0."Total") FILTER (WHERE (t0."Quantity" > 1) AND t0."IsPaid"), 0) AS "Paid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsProjectedValuesMatchingChainedWheres()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Units = g.Select(o => o.Quantity)
                    .Where(q => q > 0)
                    .Where(q => q < 10)
                    .Sum(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                CAST(COALESCE(SUM(t0."Quantity") FILTER (WHERE (t0."Quantity" < 10) AND (t0."Quantity" > 0)), 0) AS integer) AS "Units"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksGroupHasAnyRow()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, HasAny = g.Any() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", BOOL_OR(TRUE) AS "HasAny"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksGroupHasAnyRowAfterWhere()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                HasPaid = g.Where(o => o.IsPaid).Any(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId",
                COALESCE(BOOL_OR(TRUE) FILTER (WHERE t0."IsPaid"), FALSE) AS "HasPaid"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
