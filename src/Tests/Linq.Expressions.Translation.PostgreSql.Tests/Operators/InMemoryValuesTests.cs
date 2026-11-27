#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// An in-memory sequence of scalars or tuples of scalars is a VALUES list: as a root of
// two or more elements, joined, flattened by SelectMany, combined by a set operation
// or read by a subquery. The first row writes the type of every column.
public sealed record InMemoryValuesTests
{
    private static readonly (int Id, string? Label)[] Labels =
    [
        (1, "one"),
        (2, null),
        (3, "O'Brien"),
    ];

    [Fact]
    public void ReadsRootOfScalarsAsValues()
    {
        Expression query = new[] { 1, 2, 3 }.AsQueryable().Where(i => i > 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value"
                FROM (VALUES (1), (2), (3)) AS t0 ("Value")
                WHERE t0."Value" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsRootOfTuplesMemberByMember()
    {
        Expression query = Labels.AsQueryable().Select(r => r.Label).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item2"
                FROM (VALUES (1, CAST('one' AS text)), (2, NULL), (3, 'O''Brien')) AS t0 ("Item1", "Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsEveryMemberOfTuples()
    {
        Expression query = Labels.AsQueryable().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item1", t0."Item2"
                FROM (VALUES (1, CAST('one' AS text)), (2, NULL), (3, 'O''Brien')) AS t0 ("Item1", "Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    private static Expression Root<T>(params T[] items)
    {
        return items.AsQueryable().Expression;
    }

    public static TheoryData<Func<Expression>, string> TypedColumns =>
        new()
        {
            { () => Root(1, 2), "1), (2" },
            { () => Root(1L, 2L), "CAST(1 AS bigint)), (2" },
            { () => Root((short)1, (short)2), "CAST(1 AS smallint)), (2" },
            { () => Root(1m, 2.5m), "CAST(1 AS numeric)), (2.5" },
            { () => Root(1.5, 2d), "CAST(1.5 AS double precision)), (2" },
            { () => Root(1.5f, 2f), "CAST(1.5 AS real)), (2" },
            { () => Root(true, false), "TRUE), (FALSE" },
            { () => Root("a", "b"), "CAST('a' AS text)), ('b'" },
            {
                () => Root(Guid.Empty, new Guid("00000000-0000-0000-0000-000000000001")),
                "UUID '00000000-0000-0000-0000-000000000000'), (UUID '00000000-0000-0000-0000-000000000001'"
            },
            {
                () => Root(new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 1)),
                "DATE '2024-02-29'), (DATE '2024-03-01'"
            },
            {
                () => Root(new TimeOnly(10, 30), new TimeOnly(23, 59, 59)),
                "TIME '10:30:00'), (TIME '23:59:59'"
            },
            {
                () =>
                    Root(new DateTime(2024, 1, 15, 10, 30, 0), new DateTime(2024, 1, 16)),
                "TIMESTAMP '2024-01-15 10:30:00'), (TIMESTAMP '2024-01-16 00:00:00'"
            },
            {
                () =>
                    Root(
                        new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
                        new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(2))
                    ),
                "TIMESTAMPTZ '2024-01-15 10:30:00+00'), (TIMESTAMPTZ '2024-01-15 10:30:00+02:00'"
            },
            {
                () => Root(TimeSpan.FromHours(1), TimeSpan.Zero),
                "INTERVAL 'PT1H'), (INTERVAL 'PT0S'"
            },
            {
                () => Root(new byte[] { 1, 255 }, [0]),
                "BYTEA '\\x01ff'), (BYTEA '\\x00'"
            },
            { () => Root(OrderStatus.Paid, OrderStatus.Shipped), "1), (2" },
        };

    [Theory]
    [MemberData(nameof(TypedColumns))]
    public void WritesColumnTypeOnFirstRow(Func<Expression> root, string rows)
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

    public static TheoryData<Func<Expression>, string> NullColumns =>
        new()
        {
            { () => Root<int?>(null, 1), "CAST(NULL AS integer)), (1" },
            { () => Root<long?>(null, null), "CAST(NULL AS bigint)), (NULL" },
            { () => Root<decimal?>(null, 1m), "CAST(NULL AS numeric)), (1" },
            { () => Root<bool?>(null, true), "CAST(NULL AS boolean)), (TRUE" },
            { () => Root(null, "a"), "CAST(NULL AS text)), ('a'" },
            { () => Root<Guid?>(null, null), "CAST(NULL AS uuid)), (NULL" },
            { () => Root<DateOnly?>(null, null), "CAST(NULL AS date)), (NULL" },
            { () => Root<TimeOnly?>(null, null), "CAST(NULL AS time)), (NULL" },
            { () => Root<DateTime?>(null, null), "CAST(NULL AS timestamp)), (NULL" },
            {
                () => Root<DateTimeOffset?>(null, null),
                "CAST(NULL AS timestamptz)), (NULL"
            },
            { () => Root<TimeSpan?>(null, null), "CAST(NULL AS interval)), (NULL" },
            {
                () => Root<OrderStatus?>(null, OrderStatus.Paid),
                "CAST(NULL AS integer)), (1"
            },
        };

    [Theory]
    [MemberData(nameof(NullColumns))]
    public void TypesNullOnFirstRow(Func<Expression> root, string rows)
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

    [Fact]
    public void ComparesNullableColumnWithDistinctness()
    {
        int?[] priorities = [null, 1];
        Expression query = Orders
            .Join(priorities, o => o.Id, p => p ?? 0, (o, p) => o.Priority == p)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Priority" IS NOT DISTINCT FROM t1."Value"
                FROM "Order" AS t0
                INNER JOIN (VALUES (CAST(NULL AS integer)), (1)) AS t1 ("Value")
                ON t0."Id" = COALESCE(t1."Value", 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesColumnWithoutNullsWithEquality()
    {
        int?[] priorities = [2, 1];
        Expression query = Orders
            .Join(priorities, o => o.Id, p => p ?? 0, (o, p) => o.Quantity == p)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Quantity" = t1."Value"
                FROM "Order" AS t0
                INNER JOIN (VALUES (2), (1)) AS t1 ("Value")
                ON t0."Id" = COALESCE(t1."Value", 0)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsTableWithInMemoryRows()
    {
        (int Id, string? Label)[] labels = Labels;
        Expression query = Customers
            .Join(labels, c => c.Id, r => r.Id, (c, r) => new { c.Name, r.Label })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Item2" AS "Label"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (1, CAST('one' AS text)), (2, NULL), (3, 'O''Brien')) AS t1 ("Item1", "Item2")
                ON t0."Id" = t1."Item1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsInMemoryRowsWithQuerySyntax()
    {
        int[] ids = [1, 2];
        Expression query = (
            from c in Customers
            join id in ids on c.Id equals id
            select c.Name
        ).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (1), (2)) AS t1 ("Value")
                ON t0."Id" = t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsInMemoryRootWithTable()
    {
        Expression query = new[] { 1, 2 }
            .AsQueryable()
            .Join(Customers, id => id, c => c.Id, (id, c) => new { id, c.Name })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value" AS "id", t1."Name"
                FROM (VALUES (1), (2)) AS t0 ("Value")
                INNER JOIN "Customer" AS t1
                ON t0."Value" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsInMemoryRows()
    {
        (int Id, string? Label)[] labels = Labels;
        Expression query = Customers
            .GroupJoin(labels, c => c.Id, r => r.Id, (c, rs) => new { c, rs })
            .SelectMany(x => x.rs.DefaultIfEmpty(), (x, r) => new { x.c.Name, r.Label })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Item2" AS "Label"
                FROM "Customer" AS t0
                LEFT JOIN (VALUES (1, CAST('one' AS text)), (2, NULL), (3, 'O''Brien')) AS t1 ("Item1", "Item2")
                ON t0."Id" = t1."Item1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsInMemoryMatchesOfGroupJoin()
    {
        int[] ids = [1, 1, 2];
        Expression query = Customers
            .GroupJoin(ids, c => c.Id, id => id, (c, matches) => matches.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM (VALUES (1), (1), (2)) AS t1 ("Value")
                    WHERE t1."Value" = t0."Id"
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CrossJoinsInMemoryRowsToEveryRow()
    {
        Expression query = Customers
            .SelectMany(c => new[] { 1, 2 }, (c, n) => new { c.Name, n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Value" AS "n"
                FROM "Customer" AS t0
                CROSS JOIN (VALUES (1), (2)) AS t1 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensInMemoryRowsWithoutResultSelector()
    {
        Expression query = Customers.SelectMany(c => new[] { "a", "b" }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Value"
                FROM "Customer" AS t0
                CROSS JOIN (VALUES (CAST('a' AS text)), ('b')) AS t1 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnrollsInMemoryRowsOfOneElementRoot()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .SelectMany(
                one =>
                    new[]
                    {
                        new ValueTuple<long, string>(1, "a"),
                        new ValueTuple<long, string>(2, "b"),
                    },
                (one, row) => row
            )
            .Where(r => r.Item1 > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item1", t0."Item2"
                FROM (VALUES (CAST(1 AS bigint), CAST('a' AS text)), (2, 'b')) AS t0 ("Item1", "Item2")
                WHERE t0."Item1" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnrollsInMemoryRowsOfOneElementRootWithoutResultSelector()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .SelectMany(one => new[] { 5, 6 })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value"
                FROM (VALUES (5), (6)) AS t0 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnrolledRowsJoinTable()
    {
        Expression query = Customers
            .Join(
                new[] { 0 }
                    .AsQueryable()
                    .SelectMany(
                        one =>
                            new[]
                            {
                                new ValueTuple<int, string>(1, "x"),
                                new ValueTuple<int, string>(2, "y"),
                            },
                        (one, row) => row
                    ),
                c => c.Id,
                r => r.Item1,
                (c, r) => new { c.Name, r.Item2 }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Item2"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (1, CAST('x' AS text)), (2, 'y')) AS t1 ("Item1", "Item2")
                ON t0."Id" = t1."Item1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<
        string,
        Func<IQueryable<int>, IQueryable<int>>
    > SetOperations =>
        new()
        {
            { "UNION ALL", ids => ids.Concat([1, 2]) },
            { "UNION ALL", ids => ids.Concat([1, 2]) },
            { "UNION", ids => ids.Union([1, 2]) },
            { "INTERSECT", ids => ids.Intersect([1, 2]) },
            { "EXCEPT", ids => ids.Except([1, 2]) },
        };

    [Theory]
    [MemberData(nameof(SetOperations))]
    public void CombinesWithInMemorySequence(
        string operation,
        Func<IQueryable<int>, IQueryable<int>> combine
    )
    {
        Expression query = combine(Customers.Select(c => c.Id)).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id"
                FROM "Customer" AS t0
                {operation}
                SELECT t1."Value"
                FROM (VALUES (1), (2)) AS t1 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TestsExistenceInInMemoryRows()
    {
        Expression query = Customers
            .Where(c => new[] { 1, 2 }.AsQueryable().Any(i => i == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM (VALUES (1), (2)) AS t1 ("Value")
                    WHERE t1."Value" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsRestOfLongTuple()
    {
        ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>[] rows =
        [
            new(1, 2, 3, 4, 5, 6, 7, new ValueTuple<int, string>(8, "a")),
            new(9, 10, 11, 12, 13, 14, 15, new ValueTuple<int, string>(16, "b")),
        ];
        Expression query = rows.AsQueryable()
            .Select(r => new
            {
                First = r.Item1,
                Eighth = r.Rest.Item1,
                Ninth = r.Rest.Item2,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item1" AS "First", t0."Rest.Item1" AS "Eighth", t0."Rest.Item2" AS "Ninth"
                FROM (VALUES (1, 2, 3, 4, 5, 6, 7, 8, CAST('a' AS text)), (9, 10, 11, 12, 13, 14, 15, 16, 'b')) AS t0 ("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Rest.Item1", "Rest.Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsNestedTuple()
    {
        (int Id, (string Name, decimal Rate) Tax)[] rows =
        [
            (1, ("low", 0.07m)),
            (2, ("high", 0.19m)),
        ];
        Expression query = rows.AsQueryable().Select(r => r.Tax.Rate * 100).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item2.Item2" * 100
                FROM (VALUES (1, CAST('low' AS text), CAST(0.07 AS numeric)), (2, 'high', 0.19)) AS t0 ("Item1", "Item2.Item1", "Item2.Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsReferenceTuples()
    {
        Tuple<int, string>[] rows = [Tuple.Create(1, "a"), Tuple.Create(2, "b")];
        Expression query = rows.AsQueryable().Select(r => r.Item2).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item2"
                FROM (VALUES (1, CAST('a' AS text)), (2, 'b')) AS t0 ("Item1", "Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersAndPagesInMemoryRows()
    {
        Expression query = new[] { 3, 1, 2 }
            .AsQueryable()
            .OrderByDescending(i => i)
            .Skip(1)
            .Take(1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value"
                FROM (VALUES (3), (1), (2)) AS t0 ("Value")
                ORDER BY t0."Value" DESC
                LIMIT 1 OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsInMemoryRows()
    {
        (int Id, string? Label)[] labels = Labels;
        Expression query = labels
            .AsQueryable()
            .GroupBy(r => r.Label == null)
            .Select(g => new { g.Key, Total = g.Sum(r => r.Id) })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item2" IS NULL AS "Key", CAST(COALESCE(SUM(t0."Item1"), 0) AS integer) AS "Total"
                FROM (VALUES (1, CAST('one' AS text)), (2, NULL), (3, 'O''Brien')) AS t0 ("Item1", "Item2")
                GROUP BY t0."Item2" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsInMemoryRoot()
    {
        Expression query = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Count),
            [typeof(int)],
            new[] { 1, 2, 3 }.AsQueryable().Expression
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (VALUES (1), (2), (3)) AS t0 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsOneElementRootRowOfAggregates()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .Select(_ => Orders.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t0
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RepeatsAggregatesOnEveryElementOfRoot()
    {
        Expression query = new[] { 0, 1 }
            .AsQueryable()
            .Select(_ => Orders.Count())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COUNT(*) AS integer)
                    FROM "Order" AS t1
                )
                FROM (VALUES (0), (1)) AS t0 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsEmptyInMemorySequence()
    {
        int[] none = [];
        Expression query = Customers
            .Join(none, c => c.Id, i => i, (c, i) => c)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("at least one row", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEmptyInMemoryRoot()
    {
        Expression query = Array.Empty<int>().AsQueryable().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("at least one row", exception.Message, StringComparison.Ordinal);
    }

    public static TheoryData<
        Func<IQueryable<Customer>, Expression>,
        string
    > Unsupported =>
        new()
        {
            {
                c =>
                    c.Join(
                        [new[] { 1 }, [2]],
                        x => x.Id,
                        y => y[0],
                        (x, y) => x
                    ).Expression,
                "Int32[]"
            },
            { c => c.Concat([new Customer(), new Customer()]).Expression, "Customer" },
            {
                c =>
                    Expression.Call(
                        typeof(Queryable),
                        nameof(Queryable.Concat),
                        [typeof(int)],
                        c.Select(x => x.Id).Expression,
                        Expression.Constant(new Queue<int>([1, 2]), typeof(Queue<int>))
                    ),
                "Queue"
            },
            {
                c =>
                    c.Join(
                        new (int, DateOnly)?[] { (1, default), null },
                        x => x.Id,
                        y => y!.Value.Item1,
                        (x, y) => x
                    ).Expression,
                "Nullable"
            },
            {
                c =>
                    c.Join(
                        [Tuple.Create(1), null],
                        x => x.Id,
                        y => y!.Item1,
                        (x, y) => x
                    ).Expression,
                "null tuple"
            },
        };

    [Theory]
    [MemberData(nameof(Unsupported))]
    public void RejectsRowsVALUESCannotHold(
        Func<IQueryable<Customer>, Expression> query,
        string message
    )
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query(Customers)).TextValue
        );
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSequenceComputedFromOuterElement()
    {
        Expression query = Customers
            .SelectMany(c => new[] { c.Id, c.Id + 1 }, (c, n) => n)
            .Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsUnrollingThatReadsOneElementRoot()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .SelectMany(one => new[] { 1, 2 }, (one, row) => one + row)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }
}
