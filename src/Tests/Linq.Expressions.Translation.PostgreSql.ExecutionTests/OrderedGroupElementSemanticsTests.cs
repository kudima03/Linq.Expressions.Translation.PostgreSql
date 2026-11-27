namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Edge cases of First, FirstOrDefault, Last and LastOrDefault of a value of a sorted
// group, and of Min and Max of strings with StringComparer.Ordinal: nullable keys
// (nulls first ascending, as in .NET), nullable values, defaults of empty filtered
// groups, predicates, groups of joins and GroupJoin, and code-point order.
[Collection(SharedDatabase.Name)]
public sealed class OrderedGroupElementSemanticsTests(Database database)
{
    [Fact]
    public void SortsNullableKeysNullsFirstAscending()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    First = g.OrderBy(o => o.Priority)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .First(),
                    Last = g.OrderBy(o => o.Priority)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .Last(),
                    DownFirst = g.OrderByDescending(o => o.Priority)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .First(),
                    DownLast = g.OrderByDescending(o => o.Priority)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .Last(),
                })
        );
    }

    [Fact]
    public void SortsNullableKeysOfOtherTypes()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Gift = g.OrderBy(o => o.IsGift)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .First(),
                    GiftLast = g.OrderBy(o => o.IsGift)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .LastOrDefault(),
                    Shipped = g.OrderBy(o => o.ShippedOn)
                        .ThenBy(o => o.Id)
                        .Select(o => o.ShippedOn)
                        .First(),
                    Paid = g.OrderByDescending(o => o.PaidAt)
                        .ThenBy(o => o.Id)
                        .Select(o => o.PaidAt)
                        .Last(),
                    Discount = g.OrderBy(o => o.Discount)
                        .ThenByDescending(o => o.Id)
                        .Select(o => o.Discount)
                        .First(),
                })
        );
    }

    [Fact]
    public void SortsNullableStringsOrdinally()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    First = g.OrderBy(o => o.Note, StringComparer.Ordinal)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Note)
                        .First(),
                    Last = g.OrderBy(o => o.Note, StringComparer.Ordinal)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Note)
                        .Last(),
                    DownFirst = g.OrderByDescending(o => o.Note, StringComparer.Ordinal)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Number)
                        .First(),
                    DownLast = g.OrderByDescending(o => o.Note, StringComparer.Ordinal)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Number)
                        .Last(),
                    Number = g.OrderBy(o => o.IsPaid)
                        .ThenBy(o => o.Number, StringComparer.Ordinal)
                        .Select(o => o.Number)
                        .First(),
                })
        );
    }

    [Fact]
    public void ReadsNullValuesAtEitherEnd()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    FirstPriority = g.OrderBy(o => o.Id).Select(o => o.Priority).First(),
                    LastPriority = g.OrderBy(o => o.Id).Select(o => o.Priority).Last(),
                    FirstNote = g.OrderByDescending(o => o.Id)
                        .Select(o => o.Note)
                        .First(),
                    LastNote = g.OrderByDescending(o => o.Id)
                        .Select(o => o.Note)
                        .LastOrDefault(),
                })
        );
    }

    [Fact]
    public void ReadsLastAsFirstOfInvertedOrder()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Last = g.OrderBy(o => o.Total).Select(o => o.Id).Last(),
                    First = g.OrderByDescending(o => o.Total).Select(o => o.Id).First(),
                    LastDown = g.OrderByDescending(o => o.Total).Select(o => o.Id).Last(),
                    FirstUp = g.OrderBy(o => o.Total).Select(o => o.Id).First(),
                    Mixed = g.OrderBy(o => o.IsPaid)
                        .ThenByDescending(o => o.Total)
                        .Select(o => o.Id)
                        .Last(),
                })
        );
    }

    [Fact]
    public void SubstitutesDefaultsOfEmptyFilteredGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Total = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Total)
                        .FirstOrDefault(),
                    Points = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Points)
                        .LastOrDefault(),
                    Paid = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.IsPaid)
                        .FirstOrDefault(),
                    Created = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.CreatedAt)
                        .FirstOrDefault(),
                    Delivery = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.DeliveryTime)
                        .FirstOrDefault(),
                    Tracking = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.TrackingId)
                        .FirstOrDefault(),
                    Status = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Status)
                        .LastOrDefault(),
                    Note = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Note)
                        .FirstOrDefault(),
                    Priority = g.Where(o => o.Quantity > 5)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Priority)
                        .LastOrDefault(),
                })
        );
    }

    [Fact]
    public void ReadsFirstAndLastMatchingPredicates()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Long = g.OrderBy(o => o.Id)
                        .Select(o => o.Note)
                        .FirstOrDefault(n => n != null && n.Length > 5),
                    LastLong = g.OrderBy(o => o.Id)
                        .Select(o => o.Note)
                        .LastOrDefault(n => n != null && n.Length > 5),
                    Big = g.OrderBy(o => o.Id)
                        .Select(o => o.Quantity)
                        .FirstOrDefault(q => q > 3),
                    Prefix = g.OrderBy(o => o.Id)
                        .Select(o => o.Number)
                        .LastOrDefault(n => n.StartsWith("B")),
                    Both = g.Where(o => o.IsPaid)
                        .OrderByDescending(o => o.Id)
                        .Select(o => o.Quantity)
                        .FirstOrDefault(q => q < 5),
                    Null = g.OrderBy(o => o.Id)
                        .Select(o => o.Priority)
                        .FirstOrDefault(p => p == null),
                })
        );
    }

    [Fact]
    public void ReadsFirstOfDistinctValues()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    First = g.OrderBy(o => o.Id).Select(o => o.IsPaid).Distinct().First(),
                    Down = g.OrderByDescending(o => o.Id)
                        .Select(o => o.IsPaid)
                        .Distinct()
                        .First(),
                })
        );
    }

    // The distinct values of 1 (paid), 2 (open) and 9 (paid) in the order of Id are
    // paid, open: the last is open, where the last of all values is paid.
    // SQL: (ARRAY_AGG(t0."IsPaid" ORDER BY t0."Id" DESC))[1], true instead of false.
    [Fact]
    public void ReadsLastOfDistinctValues()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Last = g.OrderBy(o => o.Id).Select(o => o.IsPaid).Distinct().Last(),
                })
        );
    }

    [Fact]
    public void ReadsElementsOfElementSelectorGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId, o => o.Total)
                .Select(g => new
                {
                    g.Key,
                    Least = g.OrderBy(t => t).First(),
                    Greatest = g.OrderBy(t => t).Last(),
                    Down = g.OrderByDescending(t => t).Last(),
                    Positive = g.OrderBy(t => t).FirstOrDefault(t => t > 0),
                })
        );
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.IsPaid, o => o.Number)
                .Select(g => new
                {
                    g.Key,
                    First = g.OrderBy(n => n, StringComparer.Ordinal).First(),
                    Last = g.OrderBy(n => n, StringComparer.Ordinal).Last(),
                })
        );
    }

    [Fact]
    public void FiltersAndOrdersGroupsOnNullableFirstValues()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Where(g => g.OrderBy(o => o.Id).Select(o => o.Priority).First() == null)
                .Select(g => g.Key)
        );
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Where(g => g.OrderBy(o => o.Id).Select(o => o.Priority).Last() != 1)
                .Select(g => g.Key)
        );
        database.AssertSameSequence(
            Orders
                .GroupBy(o => o.CustomerId)
                .OrderBy(g => g.OrderBy(o => o.Id).Select(o => o.Priority).First())
                .ThenBy(g => g.Key)
                .Select(g => g.Key)
        );
        database.AssertSameSequence(
            Orders
                .GroupBy(o => o.CustomerId)
                .OrderByDescending(
                    g => g.OrderBy(o => o.Id).Select(o => o.Note).Last(),
                    StringComparer.Ordinal
                )
                .ThenBy(g => g.Key)
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void ReadsFirstAndLastOfJoinedGroups()
    {
        database.AssertSameRows(
            Customers
                .Join(
                    Orders,
                    c => c.Id,
                    o => o.CustomerId,
                    (c, o) =>
                        new
                        {
                            c.Region,
                            c.Name,
                            o.Id,
                            o.Total,
                        }
                )
                .GroupBy(x => x.Region)
                .Select(g => new
                {
                    g.Key,
                    Cheapest = g.OrderBy(x => x.Total).Select(x => x.Name).First(),
                    Latest = g.OrderBy(x => x.Id).Select(x => x.Id).Last(),
                })
        );
    }

    [Fact]
    public void ReadsFirstAndLastOfGroupJoinGroups()
    {
        database.AssertSameRows(
            Customers.GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Id,
                        First = os.OrderBy(o => o.Id)
                            .Select(o => o.Number)
                            .FirstOrDefault(),
                        Last = os.OrderBy(o => o.Id)
                            .Select(o => o.Quantity)
                            .LastOrDefault(),
                        Priority = os.OrderBy(o => o.Priority)
                            .ThenBy(o => o.Id)
                            .Select(o => o.Priority)
                            .FirstOrDefault(),
                    }
            )
        );
    }

    [Fact]
    public void ReadsFirstAndLastBesideOtherAggregates()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => new { o.Status, o.IsPaid })
                .Select(g => new
                {
                    g.Key.Status,
                    g.Key.IsPaid,
                    Count = g.Count(),
                    First = g.OrderBy(o => o.CreatedAt)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .First(),
                    Last = g.OrderBy(o => o.CreatedAt)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .Last(),
                    Sum = g.Sum(o => o.Total),
                    Numbers = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                })
        );
    }

    // The elements of a group keep the order of the source, which breaks the ties of a
    // later OrderBy: every order of a customer ties on its CustomerId, so the first is
    // the one of the greatest Id in a descending source and the last the one of the
    // least. SQL: (ARRAY_AGG(t0."Id" ORDER BY t0."CustomerId" DESC))[1] for Last, the
    // source order lost: for the customer 1 both First and Last are 9, Last should be 1.
    [Fact]
    public void BreaksTiesInSourceOrder()
    {
        database.AssertSameRows(
            Orders
                .OrderByDescending(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    First = g.OrderBy(o => o.CustomerId).Select(o => o.Id).First(),
                    Last = g.OrderBy(o => o.CustomerId).Select(o => o.Id).Last(),
                    Paid = g.OrderBy(o => o.Quantity >= 0).Select(o => o.Id).First(),
                })
        );
    }

    [Fact]
    public void TakesOrdinalExtremesOfNonAsciiText()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.IsPaid)
                .Select(g => new
                {
                    g.Key,
                    Min = g.Select(o => o.Note).Min(StringComparer.Ordinal),
                    Max = g.Select(o => o.Note).Max(StringComparer.Ordinal),
                    Upper = g.Select(o => o.Number).Min(StringComparer.Ordinal),
                    Lower = g.Select(o => o.Number).Max(StringComparer.Ordinal),
                })
        );
        database.AssertSameResult(
            Customers.Select(c => c.Name),
            q => q.Max(StringComparer.Ordinal)
        );
        database.AssertSameResult(
            Customers.Select(c => c.Name),
            q => q.Min(StringComparer.Ordinal)
        );
    }

    // UTF-16 code units order U+FF21 above the surrogates of U+1F600, code points
    // below: .NET's ordinal maximum is "Ａ", COLLATE "C" yields the emoji.
    [Fact]
    public void TakesOrdinalExtremesInUtf16Order()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => 1)
                .Select(g => new
                {
                    Max = g.Select(o => o.Id == 7 ? o.Note : "Ａ")
                        .Max(StringComparer.Ordinal),
                    Min = g.Select(o => o.Id == 7 ? o.Note : "Ａ")
                        .Min(StringComparer.Ordinal),
                })
        );
    }

    [Fact]
    public void TakesOrdinalExtremesOfEmptyAndNullGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    None = g.Where(o => o.Quantity > 100)
                        .Select(o => o.Number)
                        .Max(StringComparer.Ordinal),
                    Nulls = g.Where(o => o.Note == null)
                        .Select(o => o.Note)
                        .Min(StringComparer.Ordinal),
                    Some = g.Select(o => o.Note).Max(StringComparer.Ordinal),
                    Distinct = g.Select(o => o.IsPaid ? "paid" : "open")
                        .Distinct()
                        .Max(StringComparer.Ordinal),
                })
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 100).Select(o => o.Note),
            q => q.Max(StringComparer.Ordinal)
        );
        database.AssertSameResult(
            Orders.Where(o => o.Note == null).Select(o => o.Note),
            q => q.Min(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void ComparesAndOrdersOnOrdinalExtremes()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Where(g =>
                    g.Select(o => o.Number).Max(StringComparer.Ordinal) == "a-003"
                )
                .Select(g => g.Key)
        );
        database.AssertSameSequence(
            Orders
                .GroupBy(o => o.CustomerId)
                .OrderBy(
                    g => g.Select(o => o.Note).Min(StringComparer.Ordinal),
                    StringComparer.Ordinal
                )
                .ThenBy(g => g.Key)
                .Select(g => new
                {
                    g.Key,
                    Least = g.Select(o => o.Note).Min(StringComparer.Ordinal) + "!",
                })
        );
        database.AssertSameRows(
            Orders
                .Where(o =>
                    o.Number == Orders.Select(x => x.Number).Max(StringComparer.Ordinal)
                )
                .Select(o => o.Id)
        );
    }

    [Fact]
    public void TakesOrdinalExtremesOfSubqueriesAndGroupJoins()
    {
        database.AssertSameRows(
            Customers.GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Id,
                        Max = os.Select(o => o.Note).Max(StringComparer.Ordinal),
                        Min = os.Where(o => o.IsPaid)
                            .Select(o => o.Number)
                            .Min(StringComparer.Ordinal),
                    }
            )
        );
        database.AssertSameResult(
            Orders.OrderBy(o => o.Id).Take(3).Select(o => o.Number),
            q => q.Max(StringComparer.Ordinal)
        );
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Max = Orders.Select(x => x.Note).Max(StringComparer.Ordinal),
            })
        );
    }

    // MAX(t0."Name" COLLATE "C") inside the subquery has outer-level arguments only,
    // so PostgreSQL evaluates it as an aggregate of the outer query (42803).
    [Fact(
        Skip = "#128: an aggregate of a subquery over outer values only is evaluated in the outer query"
    )]
    public void TakesOrdinalExtremesOfOuterValues()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Max = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => c.Name)
                    .Max(StringComparer.Ordinal),
            })
        );
    }
}
