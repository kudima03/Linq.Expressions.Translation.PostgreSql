#pragma warning disable IDE0100 // comparisons with boolean literals are the subject
#pragma warning disable IDE0030 // the HasValue conditional is the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record NullHandlingTests
{
    [Fact]
    public void ComparesReferenceColumnWithNull()
    {
        Expression query = Orders.Where(o => o.Note == null).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesReferenceColumnWithNullInverted()
    {
        Expression query = Orders.Where(o => o.Note != null).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullWithReferenceColumn()
    {
        Expression query = Orders.Where(o => null == o.Note).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableValueColumnWithNull()
    {
        Expression query = Orders
            .Where(o => o.Discount == null)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Discount" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableValueColumnWithNullInverted()
    {
        Expression query = Orders
            .Where(o => o.Discount != null)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Discount" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNonNullableStringWithPlainEquality()
    {
        Expression query = Orders
            .Where(o => o.Number == "A-1")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" = 'A-1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableStringWithNullSafeEquality()
    {
        Expression query = Orders.Where(o => o.Note == "x").Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS NOT DISTINCT FROM 'x'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableStringWithNullSafeInequality()
    {
        Expression query = Orders.Where(o => o.Note != "x").Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS DISTINCT FROM 'x'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesConstantWithNullableStringNullSafe()
    {
        Expression query = Orders.Where(o => "x" == o.Note).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE 'x' IS NOT DISTINCT FROM t0."Note"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesStringColumnWithNullableStringColumnNullSafe()
    {
        Expression query = Orders
            .Where(o => o.Number == o.Note)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" IS NOT DISTINCT FROM t0."Note"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableIntegerWithNullSafeEquality()
    {
        Expression query = Orders
            .Where(o => o.Priority == 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT DISTINCT FROM 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableIntegerWithNullSafeInequality()
    {
        Expression query = Orders
            .Where(o => o.Priority != 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS DISTINCT FROM 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesConstantWithNullableIntegerNullSafe()
    {
        Expression query = Orders
            .Where(o => 3 == o.Priority)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE 3 IS NOT DISTINCT FROM t0."Priority"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableIntegerWithIntegerColumnNullSafe()
    {
        Expression query = Orders
            .Where(o => o.Priority == o.Quantity)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT DISTINCT FROM t0."Quantity"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTwoNullableColumnsNullSafe()
    {
        Expression query = Orders
            .Where(o => o.Priority == o.Priority)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT DISTINCT FROM t0."Priority"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableDecimalWithNullSafeEquality()
    {
        Expression query = Orders
            .Where(o => o.Discount == 5m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Discount" IS NOT DISTINCT FROM 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableBooleanWithTrue()
    {
        Expression query = Orders
            .Where(o => o.IsGift == true)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsGift" IS NOT DISTINCT FROM TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableBooleanWithFalse()
    {
        Expression query = Orders
            .Where(o => o.IsGift == false)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsGift" IS NOT DISTINCT FROM FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableBooleanWithNull()
    {
        Expression query = Orders
            .Where(o => o.IsGift == null)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."IsGift" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableDateOnlyWithNullSafeEquality()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn == new DateOnly(2024, 5, 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" IS NOT DISTINCT FROM DATE '2024-05-01'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableDateTimeOffsetColumnWithNull()
    {
        Expression query = Orders
            .Where(o => o.PaidAt == null)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."PaidAt" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksHasValue()
    {
        Expression query = Orders
            .Where(o => o.Priority.HasValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesHasValue()
    {
        Expression query = Orders
            .Where(o => !o.Priority.HasValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Priority" IS NOT NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesHasValueWithFalse()
    {
        Expression query = Orders
            .Where(o => o.Priority.HasValue == false)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IS NOT NULL) = FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesHasValueWithComparison()
    {
        Expression query = Orders
            .Where(o => o.Priority.HasValue && (o.Priority.Value > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IS NOT NULL) AND (t0."Priority" > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksHasValueOfNullableDecimal()
    {
        Expression query = Orders
            .Where(o => o.Discount.HasValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Discount" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksHasValueOfNullableDateOnly()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn.HasValue)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnwrapsNullableValueInComparison()
    {
        Expression query = Orders
            .Where(o => o.Priority!.Value > 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesGetValueOrDefault()
    {
        Expression query = Orders
            .Where(o => o.Priority.GetValueOrDefault() > 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."Priority", 0) > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesGetValueOrDefaultWithFallback()
    {
        Expression query = Orders
            .Where(o => o.Priority.GetValueOrDefault(10) > 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."Priority", 10) > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesGetValueOrDefaultOfBoolean()
    {
        Expression query = Orders
            .Where(o => o.IsGift.GetValueOrDefault())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."IsGift", FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesGetValueOrDefaultOfDecimal()
    {
        Expression query = Orders
            .Where(o => o.Discount.GetValueOrDefault() > 1m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."Discount", 0) > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesCoalescedValue()
    {
        Expression query = Orders
            .Where(o => (o.Priority ?? 0) > 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."Priority", 0) > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesCoalescedBoolean()
    {
        Expression query = Orders
            .Where(o => o.IsGift ?? false)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."IsGift", FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NegatesCoalescedBoolean()
    {
        Expression query = Orders
            .Where(o => !(o.IsGift ?? false))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."IsGift", FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesCoalescedBooleanWithColumn()
    {
        Expression query = Orders
            .Where(o => (o.IsGift ?? false) && o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."IsGift", FALSE) AND t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesCoalescedString()
    {
        Expression query = Orders
            .Where(o => (o.Note ?? "none") == "none")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."Note", 'none') = 'none'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesCoalescedDecimalWithColumn()
    {
        Expression query = Orders
            .Where(o => (o.Discount ?? o.Total) > 5m)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."Discount", t0."Total") > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceOfNullableColumnWithConstant()
    {
        Expression query = Orders.Select(o => o.Priority ?? 0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority", 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceOfTwoColumns()
    {
        Expression query = Orders.Select(o => o.Priority ?? o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority", t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceOfStringColumnWithConstant()
    {
        Expression query = Orders.Select(o => o.Note ?? "none").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note", 'none')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensChainOfCoalesce()
    {
        Expression query = Orders.Select(o => o.Note ?? o.Number ?? "n/a").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note", t0."Number", 'n/a')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensLongerChainOfCoalesce()
    {
        Expression query = Orders
            .Select(o => o.Note ?? o.Number ?? o.Note ?? "n/a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note", t0."Number", t0."Note", 'n/a')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceOfDecimalWithColumn()
    {
        Expression query = Orders.Select(o => o.Discount ?? o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Discount", t0."Total")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceOfDecimalWithConstant()
    {
        Expression query = Orders.Select(o => o.Discount ?? 0m).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Discount", 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceOfNullableDateOnly()
    {
        Expression query = Orders
            .Select(o => o.ShippedOn ?? new DateOnly(2000, 1, 1))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."ShippedOn", DATE '2000-01-01')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceInsideArithmetic()
    {
        Expression query = Orders.Select(o => (o.Priority ?? 0) + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority", 0) + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsCoalesceOfNestedCoalesce()
    {
        Expression query = Orders.Select(o => (o.Priority ?? o.Quantity) * 2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority", t0."Quantity") * 2
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsGetValueOrDefaultOfDecimal()
    {
        Expression query = Orders.Select(o => o.Discount.GetValueOrDefault()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Discount", 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsGetValueOrDefaultWithFallbackColumn()
    {
        Expression query = Orders.Select(o => o.Priority.GetValueOrDefault(7)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority", 7)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsGetValueOrDefaultOfDateOnly()
    {
        Expression query = Orders.Select(o => o.ShippedOn.GetValueOrDefault()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."ShippedOn", DATE '0001-01-01')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullableValue()
    {
        Expression query = Orders.Select(o => o.Priority!.Value).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsHasValueAsBoolean()
    {
        Expression query = Orders.Select(o => o.Priority.HasValue).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" IS NOT NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullCheckAsBoolean()
    {
        Expression query = Orders.Select(o => o.Note == null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Note" IS NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullCheckNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, NoNote = o.Note == null })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Note" IS NULL AS "NoNote"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsConditionalOnHasValue()
    {
        Expression query = Orders
            .Select(o => o.Discount.HasValue ? o.Discount.Value : 0m)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Discount" IS NOT NULL THEN t0."Discount" ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullableColumn()
    {
        Expression query = Orders.Select(o => o.Discount).Expression;

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

    [Fact]
    public void SelectsNullableConversionOfNonNullableColumn()
    {
        Expression query = Orders.Select(o => (int?)o.Quantity).Expression;

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
    public void SelectsNullableConversionOfDecimal()
    {
        Expression query = Orders.Select(o => (decimal?)o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNonNullableConversionOfNullableColumn()
    {
        Expression query = Orders.Select(o => (int)o.Priority!).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullLiteralOfNullableType()
    {
        Expression query = Orders.Select(o => (int?)null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullLiteralOfStringType()
    {
        Expression query = Orders.Select(o => (string?)null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT NULL
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNullNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Missing = (string?)null })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", NULL AS "Missing"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullableOrderingPlainInFilter()
    {
        Expression query = Orders
            .Where(o => o.Priority > o.Quantity)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" > t0."Quantity"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullableOrderingBetweenNullableColumnsPlainInFilter()
    {
        Expression query = Orders
            .Where(o => o.Discount <= o.Total)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Discount" <= t0."Total"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesNullCheckAndOrdering()
    {
        Expression query = Orders
            .Where(o => (o.Priority != null) && (o.Priority < 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IS NOT NULL) AND (t0."Priority" < 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CombinesNullChecksWithDisjunction()
    {
        Expression query = Orders
            .Where(o => (o.Note == null) || (o.Discount == null))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Note" IS NULL) OR (t0."Discount" IS NULL)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksNullableArithmeticForNull()
    {
        Expression query = Orders
            .Where(o => (o.Priority + 1) == null)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" + 1) IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksNullableArithmeticForValue()
    {
        Expression query = Orders
            .Where(o => (o.Priority + 1) == 5)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" + 1) IS NOT DISTINCT FROM 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksStringMemberOfColumnForNull()
    {
        Expression query = Orders
            .Where(o => o.Note!.Length == 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE LENGTH(t0."Note") = 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
