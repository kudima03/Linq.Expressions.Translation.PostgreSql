namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record BitwiseTests
{
    [Fact]
    public void AndsIntegers()
    {
        Expression query = Orders.Select(o => o.Quantity & 3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" & 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrsIntegers()
    {
        Expression query = Orders.Select(o => o.Quantity | 3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" | 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void XorsIntegers()
    {
        Expression query = Orders.Select(o => o.Quantity ^ 3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" # 3
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComplementsInteger()
    {
        Expression query = Orders.Select(o => ~o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsIntegerLeft()
    {
        Expression query = Orders.Select(o => o.Quantity << 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" << 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsIntegerRight()
    {
        Expression query = Orders.Select(o => o.Quantity >> 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" >> 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsIntegerByColumn()
    {
        Expression query = Orders.Select(o => o.Quantity << o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" << t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AndsLongs()
    {
        Expression query = Orders.Select(o => o.Points & 255L).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" & 255
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AndsTwoColumns()
    {
        Expression query = Orders.Select(o => o.Quantity & o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" & t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesNestedBitwiseOperators()
    {
        Expression query = Orders
            .Select(o => (o.Quantity & 3) | (o.CustomerId & 12))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Quantity" & 3) | (t0."CustomerId" & 12)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesBitwiseInsideArithmetic()
    {
        Expression query = Orders.Select(o => (o.Quantity & 3) + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Quantity" & 3) + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComplementsParenthesizedExpression()
    {
        Expression query = Orders.Select(o => ~(o.Quantity | 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~(t0."Quantity" | 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByMaskedValue()
    {
        Expression query = Orders
            .Where(o => (o.Quantity & 1) == 1)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" & 1) = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByShiftedValue()
    {
        Expression query = Orders
            .Where(o => (o.Quantity >> 1) > 2)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" >> 1) > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByXorOfColumns()
    {
        Expression query = Orders
            .Where(o => (o.Quantity ^ o.CustomerId) == 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" # t0."CustomerId") = 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByFlagMask()
    {
        Expression query = Orders
            .Where(o => (o.Options & ShippingOptions.Express) == ShippingOptions.Express)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Options" & 1) = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByFlagMaskInequality()
    {
        Expression query = Orders
            .Where(o => (o.Options & ShippingOptions.Insured) != ShippingOptions.None)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Options" & 2) <> 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByHasFlag()
    {
        Expression query = Orders
            .Where(o => o.Options.HasFlag(ShippingOptions.Express))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Options" & 1) = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByHasCombinedFlags()
    {
        Expression query = Orders
            .Where(o =>
                o.Options.HasFlag(ShippingOptions.Express | ShippingOptions.Insured)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Options" & 3) = 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByNegatedHasFlag()
    {
        Expression query = Orders
            .Where(o => !o.Options.HasFlag(ShippingOptions.Fragile))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT ((t0."Options" & 4) = 4)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByHasFlagFromCapturedVariable()
    {
        ShippingOptions wanted = ShippingOptions.Insured | ShippingOptions.Fragile;

        Expression query = Orders
            .Where(o => o.Options.HasFlag(wanted))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Options" & 6) = 6
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByFlagCombinationEquality()
    {
        Expression query = Orders
            .Where(o => o.Options == (ShippingOptions.Express | ShippingOptions.Fragile))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Options" = 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFlagUnion()
    {
        Expression query = Orders
            .Select(o => o.Options | ShippingOptions.Fragile)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Options" | 4
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFlagRemoval()
    {
        Expression query = Orders
            .Select(o => o.Options & ~ShippingOptions.Express)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Options" & -2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsFlagToggle()
    {
        Expression query = Orders
            .Select(o => o.Options ^ ShippingOptions.Insured)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Options" # 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
