namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

[Collection(SharedDatabase.Name)]
public sealed class NullSemanticsTests(Database database)
{
    [Fact]
    public void ComparesNullableOperandsForEquality()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Second = o.Priority == 2,
                    NotSecond = o.Priority != 2,
                    Both = o.Priority == o.Discount,
                    Unnoted = o.Note == null,
                    Renamed = o.Note != o.Number,
                })
        );
    }

    [Fact]
    public void FiltersOnInequalityWithNullableOperands()
    {
        database.AssertSameRows(
            Orders.Where(o => o.Priority != 2 && o.IsGift != false).Select(o => o.Id)
        );
    }

    [Fact]
    public void ProjectsNullableComparisonsAsFalse()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Urgent = o.Priority > 1,
                    Calm = !(o.Priority > 1),
                    Gift = o.IsGift == true,
                    Shipped = o.ShippedOn >= new DateOnly(2024, 1, 1),
                })
        );
    }

    [Fact]
    public void KeepsNullsUnderNegatedComparison()
    {
        database.AssertSameRows(Orders.Where(o => !(o.Priority > 1)).Select(o => o.Id));
    }

    [Fact]
    public void TestsMembershipOfNullableValues()
    {
        int?[] firstOrNone = [1, null];
        int?[] second = [2];

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    In = firstOrNone.Contains(o.Priority),
                    NotIn = !firstOrNone.Contains(o.Priority),
                    NotSecond = !second.Contains(o.Priority),
                })
        );
    }

    [Fact]
    public void KeepsNullsOutsideNegatedMembership()
    {
        int?[] second = [2];

        database.AssertSameRows(
            Orders.Where(o => !second.Contains(o.Priority)).Select(o => o.Id)
        );
    }

    [Fact]
    public void FindsNullInSubquery()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    In = Orders
                        .Where(x => x.Id <= 3)
                        .Select(x => x.Priority)
                        .Contains(o.Priority),
                    NotIn = !Orders
                        .Where(x => x.Id <= 3)
                        .Select(x => x.Priority)
                        .Contains(o.Priority),
                })
        );
    }

    [Fact]
    public void FindsNullInArrayColumn()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, Noted = o.Tags.Contains(o.Note) })
        );
    }

    [Fact]
    public void MissesItemInArrayColumnHoldingNull()
    {
        database.AssertSameRows(
            Orders.Where(o => !o.Tags.Contains("gift")).Select(o => o.Id)
        );
    }

    [Fact]
    public void ProjectsArrayMembershipAsFalse()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Gift = o.Tags.Contains("gift"),
                    NotGift = !o.Tags.Contains("gift"),
                    Numbered = o.Tags.Contains(o.Number),
                    Express = o.Tags.Any(t => t == "express"),
                })
        );
    }

    [Fact]
    public void KeepsLiftedExclusiveOrNull()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Either = o.IsGift ^ o.IsPaid,
                    Unknown = (o.IsGift ^ o.IsPaid) == null,
                })
        );
    }

    [Fact]
    public void ConcatenatesNullAsEmpty()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Text = o.Number + "/" + o.Note,
                    Priority = "#" + o.Priority,
                    Discount = o.Note + o.Discount,
                })
        );
    }

    [Fact]
    public void OrdersNullKeysFirstAscendingAndLastDescending()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Priority)
                .ThenByDescending(o => o.IsGift)
                .ThenBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    o.Priority,
                    o.IsGift,
                })
        );
    }

    [Fact]
    public void ComparesLeftJoinedEntityWithNull()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
                .SelectMany(
                    x => x.os.DefaultIfEmpty(),
                    (x, o) =>
                        new
                        {
                            x.c.Id,
                            Number = o == null ? "none" : o.Number,
                            Missing = o == null,
                            Order = o,
                        }
                )
        );
    }

    [Fact]
    public void NullsCoalescedColumnOfLeftJoinedProjectionForUnmatchedRows()
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
                    (x, o) => new ValueTuple<int, int?, string?>(x.c.Id, o.Item1, o.Item2)
                )
        );
    }

    [Fact]
    public void NullsConstantColumnOfLeftJoinedProjectionForUnmatchedRows()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(
                    Orders.Select(o => new ValueTuple<int?, int?>(o.CustomerId, 5)),
                    c => c.Id,
                    o => o.Item1,
                    (c, matches) => new { c, matches }
                )
                .SelectMany(
                    x => x.matches.DefaultIfEmpty(),
                    (x, o) => new ValueTuple<int, int?, int?>(x.c.Id, o.Item1, o.Item2)
                )
        );
    }

    [Fact]
    public void NullsComputedColumnOfCorrelatedLeftJoinForUnmatchedRows()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => new ValueTuple<int?, string?>(
                            o.CustomerId,
                            o.Note ?? "none"
                        ))
                        .DefaultIfEmpty(),
                (c, o) => new ValueTuple<int, int?, string?>(c.Id, o.Item1, o.Item2)
            )
        );
    }

    [Fact]
    public void NullsOuterColumnOfCorrelatedLeftJoinForUnmatchedRows()
    {
        database.AssertSameRows(
            Customers.SelectMany(
                c =>
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .Select(o => new ValueTuple<int?, string?>(o.Id, c.Name))
                        .DefaultIfEmpty(),
                (c, o) => new ValueTuple<int, int?, string?>(c.Id, o.Item1, o.Item2)
            )
        );
    }

    [Fact]
    public void FindsRowsWithoutJoinPartner()
    {
        database.AssertSameRows(
            Customers
                .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, os) => new { c, os })
                .SelectMany(x => x.os.DefaultIfEmpty(), (x, o) => new { x.c, o })
                .Where(x => x.o == null)
                .Select(x => x.c.Id)
        );
    }

    [Fact]
    public void CountsNullOnceAmongDistinctValues()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.IsPaid)
                .Select(g => new
                {
                    g.Key,
                    Priorities = g.Select(o => o.Priority).Distinct().Count(),
                    Gifts = g.Select(o => o.IsGift).Distinct().Count(),
                })
        );
    }

    [Fact]
    public void DropsNullsOfNullableValues()
    {
        database.AssertSameRows(Orders.Select(o => o.Priority).OfType<int>());
    }

    [Fact]
    public void DropsNullReferences()
    {
        database.AssertSameRows(Customers.Select(c => c.Email).OfType<string>());
    }

    [Fact]
    public void JoinsOnNullableKeys()
    {
        database.AssertSameRows(
            Employees.Join(
                Employees,
                e => e.ManagerId,
                m => m.ManagerId,
                (e, m) => new { Employee = e.Id, Peer = m.Id }
            )
        );
    }

    [Fact]
    public void JoinsOnCompositeKeysWithNullableMembers()
    {
        database.AssertSameRows(
            Employees.Join(
                Employees,
                e => new { e.ManagerId, e.Name },
                m => new { m.ManagerId, m.Name },
                (e, m) => new { Employee = e.Id, Peer = m.Id }
            )
        );
    }

    [Fact]
    public void GroupJoinsOnNullableKeys()
    {
        database.AssertSameRows(
            Employees.GroupJoin(
                Employees,
                e => e.ManagerId,
                m => m.ManagerId,
                (e, ms) => new { e.Id, Peers = ms.Count() }
            )
        );
    }

    [Fact]
    public void LeftJoinsOnNullableKeys()
    {
        database.AssertSameRows(
            Employees
                .GroupJoin(
                    Employees,
                    e => e.ManagerId,
                    m => m.ManagerId,
                    (e, ms) => new { e, ms }
                )
                .SelectMany(
                    x => x.ms.DefaultIfEmpty(),
                    (x, m) => new { x.e.Id, Peer = m == null ? 0 : m.Id }
                )
        );
    }
}
