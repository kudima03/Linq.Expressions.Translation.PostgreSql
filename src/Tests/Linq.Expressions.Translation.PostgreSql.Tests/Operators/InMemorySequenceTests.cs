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
    [Theory]
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

    public static TheoryData<Func<Expression>, string> WideFirstRows =>
        new()
        {
            { () => Root((ushort)1, ushort.MaxValue), "1), (65535" },
            { () => Root(1U, uint.MaxValue), "CAST(1 AS bigint)), (4294967295" },
            {
                () => Root(1UL, ulong.MaxValue),
                "CAST(1 AS numeric)), (18446744073709551615"
            },
            { () => Root<ulong?>(null, 1UL), "CAST(NULL AS numeric)), (1" },
        };

    // #122: an unsigned integer is the narrowest type that holds every value of it.
    [Theory]
    [MemberData(nameof(WideFirstRows))]
    public void WritesTheNextWiderTypeForUnsignedIntegers(
        Func<Expression> root,
        string rows
    )
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

    // A char is a text of one character: it compares with a number by its code, and a
    // signed integer converts to a ulong by its bits, a negative one wrapping around.
    [Fact]
    public void ConvertsCharsAndUnsignedIntegersAsDotNetDoes()
    {
        char[] letters = ['a', 'b'];
        ulong[] large = [1UL, ulong.MaxValue];
        Expression query = Customers
            .Join(letters, c => c.Id, l => l, (c, l) => c.Id)
            .Join(large, id => (ulong)id, u => u, (id, u) => id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (CAST('a' AS text)), ('b')) AS t1 ("Value") ON t0."Id" = ASCII(t1."Value")
                INNER JOIN (VALUES (CAST(1 AS numeric)), (18446744073709551615)) AS t2 ("Value") ON (CAST(t0."Id" AS numeric) + CASE WHEN t0."Id" < 0 THEN 18446744073709551616 ELSE 0 END) = t2."Value"
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
    [Theory]
    [MemberData(nameof(LoneSurrogates))]
    public void RefusesLoneSurrogates(Func<Expression> query)
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );
    }
}
