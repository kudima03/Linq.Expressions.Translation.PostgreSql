#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// An in-memory sequence read as VALUES yields its elements as LINQ to Objects does:
// the type its first row writes keeps 64-bit sums, decimal quotients and columns of
// NULLs what they are in .NET.
[Collection(SharedDatabase.Name)]
public sealed class InMemoryValuesSemanticsTests(Database database)
{
    private static readonly (int Id, string? Label)[] Labels =
    [
        (1, "one"),
        (2, null),
        (3, "O'Brien"),
        (3, "three"),
        (99, "none"),
    ];

    [Fact]
    public void JoinsRowsOnKey()
    {
        (int Id, string? Label)[] labels = Labels;

        database.AssertSameRows(
            Customers.Join(
                labels,
                c => c.Id,
                r => r.Id,
                (c, r) => new { c.Name, r.Label }
            )
        );
    }

    [Fact]
    public void LeftJoinsRowsOnKey()
    {
        (int Id, string? Label)[] labels = Labels;

        database.AssertSameRows(
            Customers
                .GroupJoin(labels, c => c.Id, r => r.Id, (c, rs) => new { c, rs })
                .SelectMany(x => x.rs.DefaultIfEmpty(), (x, r) => new { x.c.Id, r.Label })
        );
    }

    [Fact]
    public void CountsMatchesOfRows()
    {
        int[] ids = [1, 1, 2, 42];

        database.AssertSameRows(
            Customers.GroupJoin(
                ids,
                c => c.Id,
                id => id,
                (c, matches) => new { c.Id, Count = matches.Count() }
            )
        );
    }

    [Fact]
    public void ReadsRootWithFilterAndOrder()
    {
        database.AssertSameSequence(
            new[] { 5, 3, 9, 1 }.AsQueryable().Where(i => i > 2).OrderBy(i => i)
        );
    }

    [Fact]
    public void AddsBigintsBeyondInteger()
    {
        database.AssertSameRows(
            new[] { (2_000_000_000L, 2_000_000_000L), (1L, 2L) }
                .AsQueryable()
                .Select(r => r.Item1 + r.Item2)
        );
    }

    [Fact]
    public void DividesWholeDecimalsAsDecimals()
    {
        database.AssertSameRows(
            new[] { (1m, 4m), (3m, 2m) }.AsQueryable().Select(r => r.Item1 / r.Item2)
        );
    }

    [Fact]
    public void DividesWholeDoublesAsDoubles()
    {
        database.AssertSameRows(new[] { 3d, 1d }.AsQueryable().Select(d => d / 2));
    }

    [Fact]
    public void ComparesColumnOfNullsWithTypedColumn()
    {
        int?[] priorities = [null, null];

        database.AssertSameRows(
            Orders.SelectMany(
                o => priorities,
                (o, p) => new { o.Id, Same = o.Priority == p }
            )
        );
    }

    [Fact]
    public void MatchesNullableRowsByDistinctness()
    {
        int?[] priorities = [null, 1, 3];

        database.AssertSameRows(
            Orders.SelectMany(
                o => priorities,
                (o, p) =>
                    new
                    {
                        o.Id,
                        p,
                        Same = o.Priority == p,
                    }
            )
        );
    }

    [Fact]
    public void CombinesWithRows()
    {
        int[] ids = [1, 2, 2, 100];

        database.AssertSameRows(Customers.Select(c => c.Id).Concat(ids));
        database.AssertSameRows(Customers.Select(c => c.Id).Union(ids));
        database.AssertSameRows(Customers.Select(c => c.Id).Intersect(ids));
        database.AssertSameRows(Customers.Select(c => c.Id).Except(ids));
    }

    [Fact]
    public void TestsExistenceInRows()
    {
        database.AssertSameRows(
            Customers
                .Where(c => new[] { 1, 3, 77 }.AsQueryable().Any(i => i == c.Id))
                .Select(c => c.Id)
        );
    }

    [Fact]
    public void UnrollsRowsOfOneElementRoot()
    {
        database.AssertSameRows(
            new[] { 0 }
                .AsQueryable()
                .SelectMany(
                    one =>
                        new[]
                        {
                            new ValueTuple<string, int, string?>("Paid", 1, "Awaiting"),
                            new ValueTuple<string, int, string?>("Shipped", 2, null),
                        },
                    (one, row) => row
                )
                .GroupJoin(
                    Orders,
                    r => r.Item2,
                    o => (int)o.Status,
                    (r, os) =>
                        new
                        {
                            r.Item1,
                            r.Item3,
                            Count = os.Count(),
                        }
                )
        );
    }

    [Fact]
    public void ReadsRestOfLongTuple()
    {
        ValueTuple<int, int, int, int, int, int, int, ValueTuple<long, string>>[] rows =
        [
            new(1, 2, 3, 4, 5, 6, 7, new ValueTuple<long, string>(8_000_000_000, "a")),
            new(9, 10, 11, 12, 13, 14, 15, new ValueTuple<long, string>(16, "b")),
        ];

        database.AssertSameRows(
            rows.AsQueryable()
                .Select(r => new
                {
                    r.Item7,
                    r.Rest.Item1,
                    r.Rest.Item2,
                })
        );
    }

    [Fact]
    public void ReadsEveryTypeOfColumn()
    {
        ValueTuple<
            bool?,
            short,
            float,
            string?,
            Guid?,
            DateOnly?,
            ValueTuple<DateTime, TimeOnly, TimeSpan, OrderStatus?>
        >[] rows =
        [
            new(
                null,
                1,
                1.5f,
                null,
                null,
                null,
                new(
                    new DateTime(2024, 2, 29, 23, 59, 59),
                    new TimeOnly(10, 30),
                    TimeSpan.FromHours(25),
                    null
                )
            ),
            new(
                true,
                -2,
                -0.25f,
                "Zoë",
                Guid.Empty,
                new DateOnly(2024, 2, 29),
                new(
                    new DateTime(2024, 1, 1),
                    new TimeOnly(0, 0),
                    TimeSpan.Zero,
                    OrderStatus.Paid
                )
            ),
        ];

        database.AssertSameRows(rows.AsQueryable());
    }

    [Fact]
    public void OrdersTextRowsByCodePoint()
    {
        database.AssertSameSequence(
            new[] { "b", "B", "é", "a", "\U0001F600" }
                .AsQueryable()
                .OrderBy(s => s, StringComparer.Ordinal)
        );
    }

    [Fact]
    public void AggregatesRows()
    {
        (int Id, string? Label)[] labels = Labels;

        database.AssertSameRows(
            labels
                .AsQueryable()
                .GroupBy(r => r.Id)
                .Select(g => new
                {
                    g.Key,
                    Count = g.Count(),
                    Labelled = g.Count(r => r.Label != null),
                })
        );
        database.AssertSameRows(
            new[] { 0 }
                .AsQueryable()
                .Select(_ => labels.AsQueryable().Sum(r => r.Id) + Orders.Count())
        );
    }
}
