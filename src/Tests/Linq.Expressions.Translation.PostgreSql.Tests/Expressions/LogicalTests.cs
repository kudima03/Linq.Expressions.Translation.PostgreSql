namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record LogicalTests
{
    [Fact]
    public void CombinesWithConditionalAnd()
    {
        Expression query = Orders
            .Where(o => o.IsPaid && (o.Quantity > 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" AND (t0."Quantity" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesWithConditionalOr()
    {
        Expression query = Orders
            .Where(o => o.IsPaid || (o.Quantity > 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" OR (t0."Quantity" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesWithNonShortCircuitAnd()
    {
        Expression query = Orders
            .Where(o => o.IsPaid & (o.Quantity > 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" AND (t0."Quantity" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesWithNonShortCircuitOr()
    {
        Expression query = Orders
            .Where(o => o.IsPaid | (o.Quantity > 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" OR (t0."Quantity" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesWithExclusiveOr()
    {
        Expression query = Orders
            .Where(o => o.IsPaid ^ (o.Quantity > 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" <> (t0."Quantity" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesBooleanColumn()
    {
        Expression query = Orders.Where(o => !o.IsPaid).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesComparison()
    {
        Expression query = Orders
            .Where(o => !(o.Quantity > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Quantity" > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesEquality()
    {
        Expression query = Orders
            .Where(o => !(o.Quantity == 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Quantity" = 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesDisjunction()
    {
        Expression query = Orders
            .Where(o => !(o.IsPaid || (o.Quantity > 1)))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."IsPaid" OR (t0."Quantity" > 1))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesNegation()
    {
        Expression query = Orders.Where(o => !!o.IsPaid).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT NOT t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesBothOperandsOfConjunction()
    {
        Expression query = Orders
            .Where(o => !o.IsPaid && !(o.Quantity > 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT t0."IsPaid" AND NOT (t0."Quantity" > 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensChainOfConjunctions()
    {
        Expression query = Orders
            .Where(o => o.IsPaid && (o.Quantity > 1) && (o.Id > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" AND (t0."Quantity" > 1) AND (t0."Id" > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensChainOfDisjunctions()
    {
        Expression query = Orders
            .Where(o => o.IsPaid || (o.Quantity > 1) || (o.Id > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" OR (t0."Quantity" > 1) OR (t0."Id" > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesDisjunctionInsideConjunction()
    {
        Expression query = Orders
            .Where(o => (o.IsPaid || (o.Quantity > 1)) && (o.Id > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."IsPaid" OR (t0."Quantity" > 1)) AND (t0."Id" > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesConjunctionInsideDisjunction()
    {
        Expression query = Orders
            .Where(o => (o.IsPaid && (o.Quantity > 1)) || (o.Id > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."IsPaid" AND (t0."Quantity" > 1)) OR (t0."Id" > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesBothSidesOfMixedOperators()
    {
        Expression query = Orders
            .Where(o => (o.IsPaid && (o.Quantity > 1)) || (!o.IsPaid && (o.Quantity > 5)))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."IsPaid" AND (t0."Quantity" > 1)) OR (NOT t0."IsPaid" AND (t0."Quantity" > 5))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NestsThreeLevelsOfLogic()
    {
        Expression query = Orders
            .Where(o =>
                (o.IsPaid && ((o.Quantity > 1) || (o.Id > 3)))
                || ((o.Total > 10) && !o.IsPaid)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."IsPaid" AND ((t0."Quantity" > 1) OR (t0."Id" > 3))) OR ((t0."Total" > 10) AND NOT t0."IsPaid")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesBooleanColumns()
    {
        Expression query = Orders
            .Where(o => o.IsPaid && o.IsGift == true)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsPaid" AND (t0."IsGift" IS NOT DISTINCT FROM TRUE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConjunctionAsValue()
    {
        Expression query = Orders.Select(o => o.IsPaid && (o.Quantity > 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."IsPaid" AND (t0."Quantity" > 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNegationAsValue()
    {
        Expression query = Orders.Select(o => !o.IsPaid).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NOT t0."IsPaid"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsBooleanColumnAsValue()
    {
        Expression query = Orders.Select(o => o.IsPaid).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."IsPaid"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsLogicNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Flag = o.IsPaid || (o.Quantity > 1) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."IsPaid" OR (t0."Quantity" > 1) AS "Flag"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersWithLogicInSeparateWhereCalls()
    {
        Expression query = Orders
            .Where(o => o.IsPaid || (o.Quantity > 1))
            .Where(o => !o.IsPaid || (o.Id > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."IsPaid" OR (t0."Quantity" > 1)) AND (NOT t0."IsPaid" OR (t0."Id" > 3))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByLiftedConjunctionWithNullsFirst()
    {
        Expression query = Orders
            .OrderBy(o => o.IsGift & o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."IsGift" AND t0."IsPaid" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByLiftedDisjunctionDescendingWithNullsLast()
    {
        Expression query = Orders
            .OrderByDescending(o => o.IsPaid | o.IsGift)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY t0."IsPaid" OR t0."IsGift" DESC NULLS LAST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
