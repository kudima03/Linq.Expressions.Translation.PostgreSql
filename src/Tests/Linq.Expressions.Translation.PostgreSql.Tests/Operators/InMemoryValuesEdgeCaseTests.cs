#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

// Edge cases of in-memory sequences as VALUES lists: sequences of one element, every
// kind of collection, the literal of the first row of each column type, long and
// nested tuples, outer joins and the constructs that are refused.
public sealed record InMemoryValuesEdgeCaseTests
{
    private static readonly (int Id, string? Label)[] Pairs = [(1, "a"), (2, "b")];

    private static Expression Root<T>(params T[] items)
    {
        return items.AsQueryable().Expression;
    }

    [Fact]
    public void JoinsArrayOfOneElement()
    {
        int[] ids = [1];
        Expression query = Customers
            .Join(ids, c => c.Id, i => i, (c, i) => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (1)) AS t1 ("Value")
                ON t0."Id" = t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesArrayOfOneElement()
    {
        Expression query = Customers.Select(c => c.Id).Concat([7]).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                UNION ALL
                SELECT t1."Value"
                FROM (VALUES (7)) AS t1 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // NotSupportedException: The element of the in-memory source 'EnumerableQuery`1'
    // cannot be translated into SQL; select aggregates over tables from it.
    [Fact]
    public void JoinsQueryableOfOneElement()
    {
        IQueryable<int> ids = new[] { 1 }.AsQueryable();
        Expression query = Customers
            .Join(ids, c => c.Id, i => i, (c, i) => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (1)) AS t1 ("Value")
                ON t0."Id" = t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    // NotSupportedException: The element of the in-memory source 'EnumerableQuery`1'
    // cannot be translated into SQL; select aggregates over tables from it. The same
    // subquery over two elements is a VALUES list.
    [Fact]
    public void TestsExistenceInQueryableOfOneElement()
    {
        int[] ids = [1];
        Expression query = Customers
            .Where(c => ids.AsQueryable().Any(i => i == c.Id))
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE EXISTS (
                    SELECT 1
                    FROM (VALUES (1)) AS t1 ("Value")
                    WHERE t1."Value" = t0."Id"
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsOneElementRootWithoutFrom()
    {
        Expression query = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Count),
            [typeof(int)],
            new[] { 5 }.AsQueryable().Expression
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnrollsOneElementRootIntoSequenceOfOneElement()
    {
        Expression query = new[] { 0 }
            .AsQueryable()
            .SelectMany(one => new[] { 5 })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value"
                FROM (VALUES (5)) AS t0 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Func<IQueryable<Customer>, Expression>> Collections()
    {
        List<int> list = [1, 2];
        IReadOnlyList<int> readOnly = [1, 2];
        IEnumerable<int> lazy = new[] { 0, 1, 2 }.Where(i => i > 0);
        HashSet<int> set = [1, 2];
        IEnumerable<int> range = Enumerable.Range(1, 2);
        return
        [
            c => c.Join(list, x => x.Id, i => i, (x, i) => x.Name).Expression,
            c => c.Join(readOnly, x => x.Id, i => i, (x, i) => x.Name).Expression,
            c => c.Join(lazy, x => x.Id, i => i, (x, i) => x.Name).Expression,
            c => c.Join(set, x => x.Id, i => i, (x, i) => x.Name).Expression,
            c => c.Join(range, x => x.Id, i => i, (x, i) => x.Name).Expression,
            c =>
                c.Join(
                    Enumerable.Range(1, 2),
                    x => x.Id,
                    i => i,
                    (x, i) => x.Name
                ).Expression,
        ];
    }

    [Theory]
    [MemberData(nameof(Collections))]
    public void JoinsEveryKindOfSequence(Func<IQueryable<Customer>, Expression> query)
    {
        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (1), (2)) AS t1 ("Value")
                ON t0."Id" = t1."Value"
                """
            ),
            new PostgreSqlQuery(query(Customers)).TextValue
        );
    }

    [Fact]
    public void FlattensRangeComputedInTheQuery()
    {
        Expression query = Customers
            .SelectMany(c => Enumerable.Range(1, 3), (c, n) => new { c.Id, n })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Value" AS "n"
                FROM "Customer" AS t0
                CROSS JOIN (VALUES (1), (2), (3)) AS t1 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsRangeAsRoot()
    {
        Expression query = Enumerable.Range(4, 2).AsQueryable().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value"
                FROM (VALUES (4), (5)) AS t0 ("Value")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Func<Expression>, string> FirstRows =>
        new()
        {
            { () => Root(0.5m, 1m), "CAST(0.5 AS numeric)), (1" },
            { () => Root(1f, 0.5f), "CAST(1 AS real)), (0.5" },
            { () => Root<bool?>(true, null), "TRUE), (NULL" },
            {
                () => Root(double.NaN, double.PositiveInfinity),
                "CAST('NaN' AS double precision)), "
                    + "(CAST('Infinity' AS double precision)"
            },
            {
                () => Root(1d, double.NegativeInfinity),
                "CAST(1 AS double precision)), (CAST('-Infinity' AS double precision)"
            },
            { () => Root(float.NaN, 0.1f), "CAST('NaN' AS real)), (0.1" },
            {
                () => Root(double.Epsilon, double.MaxValue),
                "CAST(5E-324 AS double precision)), (1.7976931348623157E+308"
            },
            {
                () => Root(decimal.MaxValue, decimal.MinValue),
                "CAST(79228162514264337593543950335 AS numeric)), "
                    + "(-79228162514264337593543950335"
            },
            {
                () => Root(WideStatus.Low, WideStatus.High),
                "CAST(1 AS bigint)), (5000000000"
            },
            {
                () => Root<WideStatus?>(null, WideStatus.High),
                "CAST(NULL AS bigint)), (5000000000"
            },
            {
                () =>
                    Root<DateTimeOffset?>(
                        new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-3)),
                        null
                    ),
                "TIMESTAMPTZ '2024-01-01 00:00:00-03:00'), (NULL"
            },
            {
                () => Root(DateOnly.MinValue, DateOnly.MaxValue),
                "DATE '0001-01-01'), (DATE '9999-12-31'"
            },
            {
                () =>
                    Root(
                        new DateTime(2024, 1, 1, 0, 0, 0),
                        new DateTime(2024, 1, 1, 0, 0, 0, 0, 1)
                    ),
                "TIMESTAMP '2024-01-01 00:00:00'), "
                    + "(TIMESTAMP '2024-01-01 00:00:00.000001'"
            },
            {
                () => Root(TimeSpan.FromHours(-1.5), TimeSpan.FromDays(2)),
                "INTERVAL 'PT-1H-30M'), (INTERVAL 'P2D'"
            },
            { () => Root(new byte[] { 0, 255 }, []), "BYTEA '\\x00ff'), (BYTEA '\\x'" },
            {
                () => Root("tab\t", "O'Brien", "back\\slash", "\u2028"),
                "CAST(E'tab\\t' AS text)), ('O''Brien'), ('back\\slash'), (E'\\u2028'"
            },
            { () => Root("\\'", "\U0001F600"), "CAST('\\''' AS text)), ('\U0001F600'" },
        };

    [Theory]
    [MemberData(nameof(FirstRows))]
    public void WritesTypeOfColumnOnFirstRowOnly(Func<Expression> root, string rows)
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
    public void TypesNullsOfEveryMemberOnFirstRow()
    {
        Expression query = Root(
            new ValueTuple<DateTimeOffset?, TimeSpan?, byte[]?, long?>(
                null,
                null,
                null,
                null
            ),
            new ValueTuple<DateTimeOffset?, TimeSpan?, byte[]?, long?>(
                DateTimeOffset.MinValue,
                TimeSpan.Zero,
                [1],
                2
            )
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item1", t0."Item2", t0."Item3", t0."Item4"
                FROM (VALUES (CAST(NULL AS timestamptz), CAST(NULL AS interval), CAST(NULL AS bytea), CAST(NULL AS bigint)), (TIMESTAMPTZ '0001-01-01 00:00:00+00', INTERVAL 'PT0S', BYTEA '\x01', 2)) AS t0 ("Item1", "Item2", "Item3", "Item4")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsTupleOfOneMember()
    {
        Expression query = new[] { new ValueTuple<int>(1), new ValueTuple<int>(2) }
            .AsQueryable()
            .Select(t => t.Item1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item1"
                FROM (VALUES (1), (2)) AS t0 ("Item1")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsRestOfRestOfFifteenMemberTuple()
    {
        Expression query = new[]
        {
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15L),
            (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16L),
        }
            .AsQueryable()
            .Select(t => t.Item15)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Rest.Rest.Item1"
                FROM (VALUES (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, CAST(15 AS bigint)), (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16)) AS t0 ("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Rest.Item1", "Rest.Item2", "Rest.Item3", "Rest.Item4", "Rest.Item5", "Rest.Item6", "Rest.Item7", "Rest.Rest.Item1")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsRestOfReferenceTuple()
    {
        Expression query = new[]
        {
            Tuple.Create(1, 2, 3, 4, 5, 6, 7, "eight"),
            Tuple.Create(1, 2, 3, 4, 5, 6, 7, "nine"),
        }
            .AsQueryable()
            .Select(t => t.Rest.Item1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Rest.Item1"
                FROM (VALUES (1, 2, 3, 4, 5, 6, 7, CAST('eight' AS text)), (1, 2, 3, 4, 5, 6, 7, 'nine')) AS t0 ("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Rest.Item1")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsNestedTupleInRest()
    {
        Expression query = new[]
        {
            (1, 2, 3, 4, 5, 6, 7, (8, "x")),
            (1, 2, 3, 4, 5, 6, 7, (9, "y")),
        }
            .AsQueryable()
            .Select(t => t.Item8.Item2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Rest.Item1.Item2"
                FROM (VALUES (1, 2, 3, 4, 5, 6, 7, 8, CAST('x' AS text)), (1, 2, 3, 4, 5, 6, 7, 9, 'y')) AS t0 ("Item1", "Item2", "Item3", "Item4", "Item5", "Item6", "Item7", "Rest.Item1.Item1", "Rest.Item1.Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void SelectsNestedTupleMemberByMember()
    {
        (int Id, (string Name, decimal Rate) Tax)[] rows =
        [
            (1, ("a", 2m)),
            (2, ("b", 3m)),
        ];
        Expression query = rows.AsQueryable().Select(t => t.Tax).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item2.Item1" AS "Item1", t0."Item2.Item2" AS "Item2"
                FROM (VALUES (1, CAST('a' AS text), CAST(2 AS numeric)), (2, 'b', 3)) AS t0 ("Item1", "Item2.Item1", "Item2.Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsTupleAsDefaultTuple()
    {
        (int Id, string? Label)[] pairs = Pairs;
        Expression query = Customers
            .GroupJoin(pairs, c => c.Id, p => p.Id, (c, ps) => new { c, ps })
            .SelectMany(x => x.ps.DefaultIfEmpty(), (x, p) => new { x.c.Id, p })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(t1."Item1", 0) AS "p.Item1", t1."Item2" AS "p.Item2"
                FROM "Customer" AS t0
                LEFT JOIN (VALUES (1, CAST('a' AS text)), (2, 'b')) AS t1 ("Item1", "Item2")
                ON t0."Id" = t1."Item1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void LeftJoinsCorrelatedSubqueryOverValues()
    {
        Expression query = Customers
            .SelectMany(
                c => new[] { 1, 2 }.AsQueryable().Where(i => i == c.Id).DefaultIfEmpty(),
                (c, i) => new { c.Id, i }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", COALESCE(t1."Value", 0) AS "i"
                FROM "Customer" AS t0
                LEFT JOIN (VALUES (1), (2)) AS t1 ("Value")
                ON t1."Value" = t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesNullOfNullableValuesInContains()
    {
        Expression query = Orders
            .Where(o =>
                new int?[] { null, 1 }
                    .AsQueryable()
                    .Contains(o.Priority)
            )
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Priority" IN (
                    SELECT t1."Value"
                    FROM (VALUES (CAST(NULL AS integer)), (1)) AS t1 ("Value")
                )) OR ((t0."Priority" IS NULL) AND EXISTS (
                    SELECT 1
                    FROM (VALUES (CAST(NULL AS integer)), (1)) AS t2 ("Value")
                    WHERE t2."Value" IS NULL
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsNullableKeysWithEquality()
    {
        int?[] priorities = [null, 1];
        Expression query = Orders
            .Join(priorities, o => o.Priority, p => p, (o, p) => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                INNER JOIN (VALUES (CAST(NULL AS integer)), (1)) AS t1 ("Value")
                ON t0."Priority" = t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsNullableMembersOfCompositeKeyByDistinctness()
    {
        (int? Priority, int Quantity)[] keys = [(null, 1), (1, 1)];
        Expression query = Orders
            .Join(
                keys,
                o => new { o.Priority, o.Quantity },
                k => new { k.Priority, k.Quantity },
                (o, k) => o.Id
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                INNER JOIN (VALUES (CAST(NULL AS integer), 1), (1, 1)) AS t1 ("Item1", "Item2")
                ON (t0."Priority" IS NOT DISTINCT FROM t1."Item1") AND (t0."Quantity" = t1."Item2")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConvertsColumnToWiderTypeOfValues()
    {
        Expression query = Customers
            .Join([1L, 2L], c => c.Id, l => l, (c, l) => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                INNER JOIN (VALUES (CAST(1 AS bigint)), (2)) AS t1 ("Value")
                ON CAST(t0."Id" AS bigint) = t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConvertsValuesToWiderTypeOfColumn()
    {
        Expression query = Orders
            .Join([1000, 7], o => o.Points, i => i, (o, i) => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                INNER JOIN (VALUES (1000), (7)) AS t1 ("Value")
                ON t0."Points" = CAST(t1."Value" AS bigint)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsTwoValuesLists()
    {
        Expression query = new[] { 1, 2 }
            .AsQueryable()
            .Join([2, 3], a => a, b => b, (a, b) => a + b)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value" + t1."Value"
                FROM (VALUES (1), (2)) AS t0 ("Value")
                INNER JOIN (VALUES (2), (3)) AS t1 ("Value")
                ON t0."Value" = t1."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void JoinsValuesFilteredByOuterValues()
    {
        Expression query = new[] { 1, 2 }
            .AsQueryable()
            .SelectMany(
                i => new[] { 10, 20 }.AsQueryable().Where(j => j > i * 10),
                (i, j) => i + j
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value" + t1."Value"
                FROM (VALUES (1), (2)) AS t0 ("Value")
                INNER JOIN (VALUES (10), (20)) AS t1 ("Value")
                ON t1."Value" > (t0."Value" * 10)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesSortedValuesWithValuesUnderSide()
    {
        Expression query = new[] { 3, 1, 2 }
            .AsQueryable()
            .OrderBy(i => i)
            .Concat([0, 9])
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Value"
                FROM (
                    SELECT t0."Value", 0 AS "$side", t0."Value" AS "$1"
                    FROM (VALUES (3), (1), (2)) AS t0 ("Value")
                    UNION ALL
                    SELECT t1."Value", 1 AS "$side", NULL AS "$1"
                    FROM (VALUES (0), (9)) AS t1 ("Value")
                ) AS t2
                ORDER BY t2."$side", t2."$1"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnitesProjectedTuplesWithValues()
    {
        (int, string)[] pairs = [(1, "Ann"), (99, "x")];
        Expression query = Customers
            .Select(c => new ValueTuple<int, string>(c.Id, c.Name))
            .Union(pairs)
            .Select(t => t.Item2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."item2"
                FROM (
                    SELECT t0."Id" AS "item1", t0."Name" AS "item2"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Item1", t1."Item2"
                    FROM (VALUES (1, CAST('Ann' AS text)), (99, 'x')) AS t1 ("Item1", "Item2")
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void GroupsValuesByNullableKey()
    {
        Expression query = new int?[] { null, 1, null }
            .AsQueryable()
            .GroupBy(i => i)
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value" AS "Key", CAST(COUNT(*) AS integer) AS "Count"
                FROM (VALUES (CAST(NULL AS integer)), (1), (NULL)) AS t0 ("Value")
                GROUP BY t0."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersTuplesByMembers()
    {
        Expression query = new[] { (2, "b"), (1, "z"), (2, "a") }
            .AsQueryable()
            .OrderBy(t => t.Item1)
            .ThenByDescending(t => t.Item2, StringComparer.Ordinal)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Item1", t0."Item2"
                FROM (VALUES (2, CAST('b' AS text)), (1, 'z'), (2, 'a')) AS t0 ("Item1", "Item2")
                ORDER BY t0."Item1", REGEXP_REPLACE(t0."Item2", '([\U00010000-\U0010FFFF])', E'\ud7ff\\1', 'g') COLLATE "C" DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReadsElementOfValuesAtIndex()
    {
        Expression query = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.ElementAt),
            [typeof(int)],
            new[] { 3, 1, 2 }.AsQueryable().Expression,
            Expression.Constant(1)
        );

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Value"
                FROM (VALUES (3), (1), (2)) AS t0 ("Value")
                LIMIT 1 OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AggregatesSubqueryOverValuesPerRow()
    {
        Expression query = Customers
            .Select(c => new[] { 1L, 2L, 3L }.AsQueryable().Where(l => l > c.Id).Sum())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (
                    SELECT CAST(COALESCE(SUM(t1."Value"), 0) AS bigint)
                    FROM (VALUES (CAST(1 AS bigint)), (2), (3)) AS t1 ("Value")
                    WHERE t1."Value" > CAST(t0."Id" AS bigint)
                )
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    public static TheoryData<Func<Expression>, string> Refused()
    {
        int[] none = [];
        return new()
        {
            {
                () =>
                    Customers
                        .SelectMany(
                            c => Enumerable.Range(1, 3).Select(i => i * 10L),
                            (c, n) => n
                        )
                        .Expression,
                "Enumerable.Select"
            },
            {
                () =>
                    Customers
                        .SelectMany(c => Enumerable.Range(1, c.Id), (c, n) => n)
                        .Expression,
                "Enumerable.Range"
            },
            {
                () => Customers.SelectMany(c => "ab", (c, n) => n).Expression,
                "SelectMany"
            },
            {
                () =>
                    Customers
                        .SelectMany(c => none.AsQueryable().DefaultIfEmpty(), (c, n) => n)
                        .Expression,
                "at least one row"
            },
            {
                () => new[] { 0 }.AsQueryable().SelectMany(one => none).Expression,
                "at least one row"
            },
            { () => Enumerable.Range(1, 0).AsQueryable().Expression, "at least one row" },
            { () => Root((Half)1, (Half)2), "Half" },
            {
                () =>
                    Root(
                        new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
                        new DateTime(2024, 1, 2)
                    ),
                "Local"
            },
            {
                () =>
                    Root(new DateTime(2024, 1, 1).AddTicks(1), new DateTime(2024, 1, 2)),
                "microsecond"
            },
            {
                () =>
                    Root(
                        new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        new DateTime(2024, 1, 2)
                    ),
                "Utc on some rows"
            },
            { () => Root("a", "b\0c"), "NUL" },
            { () => Root<object>(1, 2), "Object" },
            { () => Root(new KeyValuePair<int, int>(1, 2), default), "KeyValuePair" },
        };
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public void RefusesWhatVALUESCannotHold(Func<Expression> query, string message)
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );
        Assert.Contains(message, exception.Message, StringComparison.Ordinal);
    }

    public static TheoryData<Func<Expression>> Unordered =>
        [
            () =>
                Expression.Call(
                    typeof(Queryable),
                    nameof(Queryable.Last),
                    [typeof(int)],
                    new[] { 3, 1 }.AsQueryable().Expression
                ),
            () => new[] { 3, 1 }.AsQueryable().Reverse().Expression,
        ];

    // The last element of an unordered source is not defined in SQL.
    [Theory]
    [MemberData(nameof(Unordered))]
    public void RefusesLastOfUnorderedValues(Func<Expression> query)
    {
        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query()).TextValue
        );
    }
}

public enum WideStatus : long
{
    Low = 1,
    High = 5_000_000_000,
}

public enum TinyStatus : byte
{
    Low = 1,
    High = 2,
}
