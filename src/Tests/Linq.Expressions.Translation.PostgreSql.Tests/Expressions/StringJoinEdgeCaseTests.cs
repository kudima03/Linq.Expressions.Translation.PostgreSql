#pragma warning disable CA1861 // the constant arrays are the values under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// Edge cases of string.Join: the generic and char overloads, groups of element and
// result selectors, null and quoted separators, captured and in-memory sequences,
// groups of joins, joins in GROUP BY and nested joins, and the sequences and value
// types that stay rejected.
public sealed record StringJoinEdgeCaseTests
{
    [Fact]
    public void JoinsGroupWithGenericOverloadOfStrings()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => string.Join<string>(",", g.Select(o => o.Number)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(t0."Number", ','), '')
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Expression<Func<Order, string>>, string> Arrays =>
        new()
        {
            {
                o => string.Join<string?>(",", new[] { o.Number, o.Note }),
                "t0.\"Number\" || ',' || COALESCE(t0.\"Note\", '')"
            },
            { o => string.Join(",", new[] { 'a', 'b' }), "'a,b'" },
            {
                o => string.Join(',', new[] { "a", o.Number }),
                "'a' || ',' || t0.\"Number\""
            },
            { o => string.Join(",", new string?[] { null, null }), "','" },
            { o => string.Join(",", new string?[] { null }), "''" },
            {
                o => string.Join("'", new[] { o.Number, "it's" }),
                "t0.\"Number\" || '''' || 'it''s'"
            },
            { o => string.Join(o.Note, new[] { o.Number }), "t0.\"Number\"" },
        };

    [Theory]
    [MemberData(nameof(Arrays))]
    public void JoinsArrayEdgeCase(Expression<Func<Order, string>> selector, string sql)
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsGroupOfElementSelector()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId, o => o.Note)
            .Select(g => string.Join(",", g))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(COALESCE(t0."Note", ''), ','), '')
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsGroupOfResultSelectorWithNullableKeyAsSeparator()
    {
        Expression query = Orders
            .GroupBy(
                o => o.Note,
                (key, os) => string.Join(key, os.OrderBy(o => o.Id).Select(o => o.Number))
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(t0."Number", COALESCE(t0."Note", '') ORDER BY t0."Id"), '')
                FROM "Order" AS t0
                GROUP BY t0."Note"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, string>>,
        string
    > Separators =>
        new()
        {
            { g => string.Join(null, g.Select(o => o.Number)), "''" },
            { g => string.Join('\'', g.Select(o => o.Number)), "''''" },
            { g => string.Join("', '", g.Select(o => o.Number)), "''', '''" },
            {
                g => string.Join(g.Key > 2 ? ',' : ';', g.Select(o => o.Number)),
                "CAST(CASE WHEN t0.\"CustomerId\" > 2 THEN ',' ELSE ';' END AS text)"
            },
        };

    [Theory]
    [MemberData(nameof(Separators))]
    public void JoinsGroupWithSeparator(
        Expression<Func<IGrouping<int, Order>, string>> selector,
        string separator
    )
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(selector).Expression;

        Assert.Equal(
            Sql.Valid(
                $"SELECT COALESCE(STRING_AGG(t0.\"Number\", {separator}), '') FROM \"Order\" AS t0 GROUP BY t0.\"CustomerId\""
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsCapturedNullSeparatorAsEmpty()
    {
        string? none = null;
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => string.Join(none, g.Select(o => o.Number)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(t0."Number", ''), '')
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsGroupsOfInnerJoin()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) =>
                    new
                    {
                        c.Region,
                        o.Id,
                        o.Number,
                    }
            )
            .GroupBy(x => x.Region)
            .Select(g => string.Join(",", g.OrderBy(x => x.Id).Select(x => x.Number)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(t1."Number", ',' ORDER BY t1."Id"), '')
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                GROUP BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsGroupsOfSelectMany()
    {
        Expression query = Customers
            .SelectMany(
                c => Orders.Where(o => o.CustomerId == c.Id),
                (c, o) =>
                    new
                    {
                        c.Region,
                        o.Id,
                        o.Note,
                    }
            )
            .GroupBy(x => x.Region)
            .Select(g =>
                string.Join(";", g.OrderByDescending(x => x.Id).Select(x => x.Note))
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(COALESCE(t1."Note", ''), ';' ORDER BY t1."Id" DESC), '')
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t1."CustomerId" = t0."Id"
                GROUP BY t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsFilteredAndOrderedGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key)
            .Select(g => string.Join(",", g.Select(o => o.Number)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(t0."Number", ','), '')
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING CAST(COUNT(*) AS integer) > 1
                ORDER BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsGroupsBeforeTheyArePaged()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Take(2)
            .Select(g => string.Join(",", g.Select(o => o.Number)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(t0."Number", ','), '')
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersDistinctValuesAfterDistinct()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g =>
                string.Join(",", g.Select(o => o.Number).Distinct().Where(n => n != "x"))
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG(DISTINCT t0."Number", ',') FILTER (WHERE t0."Number" <> 'x'), '')
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsCorrelatedJoinsOfGroupElements()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g =>
                string.Join(
                    ";",
                    g.OrderBy(o => o.Id)
                        .Select(o =>
                            string.Join(
                                ",",
                                Orders
                                    .Where(x => x.CustomerId == o.CustomerId)
                                    .Select(x => x.Number)
                            )
                        )
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(STRING_AGG((
                    SELECT COALESCE(STRING_AGG(t1."Number", ','), '')
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."CustomerId"
                ), ';' ORDER BY t0."Id"), '')
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsByJoinedSubquery()
    {
        Expression query = Customers
            .GroupBy(c =>
                string.Join(
                    ",",
                    Orders.Where(o => o.CustomerId == c.Id).Select(o => o.Number)
                )
            )
            .Select(g => g.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Customer" AS t0
                GROUP BY (
                    SELECT COALESCE(STRING_AGG(t1."Number", ','), '')
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsCapturedQueryInItsOrder()
    {
        IQueryable<string> paid = Orders.Where(x => x.IsPaid).Select(x => x.Number);
        Expression query = Orders
            .Select(o => string.Join(",", paid.OrderBy(n => n, StringComparer.Ordinal)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t1."Number", ',' ORDER BY t1."Number" COLLATE "C"), '')
                    FROM "Order" AS t1
                    WHERE t1."IsPaid"
                )
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsInMemoryValuesInTheirOrder()
    {
        Expression query = Orders
            .Select(o =>
                string.Join(",", new[] { "a", "b" }.AsQueryable().OrderBy(n => n))
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t1."Value", ',' ORDER BY t1."Value"), '')
                    FROM (VALUES (CAST('a' AS text)), ('b')) AS t1 ("Value")
                )
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsConcatenatedSubqueryInItsOrder()
    {
        Expression query = Customers
            .Select(c =>
                string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Number)
                        .Concat(new[] { "z1", "z2" })
                        .OrderBy(n => n, StringComparer.Ordinal)
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t3."Number", ',' ORDER BY t3."Number" COLLATE "C"), '')
                    FROM (
                        SELECT t1."Number"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                        UNION ALL
                        SELECT t2."Value"
                        FROM (VALUES (CAST('z1' AS text)), ('z2')) AS t2 ("Value")
                    ) AS t3
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // STRING_AGG has an ORDER BY, which PostgreSQL does not implement for window
    // aggregates: the join of the rows a projection reads stays a subquery.
    [Fact]
    public void JoinsEveryRowBesideEachRowInSubquery()
    {
        Expression query = Orders
            .Select(o =>
                string.Join(",", Orders.OrderBy(x => x.Id).Select(x => x.Number))
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t1."Number", ',' ORDER BY t1."Id"), '')
                    FROM "Order" AS t1
                )
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, string>>
    > RejectedGroups =>
        [
            g => string.Join(",", g.Select(o => o.Quantity)),
            g => string.Join(",", g.Select(o => o.Priority)),
            g => string.Join(",", g.Select(o => o.Status)),
            g => string.Join(',', new object[] { "a", g.Key }),
            g => string.Join(",", g.OrderBy(o => o.Id).Take(2).Select(o => o.Number)),
            g => string.Join(",", g.OrderBy(o => o.Id).Skip(1).Select(o => o.Number)),
            g => string.Join(",", g.OrderBy(o => o.Id).Select(o => o.Number).Reverse()),
            g => string.Join(",", g.Select(o => o.Number).DefaultIfEmpty()),
            g => string.Join(",", g.Select(o => o.Number).ToArray()),
            g => string.Join(",", g.Select(o => o.Number).ToList()),
            g => string.Join(",", g.GroupBy(o => o.Status).Select(x => x.Key.ToString())),
            g => string.Join(g.First().Number, g.Select(o => o.Number)),
            g =>
                string.Join(
                    ",",
                    g.OrderBy(o => o.Id)
                        .Select(o => o.Note)
                        .Where(n => n != null)
                        .Distinct()
                ),
            g =>
                string.Join(
                    ",",
                    g.Select(o => o.Number)
                        .OrderBy(n => n)
                        .ThenBy(n => n.Length)
                        .Distinct()
                ),
            g =>
                string.Join(
                    ",",
                    g.OrderBy(o => o.Number, StringComparer.OrdinalIgnoreCase)
                        .Select(o => o.Number)
                ),
            g =>
                string.Join(
                    ",",
                    g.OrderBy(o => o.Number, StringComparer.InvariantCulture)
                        .Select(o => o.Number)
                ),
        ];

    [Theory]
    [MemberData(nameof(RejectedGroups))]
    public void RejectsJoinOfGroupWithoutEquivalent(
        Expression<Func<IGrouping<int, Order>, string>> selector
    )
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(selector).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsJoinOfDistinctGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Distinct()
            .Select(g => string.Join(",", g.Select(o => o.Number)))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("not available", exception.Message, StringComparison.Ordinal);
    }

    public static TheoryData<Expression<Func<Order, string>>> RejectedRows =>
        [
            o => string.Join(",", new object[] { o.Number, o.Note! }),
            o => string.Join(",", new object?[] { o.Number, null }),
            o => string.Join(",", new[] { o.Quantity, o.Id }),
            o => string.Join(",", new[] { "a", o.Number }, 0, 1),
            o => string.Join(",", new List<string> { o.Number }),
            o => string.Join(",", o.Tags.Where(t => t != null)),
            o => string.Join(",", o.Tags.OrderBy(t => t)),
            o => string.Join(",", o.Tags.Reverse()),
            o => string.Join(",", o.Tags.Distinct()),
        ];

    [Theory]
    [MemberData(nameof(RejectedRows))]
    public void RejectsJoinOfRowWithoutEquivalent(
        Expression<Func<Order, string>> selector
    )
    {
        Expression query = Orders.Select(selector).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
