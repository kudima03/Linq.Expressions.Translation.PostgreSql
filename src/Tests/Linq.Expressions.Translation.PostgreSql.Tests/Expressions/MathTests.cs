namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record MathTests
{
    [Fact]
    public void TakesAbsoluteInteger()
    {
        Expression query = Orders.Select(o => Math.Abs(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesAbsoluteLong()
    {
        Expression query = Orders.Select(o => Math.Abs(o.Points)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Points")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesAbsoluteDecimal()
    {
        Expression query = Orders.Select(o => Math.Abs(o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesAbsoluteDouble()
    {
        Expression query = Orders.Select(o => Math.Abs(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesAbsoluteOfExpression()
    {
        Expression query = Orders
            .Select(o => Math.Abs(o.Quantity - o.CustomerId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Quantity" - t0."CustomerId")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesCeilingOfDecimal()
    {
        Expression query = Orders.Select(o => Math.Ceiling(o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CEILING(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesCeilingOfDouble()
    {
        Expression query = Orders.Select(o => Math.Ceiling(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CEILING(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFloorOfDecimal()
    {
        Expression query = Orders.Select(o => Math.Floor(o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FLOOR(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFloorOfDouble()
    {
        Expression query = Orders.Select(o => Math.Floor(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FLOOR(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesDecimal()
    {
        Expression query = Orders.Select(o => Math.Truncate(o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TRUNC(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesDouble()
    {
        Expression query = Orders.Select(o => Math.Truncate(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT TRUNC(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSquareRoot()
    {
        Expression query = Orders.Select(o => Math.Sqrt(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SQRT(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSquareRootOfInteger()
    {
        Expression query = Orders.Select(o => Math.Sqrt(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SQRT(CAST(t0."Quantity" AS double precision))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RaisesToPower()
    {
        Expression query = Orders.Select(o => Math.Pow(o.Weight, 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT POWER(t0."Weight", 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RaisesToPowerOfColumn()
    {
        Expression query = Orders.Select(o => Math.Pow(o.Weight, o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT POWER(t0."Weight", t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RaisesIntegerToPower()
    {
        Expression query = Orders.Select(o => Math.Pow(o.Quantity, 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT POWER(CAST(t0."Quantity" AS double precision), 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RaisesToFractionalPower()
    {
        Expression query = Orders.Select(o => Math.Pow(o.Weight, 0.5)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT POWER(t0."Weight", 0.5)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesExponential()
    {
        Expression query = Orders.Select(o => Math.Exp(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXP(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesNaturalLogarithm()
    {
        Expression query = Orders.Select(o => Math.Log(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LN(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesDecimalLogarithm()
    {
        Expression query = Orders.Select(o => Math.Log10(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LOG(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSignOfInteger()
    {
        Expression query = Orders.Select(o => Math.Sign(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(SIGN(t0."Quantity") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSignOfDecimal()
    {
        Expression query = Orders.Select(o => Math.Sign(o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(SIGN(t0."Total") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSignOfDouble()
    {
        Expression query = Orders.Select(o => Math.Sign(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(SIGN(t0."Weight") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesGreatestOfColumns()
    {
        Expression query = Orders
            .Select(o => Math.Max(o.Quantity, o.CustomerId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Quantity", t0."CustomerId")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesGreatestOfColumnAndConstant()
    {
        Expression query = Orders.Select(o => Math.Max(o.Quantity, 10)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Quantity", 10)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLeastOfColumns()
    {
        Expression query = Orders
            .Select(o => Math.Min(o.Quantity, o.CustomerId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(t0."Quantity", t0."CustomerId")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLeastOfDecimals()
    {
        Expression query = Orders.Select(o => Math.Min(o.Total, 100m)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(t0."Total", 100)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesGreatestOfDoubles()
    {
        Expression query = Orders.Select(o => Math.Max(o.Weight, 0.5)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Weight", 0.5)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesGreatestOfLongs()
    {
        Expression query = Orders.Select(o => Math.Max(o.Points, 0L)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Points", 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ClampsWithMinAndMax()
    {
        Expression query = Orders
            .Select(o => Math.Min(Math.Max(o.Quantity, 1), 10))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(GREATEST(t0."Quantity", 1), 10)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDecimalAwayFromZero()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total, MidpointRounding.AwayFromZero))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROUND(t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDecimalToDigitsAwayFromZero()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total, 2, MidpointRounding.AwayFromZero))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROUND(t0."Total", 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsDecimalToNegativeDigits()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total, -1, MidpointRounding.AwayFromZero))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROUND(t0."Total", -1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsComputedDecimal()
    {
        Expression query = Orders
            .Select(o => Math.Round(o.Total * 1.19m, 2, MidpointRounding.AwayFromZero))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROUND(t0."Total" * 1.19, 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NestsMathFunctions()
    {
        Expression query = Orders.Select(o => Math.Abs(Math.Floor(o.Weight))).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(FLOOR(t0."Weight"))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NestsMathFunctionsWithMaximum()
    {
        Expression query = Orders
            .Select(o => Math.Max(Math.Abs(o.Quantity), o.CustomerId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(ABS(t0."Quantity"), t0."CustomerId")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UsesMathFunctionInsideArithmetic()
    {
        Expression query = Orders.Select(o => Math.Abs(o.Quantity) * 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ABS(t0."Quantity") * 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UsesMathFunctionOnBothSides()
    {
        Expression query = Orders
            .Select(o => Math.Floor(o.Weight) + Math.Ceiling(o.Weight))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FLOOR(t0."Weight") + CEILING(t0."Weight")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConvertsCeilingToInteger()
    {
        Expression query = Orders.Select(o => (int)Math.Ceiling(o.Weight)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(CEILING(t0."Weight")) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesPiInMemory()
    {
        Expression query = Orders.Select(o => o.Weight * Math.PI).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" * 3.141592653589793
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesEulerNumberInMemory()
    {
        Expression query = Orders.Select(o => o.Weight * Math.E).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" * 2.718281828459045
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesConstantMathInMemory()
    {
        Expression query = Orders.Select(o => o.Weight * Math.Sqrt(16.0)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Weight" * 4
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesCapturedMathInMemory()
    {
        int offset = -5;

        Expression query = Orders.Select(o => o.Quantity + Math.Abs(offset)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" + 5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesMathFunctionNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Abs = Math.Abs(o.Quantity) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", ABS(t0."Quantity") AS "Abs"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByAbsoluteValue()
    {
        Expression query = Orders
            .Where(o => Math.Abs(o.Quantity) > 5)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE ABS(t0."Quantity") > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByRoundedValue()
    {
        Expression query = Orders
            .Where(o => Math.Round(o.Total, 0, MidpointRounding.AwayFromZero) == 10m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE ROUND(t0."Total", 0) = 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByFloor()
    {
        Expression query = Orders
            .Where(o => Math.Floor(o.Weight) == 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE FLOOR(t0."Weight") = 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByGreatest()
    {
        Expression query = Orders
            .Where(o => Math.Max(o.Quantity, o.CustomerId) > 7)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE GREATEST(t0."Quantity", t0."CustomerId") > 7
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByPower()
    {
        Expression query = Orders
            .Where(o => Math.Pow(o.Weight, 2) < 100)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE POWER(t0."Weight", 2) < 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersBySign()
    {
        Expression query = Orders
            .Where(o => Math.Sign(o.Total) == 1)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(SIGN(t0."Total") AS integer) = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByAbsoluteValue()
    {
        Expression query = Orders
            .OrderBy(o => Math.Abs(o.Quantity))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY ABS(t0."Quantity")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByFloor()
    {
        Expression query = Orders
            .GroupBy(o => Math.Floor(o.Weight))
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FLOOR(t0."Weight") AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY FLOOR(t0."Weight")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsAbsoluteValues()
    {
        Expression query = Orders.Executed(q => q.Sum(o => Math.Abs(o.Total)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(ABS(t0."Total")), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SumsRoundedValues()
    {
        Expression query = Orders.Executed(q =>
            q.Sum(o => Math.Round(o.Total, 2, MidpointRounding.AwayFromZero))
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(ROUND(t0."Total", 2)), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AveragesPowers()
    {
        Expression query = Orders.Executed(q => q.Average(o => Math.Pow(o.Weight, 2)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT AVG(POWER(t0."Weight", 2))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMaximumOfSquareRoot()
    {
        Expression query = Orders.Executed(q => q.Max(o => Math.Sqrt(o.Weight)));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(SQRT(t0."Weight"))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
