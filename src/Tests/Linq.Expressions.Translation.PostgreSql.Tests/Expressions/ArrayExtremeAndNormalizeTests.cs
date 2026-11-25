using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// new[] { a, b }.Max() and Min() are GREATEST and LEAST, which skip NULLs as Max and
// Min of nullable values do, for the types PostgreSQL orders as .NET does; Normalize
// is NORMALIZE in the same form.
public sealed record ArrayExtremeAndNormalizeTests
{
    public static TheoryData<Expression<Func<Order, object?>>, string> Extremes =>
        new()
        {
            {
                o => new[] { o.Quantity, o.Id, 3 }.Max(),
                "GREATEST(t0.\"Quantity\", t0.\"Id\", 3)"
            },
            {
                o => new[] { o.Quantity, o.Id }.Min(),
                "LEAST(t0.\"Quantity\", t0.\"Id\")"
            },
            {
                o => new[] { o.Priority, o.Quantity }.Min(),
                "LEAST(t0.\"Priority\", t0.\"Quantity\")"
            },
            { o => new[] { o.Priority, null }.Max(), "GREATEST(t0.\"Priority\", NULL)" },
            { o => new[] { o.Points, 5L }.Max(), "GREATEST(t0.\"Points\", 5)" },
            {
                o => new[] { o.Total, o.Discount ?? 0 }.Min(),
                "LEAST(t0.\"Total\", COALESCE(t0.\"Discount\", 0))"
            },
            { o => new[] { o.Discount, 1.5m }.Max(), "GREATEST(t0.\"Discount\", 1.5)" },
            {
                o => new[] { o.IsPaid, o.IsGift ?? false }.Max(),
                "GREATEST(t0.\"IsPaid\", COALESCE(t0.\"IsGift\", FALSE))"
            },
            {
                o => new[] { o.ShippedOn, new DateOnly(2024, 6, 3) }.Max(),
                "GREATEST(t0.\"ShippedOn\", DATE '2024-06-03')"
            },
            {
                o => new[] { o.DeliveryTime, new TimeOnly(12, 0) }.Min(),
                "LEAST(t0.\"DeliveryTime\", TIME '12:00:00')"
            },
            {
                o => new[] { o.CreatedAt, o.CreatedAt.AddDays(1) }.Max(),
                "GREATEST(t0.\"CreatedAt\", t0.\"CreatedAt\" + (1 * INTERVAL '1 day'))"
            },
            { o => new[] { o.PaidAt, null }.Min(), "LEAST(t0.\"PaidAt\", NULL)" },
            {
                o => new[] { o.Status, OrderStatus.Paid }.Max(),
                "GREATEST(t0.\"Status\", 1)"
            },
            {
                o => new[] { o.Number, o.Note, "m" }.Min(StringComparer.Ordinal),
                "LEAST(t0.\"Number\" COLLATE \"C\", t0.\"Note\" COLLATE \"C\", 'm' COLLATE \"C\") COLLATE \"default\""
            },
            {
                o => new[] { o.Note, null }.Max(StringComparer.Ordinal),
                "GREATEST(t0.\"Note\" COLLATE \"C\", NULL COLLATE \"C\") COLLATE \"default\""
            },
            {
                o =>
                    new[]
                    {
                        o.ShippedOn!.Value,
                        o.ShippedOn ?? new DateOnly(2024, 1, 1),
                    }.Max(),
                "GREATEST(t0.\"ShippedOn\", COALESCE(t0.\"ShippedOn\", DATE '2024-01-01'))"
            },
        };

    [Theory]
    [MemberData(nameof(Extremes))]
    public void TakesExtremeOfValues(
        Expression<Func<Order, object?>> selector,
        string sql
    )
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesExtremeOfValues()
    {
        Expression query = Orders
            .Where(o => new[] { o.Quantity, o.Priority ?? 0 }.Max() > 3)
            .OrderBy(o => new[] { o.CreatedAt, o.CreatedAt.AddHours(-1) }.Min())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE GREATEST(t0."Quantity", COALESCE(t0."Priority", 0)) > 3
                ORDER BY LEAST(t0."CreatedAt", t0."CreatedAt" + (-1 * INTERVAL '1 hour'))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesExtremeOfAggregates()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new[] { g.Max(o => o.Quantity), g.Count() }.Max())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(MAX(t0."Quantity"), CAST(COUNT(*) AS integer))
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, object?>>, string> Rejected =>
        new()
        {
            { o => new[] { o.Weight, 1.5 }.Max(), "Double" },
            { o => new[] { o.Weight, 1.5 }.Min(), "Double" },
            { o => new[] { (float)o.Weight, 1.5f }.Max(), "Single" },
            { o => new[] { (double?)o.Weight, null }.Min(), "Double" },
            { o => new[] { o.Number, "a" }.Max(), "String" },
            {
                o => new[] { o.Number, "a" }.Max(StringComparer.InvariantCulture),
                "String"
            },
            { o => new[] { o.TrackingId, Guid.Empty }.Max(), "Guid" },
            { o => new[] { o.Number[0], 'a' }.Max(), "Char" },
            { o => new[] { o.Id, o.Quantity }.Max(x => x * 2), "Max" },
        };

    [Theory]
    [MemberData(nameof(Rejected))]
    public void RejectsExtremeOrderedDifferently(
        Expression<Func<Order, object?>> selector,
        string message
    )
    {
        Expression query = Orders.Select(selector).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    // Where it may not be reached, the Max of no values is left to the database,
    // which has no GREATEST of no values.
    [Fact]
    public void RejectsExtremeOfNoValues()
    {
        Expression query = Orders
            .Select(o => o.IsPaid ? new int[0].Max() : o.Id)
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesExtremeThroughEnumerable()
    {
        Expression<Func<Order, int>> selector = o => o.Id;
        Expression query = Orders
            .Select(
                Expression.Lambda<Func<Order, int>>(
                    Expression.Call(
                        typeof(Enumerable),
                        nameof(Enumerable.Min),
                        [],
                        Expression.NewArrayInit(
                            typeof(int),
                            selector.Body,
                            Expression.Constant(7)
                        )
                    ),
                    selector.Parameters
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid("SELECT LEAST(t0.\"Id\", 7) FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Customer, string>>, string> Normalized =>
        new()
        {
            { c => c.Name.Normalize(), "NORMALIZE(t0.\"Name\", NFC)" },
            {
                c => c.Name.Normalize(NormalizationForm.FormC),
                "NORMALIZE(t0.\"Name\", NFC)"
            },
            {
                c => c.Name.Normalize(NormalizationForm.FormD),
                "NORMALIZE(t0.\"Name\", NFD)"
            },
            {
                c => c.Name.Normalize(NormalizationForm.FormKC),
                "NORMALIZE(t0.\"Name\", NFKC)"
            },
            {
                c => c.Name.Normalize(NormalizationForm.FormKD),
                "NORMALIZE(t0.\"Name\", NFKD)"
            },
            {
                c => c.Name.ToUpper().Normalize(NormalizationForm.FormD).Trim(' '),
                "BTRIM(NORMALIZE(UPPER(t0.\"Name\"), NFD), ' ')"
            },
        };

    [Theory]
    [MemberData(nameof(Normalized))]
    public void NormalizesText(Expression<Func<Customer, string>> selector, string sql)
    {
        Expression query = Customers.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Customer\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesNormalizedText()
    {
        Expression query = Customers
            .Join(
                Customers,
                c => c.Name.Normalize(NormalizationForm.FormD),
                d => d.Region.Normalize(NormalizationForm.FormD),
                (c, d) => c.Id
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                INNER JOIN "Customer" AS t1
                ON NORMALIZE(t0."Name", NFD) = NORMALIZE(t1."Region", NFD)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Customer, string>>> Unnormalized =>
        [
            c => c.Name.Normalize((NormalizationForm)7),
            c =>
                c.Name.Normalize(
                    c.IsActive ? NormalizationForm.FormC : NormalizationForm.FormD
                ),
        ];

    [Theory]
    [MemberData(nameof(Unnormalized))]
    public void RejectsFormWithoutEquivalent(Expression<Func<Customer, string>> selector)
    {
        Expression query = Customers.Select(selector).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Normalize", exception.Message, StringComparison.Ordinal);
    }
}
