#pragma warning disable CA1861 // the constant arrays are the sources under test

using Linq.Expressions.Translation.PostgreSql.Tests.Operators;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Edge cases of in-memory sequences read as VALUES: the order of the sequence, the
// values of every column type at their boundaries, the type the first row writes,
// null semantics against table columns and aggregates over the rows.
[Collection(SharedDatabase.Name)]
public sealed class InMemoryValuesEdgeCaseSemanticsTests(Database database)
{
    private static readonly (int Id, string? Label)[] Pairs =
    [
        (1, "x"),
        (1, "y"),
        (3, null),
        (42, "none"),
    ];

    // The rows of an in-memory sequence behind a source of the capturing provider,
    // so that a terminal operator can be taken of them.
    private static IQueryable<T> Rows<T>(T[] values)
    {
        return Customers.Where(c => c.Id == 1).SelectMany(c => values);
    }

    [Fact(Skip = "#134: Distinct().Take(n) of an in-memory sequence ignores its order")]
    public void TakesFirstDistinctValuesInSequenceOrder()
    {
        // SELECT DISTINCT t0."Value" FROM (VALUES (3), (1), (3), (2)) AS t0 ("Value")
        // LIMIT 2 yields 1 and 2 where LINQ yields 3 and 1.
        database.AssertSameRows(new[] { 3, 1, 3, 2 }.AsQueryable().Distinct().Take(2));
    }

    [Fact(Skip = "#134: GroupBy().Take(n) of an in-memory sequence ignores its order")]
    public void TakesFirstGroupsInSequenceOrder()
    {
        // SELECT t0."Value" FROM (VALUES (3), (1), (3), (2)) AS t0 ("Value")
        // GROUP BY t0."Value" LIMIT 2 yields 1 and 2 where LINQ yields 3 and 1.
        database.AssertSameRows(
            new[] { 3, 1, 3, 2 }.AsQueryable().GroupBy(i => i).Select(g => g.Key).Take(2)
        );
    }

    [Fact]
    public void PagesSequenceInItsOrder()
    {
        database.AssertSameSequence(new[] { 5, 3, 9, 1 }.AsQueryable().Skip(1).Take(2));
    }

    [Fact]
    public void TakesFromConcatenatedSequencesInOrder()
    {
        database.AssertSameSequence(new[] { 3, 1 }.AsQueryable().Concat([2, 0]).Take(3));
    }

    [Fact]
    public void ConcatenatesSortedSequenceWithSequenceInItsOrder()
    {
        database.AssertSameSequence(
            new[] { 3, 1, 2 }.AsQueryable().OrderBy(i => i).Concat([7, 0, 9])
        );
    }

    [Fact]
    public void ReadsFirstAndElementAtOfSequence()
    {
        database.AssertSameResult(Rows([3, 1, 2]), q => q.First());
        database.AssertSameResult(Rows([3, 1, 2]), q => q.ElementAt(1));
        database.AssertSameResult(Rows([3, 1, 2]), q => q.FirstOrDefault(i => i > 5));
    }

    [Fact]
    public void AggregatesEmptySelectionOfSequence()
    {
        database.AssertSameResult(Rows([1, 2]), q => q.Where(i => i > 5).Sum());
        database.AssertSameResult(Rows([1, 2]), q => q.Where(i => i > 5).Min());
        database.AssertSameResult(Rows([1, 2]), q => q.Where(i => i > 5).Average());
        database.AssertSameResult(Rows(new int?[] { null, null }), q => q.Max());
        database.AssertSameResult(Rows([1, 2]), q => q.Any(i => i > 5));
        database.AssertSameResult(Rows([1, 2]), q => q.All(i => i > 0));
    }

    [Fact]
    public void AggregatesTypedColumns()
    {
        database.AssertSameResult(Rows([1, 2]), q => q.Average());
        database.AssertSameResult(Rows([3_000_000_000L, 3_000_000_000L]), q => q.Sum());
        database.AssertSameResult(Rows([1m, 2m]), q => q.Average());
        database.AssertSameResult(Rows([1m, 2m]), q => q.Sum() / 3);
        database.AssertSameResult(Rows(new decimal?[] { null, 2m, 3m }), q => q.Sum());
        database.AssertSameResult(Rows([true, false, true]), q => q.Count(b => b));
    }

    [Fact(Skip = "#132: Max of a double column with NaN is NaN where .NET skips NaN")]
    public void TakesMaximumOfDoublesWithNaN()
    {
        // SELECT MAX(...) is NaN, as PostgreSQL orders NaN above every number; LINQ's
        // Max of 1, NaN, 2 is 2.
        database.AssertSameResult(Rows([1, double.NaN, 2]), q => q.Max());
    }

    [Fact(Skip = "#132: Min of a double column with NaN skips NaN where .NET returns it")]
    public void TakesMinimumOfDoublesWithNaN()
    {
        // SELECT MIN(...) is 1; LINQ's Min of 1, NaN, 2 is NaN.
        database.AssertSameResult(Rows([1, double.NaN, 2]), q => q.Min());
    }

    [Fact(Skip = "#132: OrderBy of doubles sorts NaN last where .NET sorts it first")]
    public void OrdersDoublesWithNaNAsDotNet()
    {
        // ORDER BY t0."Value" yields -Infinity, -1, 1, NaN; LINQ yields NaN first.
        database.AssertSameSequence(
            new[] { 1, double.NaN, -1, double.NegativeInfinity }
                .AsQueryable()
                .OrderBy(d => d)
        );
    }

    [Fact(Skip = "#132: comparisons of a double NaN are true in SQL, false in .NET")]
    public void ComparesNaNAsDotNet()
    {
        // NaN = NaN and NaN > 0 are TRUE in PostgreSQL and false in C#.
        double[] others = [double.NaN, 0];

        database.AssertSameRows(
            new[] { double.NaN, 1 }
                .AsQueryable()
                .SelectMany(
                    d => others,
                    (d, o) =>
                        new
                        {
                            d,
                            o,
                            Same = d == o,
                            Greater = d > o,
                        }
                )
        );
    }

    [Fact]
    public void JoinsNaNWithNaN()
    {
        // Join compares keys with Equals, for which NaN equals NaN, as = does in SQL.
        database.AssertSameRows(
            Orders.Join(
                [double.NaN, 2.5],
                o => o.Weight,
                w => w,
                (o, w) => new { o.Id, w }
            )
        );
    }

    [Fact]
    public void ReadsDoublesAtTheirBoundaries()
    {
        database.AssertSameRows(
            new[]
            {
                double.MaxValue,
                double.Epsilon,
                double.NegativeInfinity,
                double.PositiveInfinity,
                double.NaN,
                0.1,
                -1e-300,
            }.AsQueryable()
        );
    }

    [Fact]
    public void ReadsInfinityOnFirstRow()
    {
        database.AssertSameRows(
            new[] { double.PositiveInfinity, 1.5 }.AsQueryable().Select(d => d * 2)
        );
    }

    [Fact(Skip = "#131: a -0.0 double literal is the numeric -0, which loses its sign")]
    public void KeepsSignOfNegativeZero()
    {
        // VALUES (CAST(-0 AS double precision)), (1.5): the numeric -0 is 0, so its
        // text is "0" where .NET formats -0.0 as "-0".
        database.AssertSameRows(
            new[] { -0.0, 1.5 }.AsQueryable().Select(d => d.ToString())
        );
    }

    [Fact]
    public void ReadsFloatsAtTheirBoundaries()
    {
        database.AssertSameRows(
            new[]
            {
                0.1f,
                float.MaxValue,
                float.MinValue,
                float.NaN,
                -0.25f,
            }.AsQueryable()
        );
    }

    [Fact]
    public void ReadsDecimalsAtTheirBoundaries()
    {
        database.AssertSameRows(
            new[]
            {
                1m,
                1.5m,
                decimal.MaxValue,
                decimal.MinValue,
                0.0000000001m,
                1.000m,
            }.AsQueryable()
        );
    }

    [Fact]
    public void DividesWholeDecimalOfFirstRowAsDecimal()
    {
        database.AssertSameRows(
            new[] { 2m, 2.5m, 7m }.AsQueryable().Select(d => new { d, Half = d / 4 })
        );
    }

    [Fact]
    public void TypesNullOfFirstRowForLaterValues()
    {
        database.AssertSameRows(
            new[]
            {
                new ValueTuple<long?, decimal?, double?, string?>(null, null, null, null),
                new ValueTuple<long?, decimal?, double?, string?>(
                    3_000_000_000,
                    5m,
                    3,
                    "a"
                ),
            }
                .AsQueryable()
                .Select(r => new
                {
                    Twice = r.Item1 * 2,
                    Quarter = r.Item2 / 4,
                    Half = r.Item3 / 2,
                    r.Item4,
                })
        );
    }

    [Fact]
    public void TypesNullsInRestOfLongTuple()
    {
        ValueTuple<
            int,
            int,
            int,
            int,
            int,
            int,
            int,
            ValueTuple<string?, decimal?>
        >[] rows =
        [
            new(1, 2, 3, 4, 5, 6, 7, new ValueTuple<string?, decimal?>(null, null)),
            new(8, 9, 10, 11, 12, 13, 14, new ValueTuple<string?, decimal?>("a", 1m)),
        ];

        database.AssertSameRows(
            rows.AsQueryable().Select(r => new { r.Item1, Quarter = r.Rest.Item2 / 4 })
        );
    }

    [Fact]
    public void ReadsFifteenMemberTuples()
    {
        database.AssertSameRows(
            new[]
            {
                (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15L),
                (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 5_000_000_000L),
            }
                .AsQueryable()
                .Select(r => new
                {
                    r.Item8,
                    r.Item14,
                    Twice = r.Item15 * 2,
                })
        );
    }

    [Fact]
    public void ReadsReferenceTupleOfEightMembers()
    {
        database.AssertSameRows(
            new[]
            {
                new Tuple<int, int, int, int, int, int, int, Tuple<string?>>(
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    new Tuple<string?>(null)
                ),
                new Tuple<int, int, int, int, int, int, int, Tuple<string?>>(
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    new Tuple<string?>("eight")
                ),
            }
                .AsQueryable()
                .Select(r => new { r.Item7, r.Rest.Item1 })
        );
    }

    [Fact]
    public void ReadsNestedTupleAsWhole()
    {
        (int Id, (string Name, decimal Rate) Tax)[] rows =
        [
            (1, ("low", 7m)),
            (2, ("high", 0.19m)),
        ];

        database.AssertSameRows(rows.AsQueryable().Select(r => r.Tax));
    }

    [Fact]
    public void ReadsStringsThatNeedEscaping()
    {
        database.AssertSameRows(
            new[]
            {
                "O'Brien",
                "back\\slash",
                "''",
                "\\'",
                "tab\there",
                "line\nbreak\r\n",
                "\u0085\u00a0\u2028",
                "\U0001F600",
                "\u0001\u001f",
                string.Empty,
                " ",
            }.AsQueryable()
        );
    }

    [Fact]
    public void JoinsStringsThatNeedEscapingWithTable()
    {
        database.AssertSameRows(
            Customers.Join(
                [
                    "O'Brien",
                    "back\\slash",
                    "50% off",
                    "  padded\t ",
                    "\u00a0nbsp\u2003",
                    "\U0001F600 smile",
                    string.Empty,
                    "ann",
                ],
                c => c.Name,
                n => n,
                (c, n) => new { c.Id, n }
            )
        );
    }

    [Fact]
    public void OrdersGuidsAsDotNet()
    {
        database.AssertSameSequence(
            new[]
            {
                new Guid("ffffffff-0000-0000-0000-000000000000"),
                new Guid("00000000-0000-0000-0000-000000000001"),
                new Guid("80000000-0000-0000-0000-000000000000"),
                new Guid("7fffffff-ffff-ffff-ffff-ffffffffffff"),
                new Guid("00000000-8000-0000-0000-000000000000"),
                new Guid("00000000-0000-8000-0000-000000000000"),
                new Guid("00000000-0000-0000-8000-000000000000"),
                new Guid("00000000-0000-0000-0080-000000000000"),
            }
                .AsQueryable()
                .OrderBy(g => g)
        );
    }

    [Fact]
    public void JoinsGuidsWithTable()
    {
        database.AssertSameRows(
            Orders.Join(
                [
                    new Guid("ffffffff-ffff-4fff-bfff-ffffffffffff"),
                    new Guid("00000000-0000-0000-0000-000000000011"),
                    Guid.Empty,
                ],
                o => o.TrackingId,
                g => g,
                (o, g) => o.Id
            )
        );
    }

    [Fact]
    public void ComparesDateTimesOfEveryKindByTicks()
    {
        database.AssertSameRows(
            Orders.Join(
                [
                    new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                    new DateTime(2023, 12, 31, 23, 0, 0, DateTimeKind.Unspecified),
                ],
                o => o.CreatedAt,
                d => d,
                (o, d) => o.Id
            )
        );
        database.AssertSameRows(
            new[]
            {
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            }
                .AsQueryable()
                .Distinct()
        );
    }

    [Fact]
    public void ReadsDatesAndTimesAtTheirBoundaries()
    {
        database.AssertSameRows(
            new[]
            {
                (DateOnly.MinValue, new TimeOnly(0, 0), new DateTime(1, 1, 1)),
                (
                    DateOnly.MaxValue,
                    new TimeOnly(23, 59, 59, 999, 999),
                    new DateTime(9999, 12, 31, 23, 59, 59, 999, 999)
                ),
                (
                    new DateOnly(2024, 2, 29),
                    new TimeOnly(12, 0, 0, 0, 1),
                    new DateTime(2024, 2, 29, 0, 0, 0, 0, 1)
                ),
            }.AsQueryable()
        );
    }

    [Fact(Skip = "#130: a TimeOnly literal drops the ticks below a microsecond")]
    public void KeepsTicksOfTimeOnly()
    {
        // VALUES (TIME '00:00:00'), (TIME '23:59:59.999999') for new TimeOnly(1) and
        // TimeOnly.MaxValue: the 100 ns ticks are truncated silently, where a DateTime
        // constant with them is refused.
        database.AssertSameRows(
            new[] { new TimeOnly(1), TimeOnly.MaxValue }.AsQueryable()
        );
    }

    [Fact(Skip = "#130: a TimeSpan literal drops the ticks below a microsecond")]
    public void KeepsTicksOfTimeSpan()
    {
        // VALUES (INTERVAL 'PT0.0000001S'), (INTERVAL 'PT0S'): PostgreSQL rounds the
        // interval to microseconds, so both rows are zero and Distinct keeps one.
        database.AssertSameRows(
            new[] { TimeSpan.FromTicks(1), TimeSpan.Zero }.AsQueryable().Distinct()
        );
    }

    [Fact]
    public void ReadsDateTimeOffsetsAsInstants()
    {
        DateTimeOffset[] moments =
        [
            new(2024, 1, 15, 9, 0, 0, TimeSpan.Zero),
            new(2024, 1, 15, 12, 0, 0, TimeSpan.FromHours(3)),
            new(2024, 1, 1, 5, 45, 0, TimeSpan.FromMinutes(345)),
            new(2024, 3, 9, 20, 0, 0, TimeSpan.FromHours(-5)),
        ];

        database.AssertSameRows(moments.AsQueryable().Distinct());
        database.AssertSameRows(
            Orders.Join(moments, o => o.PaidAt, m => m, (o, m) => new { o.Id, m })
        );
    }

    [Fact]
    public void ComparesNullableDateTimeOffsetsWithTable()
    {
        DateTimeOffset?[] moments = [null, new(2024, 1, 15, 9, 0, 0, TimeSpan.Zero)];

        database.AssertSameRows(
            Orders.SelectMany(o => moments, (o, m) => new { o.Id, Same = o.PaidAt == m })
        );
    }

    [Fact]
    public void ReadsDurations()
    {
        database.AssertSameRows(
            new[]
            {
                TimeSpan.FromHours(-1.5),
                TimeSpan.FromDays(1),
                TimeSpan.FromHours(24),
                TimeSpan.FromMilliseconds(1.5),
                TimeSpan.FromDays(-400),
                new TimeSpan(10_000, 23, 59, 59, 999),
            }.AsQueryable()
        );
        database.AssertSameRows(
            new[] { TimeSpan.FromDays(1), TimeSpan.FromHours(24), TimeSpan.FromHours(25) }
                .AsQueryable()
                .Distinct()
        );
    }

    [Fact]
    public void OrdersDurations()
    {
        database.AssertSameSequence(
            new[]
            {
                TimeSpan.FromHours(25),
                TimeSpan.FromDays(1).Add(TimeSpan.FromSeconds(1)),
                TimeSpan.FromHours(-1),
                TimeSpan.FromMinutes(-61),
            }
                .AsQueryable()
                .OrderBy(t => t)
        );
    }

    [Fact]
    public void ReadsBytes()
    {
        database.AssertSameRows(
            new[] { new byte[] { 0, 255 }, [], [0x5c, 0x27] }.AsQueryable()
        );
    }

    [Fact]
    public void JoinsEnumsWithTable()
    {
        database.AssertSameRows(
            Orders.Join(
                [OrderStatus.Shipped, OrderStatus.Cancelled],
                o => o.Status,
                s => s,
                (o, s) => new { o.Id, s }
            )
        );
        database.AssertSameRows(
            Orders.Join(
                [ShippingOptions.Express | ShippingOptions.Insured, ShippingOptions.None],
                o => o.Options,
                s => s,
                (o, s) => new { o.Id, s }
            )
        );
    }

    [Fact]
    public void ReadsEnumsOfLongUnderlyingType()
    {
        database.AssertSameRows(
            new[] { WideStatus.Low, WideStatus.High }
                .AsQueryable()
                .Select(s => new { s, Twice = (long)s * 2 })
        );
    }

    [Fact]
    public void JoinsBooleansWithTable()
    {
        database.AssertSameRows(
            Orders.Join([true, true], o => o.IsPaid, b => b, (o, b) => o.Id)
        );
        database.AssertSameRows(
            Orders.GroupJoin(
                new bool?[] { null, false, null },
                o => o.IsGift,
                b => b,
                (o, bs) => new { o.Id, Count = bs.Count() }
            )
        );
    }

    [Fact]
    public void ComparesValuesOfWiderTypeWithTableColumn()
    {
        database.AssertSameRows(
            Customers.Join(
                [1L, 3L, 5_000_000_000L],
                c => c.Id,
                l => l,
                (c, l) => new { c.Id, l }
            )
        );
        database.AssertSameRows(
            Orders.Join([1000, 7, -50], o => o.Points, i => i, (o, i) => new { o.Id, i })
        );
        database.AssertSameRows(
            Orders.Join(
                [100m, 2.50m, -2.5m],
                o => o.Total,
                d => d,
                (o, d) => new { o.Id, d }
            )
        );
        database.AssertSameRows(
            Orders.Join(
                [2.5, 0.5, -1.5],
                o => o.Weight,
                w => w,
                (o, w) => new { o.Id, w }
            )
        );
    }

    [Fact]
    public void FiltersTableOnNullableValuesSubquery()
    {
        database.AssertSameRows(
            Orders
                .Where(o =>
                    new int?[] { null, 3 }
                        .AsQueryable()
                        .Contains(o.Priority)
                )
                .Select(o => o.Id)
        );
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                In = new int?[] { 2, 3 }
                    .AsQueryable()
                    .Contains(o.Priority),
                Null = new int?[] { null, 3 }
                    .AsQueryable()
                    .Contains(o.Priority),
            })
        );
    }

    [Fact]
    public void CombinesNullableColumnWithValuesHoldingNull()
    {
        int?[] priorities = [null, 1, 1];

        database.AssertSameRows(Orders.Select(o => o.Priority).Except(priorities));
        database.AssertSameRows(Orders.Select(o => o.Priority).Intersect(priorities));
        database.AssertSameRows(Orders.Select(o => o.Priority).Union(priorities));
        database.AssertSameRows(Orders.Select(o => o.Priority).Concat(priorities));
    }

    [Fact]
    public void NeverJoinsNullKeys()
    {
        int?[] priorities = [null, 1, 3];

        database.AssertSameRows(
            Orders.Join(priorities, o => o.Priority, p => p, (o, p) => new { o.Id, p })
        );
        database.AssertSameRows(
            Orders.GroupJoin(
                priorities,
                o => o.Priority,
                p => p,
                (o, ps) => new { o.Id, Count = ps.Count() }
            )
        );
    }

    [Fact]
    public void JoinsCompositeKeysWithNullMembers()
    {
        (int? Priority, int Quantity)[] keys = [(null, 1), (3, 0), (null, 99)];

        database.AssertSameRows(
            Orders.Join(
                keys,
                o => new { o.Priority, o.Quantity },
                k => new { k.Priority, k.Quantity },
                (o, k) => o.Id
            )
        );
    }

    [Fact]
    public void LeftJoinsTuplesAsDefaultTuple()
    {
        (int Id, string? Label)[] pairs = Pairs;

        database.AssertSameRows(
            Customers
                .GroupJoin(pairs, c => c.Id, p => p.Id, (c, ps) => new { c, ps })
                .SelectMany(
                    x => x.ps.DefaultIfEmpty(),
                    (x, p) =>
                        new
                        {
                            x.c.Id,
                            p,
                            Unlabelled = p.Label == null,
                        }
                )
        );
    }

    [Fact]
    public void LeftJoinsCorrelatedValuesSubquery()
    {
        (int Id, string? Label)[] pairs = Pairs;

        database.AssertSameRows(
            Customers.SelectMany(
                c => pairs.AsQueryable().Where(p => p.Id == c.Id).DefaultIfEmpty(),
                (c, p) =>
                    new
                    {
                        c.Id,
                        Pair = p.Id,
                        p.Label,
                    }
            )
        );
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    new[] { 1, 3, 3 }
                        .AsQueryable()
                        .Where(i => i >= c.Id)
                        .DefaultIfEmpty(),
                (c, i) => new { c.Id, i }
            )
        );
    }

    [Fact]
    public void AggregatesCorrelatedValuesSubqueries()
    {
        long[] longs = [3_000_000_000, 2, 5];
        int?[] nullable = [null, 4, 2];

        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Count = longs.AsQueryable().Count(l => l > c.Id),
                Any = longs.AsQueryable().Any(l => l == c.Id),
                Sum = longs.AsQueryable().Where(l => l > c.Id).Sum(),
                Max = nullable.AsQueryable().Where(i => i > c.Id).Max(),
                Average = longs
                    .AsQueryable()
                    .Where(l => l < c.Id)
                    .Select(l => (double?)l)
                    .Average(),
            })
        );
    }

    [Fact]
    public void ReadsFirstOfCorrelatedValuesSubquery()
    {
        (int Id, string? Label)[] pairs = Pairs;

        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Label = pairs
                    .AsQueryable()
                    .Where(p => p.Id == c.Id)
                    .Select(p => p.Label)
                    .FirstOrDefault(),
            })
        );
    }

    [Fact]
    public void GroupsTuplesWithTypedAggregates()
    {
        (int Key, long Big, decimal Whole, string? Name)[] rows =
        [
            (1, 3_000_000_000, 1m, "a"),
            (1, 3_000_000_000, 2m, null),
            (2, 1, 3m, "b"),
        ];

        database.AssertSameRows(
            rows.AsQueryable()
                .GroupBy(r => r.Key)
                .Select(g => new
                {
                    g.Key,
                    Big = g.Sum(r => r.Big),
                    Average = g.Average(r => r.Whole),
                    Named = g.Count(r => r.Name != null),
                    Max = g.Max(r => r.Name),
                })
        );
    }

    [Fact]
    public void GroupsByNullableKey()
    {
        database.AssertSameRows(
            new int?[] { null, 1, null, 2 }
                .AsQueryable()
                .GroupBy(i => i)
                .Select(g => new { g.Key, Count = g.Count() })
        );
    }

    [Fact]
    public void RemovesDuplicateTuplesWithNulls()
    {
        database.AssertSameRows(
            new (int, string?)[] { (1, "a"), (1, "a"), (2, null), (2, null), (2, "") }
                .AsQueryable()
                .Distinct()
        );
    }

    [Fact]
    public void JoinsDuplicateRows()
    {
        database.AssertSameRows(
            Customers.Join([1, 1, 1, 2], c => c.Id, i => i, (c, i) => c.Name)
        );
    }

    [Fact]
    public void UnitesProjectedTuplesWithValues()
    {
        (int, string)[] pairs = [(1, "Ann"), (99, "x")];

        database.AssertSameRows(
            Customers.Select(c => new ValueTuple<int, string>(c.Id, c.Name)).Union(pairs)
        );
        database.AssertSameRows(
            Customers
                .Select(c => new ValueTuple<int, string>(c.Id, c.Name))
                .Union(pairs)
                .Select(t => t.Item1)
        );
    }

    [Fact]
    public void CombinesValuesOfWiderTypeWithTable()
    {
        database.AssertSameRows(
            Orders.Select(o => (long?)o.Points).Concat([null, 5_000_000_000])
        );
        database.AssertSameRows(
            new[] { 3_000_000_000L, 1L }
                .AsQueryable()
                .Concat(Orders.Select(o => o.Points))
        );
        database.AssertSameRows(Orders.Select(o => o.Total).Union([100m, 0.5m, 2.50m]));
    }

    [Fact]
    public void IntersectsTwoValuesLists()
    {
        database.AssertSameRows(
            new[] { "a", "A", "b", "b" }.AsQueryable().Intersect(["A", "b", "c"])
        );
        database.AssertSameRows(new[] { 1, 2, 2, 3 }.AsQueryable().Except([3, 4]));
    }

    [Fact]
    public void JoinsTwoValuesLists()
    {
        database.AssertSameRows(
            new[] { 1, 2, 2 }
                .AsQueryable()
                .Join([2, 2, 3], a => a, b => b, (a, b) => (a * 10) + b)
        );
        database.AssertSameRows(
            new[] { 1, 2 }
                .AsQueryable()
                .SelectMany(
                    i => new[] { 10, 20 }.AsQueryable().Where(j => j > i * 10),
                    (i, j) => i + j
                )
        );
    }

    [Fact]
    public void ReadsTableForEachValue()
    {
        database.AssertSameRows(
            new[] { 1, 2, 99 }
                .AsQueryable()
                .SelectMany(
                    i => Orders.Where(o => o.CustomerId == i),
                    (i, o) => new { i, o.Id }
                )
        );
        database.AssertSameRows(
            new[] { 1, 2, 99 }
                .AsQueryable()
                .Select(i => new { i, Count = Orders.Count(o => o.CustomerId == i) })
        );
    }

    [Fact]
    public void ReadsSequencesOfEveryCollection()
    {
        List<int> list = [1, 2];
        IReadOnlyList<int> readOnly = [3, 4];
        IEnumerable<int> lazy = new[] { 5, 6, 7 }.Where(i => i != 6);
        HashSet<int> set = [8, 9];
        IEnumerable<long> range = Enumerable.Range(1, 3).Select(i => i * 4_000_000_000L);

        database.AssertSameRows(
            Customers
                .Join(list, c => c.Id, i => i, (c, i) => c.Id)
                .Concat(Customers.Join(readOnly, c => c.Id, i => i, (c, i) => c.Id))
                .Concat(Customers.Join(lazy, c => c.Id, i => i, (c, i) => c.Id))
                .Concat(Customers.Join(set, c => c.Id, i => i, (c, i) => c.Id))
        );
        database.AssertSameRows(
            Customers.SelectMany(c => range, (c, l) => new { c.Id, l })
        );
        database.AssertSameRows(
            Customers.SelectMany(c => Enumerable.Range(1, 2), (c, n) => new { c.Id, n })
        );
    }
}
