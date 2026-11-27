namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// LINQ to Objects sorts stably and takes first occurrences in the order of the
// source: tying keys keep the source order, Last of a descending order is the last
// of the tied elements, and Distinct keeps the first occurrence of every value. A
// source without an order, the rows of a table, has none to keep on either side.
[Collection(SharedDatabase.Name)]
public sealed class SourceOrderTieSemanticsTests(Database database)
{
    // 200 rows whose keys 0, 1 and 2 tie: a stable sort keeps the positions of each
    // key ascending.
    private static readonly (int Key, int Position)[] Ties =
    [
        .. Enumerable.Range(0, 200).Select(x => (x * 7 % 3, x)),
    ];

    // #134: every element of a customer's group ties on the CustomerId; in the order
    // of Id the last of a descending order is the greatest Id of the group.
    // SQL: (ARRAY_AGG(t0."Id" ORDER BY t0."CustomerId"))[1], the first of the ties.
    [Fact]
    public void ReadsLastOfDescendingOrderAmongTies()
    {
        database.AssertSameRows(
            Orders
                .OrderBy(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Last = g.OrderByDescending(o => o.CustomerId)
                        .Select(o => o.Id)
                        .Last(),
                    First = g.OrderByDescending(o => o.CustomerId)
                        .Select(o => o.Id)
                        .First(),
                })
        );
    }

    // #134: VALUES carries no position, so its ties are sorted in any order.
    [Fact]
    public void SortsInMemorySequenceStably()
    {
        (int Key, int Position)[] ties = Ties;

        database.AssertSameSequence(
            ties.AsQueryable().OrderBy(t => t.Key).Select(t => t.Position)
        );
    }

    // #134: the same VALUES joined to a row of a table.
    [Fact]
    public void SortsJoinedInMemorySequenceStably()
    {
        (int Key, int Position)[] ties = Ties;

        database.AssertSameSequence(
            Customers
                .Where(c => c.Id == 1)
                .SelectMany(
                    c => ties,
                    (c, t) =>
                        new
                        {
                            c.Id,
                            t.Key,
                            t.Position,
                        }
                )
                .OrderByDescending(x => x.Key)
        );
    }

    // #134: the distinct states of a group in the order of their first occurrence;
    // STRING_AGG(DISTINCT ...) sorts them by value.
    [Fact]
    public void JoinsDistinctValuesOfGroupInFirstOccurrenceOrder()
    {
        database.AssertSameRows(
            Orders
                .OrderByDescending(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    States = string.Join(
                        ",",
                        g.Select(o => o.IsPaid ? "paid" : "open").Distinct()
                    ),
                })
        );
    }

    // #134: an unordered group of a table has no order to keep: LINQ to Objects takes
    // its elements in the order of the rows it happens to be given, and SQL gives the
    // distinct values in an order of its own (STRING_AGG(DISTINCT ...) sorts them), as
    // the rows of any query without ORDER BY come. The values are the same; their
    // order is not defined on either side, so they are compared without it.
    [Fact]
    public void JoinsDistinctValuesOfUnorderedGroupInNoOrder()
    {
        AssertSameValues(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new Joined
                {
                    Key = g.Key,
                    Values = string.Join(
                        ",",
                        g.Select(o => o.IsPaid ? "paid" : "open").Distinct()
                    ),
                })
        );
    }

    // #134: string.Join of the distinct values of an ordered subquery keeps the order
    // of their first occurrences.
    [Fact]
    public void JoinsDistinctValuesOfSubqueryInFirstOccurrenceOrder()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id == 1)
                .Select(c => new
                {
                    c.Id,
                    Notes = string.Join(
                        ",",
                        Orders.OrderByDescending(o => o.Id).Select(o => o.Note).Distinct()
                    ),
                })
        );
    }

    // #134: the same over an unordered subquery: SELECT DISTINCT in a derived table
    // has no order, and neither have the rows LINQ to Objects reads the values from.
    [Fact]
    public void JoinsDistinctValuesOfUnorderedSubqueryInNoOrder()
    {
        AssertSameValues(
            Customers
                .Where(c => c.Id == 1)
                .Select(c => new Joined
                {
                    Key = c.Id,
                    Values = string.Join(
                        ",",
                        Orders.Select(o => o.CustomerId == 1 ? "x" : o.Number).Distinct()
                    ),
                })
        );
    }

    // The joined values of each key, sorted, on both sides.
    private void AssertSameValues(IQueryable<Joined> query)
    {
        static string Sorted(Joined row)
        {
            return row.Key
                + ": "
                + string.Join(",", row.Values.Split(',').Order(StringComparer.Ordinal));
        }
        List<string> expected =
        [
            .. new EnumerableQuery<Joined>(InMemory.Rewrite(query.Expression))
                .AsEnumerable()
                .Select(Sorted)
                .Order(StringComparer.Ordinal),
        ];
        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );

        Assert.NotEmpty(expected);
        Assert.Equal(
            expected,
            result
                .Rows.Select(row =>
                    Sorted((Joined)Materializer.Element(typeof(Joined), result, row)!)
                )
                .Order(StringComparer.Ordinal)
        );
    }

    public sealed record Joined
    {
        public int Key { get; init; }

        public string Values { get; init; } = string.Empty;
    }
}
