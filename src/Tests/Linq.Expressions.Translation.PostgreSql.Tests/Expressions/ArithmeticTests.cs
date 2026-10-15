namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record ArithmeticTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void AddsIntegers()
    {
        Expression query = Orders.Select(o => o.Quantity + o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SubtractsIntegers()
    {
        Expression query = Orders.Select(o => o.Quantity - o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" - t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void MultipliesIntegers()
    {
        Expression query = Orders.Select(o => o.Quantity * o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" * t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void DividesIntegersWithTruncation()
    {
        Expression query = Orders.Select(o => o.Quantity / o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" / t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void TakesIntegerRemainder()
    {
        Expression query = Orders.Select(o => o.Quantity % o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" % t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AddsConstantToColumn()
    {
        Expression query = Orders.Select(o => o.Quantity + 10).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + 10
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AddsColumnToConstant()
    {
        Expression query = Orders.Select(o => 10 + o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 10 + t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void DividesDecimals()
    {
        Expression query = Orders.Select(o => o.Total / o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" / t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void DividesDecimalByConstant()
    {
        Expression query = Orders.Select(o => o.Total / 3m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" / 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void MultipliesDecimalByFractionalConstant()
    {
        Expression query = Orders.Select(o => o.Total * 1.19m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" * 1.19
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void DividesDoubles()
    {
        Expression query = Orders.Select(o => o.Weight / 2.5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" / 2.5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AddsLongs()
    {
        Expression query = Orders.Select(o => o.Points + 1L).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void NegatesInteger()
    {
        Expression query = Orders.Select(o => -o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void NegatesDecimal()
    {
        Expression query = Orders.Select(o => -o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void NegatesParenthesizedSum()
    {
        Expression query = Orders.Select(o => -(o.Quantity + 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -(t0."Quantity" + 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void IgnoresUnaryPlus()
    {
        Expression query = Orders.Select(o => +o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AddsCheckedIntegers()
    {
        Expression query = Orders.Select(o => checked(o.Quantity + 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ParenthesizesSumInsideProduct()
    {
        Expression query = Orders
            .Select(o => (o.Quantity + o.CustomerId) * o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Quantity" + t0."CustomerId") * t0."Id"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ParenthesizesProductInsideSum()
    {
        Expression query = Orders
            .Select(o => o.Quantity + (o.CustomerId * o.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + (t0."CustomerId" * t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ParenthesizesLeftNestedOperatorOfSamePrecedence()
    {
        Expression query = Orders
            .Select(o => o.Quantity + o.CustomerId + o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Quantity" + t0."CustomerId") + t0."Id"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ParenthesizesRightNestedSubtraction()
    {
        Expression query = Orders
            .Select(o => o.Quantity - (o.CustomerId - o.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" - (t0."CustomerId" - t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ParenthesizesNestedDivision()
    {
        Expression query = Orders
            .Select(o => o.Quantity / (o.CustomerId / o.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" / (t0."CustomerId" / t0."Id")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void NestsFourOperators()
    {
        Expression query = Orders
            .Select(o => (o.Quantity + 1) * (o.CustomerId - 2) / (o.Id % 3))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ((t0."Quantity" + 1) * (t0."CustomerId" - 2)) / (t0."Id" % 3)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EvaluatesConstantSubtreeInMemory()
    {
        Expression query = Orders.Select(o => o.Quantity + (2 * 3)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + 6
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EvaluatesConstantSubtreeOnTheLeftInMemory()
    {
        Expression query = Orders.Select(o => (2 * 3) + o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 6 + t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EvaluatesCapturedArithmeticInMemory()
    {
        int factor = 3;

        Expression query = Orders.Select(o => o.Quantity * (factor + 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" * 4
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EvaluatesNegatedCapturedVariableInMemory()
    {
        int factor = 3;

        Expression query = Orders.Select(o => o.Quantity + -factor).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + -3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void MultipliesByCapturedVariable()
    {
        int factor = 3;

        Expression query = Orders.Select(o => o.Quantity * factor).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" * 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CastsIntegerOperandWhenMultiplyingDecimal()
    {
        Expression query = Orders.Select(o => o.Total * o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" * CAST(t0."Quantity" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CastsIntegerOperandWhenDividingDecimal()
    {
        Expression query = Orders.Select(o => o.Total / o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" / CAST(t0."Quantity" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CastsIntegerOperandWhenMultiplyingDouble()
    {
        Expression query = Orders.Select(o => o.Weight * o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" * CAST(t0."Quantity" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CastsIntegerOperandWhenAddingToLong()
    {
        Expression query = Orders.Select(o => o.Points + o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" + CAST(t0."Quantity" AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void PropagatesNullThroughNullableOperand()
    {
        Expression query = Orders.Select(o => o.Priority + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void PropagatesNullThroughTwoNullableOperands()
    {
        Expression query = Orders.Select(o => o.Priority * o.Priority).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" * t0."Priority"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void PropagatesNullThroughNullableDecimal()
    {
        Expression query = Orders.Select(o => o.Discount + o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Discount" + t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SubtractsNullableDecimalFromDecimal()
    {
        Expression query = Orders.Select(o => o.Total - o.Discount).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" - t0."Discount"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void MultipliesNullableByCastInteger()
    {
        Expression query = Orders.Select(o => o.Discount * o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Discount" * CAST(t0."Quantity" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsArithmeticNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Doubled = o.Quantity * 2 })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Quantity" * 2 AS "Doubled"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByArithmeticOnColumns()
    {
        Expression query = Orders
            .Where(o => (o.Quantity * 2) > o.CustomerId)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" * 2) > t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByRemainder()
    {
        Expression query = Orders
            .Where(o => (o.Id % 2) == 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Id" % 2) = 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByDecimalProduct()
    {
        Expression query = Orders
            .Where(o => (o.Total * o.Quantity) > 100m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Total" * CAST(t0."Quantity" AS numeric)) > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNegatedColumn()
    {
        Expression query = Orders
            .Where(o => -o.Quantity < -3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE -t0."Quantity" < -3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByArithmeticOnBothSides()
    {
        Expression query = Orders
            .Where(o => (o.Quantity + 1) == (o.CustomerId - 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" + 1) = (t0."CustomerId" - 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
