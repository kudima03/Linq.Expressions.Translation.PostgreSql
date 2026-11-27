#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// In-memory sequences inside a query: VALUES of every integer type and of chars, a
// one-element AsQueryable() wherever a sequence is read, Enumerable operators and
// ToList, AsEnumerable and AsQueryable over sequences, and tuples in a sequence of
// tuples.
[Collection(SharedDatabase.Name)]
public sealed class InMemorySequenceSemanticsTests(Database database)
{
    private static readonly int[] Ids = [1, 3, 5];

    private static readonly string[] Ann = ["Ann"];

    // The rows of an in-memory sequence behind a source of the capturing provider,
    // so that a terminal operator can be taken of them.
    private static IQueryable<T> Rows<T>(T[] values)
    {
        return Customers.Where(c => c.Id == 1).SelectMany(c => values);
    }

    // #122
    [Fact]
    public void ReadsUnsignedValuesAtTheirBoundaries()
    {
        database.AssertSameRows(new ushort[] { 0, 1, ushort.MaxValue }.AsQueryable());
        database.AssertSameRows(new uint[] { 0, 1, uint.MaxValue }.AsQueryable());
        database.AssertSameRows(
            new ulong[] { 0, long.MaxValue, ulong.MaxValue }.AsQueryable()
        );
    }

    // #122: a ulong above long.MaxValue on a later row needs numeric for the column.
    [Fact]
    public void TypesUlongAboveLongRangeOnLaterRow()
    {
        database.AssertSameRows(
            new[] { 1UL, ulong.MaxValue, (ulong)long.MaxValue + 1, 1UL }
                .AsQueryable()
                .Distinct()
        );
        database.AssertSameSequence(
            new[] { ulong.MaxValue, 1UL, (ulong)long.MaxValue + 1, (ulong)long.MaxValue }
                .AsQueryable()
                .OrderBy(u => u)
        );
        database.AssertSameRows(
            new[] { 1UL, ulong.MaxValue, 10_000_000_000_000_000_001UL }
                .AsQueryable()
                .Where(u => u > 10_000_000_000_000_000_000UL)
        );
        database.AssertSameResult(Rows([1UL, ulong.MaxValue]), q => q.Max());
    }

    // #122
    [Fact]
    public void JoinsUnsignedValuesWithTable()
    {
        ulong[] ulongs = [1, 2, 99];
        uint[] uints = [3, 4, uint.MaxValue];
        ushort[] ushorts = [5, 6, ushort.MaxValue];

        database.AssertSameRows(
            Customers.Join(ulongs, c => c.Id, i => (long)i, (c, i) => c.Name)
        );
        database.AssertSameRows(
            Customers.Join(uints, c => c.Id, i => (long)i, (c, i) => new { c.Id, i })
        );
        database.AssertSameRows(
            Customers.Join(ushorts, c => c.Id, i => i, (c, i) => new { c.Id, i })
        );
        database.AssertSameRows(
            Customers.Join(ulongs, c => (ulong)c.Id, i => i, (c, i) => new { c.Id, i })
        );
    }

    // #122
    [Fact]
    public void ComputesWithUnsignedValues()
    {
        database.AssertSameRows(
            new ushort[] { 1, ushort.MaxValue }
                .AsQueryable()
                .Select(u => u * 2)
        );
        database.AssertSameRows(
            new uint[] { 1, uint.MaxValue }
                .AsQueryable()
                .Select(u => (long)u * 2)
        );
    }

    // #122
    [Fact]
    public void ReadsTuplesOfUnsignedMembers()
    {
        (ushort Small, uint Middle, ulong Large)[] rows =
        [
            (1, 2, 3),
            (ushort.MaxValue, uint.MaxValue, ulong.MaxValue),
        ];

        database.AssertSameRows(rows.AsQueryable());
        database.AssertSameRows(
            rows.AsQueryable().Select(r => new { r.Large, Twice = r.Small * 2 })
        );
    }

    // #136
    [Fact]
    public void ReadsBytesAndSBytesAtTheirBoundaries()
    {
        database.AssertSameRows(new byte[] { 0, 1, byte.MaxValue }.AsQueryable());
        database.AssertSameRows(
            new sbyte[] { sbyte.MinValue, 0, sbyte.MaxValue }.AsQueryable()
        );
        database.AssertSameRows(
            new byte?[] { null, byte.MaxValue }
                .AsQueryable()
                .Distinct()
        );
    }

    // #136
    [Fact]
    public void JoinsBytesWithTable()
    {
        byte[] quantities = [0, 2, 7, byte.MaxValue];
        sbyte[] signed = [-1, 3, 5];

        database.AssertSameRows(
            Orders.Join(quantities, o => o.Quantity, b => b, (o, b) => new { o.Id, b })
        );
        database.AssertSameRows(
            Orders.Join(signed, o => o.Quantity, s => s, (o, s) => new { o.Id, s })
        );
    }

    // #136: C# computes bytes as int, so the products exceed smallint.
    [Fact]
    public void ComputesWithBytesAsIntegers()
    {
        database.AssertSameRows(
            new byte[] { 200, byte.MaxValue }
                .AsQueryable()
                .Select(b => b * b)
        );
        database.AssertSameRows(
            new sbyte[] { sbyte.MinValue, sbyte.MaxValue }
                .AsQueryable()
                .Select(s => s * 1000)
        );
    }

    // #136
    [Fact]
    public void ReadsCharsAsText()
    {
        database.AssertSameRows(
            new[] { 'a', 'Z', '\'', '\\', ' ', '\t', 'é', '中' }.AsQueryable()
        );
        database.AssertSameRows(new[] { 'a', 'A', 'a' }.AsQueryable().Distinct());
        database.AssertSameRows(new char?[] { null, 'x' }.AsQueryable());
    }

    // #136: chars order by code point, as the database's C.UTF-8 orders text.
    [Fact]
    public void OrdersCharsByCodePoint()
    {
        database.AssertSameSequence(
            new[] { 'b', 'B', 'a', 'é', '0', ' ' }.AsQueryable().OrderBy(c => c)
        );
    }

    // #136
    [Fact]
    public void ReadsTuplesOfBytesAndChars()
    {
        (byte Unsigned, sbyte Signed, char Letter)[] rows =
        [
            (0, -1, 'a'),
            (byte.MaxValue, sbyte.MaxValue, 'z'),
        ];

        database.AssertSameRows(rows.AsQueryable());
        database.AssertSameRows(
            Customers
                .Where(c => c.Id < 3)
                .SelectMany(c => rows, (c, r) => new { c.Id, r.Letter })
        );
    }

    // A char compares with a number by its code, and a negative integer converts to
    // a ulong by its bits.
    [Fact]
    public void ConvertsCharsAndNegativeIntegersAsDotNetDoes()
    {
        char[] codes = [(char)1, (char)3, 'a'];
        ulong[] large = [2UL, ulong.MaxValue - 2];

        database.AssertSameRows(
            Customers.Join(codes, c => c.Id, l => l, (c, l) => new { c.Id, l })
        );
        database.AssertSameRows(
            Customers.Join(large, c => (ulong)(c.Id - 5), u => u, (c, u) => c.Id)
        );
        database.AssertSameRows(
            Customers.Join(large, c => (ulong)c.Id, u => u, (c, u) => c.Id)
        );
    }

    // #137
    [Fact(Skip = "#137: a one-element AsQueryable() side of a set operation is refused")]
    public void CombinesTableWithQueryableOfOneElement()
    {
        database.AssertSameRows(Customers.Select(c => c.Name).Union(Ann.AsQueryable()));
        database.AssertSameRows(
            Customers.Select(c => c.Name).Intersect(Ann.AsQueryable())
        );
        database.AssertSameRows(Customers.Select(c => c.Name).Except(Ann.AsQueryable()));
        database.AssertSameRows(Customers.Select(c => c.Name).Concat(Ann.AsQueryable()));
    }

    // #137
    [Fact(Skip = "#137: a one-element AsQueryable() side of a set operation is refused")]
    public void CombinesQueryableOfOneElementWithTable()
    {
        database.AssertSameRows(Ann.AsQueryable().Union(Customers.Select(c => c.Name)));
        database.AssertSameRows(
            Ann.AsQueryable().Intersect(Customers.Select(c => c.Name))
        );
        database.AssertSameRows(
            new[] { "nobody" }.AsQueryable().Except(Customers.Select(c => c.Name))
        );
        database.AssertSameRows(Ann.AsQueryable().Concat(Customers.Select(c => c.Name)));
    }

    // #137
    [Fact(Skip = "#137: the one-element AsQueryable() inner of Join is refused")]
    public void JoinsQueryableOfOneElement()
    {
        database.AssertSameRows(
            Customers.Join(Ann.AsQueryable(), c => c.Name, n => n, (c, n) => c.Id)
        );
    }

    // #137
    [Fact(Skip = "#137: a one-element AsQueryable() in SelectMany is refused")]
    public void FlattensQueryableOfOneElement()
    {
        database.AssertSameRows(
            Customers.SelectMany(c => Ann.AsQueryable(), (c, n) => new { c.Id, n })
        );
        database.AssertSameRows(
            Customers.SelectMany(
                c => Ann.AsQueryable().Where(n => n == c.Name),
                (c, n) => c.Id
            )
        );
    }

    // #137
    [Fact(Skip = "#137: a subquery over a one-element AsQueryable() is refused")]
    public void FiltersOnQueryableOfOneElement()
    {
        database.AssertSameRows(
            Customers.Where(c => Ann.AsQueryable().Contains(c.Name)).Select(c => c.Id)
        );
        database.AssertSameRows(
            Customers
                .Where(c => Ann.AsQueryable().Any(n => n == c.Name))
                .Select(c => c.Id)
        );
    }

    // #137
    [Fact(Skip = "#137: a Count() over a one-element AsQueryable() is refused")]
    public void CountsQueryableOfOneElementPerRow()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Count = Ann.AsQueryable().Count(n => n == c.Name),
            })
        );
    }

    // #137: only the root of aggregates is the one-row root without FROM; an element
    // that is read is a one-row VALUES.
    [Fact(Skip = "#137: the element of a one-element AsQueryable() root is refused")]
    public void ReadsElementOfQueryableOfOneElement()
    {
        database.AssertSameRows(
            new[] { 5 }.AsQueryable().Select(x => new { x, Count = Orders.Count() })
        );
        database.AssertSameRows(new[] { 5 }.AsQueryable());
        database.AssertSameRows(new[] { 5 }.AsQueryable().Select(x => x + 1));
    }

    // #138
    [Fact(Skip = "#138: Enumerable.All over a captured array is refused")]
    public void TestsAllOfCapturedArray()
    {
        int[] ids = Ids;

        database.AssertSameRows(
            Customers.Where(c => ids.All(y => y != c.Id)).Select(c => c.Id)
        );
    }

    // #138: only Any(x => x == item) is rewritten to Contains.
    [Fact(Skip = "#138: Enumerable.Any with another predicate over an array is refused")]
    public void TestsAnyOfCapturedArrayWithComparison()
    {
        int[] ids = Ids;

        database.AssertSameRows(
            Customers.Where(c => ids.Any(y => y > c.Id)).Select(c => c.Id)
        );
    }

    // #138
    [Fact(Skip = "#138: Enumerable.Count over a captured array is refused")]
    public void CountsCapturedArrayPerRow()
    {
        int[] ids = Ids;

        database.AssertSameRows(
            Customers.Select(c => new { c.Id, Count = ids.Count(y => y < c.Id) })
        );
    }

    // #138: the call is refused even when it does not read the row.
    [Fact(Skip = "#138: Enumerable.Count over a captured array is refused")]
    public void CountsCapturedArrayWithoutReadingRow()
    {
        int[] ids = Ids;

        database.AssertSameRows(
            Customers.Select(c => new { c.Id, Count = ids.Count(y => y < 4) })
        );
        database.AssertSameRows(
            Customers.Where(c => ids.Count(y => y < 4) > 1).Select(c => c.Id)
        );
    }

    // #138: every customer's Id is at least 1, so no selection is empty.
    [Fact(Skip = "#138: Enumerable.Where over a captured array is refused")]
    public void AggregatesFilteredCapturedArrayPerRow()
    {
        int[] ids = Ids;

        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Sum = ids.Where(y => y <= c.Id).Sum(),
                Max = ids.Where(y => y <= c.Id).Max(),
                Min = ids.Where(y => y <= c.Id).Min(),
                Average = ids.Where(y => y <= c.Id).Average(),
            })
        );
    }

    // #138
    [Fact(Skip = "#138: Enumerable.Any over a table read as IEnumerable is refused")]
    public void TestsExistenceInTableReadAsSequence()
    {
        database.AssertSameRows(
            Customers
                .Where(c => ((IEnumerable<Order>)Orders).Any(o => o.CustomerId == c.Id))
                .Select(c => c.Id)
        );
    }

    // #138
    [Fact(Skip = "#138: the query below Enumerable.Any of a sequence is refused")]
    public void TestsExistenceInQueryReadAsSequence()
    {
        IQueryable<Order> paid = Orders.Where(o => o.IsPaid);
        IQueryable<Employee> reports = Trees.WithRecursive(
            Employees.Where(e => e.ManagerId == null),
            level => level.Join(Employees, m => m.Id, e => e.ManagerId, (m, e) => e)
        );

        database.AssertSameRows(
            Customers
                .Where(c => ((IEnumerable<Order>)paid).Any(o => o.CustomerId == c.Id))
                .Select(c => c.Id)
        );
        database.AssertSameRows(
            Customers
                .Where(c => ((IEnumerable<Employee>)reports).Any(e => e.Id == c.Id))
                .Select(c => c.Id)
        );
    }

    // #138: ToList and AsEnumerable of a subquery are the IN of that subquery.
    [Fact(Skip = "#138: ToList/AsEnumerable over a subquery is refused")]
    public void FiltersOnSubqueryAsListOrSequence()
    {
        database.AssertSameRows(
            Customers
                .Where(c =>
                    Orders
                        .Where(o => o.IsPaid)
                        .Select(o => o.CustomerId)
                        .ToList()
                        .Contains(c.Id)
                )
                .Select(c => c.Id)
        );
        database.AssertSameRows(
            Customers
                .Where(c =>
                    Orders
                        .Where(o => o.IsPaid)
                        .Select(o => o.CustomerId)
                        .AsEnumerable()
                        .Contains(c.Id)
                )
                .Select(c => c.Id)
        );
    }

    // #138
    [Fact(Skip = "#138: AsQueryable over a query that is not a root is refused")]
    public void TestsExistenceInSubqueryMadeQueryableAgain()
    {
        database.AssertSameRows(
            Customers
                .Where(c =>
                    Orders
                        .Where(o => o.IsPaid)
                        .AsQueryable()
                        .Any(o => o.CustomerId == c.Id)
                )
                .Select(c => c.Id)
        );
    }

    // #139
    [Fact(Skip = "#139: Contains of a tuple in a sequence of tuples is refused")]
    public void FiltersOnTupleInSequenceOfTuples()
    {
        (int, string)[] pairs = [(1, "Ann"), (2, "x"), (12, "Ann")];
        (long, string)[] wide = [(1, "Ann"), (3_000_000_000, "bob"), (2, "bob")];

        database.AssertSameRows(
            Customers
                .Where(c => pairs.Contains(new ValueTuple<int, string>(c.Id, c.Name)))
                .Select(c => c.Id)
        );
        database.AssertSameRows(
            Customers
                .Where(c =>
                    pairs
                        .AsQueryable()
                        .Contains(new ValueTuple<int, string>(c.Id, c.Name))
                )
                .Select(c => c.Id)
        );
        database.AssertSameRows(
            Customers
                .Where(c => wide.Contains(new ValueTuple<long, string>(c.Id, c.Name)))
                .Select(c => c.Id)
        );
    }

    // #139
    [Fact(Skip = "#139: Contains of a tuple in a sequence of tuples is refused")]
    public void SelectsTupleMembershipAsValue()
    {
        (int, string)[] pairs = [(1, "Ann"), (2, "x")];

        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Listed = pairs.Contains(new ValueTuple<int, string>(c.Id, c.Name)),
            })
        );
    }

    // #139: tuples are equal when their null members are, as EqualityComparer has it.
    [Fact(Skip = "#139: Contains of a tuple in a sequence of tuples is refused")]
    public void MatchesNullMembersOfTuples()
    {
        ValueTuple<int?, int>[] keys = [new(null, 1), new(3, 0), new(null, 99)];

        database.AssertSameRows(
            Orders
                .Where(o =>
                    keys.Contains(new ValueTuple<int?, int>(o.Priority, o.Quantity))
                )
                .Select(o => o.Id)
        );
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Listed = keys.AsQueryable()
                    .Contains(new ValueTuple<int?, int>(o.Priority, o.Quantity)),
            })
        );
    }
}
