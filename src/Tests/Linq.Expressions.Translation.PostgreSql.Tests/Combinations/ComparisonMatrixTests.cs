#pragma warning disable IDE0100 // the comparison with a boolean literal is under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Combinations;

public sealed record ComparisonMatrixTests
{
    private static readonly Guid Tracking = new("0f8fad5b-d9cb-469f-a165-70867728950e");

    public static TheoryData<Expression<Func<Order, bool>>, string> NonNullable =>
        new()
        {
            { o => o.Quantity > 1, "t0.\"Quantity\" > 1" },
            { o => o.Points <= 10L, "t0.\"Points\" <= 10" },
            { o => o.Total >= 2.5m, "t0.\"Total\" >= 2.5" },
            { o => o.Weight < 0.5, "t0.\"Weight\" < 0.5" },
            {
                o => o.CreatedAt > new DateTime(2024, 1, 1),
                "t0.\"CreatedAt\" > TIMESTAMP '2024-01-01 00:00:00'"
            },
            {
                o => o.DeliveryTime <= new TimeOnly(8, 0),
                "t0.\"DeliveryTime\" <= TIME '08:00:00'"
            },
            { o => o.Status == OrderStatus.Shipped, "t0.\"Status\" = 2" },
            { o => o.Status != OrderStatus.Cancelled, "t0.\"Status\" <> 3" },
            {
                o => o.TrackingId == Tracking,
                "t0.\"TrackingId\" = UUID '0f8fad5b-d9cb-469f-a165-70867728950e'"
            },
            { o => o.Number == "A", "t0.\"Number\" = 'A'" },
            {
                o => o.Number != o.Number.ToUpper(),
                "t0.\"Number\" <> UPPER(t0.\"Number\")"
            },
            { o => o.IsPaid == false, "t0.\"IsPaid\" = FALSE" },
            { o => o.Quantity == o.CustomerId, "t0.\"Quantity\" = t0.\"CustomerId\"" },
        };

    public static TheoryData<Expression<Func<Order, bool>>, string> NullableOrderings =>
        new()
        {
            { o => o.Priority > 1, "t0.\"Priority\" > 1" },
            { o => o.Discount < 5m, "t0.\"Discount\" < 5" },
            {
                o => o.PaidAt >= new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                "t0.\"PaidAt\" >= TIMESTAMPTZ '2024-01-01 00:00:00+00'"
            },
            {
                o => o.ShippedOn < new DateOnly(2024, 6, 1),
                "t0.\"ShippedOn\" < DATE '2024-06-01'"
            },
            { o => o.Priority <= o.Quantity, "t0.\"Priority\" <= t0.\"Quantity\"" },
            { o => o.Discount > o.Total, "t0.\"Discount\" > t0.\"Total\"" },
        };

    public static TheoryData<Expression<Func<Order, bool>>, string> NullableEqualities =>
        new()
        {
            { o => o.IsGift == true, "t0.\"IsGift\" IS NOT DISTINCT FROM TRUE" },
            { o => o.Priority == 1, "t0.\"Priority\" IS NOT DISTINCT FROM 1" },
            { o => o.Priority != 1, "t0.\"Priority\" IS DISTINCT FROM 1" },
            { o => o.Note == "x", "t0.\"Note\" IS NOT DISTINCT FROM 'x'" },
            { o => o.Note != "x", "t0.\"Note\" IS DISTINCT FROM 'x'" },
            {
                o => o.Discount == o.Total,
                "t0.\"Discount\" IS NOT DISTINCT FROM t0.\"Total\""
            },
            {
                o => o.ShippedOn == DateOnly.FromDateTime(o.CreatedAt),
                "t0.\"ShippedOn\" IS NOT DISTINCT FROM CAST(t0.\"CreatedAt\" AS date)"
            },
            { o => o.Note != null, "t0.\"Note\" IS NOT NULL" },
            { o => null == o.Note, "t0.\"Note\" IS NULL" },
            { o => o.PaidAt == null, "t0.\"PaidAt\" IS NULL" },
        };

    [Theory]
    [MemberData(nameof(NonNullable))]
    [MemberData(nameof(NullableOrderings))]
    [MemberData(nameof(NullableEqualities))]
    public void FiltersByComparisonWithoutCoalescing(
        Expression<Func<Order, bool>> predicate,
        string condition
    )
    {
        Expression query = Orders.Where(predicate).Select(o => o.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE {condition}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [MemberData(nameof(NonNullable))]
    [MemberData(nameof(NullableEqualities))]
    public void ProjectsNullSafeComparisonAsIs(
        Expression<Func<Order, bool>> predicate,
        string value
    )
    {
        Expression query = Orders.Select(predicate).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT {value}
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [MemberData(nameof(NullableOrderings))]
    public void ProjectsNullableOrderingAsFalseWhenNull(
        Expression<Func<Order, bool>> predicate,
        string comparison
    )
    {
        Expression query = Orders.Select(predicate).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT COALESCE({comparison}, FALSE)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [MemberData(nameof(NullableOrderings))]
    public void NegatesNullableOrderingAsFalseWhenNull(
        Expression<Func<Order, bool>> predicate,
        string comparison
    )
    {
        Expression query = Orders
            .Where(
                Expression.Lambda<Func<Order, bool>>(
                    Expression.Not(predicate.Body),
                    predicate.Parameters
                )
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT COALESCE({comparison}, FALSE)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [MemberData(nameof(NonNullable))]
    [MemberData(nameof(NullableEqualities))]
    public void NegatesNullSafeComparisonDirectly(
        Expression<Func<Order, bool>> predicate,
        string condition
    )
    {
        Expression query = Orders
            .Where(
                Expression.Lambda<Func<Order, bool>>(
                    Expression.Not(predicate.Body),
                    predicate.Parameters
                )
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT ({condition})
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory]
    [MemberData(nameof(NullableOrderings))]
    public void CountsNullableOrderingInGroupFilter(
        Expression<Func<Order, bool>> predicate,
        string comparison
    )
    {
        ParameterExpression group = Expression.Parameter(
            typeof(IGrouping<int, Order>),
            "g"
        );
        Expression<Func<IGrouping<int, Order>, int>> counted = Expression.Lambda<
            Func<IGrouping<int, Order>, int>
        >(
            Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Count),
                [typeof(Order)],
                group,
                predicate
            ),
            group
        );
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(counted).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT CAST(COUNT(*) FILTER (WHERE {comparison}) AS integer)
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
