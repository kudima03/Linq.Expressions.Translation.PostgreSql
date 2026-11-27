#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// In-memory sequences inside a query: the column type VALUES writes for bytes and
// chars, sequences captured as IEnumerable read as the queries they are, and the
// strings PostgreSQL cannot hold.
public sealed record InMemorySequenceTests
{
    private static Expression Root<T>(params T[] items)
    {
        return items.AsQueryable().Expression;
    }

    public static TheoryData<Func<Expression>, string> NarrowFirstRows =>
        new()
        {
            { () => Root((byte)0, byte.MaxValue), "CAST(0 AS smallint)), (255" },
            {
                () => Root(sbyte.MinValue, sbyte.MaxValue),
                "CAST(-128 AS smallint)), (127"
            },
            { () => Root<byte?>(null, 1), "CAST(NULL AS smallint)), (1" },
            { () => Root(TinyStatus.Low, TinyStatus.High), "CAST(1 AS smallint)), (2" },
            { () => Root('a', '\''), "CAST('a' AS text)), (''''" },
            { () => Root<char?>(null, 'b'), "CAST(NULL AS text)), ('b'" },
        };

    // #136: bytes are smallint as the other numbers, chars are text.
    [Theory(Skip = "#136: VALUES of byte, sbyte and char is refused")]
    [MemberData(nameof(NarrowFirstRows))]
    public void WritesSmallintForBytesAndTextForChars(Func<Expression> root, string rows)
    {
        Expression query = root();

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Value"
                FROM (VALUES ({rows})) AS t0 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #138: the same SQL as Orders.Any(...).
    [Fact(Skip = "#138: Enumerable.Any over a table captured as IEnumerable is refused")]
    public void TestsExistenceInTableCapturedAsSequence()
    {
        IEnumerable<Order> orders = Orders;
        Expression query = Customers
            .Where(c => orders.Any(o => o.CustomerId == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."CustomerId" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #138: the same SQL as Orders.Where(...).Any(...).
    [Fact(
        Skip = "#138: the query below Enumerable.Any of a captured sequence is refused"
    )]
    public void TestsExistenceInQueryCapturedAsSequence()
    {
        IEnumerable<Order> paid = Orders.Where(o => o.IsPaid);
        Expression query = Customers
            .Where(c => paid.Any(o => o.CustomerId == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Order" AS t1
                    WHERE t1."IsPaid" AND (t1."CustomerId" = t0."Id")
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // #138: the same SQL as the recursion's Any(...).
    [Fact(
        Skip = "#138: WithRecursive below Enumerable.Any of a captured sequence is refused"
    )]
    public void TestsExistenceInRecursionCapturedAsSequence()
    {
        IEnumerable<Employee> reports = Trees.WithRecursive(
            Employees.Where(e => e.ManagerId == null),
            level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
        );
        Expression query = Customers
            .Where(c => reports.Any(e => e.Name == c.Name))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM (
                        WITH RECURSIVE t2 AS (
                            SELECT t1."Id", t1."Name", t1."ManagerId"
                            FROM "Employee" AS t1
                            WHERE t1."ManagerId" IS NULL
                            UNION ALL
                            SELECT t4."Id", t4."Name", t4."ManagerId"
                            FROM t2 AS t3
                            INNER JOIN "Employee" AS t4 ON t3."Id" = t4."ManagerId"
                        )
                        SELECT t2."Id", t2."Name", t2."ManagerId"
                        FROM t2
                    ) AS t5
                    WHERE t5."Name" = t0."Name"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Func<Expression>> LoneSurrogates()
    {
        string reversed = "\udc00\ud800";
        string[] names = ["Ann", "\ud800"];
        return
        [
            () => Root("lone\ud800surrogate", "b"),
            () => Root("a", "\udc00"),
            () => Root((1, "x\ud800"), (2, "y")),
            () => Customers.Where(c => c.Name == "lone\ud800surrogate").Expression,
            () => Customers.Where(c => c.Name == reversed).Expression,
            () => Customers.Where(c => c.Name.StartsWith("smile\ud83d")).Expression,
            () => Customers.Where(c => c.Name.Contains("\ude00")).Expression,
            () => Customers.Where(c => c.Name.StartsWith('\ud800')).Expression,
            () => Customers.Where(c => names.Contains(c.Name)).Expression,
            () => Customers.Select(c => c.Name + "\udfff").Expression,
        ];
    }

    // #135: a lone UTF-16 surrogate has no UTF-8 encoding, so the statement cannot be
    // sent; it is refused before any statement runs, as the NUL character is.
    [Theory(Skip = "#135: a string with a lone surrogate is written into the statement")]
    [MemberData(nameof(LoneSurrogates))]
    public void RefusesLoneSurrogates(Func<Expression> query)
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );
    }
}
