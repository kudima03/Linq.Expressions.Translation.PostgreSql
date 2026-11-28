namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// The first or last value of a group in an order is the first element of the array
// of its values in that order, or in the inverted one; Min and Max with
// StringComparer.Ordinal are MIN and MAX in the "C" collation.
public sealed record OrderedGroupElementTests
{
    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, object?>>,
        string
    > Elements =>
        new()
        {
            {
                g =>
                    g.OrderBy(o => o.CreatedAt)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Total)
                        .First(),
                "(ARRAY_AGG(t0.\"Total\" ORDER BY t0.\"CreatedAt\", t0.\"Id\"))[1]"
            },
            {
                g => g.OrderBy(o => o.CreatedAt).Select(o => o.Total).FirstOrDefault(),
                "(ARRAY_AGG(t0.\"Total\" ORDER BY t0.\"CreatedAt\"))[1]"
            },
            {
                g => g.OrderByDescending(o => o.Priority).Select(o => o.Note).First(),
                "(ARRAY_AGG(t0.\"Note\" ORDER BY t0.\"Priority\" DESC NULLS LAST))[1]"
            },
            {
                g =>
                    g.OrderBy(o => o.Priority)
                        .ThenByDescending(o => o.Id)
                        .Select(o => o.Id)
                        .Last(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Priority\" DESC NULLS LAST, t0.\"Id\"))[1]"
            },
            {
                g => g.OrderBy(o => o.CreatedAt).Select(o => o.Note).LastOrDefault(),
                "(ARRAY_AGG(t0.\"Note\" ORDER BY t0.\"CreatedAt\" DESC))[1]"
            },
            {
                g =>
                    g.OrderBy(o => o.Note, StringComparer.Ordinal)
                        .ThenByDescending(o => o.Id)
                        .Select(o => o.Id)
                        .First(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY REGEXP_REPLACE(t0.\"Note\", '([\\U00010000-\\U0010FFFF])', E'\\ud7ff\\\\1', 'g') COLLATE \"C\" NULLS FIRST, t0.\"Id\" DESC))[1]"
            },
            {
                g =>
                    g.OrderByDescending(o => o.Number, StringComparer.Ordinal)
                        .Select(o => o.Id)
                        .First(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY REGEXP_REPLACE(t0.\"Number\", '([\\U00010000-\\U0010FFFF])', E'\\ud7ff\\\\1', 'g') COLLATE \"C\" DESC))[1]"
            },
            {
                g => g.OrderBy(o => new { o.Quantity, o.Id }).Select(o => o.Id).First(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Quantity\", t0.\"Id\"))[1]"
            },
            {
                g => g.OrderBy(o => 1).ThenBy(o => o.Id).Select(o => o.Id).First(),
                "(ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Id\"))[1]"
            },
            { g => g.Select(o => o.Id).First(), "(ARRAY_AGG(t0.\"Id\"))[1]" },
            {
                g =>
                    g.Where(o => o.IsPaid)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Quantity)
                        .FirstOrDefault(),
                "COALESCE((ARRAY_AGG(t0.\"Quantity\" ORDER BY t0.\"Id\") FILTER (WHERE t0.\"IsPaid\"))[1], 0)"
            },
            {
                g =>
                    g.Where(o => o.IsPaid)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Priority)
                        .FirstOrDefault(),
                "(ARRAY_AGG(t0.\"Priority\" ORDER BY t0.\"Id\") FILTER (WHERE t0.\"IsPaid\"))[1]"
            },
            {
                g =>
                    g.OrderBy(o => o.Id)
                        .Select(o => o.Note)
                        .FirstOrDefault(n => n != null),
                "(ARRAY_AGG(t0.\"Note\" ORDER BY t0.\"Id\") FILTER (WHERE t0.\"Note\" IS NOT NULL))[1]"
            },
            {
                g => g.OrderBy(o => o.Id).Select(o => o.Id).LastOrDefault(id => id > 2),
                "COALESCE((ARRAY_AGG(t0.\"Id\" ORDER BY t0.\"Id\" DESC) FILTER (WHERE t0.\"Id\" > 2))[1], 0)"
            },
            {
                g => g.OrderBy(o => o.Id).Select(o => o.Number).Distinct().First(),
                "(ARRAY_AGG(t0.\"Number\" ORDER BY t0.\"Id\"))[1]"
            },
            {
                g => g.Select(o => o.Note).Min(StringComparer.Ordinal),
                "REGEXP_REPLACE(MIN(REGEXP_REPLACE(t0.\"Note\", '([\\U00010000-\\U0010FFFF])', E'\\ud7ff\\\\1', 'g') COLLATE \"C\"), E'\\ud7ff([\\\\U00010000-\\\\U0010FFFF])', '\\1', 'g') COLLATE \"default\""
            },
            {
                g =>
                    g.Where(o => o.IsPaid)
                        .Select(o => o.Number)
                        .Max(StringComparer.Ordinal),
                "REGEXP_REPLACE(MAX(REGEXP_REPLACE(t0.\"Number\", '([\\U00010000-\\U0010FFFF])', E'\\ud7ff\\\\1', 'g') COLLATE \"C\") FILTER (WHERE t0.\"IsPaid\"), E'\\ud7ff([\\\\U00010000-\\\\U0010FFFF])', '\\1', 'g') COLLATE \"default\""
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
    public void OrdersGroupsByFirstValue()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .OrderBy(g =>
                g.OrderByDescending(o => o.Total).Select(o => o.Id).FirstOrDefault()
            )
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY (ARRAY_AGG(t0."Id" ORDER BY t0."Total" DESC))[1]
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersGroupsOnLastValue()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.OrderBy(o => o.CreatedAt).Select(o => o.Total).Last() > 100)
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                HAVING (ARRAY_AGG(t0."Total" ORDER BY t0."CreatedAt" DESC))[1] > 100
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsFirstAndLastOfComputedKey()
    {
        Expression query = Orders
            .GroupBy(o => new { o.CustomerId, o.IsPaid })
            .Select(g => new
            {
                g.Key.CustomerId,
                First = g.OrderBy(o => o.Id).Select(o => o.Quantity * 2).First(),
                Last = g.OrderBy(o => o.Id).Select(o => o.Number).Last(),
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", (ARRAY_AGG(t0."Quantity" * 2 ORDER BY t0."Id"))[1] AS "First", (ARRAY_AGG(t0."Number" ORDER BY t0."Id" DESC))[1] AS "Last"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId", t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsOrdinalExtremeOfSubquery()
    {
        Expression query = Customers
            .Select(c =>
                Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Number)
                    .Max(StringComparer.Ordinal)
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT REGEXP_REPLACE(MAX(REGEXP_REPLACE(t1."Number", '([\U00010000-\U0010FFFF])', E'\ud7ff\\1', 'g') COLLATE "C"), E'\ud7ff([\\U00010000-\\U0010FFFF])', '\1', 'g') COLLATE "default"
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
    public void ReadsOrdinalMinimumOfTable()
    {
        Expression query = Orders
            .Select(o => o.Note)
            .Executed(q => q.Min(StringComparer.Ordinal));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT REGEXP_REPLACE(MIN(REGEXP_REPLACE(t0."Note", '([\U00010000-\U0010FFFF])', E'\ud7ff\\1', 'g') COLLATE "C"), E'\ud7ff([\\U00010000-\\U0010FFFF])', '\1', 'g') COLLATE "default"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        Expression<Func<IGrouping<int, Order>, object?>>,
        string
    > Rejected =>
        new()
        {
            {
                g => g.Where(o => o.IsPaid).OrderBy(o => o.Id).Select(o => o.Id).Last(),
                "LastOrDefault"
            },
            {
                g => g.Where(o => o.IsPaid).OrderBy(o => o.Id).Select(o => o.Id).First(),
                "FirstOrDefault"
            },
            {
                g => g.OrderBy(o => o.Id).Select(o => o.Id).First(id => id > 1),
                "FirstOrDefault"
            },
            { g => g.Select(o => o.Id).LastOrDefault(), "sorted with OrderBy" },
            { g => g.OrderBy(o => 1).Select(o => o.Id).Last(), "sorted with OrderBy" },
            {
                g => g.OrderBy(o => o.Id).OrderBy(o => o.Total).Select(o => o.Id).First(),
                "already ordered"
            },
            {
                g =>
                    g.OrderBy(o => o.Note, StringComparer.InvariantCulture)
                        .Select(o => o.Id)
                        .First(),
                "OrderBy"
            },
            { g => g.OrderBy(o => o.Id).First(), "single value" },
            {
                g => g.OrderBy(o => o.Id).Select(o => o.Id).FirstOrDefault(-1),
                "FirstOrDefault"
            },
            { g => g.Select(o => o.Note).Min(StringComparer.InvariantCulture), "Min" },
            { g => g.Select(o => o.Id).Max(Comparer<int>.Default), "Max" },
            { g => g.OrderBy(o => o.Id).Select(o => o.Id).ElementAt(1), "ElementAt" },
        };

    [Theory]
    [MemberData(nameof(Rejected))]
    public void RejectsElementWithoutEquivalent(
        Expression<Func<IGrouping<int, Order>, object?>> selector,
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
    public void RejectsOrdinalMinimumWithOtherComparer()
    {
        Expression query = Orders
            .Select(o => o.Note)
            .Executed(q => q.Min(StringComparer.InvariantCulture));

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
