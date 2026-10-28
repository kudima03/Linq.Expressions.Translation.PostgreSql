namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

[Collection(SharedDatabase.Name)]
public sealed class OuterJoinSemanticsTests(Database database)
{
    [Fact]
    public void DefaultsUnmatchedValueOfLeftJoinedScalar()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Quantity)
                        .DefaultIfEmpty(),
                (c, q) => new { c.Id, Quantity = q }
            )
        );
    }

    [Fact]
    public void DefaultsUnmatchedValueMembersOfLeftJoinedValueTuple()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int, int>(
                        o.CustomerId,
                        o.Quantity
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) => new ValueTuple<int, int, int>(x.c.Id, o.Item1, o.Item2)
                )
        );
    }

    [Fact]
    public void DefaultsUnmatchedValueMembersOfEveryType()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<
                        int,
                        bool,
                        decimal,
                        Guid,
                        OrderStatus
                    >(o.CustomerId, o.IsPaid, o.Total, o.TrackingId, o.Status)),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) =>
                        new ValueTuple<int, bool, decimal, Guid, OrderStatus>(
                            x.c.Id,
                            o.Item2,
                            o.Item3,
                            o.Item4,
                            o.Item5
                        )
                )
        );
    }

    [Fact]
    public void DefaultsUnmatchedNestedValueTupleButNullsReference()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, ValueTuple<int>, string?>(
                        o.CustomerId,
                        new ValueTuple<int>(o.Quantity),
                        o.Note
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) =>
                        new ValueTuple<int, int, string?>(x.c.Id, o.Item2.Item1, o.Item3)
                )
        );
    }

    [Fact]
    public void GuardsLeftJoinedProjectionWithNullCheck()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new
                    {
                        o.CustomerId,
                        o.Quantity,
                        Note = o.Note ?? "none",
                    }),
                    c => c.Id,
                    o => o.CustomerId,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) =>
                        new
                        {
                            x.c.Id,
                            Quantity = o == null ? -1 : o.Quantity,
                            Note = o != null ? o.Note : "missing",
                            Missing = o == null,
                        }
                )
        );
    }

    [Fact]
    public void KeepsUnmatchedRowsUnderInequalityOnComputedColumn()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, string?>(
                        o.CustomerId,
                        o.Note ?? "none"
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) => new ValueTuple<int, string?>(x.c.Id, o.Item2)
                )
                .Where(r => r.Item2 != "none")
        );
    }

    [Fact]
    public void GroupsUnmatchedRowsUnderNullComputedKey()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, string?>(
                        o.CustomerId,
                        o.Note ?? "none"
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) => new ValueTuple<int, string?>(x.c.Id, o.Item2)
                )
                .GroupBy(r => r.Item2)
                .Select(g => new { g.Key, Count = g.Count() })
        );
    }

    [Fact]
    public void OrdersUnmatchedRowsFirstByComputedColumn()
    {
        database.AssertSameSequence(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, int?>(
                        o.CustomerId,
                        o.Quantity * 10
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) => new ValueTuple<int, int?>(x.c.Id, o.Item2)
                )
                .OrderBy(r => r.Item2)
                .ThenBy(r => r.Item1)
        );
    }

    [Fact]
    public void FiltersGroupOfComputedProjectionBeforeDefaultIfEmpty()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, string?>(
                        o.CustomerId,
                        o.Note ?? "none"
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.Where(m => m.Item2 == "none").DefaultIfEmpty(),
                    (x, o) => new ValueTuple<int, int?, string?>(x.c.Id, o.Item1, o.Item2)
                )
        );
    }

    [Fact]
    public void ChainsTwoLeftJoinedComputedProjections()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, string?>(
                        o.CustomerId,
                        o.Note ?? "none"
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) => new { x.c, Note = o.Item2 }
                )
                .GroupJoin(
                    Employees.Select(e => new ValueTuple<int?, string?>(
                        e.ManagerId,
                        e.Name + "!"
                    )),
                    y => y.c.Id,
                    e => e.Item1,
                    (y, reports) => new { y, reports }
                )
                .SelectMany(
                    z => z.reports.DefaultIfEmpty(),
                    (z, e) =>
                        new ValueTuple<int, string?, string?>(z.y.c.Id, z.y.Note, e.Item2)
                )
        );
    }

    [Fact]
    public void LeftJoinsPagedComputedProjectionLaterally()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Select(o => new ValueTuple<int?, string?>(
                            o.Id,
                            o.Note ?? "none"
                        ))
                        .Take(1)
                        .DefaultIfEmpty(),
                (c, o) => new ValueTuple<int, int?, string?>(c.Id, o.Item1, o.Item2)
            )
        );
    }

    [Fact]
    public void CopiesOuterColumnThroughInnerJoinedProjection()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => new ValueTuple<int, string>(o.Id, c.Name)),
                (c, o) => new ValueTuple<int, int, string>(c.Id, o.Item1, o.Item2)
            )
        );
    }

    [Fact]
    public void AggregatesComputedColumnsOfLeftJoinedRows()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, int?, string?>(
                        o.CustomerId,
                        o.Quantity * 2,
                        o.Note ?? "none"
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) =>
                        new
                        {
                            x.c.Id,
                            Doubled = o.Item2,
                            Note = o.Item3,
                        }
                )
                .GroupBy(r => r.Id)
                .Select(g => new
                {
                    g.Key,
                    Sum = g.Sum(r => r.Doubled),
                    Max = g.Max(r => r.Doubled),
                    Noted = g.Count(r => r.Note != null),
                    Rows = g.Count(),
                })
        );
    }

    [Fact]
    public void AggregatesGroupOfComputedProjectionAsSubquery()
    {
        database.AssertSameRows(
            Customers.GroupJoin(
                Orders.Select(o => new ValueTuple<int?, string?>(
                    o.CustomerId,
                    o.Note ?? "none"
                )),
                c => c.Id,
                o => o.Item1,
                (c, matches) =>
                    new
                    {
                        c.Id,
                        Unnoted = matches.Count(m => m.Item2 == "none"),
                        Any = matches.Any(),
                    }
            )
        );
    }

    [Fact]
    public void DistinguishesComputedColumnsOfLeftJoinedRows()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, string?>(
                        o.CustomerId,
                        o.Note ?? "none"
                    )),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) => new ValueTuple<string, string?>(x.c.Region, o.Item2)
                )
                .Distinct()
        );
    }

    [Fact]
    public void KeepsNullsOfThreeSidedConcatWithLastNullableSide()
    {
        database.AssertSameRows(
            Customers
                .Select(c => new { c.Id, Note = (string?)"first" })
                .Concat(Employees.Select(e => new { e.Id, Note = (string?)"second" }))
                .Concat(Orders.Select(o => new { o.Id, o.Note }))
                .Where(x => x.Note != "first")
        );
    }

    [Fact]
    public void KeepsNullsOfExceptWithNullableLeftSide()
    {
        database.AssertSameRows(
            Orders
                .Select(o => o.Note)
                .Except(Customers.Select(c => (string?)c.Name))
                .Where(n => n != "orphan")
        );
    }

    [Fact]
    public void MatchesNullItemWithNullOfUnion()
    {
        database.AssertSameRows(
            Customers
                .Where(c =>
                    Customers
                        .Select(x => (string?)x.Name)
                        .Union(Orders.Select(o => o.Note))
                        .Contains(c.Email)
                )
                .Select(c => c.Id)
        );
    }

    [Fact]
    public void DropsNullsOfUnionForOfType()
    {
        database.AssertSameRows(
            Customers
                .Select(c => (string?)c.Name)
                .Union(Orders.Select(o => o.Note))
                .OfType<string>()
        );
    }

    [Fact]
    public void UnionsByKeyWithConstantSide()
    {
        database.AssertSameRows(
            Customers
                .Select(c => new { Note = (string?)"none", Side = 1 })
                .UnionBy(Orders.Select(o => new { o.Note, Side = 2 }), x => x.Note)
                .Where(x => x.Note != "x")
        );
    }

    [Fact]
    public void SubstitutesExplicitDefaultForUnmatchedScalar()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Quantity)
                        .DefaultIfEmpty(-1),
                (c, q) => new { c.Id, Quantity = q }
            )
        );
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Note ?? "noted")
                        .DefaultIfEmpty("none"),
                (c, n) => new { c.Id, Note = n }
            )
        );
    }

    [Fact]
    public void SubstitutesCorrelatedDefaultForUnmatchedScalar()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Quantity)
                        .DefaultIfEmpty(c.Id * 100),
                (c, q) => new { c.Id, Quantity = q }
            )
        );
    }

    [Fact]
    public void SubstitutesNullDefaultForUnmatchedNullableScalar()
    {
        int? none = null;
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => o.Priority)
                        .DefaultIfEmpty(none),
                (c, p) => new { c.Id, Priority = p }
            )
        );
    }
}
