#pragma warning disable IDE0004 // the casts and checked operators are the subject
#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record CheckedArithmeticSemanticsTests
{
    [Fact]
    public void NegatesCheckedInteger()
    {
        Expression query = Orders.Select(o => checked(-o.Quantity)).Expression;

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

    [Fact]
    public void NegatesCheckedDecimal()
    {
        Expression query = Orders.Select(o => checked(-o.Total)).Expression;

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

    [Fact]
    public void NegatesCheckedNullableDecimal()
    {
        Expression query = Orders.Select(o => checked(-o.Discount)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -t0."Discount"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AddsCheckedLongs()
    {
        Expression query = Orders.Select(o => checked(o.Points + 1L)).Expression;

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

    [Fact]
    public void TruncatesCheckedDecimalToInteger()
    {
        Expression query = Orders.Select(o => checked((int)o.Total)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(t0."Total") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsUnaryPlusOfNullableDecimal()
    {
        Expression query = Orders.Select(o => +o.Discount).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Discount"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "MultiplyChecked is not translated.")]
    public void MultipliesCheckedIntegers()
    {
        Expression query = Orders.Select(o => checked(o.Quantity * 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" * 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "SubtractChecked is not translated.")]
    public void SubtractsCheckedIntegers()
    {
        Expression query = Orders.Select(o => checked(o.Quantity - 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" - 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DividesCheckedIntegers()
    {
        Expression query = Orders.Select(o => checked(o.Quantity / 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" / 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MultipliesCheckedDecimals()
    {
        Expression query = Orders.Select(o => checked(o.Total * 2m)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" * 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DividesCheckedDecimals()
    {
        Expression query = Orders.Select(o => checked(o.Total / 2m)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" / 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComplementsLong()
    {
        Expression query = Orders.Select(o => ~o.Points).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~t0."Points"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsLongRight()
    {
        Expression query = Orders.Select(o => o.Points >> 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" >> 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void XorsLongWithConstant()
    {
        Expression query = Orders.Select(o => o.Points ^ 1L).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" # 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
