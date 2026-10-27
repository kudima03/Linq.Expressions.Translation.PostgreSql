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

    [Theory]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    // Two nullable sides: a null lesser side wins only over a non-null greater side.
    public static TheoryData<
        Expression<Func<ValueTuple<Order, Customer>, bool>>,
        string
    > TwoNullableColumns =>
        new()
        {
            {
                x => string.CompareOrdinal(x.Item1.Note, x.Item2.Email) < 0,
                "((t0.\"Note\" IS NULL) AND (t1.\"Email\" IS NOT NULL)) OR (t0.\"Note\" COLLATE \"C\" < t1.\"Email\")"
            },
            {
                x => string.CompareOrdinal(x.Item1.Note, x.Item2.Email) <= 0,
                "(t0.\"Note\" IS NULL) OR (t0.\"Note\" COLLATE \"C\" <= t1.\"Email\")"
            },
            {
                x => string.CompareOrdinal(x.Item1.Note, x.Item2.Email) > 0,
                "((t1.\"Email\" IS NULL) AND (t0.\"Note\" IS NOT NULL)) OR (t0.\"Note\" COLLATE \"C\" > t1.\"Email\")"
            },
            {
                x => string.CompareOrdinal(x.Item1.Note, x.Item2.Email) >= 0,
                "(t1.\"Email\" IS NULL) OR (t0.\"Note\" COLLATE \"C\" >= t1.\"Email\")"
            },
        };

    [Theory]
    [MemberData(nameof(TwoNullableColumns))]
    public void ComparesTwoNullableColumnsOrdinally(
        Expression<Func<ValueTuple<Order, Customer>, bool>> predicate,
        string condition
    )
    {
        Expression query = Orders
            .Join(
                Customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new ValueTuple<Order, Customer>(o, c)
            )
            .Where(predicate)
            .Select(x => x.Item1.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Order" AS t0
                INNER JOIN "Customer" AS t1 ON t0."CustomerId" = t1."Id"
                WHERE {condition}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsOrdinalComparisonOfNullableColumnAsValue()
    {
        Expression query = Customers
            .Select(c => string.CompareOrdinal(c.Email, "C") > 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Email" COLLATE "C" > 'C', FALSE)
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesCorrelationKeyOrdinally()
    {
        Expression query = Customers
            .Where(c => Orders.Any(o => string.CompareOrdinal(o.Number, c.Email) == 0))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."Number" COLLATE "C" = t0."Email"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersValuesAscendingOrdinally()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Order(StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY t0."Name" COLLATE "C"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersValuesDescendingOrdinally()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .OrderDescending(StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY t0."Name" COLLATE "C" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsOrderingValuesWithOtherComparer()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Order", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsOrdinalComparisonWithOtherThanZero()
    {
        Expression query = Customers
            .Where(c => string.CompareOrdinal(c.Name, "C") > 1)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("CompareOrdinal", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsReversedOrdinalComparison()
    {
        Expression query = Customers
            .Where(c => 0 < string.CompareOrdinal(c.Name, "C"))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("CompareOrdinal", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsOrdinalComparisonOfSubstrings()
    {
        Expression query = Customers
            .Where(c => string.CompareOrdinal(c.Name, 0, "C", 0, 1) == 0)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("CompareOrdinal", exception.Message, StringComparison.Ordinal);
    }
}
