namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record ConditionalTests
{
    [Fact]
    public void SelectsConditionalOnBooleanColumn()
    {
        Expression query = Orders.Select(o => o.IsPaid ? 1 : 0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOnComparison()
    {
        Expression query = Orders
            .Select(o => o.Quantity > 10 ? "big" : "small")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Quantity" > 10 THEN 'big' ELSE 'small' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalBetweenColumns()
    {
        Expression query = Orders
            .Select(o => o.IsPaid ? o.Total : o.Total * 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN t0."Total" ELSE t0."Total" * 2 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOnNegation()
    {
        Expression query = Orders.Select(o => !o.IsPaid ? "open" : "closed").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN NOT t0."IsPaid" THEN 'open' ELSE 'closed' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOnConjunction()
    {
        Expression query = Orders
            .Select(o => (o.IsPaid && (o.Quantity > 1)) ? 1 : 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" AND (t0."Quantity" > 1) THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalWithNullBranch()
    {
        Expression query = Orders.Select(o => o.IsPaid ? o.Discount : null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN t0."Discount" ELSE NULL END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalWithNullFirstBranch()
    {
        Expression query = Orders.Select(o => o.IsPaid ? null : o.Discount).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN NULL ELSE t0."Discount" END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalWithNullableCondition()
    {
        Expression query = Orders.Select(o => o.Priority > 3 ? "high" : "low").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Priority" > 3 THEN 'high' ELSE 'low' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalWithNullSafeCondition()
    {
        Expression query = Orders
            .Select(o => o.Priority == 3 ? "three" : "other")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Priority" IS NOT DISTINCT FROM 3 THEN 'three' ELSE 'other' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensConditionalInElseBranch()
    {
        Expression query = Orders
            .Select(o => o.Quantity > 10 ? "big" : (o.Quantity > 5 ? "medium" : "small"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Quantity" > 10 THEN 'big' WHEN t0."Quantity" > 5 THEN 'medium' ELSE 'small' END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensThreeLevelsOfConditionalsInElseBranch()
    {
        Expression query = Orders
            .Select(o =>
                o.Quantity > 100 ? 4 : (o.Quantity > 10 ? 3 : (o.Quantity > 5 ? 2 : 1))
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Quantity" > 100 THEN 4 WHEN t0."Quantity" > 10 THEN 3 WHEN t0."Quantity" > 5 THEN 2 ELSE 1 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NestsConditionalInThenBranch()
    {
        Expression query = Orders
            .Select(o => o.IsPaid ? (o.Quantity > 5 ? 2 : 1) : 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN CASE WHEN t0."Quantity" > 5 THEN 2 ELSE 1 END ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NestsConditionalInCondition()
    {
        Expression query = Orders
            .Select(o => (o.IsPaid ? o.Quantity > 5 : o.Quantity > 1) ? 1 : 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN CASE WHEN t0."IsPaid" THEN t0."Quantity" > 5 ELSE t0."Quantity" > 1 END THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UsesConditionalInsideArithmetic()
    {
        Expression query = Orders.Select(o => (o.IsPaid ? o.Total : 0m) * 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN t0."Total" ELSE 0 END * 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UsesArithmeticInsideConditionalBranches()
    {
        Expression query = Orders
            .Select(o => o.IsPaid ? o.Total * 2 : o.Total / 2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN t0."Total" * 2 ELSE t0."Total" / 2 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Size = o.Quantity > 10 ? "big" : "small" })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id",
                CASE WHEN t0."Quantity" > 10 THEN 'big' ELSE 'small' END AS "Size"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOnCapturedVariable()
    {
        int limit = 5;

        Expression query = Orders.Select(o => o.Quantity > limit ? 1 : 0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Quantity" > 5 THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsBooleanConditional()
    {
        Expression query = Orders
            .Select(o => o.IsPaid ? o.Quantity > 1 : o.Quantity > 5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN t0."Quantity" > 1 ELSE t0."Quantity" > 5 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOfEnum()
    {
        Expression query = Orders
            .Select(o => o.IsPaid ? OrderStatus.Paid : OrderStatus.Created)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByConditionalValue()
    {
        Expression query = Orders
            .Where(o => (o.IsPaid ? o.Total : 0m) > 100m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE WHEN t0."IsPaid" THEN t0."Total" ELSE 0 END > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByBooleanConditional()
    {
        Expression query = Orders
            .Where(o => o.IsPaid ? o.Quantity > 1 : o.Quantity > 5)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE WHEN t0."IsPaid" THEN t0."Quantity" > 1 ELSE t0."Quantity" > 5 END
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByConditionalEquality()
    {
        Expression query = Orders
            .Where(o => (o.Quantity > 10 ? "big" : "small") == "big")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE WHEN t0."Quantity" > 10 THEN 'big' ELSE 'small' END = 'big'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByConditional()
    {
        Expression query = Orders
            .OrderBy(o => o.IsPaid ? 0 : 1)
            .ThenBy(o => o.Id)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY CASE WHEN t0."IsPaid" THEN 0 ELSE 1 END, t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByConditional()
    {
        Expression query = Orders
            .GroupBy(o => o.Quantity > 10 ? "big" : "small")
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Quantity" > 10 THEN 'big' ELSE 'small' END AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY CASE WHEN t0."Quantity" > 10 THEN 'big' ELSE 'small' END
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsConditionalValue()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.IsPaid ? o.Total : 0m));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(CASE WHEN t0."IsPaid" THEN t0."Total" ELSE 0 END),
                0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsWithConditionalOneZeroSum()
    {
        Expression query = Orders.Executed(q => q.Sum(o => o.IsPaid ? 1 : 0));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(SUM(CASE WHEN t0."IsPaid" THEN 1 ELSE 0 END),
                0) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
