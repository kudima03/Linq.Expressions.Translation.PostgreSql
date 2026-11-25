#pragma warning disable CA1861 // the constant arrays are the values under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// string.Join joins a null separator or value as the empty string, as .NET does: an
// array is concatenated, a group, a subquery or a GroupJoin group is STRING_AGG in
// its order, and an array column is ARRAY_TO_STRING.
public sealed record StringJoinTests
{
    public static TheoryData<Expression<Func<Order, string>>, string> Arrays =>
        new()
        {
            {
                o => string.Join(", ", new[] { o.Number, o.Note, "x" }),
                "t0.\"Number\" || ', ' || COALESCE(t0.\"Note\", '') || ', ' || 'x'"
            },
            {
                o => string.Join(o.Number, new[] { o.Note, "x" }),
                "COALESCE(t0.\"Note\", '') || t0.\"Number\" || 'x'"
            },
            {
                o => string.Join(o.Note, new[] { o.Number, o.Number }),
                "t0.\"Number\" || COALESCE(t0.\"Note\", '') || t0.\"Number\""
            },
            {
                o => string.Join(o.Number, new[] { "a", null, "b" }),
                "'a' || t0.\"Number\" || t0.\"Number\" || 'b'"
            },
            { o => string.Join(o.Number, new[] { "a" }), "'a'" },
            { o => string.Join(",", new string[0]), "''" },
            { o => string.Join(",", new[] { o.Number }), "t0.\"Number\"" },
            {
                o => string.Join('-', new[] { o.Number, o.Number }),
                "t0.\"Number\" || '-' || t0.\"Number\""
            },
            { o => string.Join(",", o.Tags), "ARRAY_TO_STRING(t0.\"Tags\", ',', '')" },
            { o => string.Join('|', o.Tags), "ARRAY_TO_STRING(t0.\"Tags\", '|', '')" },
            {
                o => string.Join(o.Note, o.Tags),
                "ARRAY_TO_STRING(t0.\"Tags\", COALESCE(t0.\"Note\", ''), '')"
            },
        };

    [Theory]
    [MemberData(nameof(Arrays))]
    public void JoinsArray(Expression<Func<Order, string>> selector, string sql)
    {
        Expression query = Orders.Select(selector).Expression;

        Assert.Equal(
            Sql.Valid($"SELECT {sql} FROM \"Order\" AS t0"),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsNullSeparatorAsEmpty()
    {
        string? separator = null;
        Expression query = Orders
            .Select(o => string.Join(separator, new[] { o.Number, o.Note }))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                "SELECT t0.\"Number\" || COALESCE(t0.\"Note\", '') FROM \"Order\" AS t0"
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, string>>,
        string
    > Groups =>
        new()
        {
            {
                g => string.Join(", ", g.OrderBy(o => o.Id).Select(o => o.Note)),
                "COALESCE(STRING_AGG(COALESCE(t0.\"Note\", ''), ', ' ORDER BY t0.\"Id\"), '')"
            },
            {
                g => string.Join(", ", g.Select(o => o.Number)),
                "COALESCE(STRING_AGG(t0.\"Number\", ', '), '')"
            },
            {
                g =>
                    string.Join(
                        "/",
                        g.Where(o => o.IsPaid)
                            .OrderByDescending(o => o.Total)
                            .ThenBy(o => o.Id)
                            .Select(o => o.Note)
                            .Where(v => v != null)
                    ),
                "COALESCE(STRING_AGG(t0.\"Note\", '/' ORDER BY t0.\"Total\" DESC, t0.\"Id\") FILTER (WHERE (t0.\"Note\" IS NOT NULL) AND t0.\"IsPaid\"), '')"
            },
            {
                g =>
                    string.Join(
                        "",
                        g.OrderBy(o => o.Note, StringComparer.Ordinal)
                            .Select(o => o.Number)
                    ),
                "COALESCE(STRING_AGG(t0.\"Number\", '' ORDER BY t0.\"Note\" COLLATE \"C\" NULLS FIRST), '')"
            },
            {
                g =>
                    string.Join(
                        ",",
                        g.OrderByDescending(o => o.Note, StringComparer.Ordinal)
                            .Select(o => o.Note)
                            .Where(v => v != null)
                            .Distinct()
                    ),
                "COALESCE(STRING_AGG(DISTINCT t0.\"Note\" COLLATE \"C\", ',' ORDER BY t0.\"Note\" COLLATE \"C\" DESC) FILTER (WHERE t0.\"Note\" IS NOT NULL), '') COLLATE \"default\""
            },
            {
                g =>
                    string.Join(
                        ",",
                        g.OrderBy(o => o.Number).Select(o => o.Number).Distinct()
                    ),
                "COALESCE(STRING_AGG(DISTINCT t0.\"Number\", ',' ORDER BY t0.\"Number\"), '')"
            },
            {
                g => string.Join(",", g.Select(o => o.Number).Distinct()),
                "COALESCE(STRING_AGG(DISTINCT t0.\"Number\", ','), '')"
            },
            {
                g => string.Join(g.Key.ToString(), g.Select(o => o.Number)),
                "COALESCE(STRING_AGG(t0.\"Number\", CAST(t0.\"CustomerId\" AS text)), '')"
            },
        };

    [Theory]
    [MemberData(nameof(Groups))]
    public void JoinsGroup(
        Expression<Func<IGrouping<int, Order>, string>> selector,
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
    public void FiltersGroupsOnJoinedValues()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g =>
                string.Join("", g.OrderBy(o => o.Id).Select(o => o.Number)).Length > 3
            )
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING LENGTH(COALESCE(STRING_AGG(t0."Number", '' ORDER BY t0."Id"), '')) > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsCorrelatedSubquery()
    {
        Expression query = Customers
            .Select(c =>
                string.Join(
                    "; ",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Number)
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t1."Number", '; ' ORDER BY t1."Id"), '')
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
    public void JoinsPagedSubqueryInItsOrder()
    {
        Expression query = Customers
            .Select(c =>
                string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Take(2)
                        .Select(o => o.Note)
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(COALESCE(t2."Note", ''), ',' ORDER BY t2."$0"), '')
                    FROM (
                        SELECT t1."Note", t1."Id" AS "$0"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                        ORDER BY t1."Id"
                        LIMIT 2
                    ) AS t2
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsDistinctSubquery()
    {
        Expression query = Customers
            .Select(c =>
                string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Number)
                        .Distinct()
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t2."Number", ','), '')
                    FROM (
                        SELECT DISTINCT t1."Number"
                        FROM "Order" AS t1
                        WHERE t1."CustomerId" = t0."Id"
                    ) AS t2
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsEveryRowInOneRowOfAggregates()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ =>
                string.Join(
                    " + ",
                    Orders.OrderByDescending(o => o.Total).Select(o => o.Number)
                )
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t0."Number", ' + ' ORDER BY t0."Total" DESC), '')
                    FROM "Order" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsGroupOfGroupJoin()
    {
        Expression query = Customers
            .GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => string.Join(",", os.OrderBy(o => o.Id).Select(o => o.Number))
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT COALESCE(STRING_AGG(t1."Number", ',' ORDER BY t1."Id"), '')
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
    public void ComparesJoinedSubqueryWithoutNullCheck()
    {
        Expression query = Customers
            .Where(c =>
                string.Join(
                    ",",
                    Orders.Where(o => o.CustomerId == c.Id).Select(o => o.Number)
                ) == c.Name
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE (
                    SELECT COALESCE(STRING_AGG(t1."Number", ','), '')
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                ) = t0."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, string>>,
        string
    > Rejected =>
        new()
        {
            { g => string.Join(",", g.Select(o => o.Id)), "string values only" },
            { g => string.Join(",", g.Select(o => o.Note).Distinct()), "never null" },
            {
                g =>
                    string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Number).Distinct()
                    ),
                "sorted by themselves"
            },
            {
                g =>
                    string.Join(
                        ",",
                        g.OrderBy(o => o.Number)
                            .ThenBy(o => o.Id)
                            .Select(o => o.Number)
                            .Distinct()
                    ),
                "sorted by themselves"
            },
            { g => string.Join(",", new object[] { g.Key, "a" }), "string values only" },
            { g => string.Join(",", g.Select(o => o.Number).ToArray(), 0, 1), "Join" },
        };

    [Theory]
    [MemberData(nameof(Rejected))]
    public void RejectsJoinWithoutEquivalent(
        Expression<Func<IGrouping<int, Order>, string>> selector,
        string message
    )
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(selector).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void JoinsCapturedListWithComputedSeparator()
    {
        List<string?> values = ["a", null, "b"];
        Expression query = Orders
            .Select(o => string.Join(o.IsPaid ? '+' : '-', values))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'a' || CAST(CASE WHEN t0."IsPaid" THEN '+' ELSE '-' END AS text) || CAST(CASE WHEN t0."IsPaid" THEN '+' ELSE '-' END AS text) || 'b'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsJoinOfNullSequence()
    {
        IEnumerable<string>? values = null;
        Expression query = Orders.Select(o => string.Join(o.Number, values!)).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Join", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsJoinOfComputedSequence()
    {
        Expression query = Orders
            .Select(o => string.Join(",", o.Tags.Select(t => t.ToUpper())))
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
