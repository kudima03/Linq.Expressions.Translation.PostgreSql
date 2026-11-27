namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// LINQ to Objects sorts stably and takes first occurrences in the order of the
// source: tying keys keep the source order, Last of a descending order is the last
// of the tied elements, and Distinct keeps the first occurrence of every value.
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
    [Fact(
        Skip = "#134: Last of a descending order of a group is First of the inverted order, the first of the ties"
    )]
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
    [Fact(
        Skip = "#134: OrderBy over VALUES breaks ties in no order instead of the sequence's"
    )]
    public void SortsInMemorySequenceStably()
    {
        (int Key, int Position)[] ties = Ties;

        database.AssertSameSequence(
            ties.AsQueryable().OrderBy(t => t.Key).Select(t => t.Position)
        );
    }

    // #134: the same VALUES joined to a row of a table.
    [Fact(
        Skip = "#134: OrderBy over VALUES breaks ties in no order instead of the sequence's"
    )]
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
    [Fact(
        Skip = "#134: string.Join of Distinct of a group sorts the values instead of keeping first occurrences"
    )]
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

    // #134: the same over an unordered group of the table, whose elements LINQ to
    // Objects takes in the order of the rows. A refusal, where the source has no order
    // to keep, is an acceptable fix as well.
    [Fact(
        Skip = "#134: string.Join of Distinct of an unordered group sorts the values (a refusal is acceptable)"
    )]
    public void JoinsDistinctValuesOfUnorderedGroup()
    {
        database.AssertSameRows(
            Orders
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

    // #134: the same over an unordered subquery, in the order of the rows: SELECT
    // DISTINCT in a derived table has no order. A refusal is an acceptable fix as well.
    [Fact(
        Skip = "#134: string.Join of Distinct of an unordered subquery joins the values in no order (a refusal is acceptable)"
    )]
    public void JoinsDistinctValuesOfUnorderedSubquery()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id == 1)
                .Select(c => new
                {
                    c.Id,
                    Notes = string.Join(
                        ",",
                        Orders.Select(o => o.CustomerId == 1 ? "x" : o.Number).Distinct()
                    ),
                })
        );
    }
}
