#pragma warning disable CA1861 // the constant arrays are the values under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// Edge cases of string.Join over arrays, array columns, groups, subqueries and the
// groups of GroupJoin: null separators and values, empty and one-element sequences,
// char, column and captured separators, computed values, the sequence's order, and
// joins composed with grouping, joining, filtering, ordering and other aggregates.
[Collection(SharedDatabase.Name)]
public sealed class StringAggregationEdgeCaseSemanticsTests(Database database)
{
    [Fact]
    public void JoinsGroupsOfElementSelector()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId, o => o.Number)
                .Select(g => new
                {
                    g.Key,
                    Up = string.Join(",", g.OrderBy(n => n, StringComparer.Ordinal)),
                    Down = string.Join(
                        "|",
                        g.OrderByDescending(n => n, StringComparer.Ordinal)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsOneElementGroupsWithoutOrder()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId, o => o.Note)
                .Where(g => g.Count() == 1)
                .Select(g => new { g.Key, Joined = string.Join(", ", g) })
        );
    }

    [Fact]
    public void JoinsGroupsOfResultSelector()
    {
        database.AssertSameRows(
            Orders.GroupBy(
                o => o.CustomerId,
                (key, os) =>
                    new
                    {
                        Key = key,
                        Numbers = string.Join(
                            ",",
                            os.OrderBy(o => o.Id).Select(o => o.Number)
                        ),
                    }
            )
        );
        database.AssertSameRows(
            Orders.GroupBy(
                o => o.Status,
                o => o.Note,
                (key, notes) =>
                    new
                    {
                        Key = key,
                        Notes = string.Join(
                            "/",
                            notes
                                .Where(n => n != null)
                                .OrderBy(n => n, StringComparer.Ordinal)
                        ),
                    }
            )
        );
    }

    [Fact]
    public void JoinsEmptyFilteredGroupsAsEmptyString()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    None = string.Join(
                        ",",
                        g.Where(o => o.Quantity > 100)
                            .OrderBy(o => o.Id)
                            .Select(o => o.Number)
                    ),
                    Unpaid = string.Join(
                        ",",
                        g.Where(o => !o.IsPaid).OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                    IsEmpty = string.Join(
                        ",",
                        g.Where(o => !o.IsPaid).Select(o => o.Number)
                    ) == "",
                    string.Join(
                        "",
                        g.Where(o => o.Quantity > 100).Select(o => o.Number)
                    ).Length,
                })
        );
    }

    [Fact]
    public void JoinsNullValuesAndOnlyNullsAsEmptyStrings()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Notes = string.Join(",", g.OrderBy(o => o.Id).Select(o => o.Note)),
                    Nulls = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Note).Where(n => n == null)
                    ),
                    Missing = string.Join(
                        ";",
                        g.OrderBy(o => o.Id).Select(o => (string?)null)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsWithNullSeparators()
    {
        string? none = null;

        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Glued = string.Join(none, g.OrderBy(o => o.Id).Select(o => o.Number)),
                })
        );
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Note)
                .Select(g => new
                {
                    g.Key,
                    ByKey = string.Join(
                        g.Key,
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsWithCharSeparators()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.IsPaid)
                .Select(g => new
                {
                    g.Key,
                    Plus = string.Join('+', g.OrderBy(o => o.Id).Select(o => o.Number)),
                    Quote = string.Join('\'', g.OrderBy(o => o.Id).Select(o => o.Number)),
                    Slash = string.Join('\\', g.OrderBy(o => o.Id).Select(o => o.Number)),
                    ByKey = string.Join(
                        g.Key ? '+' : '-',
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsWithEscapedSeparators()
    {
        string captured = "'; --\\%_";

        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Quoted = string.Join(
                        "', '",
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                    Captured = string.Join(
                        captured,
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                    Emoji = string.Join(
                        "\U0001F600",
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                    Empty = string.Join("", g.OrderBy(o => o.Id).Select(o => o.Note)),
                })
        );
    }

    [Fact]
    public void JoinsComputedValuesOfGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Pairs = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Number + ":" + o.Quantity)
                    ),
                    Defaulted = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Note ?? "-")
                    ),
                    Ids = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Id.ToString())
                    ),
                    Lower = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Number.ToLower())
                    ),
                    Conditional = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.IsPaid ? o.Note : null)
                    ),
                    Interpolated = string.Join(
                        " ",
                        g.OrderBy(o => o.Id).Select(o => $"[{o.Number}]")
                    ),
                    Keys = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => g.Key.ToString())
                    ),
                })
        );
    }

    [Fact]
    public void JoinsInOrderOfComputedAndNullableKeys()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    ByPriority = string.Join(
                        ",",
                        g.OrderBy(o => o.Priority).ThenBy(o => o.Id).Select(o => o.Number)
                    ),
                    ByPriorityDown = string.Join(
                        ",",
                        g.OrderByDescending(o => o.Priority)
                            .ThenByDescending(o => o.Id)
                            .Select(o => o.Number)
                    ),
                    ByNote = string.Join(
                        ",",
                        g.OrderBy(o => o.Note, StringComparer.Ordinal)
                            .ThenBy(o => o.Id)
                            .Select(o => o.Number)
                    ),
                    ByNoteDown = string.Join(
                        ",",
                        g.OrderByDescending(o => o.Note, StringComparer.Ordinal)
                            .ThenBy(o => o.Id)
                            .Select(o => o.Number)
                    ),
                    ByNegatedTotal = string.Join(
                        ",",
                        g.OrderBy(o => -o.Total).Select(o => o.Number)
                    ),
                    ByGift = string.Join(
                        ",",
                        g.OrderBy(o => o.IsGift).ThenBy(o => o.Id).Select(o => o.Number)
                    ),
                    ByShipped = string.Join(
                        ",",
                        g.OrderByDescending(o => o.ShippedOn)
                            .ThenBy(o => o.Id)
                            .Select(o => o.Number)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsOrderedThenFilteredValues()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Later = string.Join(
                        ",",
                        g.OrderByDescending(o => o.Id)
                            .Where(o => o.Quantity > 1)
                            .Select(o => o.Number)
                    ),
                    Selected = string.Join(
                        ",",
                        g.Select(o => o.Number)
                            .OrderBy(n => n, StringComparer.Ordinal)
                            .Where(n => n.Contains('-'))
                    ),
                })
        );
    }

    [Fact]
    public void JoinsDistinctValuesSortedByThemselves()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    States = string.Join(
                        ",",
                        g.Select(o => o.IsPaid ? "paid" : "open")
                            .Distinct()
                            .OrderBy(s => s, StringComparer.Ordinal)
                    ),
                    Down = string.Join(
                        ",",
                        g.Select(o => o.IsPaid ? "paid" : "open")
                            .Distinct()
                            .OrderByDescending(s => s, StringComparer.Ordinal)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsBesideOtherAggregates()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Count = g.Count(),
                    Quantity = g.Sum(o => o.Quantity),
                    Numbers = string.Join(
                        ",",
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                    Paid = g.Count(o => o.IsPaid),
                    Top = g.Select(o => o.Number).Max(StringComparer.Ordinal),
                    First = g.OrderBy(o => o.Id).Select(o => o.Number).First(),
                    Notes = string.Join(
                        "|",
                        g.OrderByDescending(o => o.Id).Select(o => o.Note)
                    ),
                })
        );
    }

    [Fact]
    public void FiltersAndOrdersGroupsOnJoins()
    {
        database.AssertSameSequence(
            Orders
                .GroupBy(o => o.CustomerId)
                .Where(g =>
                    string.Join(",", g.OrderBy(o => o.Id).Select(o => o.Note)) != ""
                )
                .OrderBy(
                    g => string.Join(",", g.OrderBy(o => o.Id).Select(o => o.Number)),
                    StringComparer.Ordinal
                )
                .Select(g => g.Key)
        );
        database.AssertSameSequence(
            Orders
                .GroupBy(o => o.CustomerId)
                .OrderByDescending(g =>
                    string.Join("", g.OrderBy(o => o.Id).Select(o => o.Number)).Length
                )
                .ThenBy(g => g.Key)
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void FiltersCustomersOnEmptyJoins()
    {
        database.AssertSameRows(
            Customers
                .Where(c =>
                    string.Join(
                        ",",
                        Orders.Where(o => o.CustomerId == c.Id).Select(o => o.Number)
                    ) == ""
                )
                .Select(c => c.Id)
        );
        database.AssertSameRows(
            Customers
                .Where(c =>
                    string.IsNullOrEmpty(
                        string.Join(
                            ",",
                            Orders.Where(o => o.CustomerId == c.Id).Select(o => o.Note)
                        )
                    )
                )
                .Select(c => c.Id)
        );
    }

    [Fact]
    public void ProjectsAfterJoinsAndAggregatesThem()
    {
        IQueryable<string> joined = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => string.Join(",", g.OrderBy(o => o.Id).Select(o => o.Number)));

        database.AssertSameSequence(
            joined.Where(s => s.Contains(',')).OrderBy(s => s, StringComparer.Ordinal)
        );
        database.AssertSameResult(joined, q => q.Count(s => s.Length > 5));
        database.AssertSameResult(joined, q => q.Max(StringComparer.Ordinal));
    }

    [Fact]
    public void JoinsGroupsOfInnerJoin()
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
                            o.Number,
                        }
                )
                .GroupBy(x => x.Region)
                .Select(g => new
                {
                    g.Key,
                    Numbers = string.Join(
                        ",",
                        g.OrderBy(x => x.Id).Select(x => x.Number)
                    ),
                    Names = string.Join(
                        ",",
                        g.Select(x => x.Name)
                            .Distinct()
                            .OrderBy(n => n, StringComparer.Ordinal)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsGroupsOfSelectMany()
    {
        database.AssertSameRows(
            Customers
                .SelectMany(
                    c => Orders.Where(o => o.CustomerId == c.Id),
                    (c, o) =>
                        new
                        {
                            c.Region,
                            o.Id,
                            o.Note,
                        }
                )
                .GroupBy(x => x.Region)
                .Select(g => new
                {
                    g.Key,
                    Notes = string.Join(
                        ";",
                        g.OrderByDescending(x => x.Id).Select(x => x.Note)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsJoinsOfArrayColumns()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Tags = string.Join(
                        ";",
                        g.OrderBy(o => o.Id).Select(o => string.Join(",", o.Tags))
                    ),
                })
        );
    }

    [Fact]
    public void JoinsJoinsOfSubqueries()
    {
        database.AssertSameRows(
            new[] { 0 }
                .AsQueryable()
                .Select(_ =>
                    string.Join(
                        "|",
                        Customers
                            .OrderBy(c => c.Id)
                            .Select(c =>
                                string.Join(
                                    ",",
                                    Orders
                                        .Where(o => o.CustomerId == c.Id)
                                        .OrderBy(o => o.Id)
                                        .Select(o => o.Number)
                                )
                            )
                    )
                )
        );
    }

    [Fact]
    public void JoinsJoinsOfGroupsInSubqueries()
    {
        database.AssertSameRows(
            new[] { 0 }
                .AsQueryable()
                .Select(_ =>
                    string.Join(
                        "|",
                        Orders
                            .GroupBy(o => o.CustomerId)
                            .OrderBy(g => g.Key)
                            .Select(g =>
                                string.Join(
                                    ",",
                                    g.OrderBy(o => o.Id).Select(o => o.Number)
                                )
                            )
                    )
                )
        );
    }

    [Fact]
    public void JoinsSubqueriesWithCorrelatedSeparator()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                ByName = string.Join(
                    c.Name,
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Number)
                ),
                ByEmail = string.Join(
                    c.Email,
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Number)
                ),
            })
        );
    }

    // STRING_AGG(t0."Name", ',') inside the subquery has outer-level arguments only,
    // so PostgreSQL evaluates it as an aggregate of the outer query:
    // 42803: column "t0.Id" must appear in the GROUP BY clause or be used in an
    // aggregate function.
    [Fact(
        Skip = "#128: an aggregate of a subquery over outer values only is evaluated in the outer query"
    )]
    public void JoinsOuterValuesOfSubqueries()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Names = string.Join(
                    ",",
                    Orders.Where(o => o.CustomerId == c.Id).Select(o => c.Name)
                ),
            })
        );
    }

    // SELECT t0."Id", (SELECT COALESCE(STRING_AGG(t0."Region", ','), '') FROM "Order"
    // AS t1 WHERE t1."CustomerId" = t0."Id") FROM "Customer" AS t0 fails with 42803.
    [Fact(
        Skip = "#128: an aggregate of a GroupJoin group over outer values only is evaluated in the outer query"
    )]
    public void JoinsOuterValuesOfGroupJoinGroups()
    {
        database.AssertSameRows(
            Customers.GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new { c.Id, Names = string.Join(",", os.Select(o => c.Region)) }
            )
        );
    }

    [Fact]
    public void JoinsPagedAndReversedSubqueries()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Skipped = string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Skip(1)
                        .Select(o => o.Number)
                ),
                Filtered = string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderByDescending(o => o.Id)
                        .Take(2)
                        .Where(o => o.IsPaid)
                        .Select(o => o.Number)
                ),
                Reversed = string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Number)
                        .Reverse()
                ),
                Then = string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.IsPaid)
                        .ThenByDescending(o => o.Id)
                        .Select(o => o.Number)
                ),
            })
        );
    }

    [Fact]
    public void JoinsDistinctSubqueriesInTheirOrder()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                States = string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderByDescending(o => o.Id)
                        .Select(o => o.IsPaid ? "paid" : "open")
                        .Distinct()
                ),
            })
        );
    }

    [Fact]
    public void JoinsConcatenatedSubqueries()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id < 4)
                .Select(c => new
                {
                    c.Id,
                    Numbers = string.Join(
                        ",",
                        Orders
                            .Where(o => o.CustomerId == c.Id)
                            .Select(o => o.Number)
                            .Concat(new[] { "z1", "z2" })
                            .OrderBy(n => n, StringComparer.Ordinal)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsEveryRowBesideEachRow()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                All = string.Join(",", Orders.OrderBy(x => x.Id).Select(x => x.Number)),
            })
        );
        database.AssertSameRows(
            Orders
                .Where(o => o.IsPaid)
                .Select(o => new
                {
                    o.Id,
                    Paid = string.Join(
                        ",",
                        Orders
                            .Where(x => x.IsPaid)
                            .OrderBy(x => x.Id)
                            .Select(x => x.Number)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsGroupJoinGroupsFilteredAndDistinct()
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
                        Paid = string.Join(
                            ",",
                            os.Where(o => o.IsPaid).OrderBy(o => o.Id).Select(o => o.Note)
                        ),
                        States = string.Join(
                            ",",
                            os.Select(o => o.IsPaid ? "paid" : "open")
                                .Distinct()
                                .OrderBy(s => s, StringComparer.Ordinal)
                        ),
                        Count = os.Count(),
                    }
            )
        );
    }

    [Fact]
    public void JoinsConstantArrays()
    {
        List<string?> empty = [];
        IEnumerable<string> repeated = Enumerable.Repeat("x'y", 3);

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Nulls = string.Join(",", new string?[] { null, null }),
                OneNull = string.Join(",", new string?[] { null }),
                Empty = string.Join(o.Number, empty),
                Repeated = string.Join(o.Number, repeated),
                Single = string.Join(",", new[] { o.Note }),
            })
        );
    }

    [Fact]
    public void JoinsArrayColumnsWithNullSeparators()
    {
        string? none = null;

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Glued = string.Join(none, o.Tags),
                Quoted = string.Join("', '", o.Tags),
                Char = string.Join('\'', o.Tags),
                string.Join(",", o.Tags).Length,
            })
        );
    }

    // In LINQ to Objects the elements of a group keep the order of the source; the
    // groups come in that order (ORDER BY MAX(t0."Id") DESC), but STRING_AGG has no
    // ORDER BY: "A-001,A-002,a-003" instead of "a-003,A-002,A-001".
    [Fact(
        Skip = "#134: string.Join of a group ignores the order of the source before GroupBy"
    )]
    public void JoinsGroupsOfOrderedSourceInSourceOrder()
    {
        database.AssertSameRows(
            Orders
                .OrderByDescending(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Numbers = string.Join(",", g.Select(o => o.Number)),
                })
        );
    }

    // The separator is evaluated once in .NET; SQL nests the aggregate in STRING_AGG:
    // STRING_AGG(t0."Number", CAST(CAST(COUNT(*) AS integer) AS text) ORDER BY t0."Id")
    // fails with 42803: aggregate function calls cannot be nested.
    [Fact(
        Skip = "#128: a separator that aggregates the group is nested in STRING_AGG, which PostgreSQL rejects"
    )]
    public void JoinsWithSeparatorsOfGroupAggregates()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    ByCount = string.Join(
                        g.Count().ToString(),
                        g.OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsCorrelatedJoinsOfGroupElements()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Siblings = string.Join(
                        ";",
                        g.OrderBy(o => o.Id)
                            .Select(o =>
                                string.Join(
                                    ",",
                                    Orders
                                        .Where(x => x.CustomerId == o.CustomerId)
                                        .OrderBy(x => x.Id)
                                        .Select(x => x.Number)
                                )
                            )
                    ),
                })
        );
    }

    [Fact]
    public void GroupsByJoins()
    {
        database.AssertSameRows(
            Customers
                .GroupBy(c =>
                    string.Join(
                        ",",
                        Orders
                            .Where(o => o.CustomerId == c.Id)
                            .OrderBy(o => o.Id)
                            .Select(o => o.Status == OrderStatus.Paid ? "p" : "o")
                    )
                )
                .Select(g => new { g.Key, Count = g.Count() })
        );
    }

    // A captured query is a constant, which string.Join enumerates as an in-memory
    // sequence instead of translating it as a subquery: the capturing root throws
    // "The test root only captures expression trees.", a real provider would run the
    // query on the client while translating.
    [Fact(
        Skip = "#128: string.Join of a captured IQueryable enumerates it instead of translating a subquery"
    )]
    public void JoinsCapturedQueriesAsSubqueries()
    {
        IQueryable<string> numbers = Orders.OrderBy(o => o.Id).Select(o => o.Number);

        database.AssertSameRows(
            Customers.Select(c => new { c.Id, Numbers = string.Join(",", numbers) })
        );
    }
}
