using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// Edge cases of the first and last value of a sorted group and of Min and Max with
// StringComparer.Ordinal: nullable keys sort nulls first ascending and last
// descending, Last inverts every key with its nulls, defaults of the value types
// substitute an empty filtered group, and culture comparisons stay rejected.
public sealed record OrderedGroupElementEdgeCaseTests
{
    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, object?>>,
        string
    > Elements =>
        new()
        {
            {
                g => g.OrderBy(o => o.Priority).Select(o => o.Id).First(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Priority\" NULLS FIRST))[1]"
            },
            {
                g => g.OrderBy(o => o.Priority).Select(o => o.Id).Last(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Priority\" DESC NULLS LAST))[1]"
            },
            {
                g => g.OrderByDescending(o => o.Priority).Select(o => o.Id).Last(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Priority\" NULLS FIRST))[1]"
            },
            {
                g => g.OrderBy(o => o.Id).ThenBy(o => o.IsGift).Select(o => o.Id).Last(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Id\" DESC, t0.\"IsGift\" DESC NULLS LAST))[1]"
            },
            {
                g =>
                    g.OrderBy(o => o.Id)
                        .ThenBy(o => o.Total)
                        .ThenByDescending(o => o.Quantity)
                        .Select(o => o.Id)
                        .Last(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Id\" DESC, t0.\"Total\" DESC, t0.\"Quantity\"))[1]"
            },
            {
                g =>
                    g.OrderBy(o => o.Note, StringComparer.Ordinal)
                        .Select(o => o.Id)
                        .Last(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Note\" COLLATE \"C\" DESC NULLS LAST))[1]"
            },
            {
                g =>
                    g.OrderByDescending(o => o.Note, StringComparer.Ordinal)
                        .Select(o => o.Id)
                        .First(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Note\" COLLATE \"C\" DESC NULLS LAST))[1]"
            },
            {
                g => g.Select(o => o.Note).OrderBy(n => n, StringComparer.Ordinal).Last(),
                "(ARRAY_AGG(t0.\"Note\" ORDER BY t0.\"Note\" COLLATE \"C\" DESC NULLS LAST))[1]"
            },
            {
                g => g.OrderBy(o => g.Key).Select(o => o.Id).First(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"CustomerId\"))[1]"
            },
            {
                g => g.Select(o => o.Number).Distinct().OrderBy(n => n).Last(),
                "(ARRAY_AGG(t0.\"Number\" ORDER BY t0.\"Number\" DESC))[1]"
            },
            {
                g => g.OrderBy(o => o.Id).Select(o => o.Tags).First(),
                "(ARRAY_AGG(t0.\"Tags\" ORDER BY t0.\"Id\"))[1]"
            },
            {
                g => g.OrderBy(o => o.Id).Select(o => string.Join(",", o.Tags)).First(),
                "(ARRAY_AGG(ARRAY_TO_STRING(t0.\"Tags\", ',', '') ORDER BY t0.\"Id\"))[1]"
            },
            {
                g => g.OrderBy(o => o.Id).Select(o => o.IsPaid).First() ? 1 : 0,
                "CASE WHEN (ARRAY_AGG(t0.\"IsPaid\" ORDER BY t0.\"Id\"))[1] THEN 1 ELSE 0 END"
            },
            {
                g =>
                    g.Where(o => o.IsPaid)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Status)
                        .FirstOrDefault(),
                "COALESCE((ARRAY_AGG(t0.\"Status\" ORDER BY t0.\"Id\") FILTER (WHERE t0.\"IsPaid\"))[1], 0)"
            },
            {
                g =>
                    g.Where(o => o.IsPaid)
                        .OrderBy(o => o.Id)
                        .Select(o => o.CreatedAt)
                        .FirstOrDefault(),
                "COALESCE((ARRAY_AGG(t0.\"CreatedAt\" ORDER BY t0.\"Id\") FILTER (WHERE t0.\"IsPaid\"))[1], TIMESTAMP '0001-01-01 00:00:00')"
            },
            {
                g =>
                    g.Select(o => o.Note)
                        .Max(StringComparer.FromComparison(StringComparison.Ordinal)),
                "MAX(t0.\"Note\" COLLATE \"C\") COLLATE \"default\""
            },
        };

    [Theory]
    [MemberData(nameof(Elements))]
    public void ReadsElementOfGroup(
        Expression<Func<IGrouping<int, Order>, object?>> selector,
        string sql
    )
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0 GROUP BY t0.\"CustomerId\""),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsLastOfElementSelectorGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Total)
            .Select(g => g.OrderBy(t => t).Last())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (ARRAY_AGG(t0."Total" ORDER BY t0."Total" DESC))[1]
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsLastOfResultSelectorGroup()
    {
        Expression query = Orders
            .GroupBy(
                o => o.CustomerId,
                (key, os) => os.OrderBy(o => o.Id).Select(o => o.Number).Last()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (ARRAY_AGG(t0."Number" ORDER BY t0."Id" DESC))[1]
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersGroupsOnFirstBoolean()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.OrderBy(o => o.Id).Select(o => o.IsPaid).First())
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING (ARRAY_AGG(t0."IsPaid" ORDER BY t0."Id"))[1]
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesFirstValuesOfGroupedSubquery()
    {
        Expression query = Customers
            .Select(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .GroupBy(o => o.Status)
                    .Select(g => g.OrderBy(o => o.Id).Select(o => o.Id).First())
                    .Max()
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT MAX(t2."Value")
                    FROM (
                        SELECT (ARRAY_AGG(t1."Id" ORDER BY t1."Id"))[1] AS "Value"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                        GROUP BY t1."Status"
                    ) AS t2
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsOrdinalMaximumOfGroupJoinGroup()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => os.Select(o => o.Note).Max(StringComparer.Ordinal)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT MAX(t1."Note" COLLATE "C") COLLATE "default"
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsOrdinalMaximumWithCapturedComparer()
    {
        StringComparer comparer = StringComparer.Ordinal;
        IComparer<string?> typed = StringComparer.Ordinal;
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                Max = g.Select(o => o.Note).Max(comparer),
                Min = g.Select(o => o.Note).Min(typed),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(t0."Note" COLLATE "C") COLLATE "default" AS "Max", MIN(t0."Note" COLLATE "C") COLLATE "default" AS "Min"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesOrdinalGreatestOfColumnAndConstant()
    {
        Expression query = Orders
            .Select(o => new[] { o.Note, "Ａ" }.Max(StringComparer.Ordinal))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT GREATEST(t0."Note" COLLATE "C", 'Ａ' COLLATE "C") COLLATE "default"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<IGrouping<int, Order>, object?>>> Rejected =>
        [
            g => g.OrderBy(o => o.Id).First().Number,
            g => g.OrderBy(o => o.Id).Select(o => new { o.Id, o.Number }).First(),
            g => g.OrderBy(o => o.Id).Select(o => o.Id).Single(),
            g => g.OrderBy(o => o.Id).Select(o => o.Id).SingleOrDefault(),
            g => g.OrderBy(o => o.Id).Select(o => o.Id).ElementAtOrDefault(0),
            g => g.OrderBy(o => o.Id).Select(o => o.Id).Last(id => id > 1),
            g => g.OrderBy(o => o.Id).Select(o => o.Id).LastOrDefault(-1),
            g => g.OrderBy(o => "x").ThenBy(o => 2).Select(o => o.Id).Last(),
            g => g.Select(o => o.Id).Order().First(),
            g => g.Select(o => o.Id).OrderDescending().Last(),
            g =>
                g.OrderBy(o => o.Note, StringComparer.CurrentCulture)
                    .Select(o => o.Id)
                    .First(),
            g =>
                g.OrderBy(o => o.Note, StringComparer.OrdinalIgnoreCase)
                    .Select(o => o.Id)
                    .First(),
            g => g.OrderBy(o => o.Note, null).Select(o => o.Id).First(),
            g =>
                g.OrderBy(o => o.Id)
                    .ThenBy(o => o.Note, StringComparer.InvariantCulture)
                    .Select(o => o.Id)
                    .Last(),
            g =>
                g.OrderByDescending(
                        o => o.Note,
                        StringComparer.InvariantCultureIgnoreCase
                    )
                    .Select(o => o.Id)
                    .First(),
            g => g.Select(o => o.Note).Min(StringComparer.OrdinalIgnoreCase),
            g => g.Select(o => o.Note).Max(StringComparer.CurrentCulture),
            g => g.Select(o => o.Note).Max(StringComparer.CurrentCultureIgnoreCase),
            g =>
                g.Select(o => o.Note)
                    .Max(StringComparer.Create(CultureInfo.InvariantCulture, false)),
            g => g.Select(o => o.Note).Max(Comparer<string?>.Default),
            g => g.Select(o => o.Note).Min((IComparer<string?>?)null),
            g => g.Select(o => o.IsPaid ? 1 : 0).Max(Comparer<int>.Default),
        ];

    [Theory]
    [MemberData(nameof(Rejected))]
    public void RejectsElementWithoutEquivalent(
        Expression<Func<IGrouping<int, Order>, object?>> selector
    )
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(selector).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, string?>>> RejectedArrays =>
        [
            o => new[] { o.Number, o.Note }.Max(),
            o => new[] { o.Number, o.Note }.Min(StringComparer.OrdinalIgnoreCase),
            o => new[] { o.Number, o.Note }.Max(StringComparer.InvariantCulture),
            o => o.Tags.Max(StringComparer.Ordinal),
        ];

    [Theory]
    [MemberData(nameof(RejectedArrays))]
    public void RejectsCultureExtremeOfArray(Expression<Func<Order, string?>> selector)
    {
        Expression query = Orders.Select(selector).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsCultureExtremesOfQueries()
    {
        Expression maximum = Orders
            .Select(o => o.Note)
            .Executed(q => q.Max(StringComparer.CurrentCulture));
        Expression minimum = Orders
            .Select(o => o.Number)
            .Executed(q => q.Min(StringComparer.OrdinalIgnoreCase));

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(maximum).TextValue
        );
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(minimum).TextValue
        );
    }

    [Fact]
    public void RejectsLastOfUnsortedGroupJoinGroup()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => os.Select(o => o.Quantity).LastOrDefault()
            )
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
