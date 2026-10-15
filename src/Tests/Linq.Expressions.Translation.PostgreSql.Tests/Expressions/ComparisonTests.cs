#pragma warning disable IDE0100 // comparisons with boolean literals are the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record ComparisonTests
{
    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesIntegerColumnWithConstantData =>
        new()
        {
            { o => o.Quantity > 5, "t0.\"Quantity\" > 5" },
            { o => o.Quantity >= 5, "t0.\"Quantity\" >= 5" },
            { o => o.Quantity < 5, "t0.\"Quantity\" < 5" },
            { o => o.Quantity <= 5, "t0.\"Quantity\" <= 5" },
            { o => o.Quantity == 5, "t0.\"Quantity\" = 5" },
            { o => o.Quantity != 5, "t0.\"Quantity\" <> 5" },
        };

    [Theory]
    [MemberData(nameof(ComparesIntegerColumnWithConstantData))]
    public void ComparesIntegerColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesLongColumnWithConstantData =>
        new()
        {
            { o => o.Points > 5000000000L, "t0.\"Points\" > 5000000000" },
            { o => o.Points >= 5000000000L, "t0.\"Points\" >= 5000000000" },
            { o => o.Points < 5000000000L, "t0.\"Points\" < 5000000000" },
            { o => o.Points <= 5000000000L, "t0.\"Points\" <= 5000000000" },
            { o => o.Points == 5000000000L, "t0.\"Points\" = 5000000000" },
            { o => o.Points != 5000000000L, "t0.\"Points\" <> 5000000000" },
        };

    [Theory]
    [MemberData(nameof(ComparesLongColumnWithConstantData))]
    public void ComparesLongColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesDecimalColumnWithConstantData =>
        new()
        {
            { o => o.Total > 10.5m, "t0.\"Total\" > 10.5" },
            { o => o.Total >= 10.5m, "t0.\"Total\" >= 10.5" },
            { o => o.Total < 10.5m, "t0.\"Total\" < 10.5" },
            { o => o.Total <= 10.5m, "t0.\"Total\" <= 10.5" },
            { o => o.Total == 10.5m, "t0.\"Total\" = 10.5" },
            { o => o.Total != 10.5m, "t0.\"Total\" <> 10.5" },
        };

    [Theory]
    [MemberData(nameof(ComparesDecimalColumnWithConstantData))]
    public void ComparesDecimalColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesDoubleColumnWithConstantData =>
        new()
        {
            { o => o.Weight > 1.5, "t0.\"Weight\" > 1.5" },
            { o => o.Weight >= 1.5, "t0.\"Weight\" >= 1.5" },
            { o => o.Weight < 1.5, "t0.\"Weight\" < 1.5" },
            { o => o.Weight <= 1.5, "t0.\"Weight\" <= 1.5" },
            { o => o.Weight == 1.5, "t0.\"Weight\" = 1.5" },
            { o => o.Weight != 1.5, "t0.\"Weight\" <> 1.5" },
        };

    [Theory]
    [MemberData(nameof(ComparesDoubleColumnWithConstantData))]
    public void ComparesDoubleColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesStringColumnForEqualityData =>
        new()
        {
            { o => o.Number == "A-1", "t0.\"Number\" = 'A-1'" },
            { o => o.Number != "A-1", "t0.\"Number\" <> 'A-1'" },
            { o => "A-1" == o.Number, "'A-1' = t0.\"Number\"" },
        };

    [Theory]
    [MemberData(nameof(ComparesStringColumnForEqualityData))]
    public void ComparesStringColumnForEquality(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesDateTimeColumnWithConstantData =>
        new()
        {
            {
                o => o.CreatedAt > new DateTime(2024, 1, 31, 12, 30, 0),
                "t0.\"CreatedAt\" > TIMESTAMP '2024-01-31 12:30:00'"
            },
            {
                o => o.CreatedAt >= new DateTime(2024, 1, 31, 12, 30, 0),
                "t0.\"CreatedAt\" >= TIMESTAMP '2024-01-31 12:30:00'"
            },
            {
                o => o.CreatedAt < new DateTime(2024, 1, 31, 12, 30, 0),
                "t0.\"CreatedAt\" < TIMESTAMP '2024-01-31 12:30:00'"
            },
            {
                o => o.CreatedAt <= new DateTime(2024, 1, 31, 12, 30, 0),
                "t0.\"CreatedAt\" <= TIMESTAMP '2024-01-31 12:30:00'"
            },
            {
                o => o.CreatedAt == new DateTime(2024, 1, 31, 12, 30, 0),
                "t0.\"CreatedAt\" = TIMESTAMP '2024-01-31 12:30:00'"
            },
            {
                o => o.CreatedAt != new DateTime(2024, 1, 31, 12, 30, 0),
                "t0.\"CreatedAt\" <> TIMESTAMP '2024-01-31 12:30:00'"
            },
        };

    [Theory]
    [MemberData(nameof(ComparesDateTimeColumnWithConstantData))]
    public void ComparesDateTimeColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesTimeColumnWithConstantData =>
        new()
        {
            {
                o => o.DeliveryTime > new TimeOnly(9, 30),
                "t0.\"DeliveryTime\" > TIME '09:30:00'"
            },
            {
                o => o.DeliveryTime >= new TimeOnly(9, 30),
                "t0.\"DeliveryTime\" >= TIME '09:30:00'"
            },
            {
                o => o.DeliveryTime < new TimeOnly(9, 30),
                "t0.\"DeliveryTime\" < TIME '09:30:00'"
            },
            {
                o => o.DeliveryTime <= new TimeOnly(9, 30),
                "t0.\"DeliveryTime\" <= TIME '09:30:00'"
            },
            {
                o => o.DeliveryTime == new TimeOnly(9, 30),
                "t0.\"DeliveryTime\" = TIME '09:30:00'"
            },
            {
                o => o.DeliveryTime != new TimeOnly(9, 30),
                "t0.\"DeliveryTime\" <> TIME '09:30:00'"
            },
        };

    [Theory]
    [MemberData(nameof(ComparesTimeColumnWithConstantData))]
    public void ComparesTimeColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesEnumColumnWithConstantData =>
        new()
        {
            { o => o.Status == OrderStatus.Paid, "t0.\"Status\" = 1" },
            { o => o.Status != OrderStatus.Paid, "t0.\"Status\" <> 1" },
            { o => o.Status > OrderStatus.Created, "t0.\"Status\" > 0" },
            { o => o.Status <= OrderStatus.Shipped, "t0.\"Status\" <= 2" },
        };

    [Theory]
    [MemberData(nameof(ComparesEnumColumnWithConstantData))]
    public void ComparesEnumColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<Order, bool>>,
        string
    > ComparesBooleanColumnWithConstantData =>
        new()
        {
            { o => o.IsPaid == true, "t0.\"IsPaid\" = TRUE" },
            { o => o.IsPaid == false, "t0.\"IsPaid\" = FALSE" },
            { o => o.IsPaid != true, "t0.\"IsPaid\" <> TRUE" },
        };

    [Theory]
    [MemberData(nameof(ComparesBooleanColumnWithConstantData))]
    public void ComparesBooleanColumnWithConstant(
        Expression<Func<Order, bool>> expression,
        string sql
    )
    {
        Expression query = Orders.Where(expression).Select(e => e.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {sql}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesGuidColumnWithConstant()
    {
        Expression query = Orders
            .Where(o =>
                o.TrackingId == Guid.Parse("11111111-2222-3333-4444-555555555555")
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" = UUID '11111111-2222-3333-4444-555555555555'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesGuidColumnWithEmptyGuid()
    {
        Expression query = Orders
            .Where(o => o.TrackingId != Guid.Empty)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."TrackingId" <> UUID '00000000-0000-0000-0000-000000000000'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableDateOnlyColumnInFilter()
    {
        Expression query = Orders
            .Where(o => o.ShippedOn > new DateOnly(2024, 1, 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."ShippedOn" > DATE '2024-01-01'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesNullableDateTimeOffsetColumnInFilter()
    {
        Expression query = Orders
            .Where(o => o.PaidAt < new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."PaidAt" < TIMESTAMPTZ '2024-01-01 00:00:00+00'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesTwoColumns()
    {
        Expression query = Orders
            .Where(o => o.Quantity > o.CustomerId)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesConstantWithColumn()
    {
        Expression query = Orders.Where(o => 5 < o.Quantity).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE 5 < t0."Quantity"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesDecimalColumnWithNullableDecimalColumn()
    {
        Expression query = Orders
            .Where(o => o.Total > o.Discount)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" > t0."Discount"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesComputedValues()
    {
        Expression query = Orders
            .Where(o => (o.Quantity * 2) >= (o.CustomerId + 1))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Quantity" * 2) >= (t0."CustomerId" + 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedVariable()
    {
        int limit = 5;

        Expression query = Orders
            .Where(o => o.Quantity > limit)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesWithCapturedMemberChain()
    {
        Product product = new() { Price = 99.9m };

        Expression query = Orders
            .Where(o => o.Total > product.Price)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Total" > 99.9
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsComparisonAsBooleanValue()
    {
        Expression query = Orders.Select(o => o.Quantity > 5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" > 5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEqualityAsBooleanValue()
    {
        Expression query = Orders.Select(o => o.Number == "A-1").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" = 'A-1'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsComparisonNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, Big = o.Quantity > 5 })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Quantity" > 5 AS "Big"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsNullableOrderingInCoalesceAsValue()
    {
        Expression query = Orders.Select(o => o.Priority > 5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Priority" > 5, FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsNullableOrderingOfTwoColumnsInCoalesceAsValue()
    {
        Expression query = Orders.Select(o => o.Total >= o.Discount).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Total" >= t0."Discount", FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullSafeEqualityOfNullableColumnWithoutCoalesce()
    {
        Expression query = Orders.Select(o => o.Priority == 5).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" IS NOT DISTINCT FROM 5
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullableOrderingPlainInFilter()
    {
        Expression query = Orders.Where(o => o.Priority > 5).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Priority" > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullableOrderingPlainInsideConjunction()
    {
        Expression query = Orders
            .Where(o => (o.Priority > 5) && o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" > 5) AND t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsNullableOrderingPlainInsideDisjunction()
    {
        Expression query = Orders
            .Where(o => (o.Priority > 5) || (o.Quantity < 2))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" > 5) OR (t0."Quantity" < 2)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsNullableOrderingInCoalesceUnderNot()
    {
        Expression query = Orders
            .Where(o => !(o.Priority > 5))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Priority" > 5, FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsNullableOrderingInCoalesceUnderNotInsideConjunction()
    {
        Expression query = Orders
            .Where(o => !(o.Priority > 5) && o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE(t0."Priority" > 5, FALSE) AND t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesStringEqualsMethod()
    {
        Expression query = Orders
            .Where(o => string.Equals(o.Number, "A-1"))
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
    public void ComparesInstanceEqualsMethod()
    {
        Expression query = Orders
            .Where(o => o.Number.Equals("A-1"))
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
    public void ComparesIntegerEqualsMethod()
    {
        Expression query = Orders
            .Where(o => o.Quantity.Equals(5))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" = 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
