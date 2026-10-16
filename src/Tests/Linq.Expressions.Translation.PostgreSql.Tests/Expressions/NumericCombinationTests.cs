#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record NumericCombinationTests
{
    [Fact(Skip = "Nested negation renders --, which PostgreSQL reads as a line comment.")]
    public void NegatesNegatedColumn()
    {
        Expression query = Orders.Select(o => -(-o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -(-t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "Nested negation renders --, which comments out the rest of the statement."
    )]
    public void FiltersByNestedNegation()
    {
        Expression query = Orders
            .Where(o => -(-o.Quantity) > 1)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE -(-t0."Quantity") > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "PostgreSQL binds prefix ~ looser than +, so ~a + 1 parses as ~(a + 1)."
    )]
    public void AddsToComplement()
    {
        Expression query = Orders.Select(o => ~o.Quantity + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (~t0."Quantity") + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "PostgreSQL binds prefix ~ looser than *, so ~a * 2 parses as ~(a * 2)."
    )]
    public void MultipliesComplement()
    {
        Expression query = Orders.Select(o => ~o.Quantity * 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (~t0."Quantity") * 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MasksComplement()
    {
        Expression query = Orders.Select(o => ~o.Quantity & 255).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ~t0."Quantity" & 255
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "NOT binds looser than IS NOT DISTINCT FROM, so the whole comparison is negated."
    )]
    public void ComparesNegatedBooleanWithNullableBoolean()
    {
        Expression query = Orders
            .Where(o => !o.IsPaid == o.IsGift)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (NOT t0."IsPaid") IS NOT DISTINCT FROM t0."IsGift"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MultipliesNegatedColumn()
    {
        Expression query = Orders.Select(o => -o.Quantity * 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT -t0."Quantity" * 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsNegatedColumn()
    {
        Expression query = Orders.Select(o => o.Quantity - -o.CustomerId).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" - -t0."CustomerId"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsGroupingOfRightOperandOfSubtraction()
    {
        Expression query = Orders.Select(o => o.Quantity - (o.CustomerId - 1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" - (t0."CustomerId" - 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsGroupingOfRightOperandOfDivision()
    {
        Expression query = Orders.Select(o => o.Quantity / (o.CustomerId / 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" / (t0."CustomerId" / 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
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

    [Fact]
    public void WidensIntegerAddedToLong()
    {
        Expression query = Orders.Select(o => o.Quantity + o.Points).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS bigint) + t0."Points"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NarrowsLongWithCheckedCast()
    {
        Expression query = Orders.Select(o => checked((int)o.Points)).Expression;

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
    public void DividesDecimalByWidenedInteger()
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

    [Fact]
    public void DividesWidenedIntegerByDoubleLiteral()
    {
        Expression query = Orders.Select(o => o.Quantity / 2.0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS double precision) / 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DividesLongByWidenedInteger()
    {
        Expression query = Orders.Select(o => o.Points / o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Points" / CAST(t0."Quantity" AS bigint)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesScaledDoubleWithWidenedInteger()
    {
        Expression query = Orders
            .Where(o => o.Weight * 1.5 > o.Quantity)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Weight" * 1.5) > CAST(t0."Quantity" AS double precision)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersMultiplesOfThree()
    {
        Expression query = Orders
            .Where(o => o.Quantity % 3 == 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" % 3) = 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithDecimalLiteralKeepingScale()
    {
        Expression query = Orders
            .Where(o => o.Total == 10.50m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" = 10.50
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesLongWithLargestInteger()
    {
        Expression query = Orders
            .Where(o => o.Points > int.MaxValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Points" > 2147483647
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FloorsDecimalQuotient()
    {
        Expression query = Orders.Select(o => Math.Floor(o.Total / 3)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT FLOOR(t0."Total" / 3)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesAbsoluteDifference()
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
    public void TakesGreatestOfColumnAndCoalescedNullable()
    {
        Expression query = Orders
            .Select(o => Math.Max(o.Quantity, o.Priority ?? 0))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Quantity", COALESCE(t0."Priority", 0))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersBySquareRoot()
    {
        Expression query = Orders
            .Where(o => Math.Sqrt(o.Weight) > 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE SQRT(t0."Weight") > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SquaresWithPower()
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
    public void TakesSignOfDifference()
    {
        Expression query = Orders.Select(o => Math.Sign(o.Total - 100)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(SIGN(t0."Total" - 100) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ClampsComputedValueToColumn()
    {
        Expression query = Orders
            .Select(o => Math.Clamp(o.Quantity * 2, 0, o.CustomerId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LEAST(GREATEST(t0."Quantity" * 2, 0), t0."CustomerId")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersRowsThatAreNotNaN()
    {
        Expression query = Orders
            .Where(o => !double.IsNaN(o.Weight))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Weight" = CAST('NaN' AS double precision))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersFinitePositiveWeights()
    {
        Expression query = Orders
            .Where(o => double.IsFinite(o.Weight) && o.Weight > 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Weight" NOT IN (CAST('NaN' AS double precision), CAST('Infinity' AS double precision), CAST('-Infinity' AS double precision))) AND (t0."Weight" > 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByAbsoluteLongDescending()
    {
        Expression query = Orders
            .OrderByDescending(o => long.Abs(o.Points))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY ABS(t0."Points") DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByWideProduct()
    {
        Expression query = Orders
            .Where(o => Math.BigMul(o.Quantity, o.CustomerId) > 3000000000L)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (CAST(t0."Quantity" AS bigint) * CAST(t0."CustomerId" AS bigint)) > 3000000000
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksFlagWithMask()
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
    public void ChecksAnyOfSeveralFlags()
    {
        Expression query = Orders
            .Where(o =>
                (o.Options & (ShippingOptions.Express | ShippingOptions.Fragile)) != 0
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Options" & 5) <> 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ClearsFlag()
    {
        Expression query = Orders
            .Select(o => o.Options & ~ShippingOptions.Fragile)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Options" & -5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TogglesFlag()
    {
        Expression query = Orders
            .Select(o => o.Options ^ ShippingOptions.Express)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Options" # 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ShiftsConstantByMaskedColumn()
    {
        Expression query = Orders.Select(o => 1 << o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1 << (t0."Quantity" & 31)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RoundsUnitPriceToCents()
    {
        Expression query = Orders
            .Select(o =>
                Math.Round(o.Total / o.Quantity, 2, MidpointRounding.AwayFromZero)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT ROUND(t0."Total" / CAST(t0."Quantity" AS numeric), 2)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TruncatesProductToInteger()
    {
        Expression query = Orders.Select(o => (int)(o.Weight * o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(TRUNC(t0."Weight" * CAST(t0."Quantity" AS double precision)) AS integer)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesEnumAsInteger()
    {
        Expression query = Orders
            .Where(o => (int)o.Status > 1)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Status" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesIntegerCastToEnum()
    {
        Expression query = Orders
            .Where(o => (OrderStatus)o.Quantity == OrderStatus.Paid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SubtractsCoalescedDiscount()
    {
        Expression query = Orders.Select(o => o.Total - (o.Discount ?? 0)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" - COALESCE(t0."Discount", 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesValueInConditionalBranch()
    {
        Expression query = Orders.Select(o => o.IsPaid ? o.Total : -o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."IsPaid" THEN t0."Total" ELSE -t0."Total" END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
