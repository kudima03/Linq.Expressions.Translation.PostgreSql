#pragma warning disable IDE0004 // the casts are the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record ConversionTests
{
    [Fact]
    public void CastsIntegerToLong()
    {
        Expression query = Orders.Select(o => (long)o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsIntegerToDecimal()
    {
        Expression query = Orders.Select(o => (decimal)o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsIntegerToDouble()
    {
        Expression query = Orders.Select(o => (double)o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsIntegerToFloat()
    {
        Expression query = Orders.Select(o => (float)o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS real)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsIntegerToShort()
    {
        Expression query = Orders.Select(o => (short)o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS smallint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsLongToInteger()
    {
        Expression query = Orders.Select(o => (int)o.Points).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Points" AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsLongToDouble()
    {
        Expression query = Orders.Select(o => (double)o.Points).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Points" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsLongToDecimal()
    {
        Expression query = Orders.Select(o => (decimal)o.Points).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Points" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsDecimalToDouble()
    {
        Expression query = Orders.Select(o => (double)o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Total" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsDoubleToDecimal()
    {
        Expression query = Orders.Select(o => (decimal)o.Weight).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Weight" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsDoubleToFloat()
    {
        Expression query = Orders.Select(o => (float)o.Weight).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Weight" AS real)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesDoubleWhenCastingToInteger()
    {
        Expression query = Orders.Select(o => (int)o.Weight).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(t0."Weight") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesDoubleWhenCastingToLong()
    {
        Expression query = Orders.Select(o => (long)o.Weight).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(t0."Weight") AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesDecimalWhenCastingToInteger()
    {
        Expression query = Orders.Select(o => (int)o.Total).Expression;

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
    public void TruncatesDecimalWhenCastingToLong()
    {
        Expression query = Orders.Select(o => (long)o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(t0."Total") AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesNullableDecimalWhenCastingToNullableInteger()
    {
        Expression query = Orders.Select(o => (int?)o.Discount).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(t0."Discount") AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsNullableIntegerToNullableLong()
    {
        Expression query = Orders.Select(o => (long?)o.Priority).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Priority" AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsNullableIntegerToNullableDouble()
    {
        Expression query = Orders.Select(o => (double?)o.Priority).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Priority" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IgnoresEnumToUnderlyingConversion()
    {
        Expression query = Orders.Select(o => (int)o.Status).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Status"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IgnoresUnderlyingToEnumConversion()
    {
        Expression query = Orders.Select(o => (OrderStatus)o.Quantity).Expression;

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

    [Fact]
    public void IgnoresFlagsEnumToIntegerConversion()
    {
        Expression query = Orders.Select(o => (int)o.Options).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Options"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsEnumToLong()
    {
        Expression query = Orders.Select(o => (long)o.Status).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Status" AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsEnumToDecimal()
    {
        Expression query = Orders.Select(o => (decimal)o.Status).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Status" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IgnoresIdentityConversion()
    {
        Expression query = Orders.Select(o => (int)o.Quantity).Expression;

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

    [Fact]
    public void CastsInsideArithmetic()
    {
        Expression query = Orders.Select(o => (long)o.Quantity * o.Points).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS bigint) * t0."Points"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsBeforeDivisionToKeepFraction()
    {
        Expression query = Orders
            .Select(o => (double)o.Quantity / o.CustomerId)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS double precision) / CAST(t0."CustomerId" AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsOperandWhenAddingToDecimal()
    {
        Expression query = Orders.Select(o => (decimal)o.Quantity + o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS numeric) + t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsComputedExpression()
    {
        Expression query = Orders.Select(o => (long)(o.Quantity + 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" + 1 AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsCoalescedValue()
    {
        Expression query = Orders.Select(o => (long)(o.Priority ?? 0)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COALESCE(t0."Priority", 0) AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsConditionalValue()
    {
        Expression query = Orders
            .Select(o => (double)(o.IsPaid ? o.Quantity : 0))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(CASE WHEN t0."IsPaid" THEN t0."Quantity" ELSE 0 END AS double precision)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CastsNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, AsLong = (long)o.Quantity })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", CAST(t0."Quantity" AS bigint) AS "AsLong"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCastColumn()
    {
        Expression query = Orders
            .Where(o => (long)o.Quantity > o.Points)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(t0."Quantity" AS bigint) > t0."Points"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByCastColumnToDecimal()
    {
        Expression query = Orders
            .Where(o => (decimal)o.Quantity > o.Total)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(t0."Quantity" AS numeric) > t0."Total"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByTruncatedColumn()
    {
        Expression query = Orders
            .Where(o => (int)o.Weight == 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CAST(TRUNC(t0."Weight") AS integer) = 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEnumCastToInteger()
    {
        Expression query = Orders
            .Where(o => (int)o.Status == 2)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" = 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByIntegerCastToEnum()
    {
        Expression query = Orders
            .Where(o => (OrderStatus)o.Quantity == OrderStatus.Shipped)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" = 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByEnumCastFromCapturedInteger()
    {
        int code = 3;

        Expression query = Orders
            .Where(o => o.Status == (OrderStatus)code)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" = 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByCastColumn()
    {
        Expression query = Orders
            .OrderBy(o => (decimal)o.Quantity)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY CAST(t0."Quantity" AS numeric)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByCastColumn()
    {
        Expression query = Orders
            .GroupBy(o => (long)o.Quantity)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS bigint) AS "Key",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY CAST(t0."Quantity" AS bigint)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SumsCastColumn()
    {
        Expression query = Orders.Executed(q => q.Sum(o => (decimal)o.Quantity));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(SUM(CAST(t0."Quantity" AS numeric)), 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
