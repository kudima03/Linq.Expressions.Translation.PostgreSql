#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet
#pragma warning disable CA2251 // the comparison of CompareOrdinal with 0 is under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record OrdinalStringComparisonTests
{
    // "C" also keeps = and <> byte-wise when the column has a nondeterministic collation.
    public static TheoryData<Expression<Func<Customer, bool>>, string> NonNullable =>
        new()
        {
            {
                c => string.CompareOrdinal(c.Name, "C") < 0,
                "t0.\"Name\" COLLATE \"C\" < 'C'"
            },
            {
                c => string.CompareOrdinal(c.Name, "C") <= 0,
                "t0.\"Name\" COLLATE \"C\" <= 'C'"
            },
            {
                c => string.CompareOrdinal(c.Name, "C") > 0,
                "t0.\"Name\" COLLATE \"C\" > 'C'"
            },
            {
                c => string.CompareOrdinal(c.Name, "C") >= 0,
                "t0.\"Name\" COLLATE \"C\" >= 'C'"
            },
            {
                c => string.CompareOrdinal(c.Name, "C") == 0,
                "t0.\"Name\" COLLATE \"C\" = 'C'"
            },
            {
                c => string.CompareOrdinal(c.Name, "C") != 0,
                "t0.\"Name\" COLLATE \"C\" <> 'C'"
            },
        };

    // CompareOrdinal ranks null below every string, so "less than" keeps the null rows.
    public static TheoryData<Expression<Func<Customer, bool>>, string> NullableColumn =>
        new()
        {
            {
                c => string.CompareOrdinal(c.Email, "C") < 0,
                "(t0.\"Email\" IS NULL) OR (t0.\"Email\" COLLATE \"C\" < 'C')"
            },
            {
                c => string.CompareOrdinal(c.Email, "C") <= 0,
                "(t0.\"Email\" IS NULL) OR (t0.\"Email\" COLLATE \"C\" <= 'C')"
            },
            {
                c => string.CompareOrdinal(c.Email, "C") > 0,
                "t0.\"Email\" COLLATE \"C\" > 'C'"
            },
            {
                c => string.CompareOrdinal(c.Email, "C") >= 0,
                "t0.\"Email\" COLLATE \"C\" >= 'C'"
            },
            {
                c => string.CompareOrdinal(c.Email, "C") == 0,
                "t0.\"Email\" COLLATE \"C\" IS NOT DISTINCT FROM 'C'"
            },
            {
                c => string.CompareOrdinal(c.Email, "C") != 0,
                "t0.\"Email\" COLLATE \"C\" IS DISTINCT FROM 'C'"
            },
        };

    [Theory(Skip = "string.CompareOrdinal is not translated (#45).")]
    [MemberData(nameof(NonNullable))]
    [MemberData(nameof(NullableColumn))]
    public void ComparesOrdinallyWithBinaryCollation(
        Expression<Func<Customer, bool>> predicate,
        string condition
    )
    {
        Expression query = Customers.Where(predicate).Select(c => c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE {condition}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "string.CompareOrdinal is not translated (#45).")]
    public void ComparesTwoColumnsOrdinally()
    {
        Expression query = Customers
            .Where(c => string.CompareOrdinal(c.Name, c.Region) > 0)
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE t0."Name" COLLATE "C" > t0."Region"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Ordinal OrderBy is not translated (#45).")]
    public void OrdersAscendingOrdinally()
    {
        Expression query = Customers
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name" COLLATE "C"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Ordinal OrderByDescending is not translated (#45).")]
    public void OrdersDescendingOrdinally()
    {
        Expression query = Customers
            .OrderByDescending(c => c.Name, StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Name" COLLATE "C" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Ordinal ThenBy is not translated (#45).")]
    public void AddsOrdinalThenByKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id", t0."Name" COLLATE "C"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Ordinal ThenByDescending is not translated (#45).")]
    public void AddsOrdinalThenByDescendingKey()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .ThenByDescending(c => c.Name, StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id", t0."Name" COLLATE "C" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Ordinal OrderBy is not translated (#45).")]
    public void PutsNullsFirstWhenOrderingNullableColumnOrdinally()
    {
        Expression query = Customers
            .OrderBy(c => c.Email, StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Email" COLLATE "C" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
