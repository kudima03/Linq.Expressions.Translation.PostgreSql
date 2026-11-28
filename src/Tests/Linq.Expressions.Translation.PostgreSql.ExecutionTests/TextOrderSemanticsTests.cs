#pragma warning disable CA1861 // the constant arrays are the sources under test

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The order of text: StringComparer.Ordinal compares UTF-16 code units, so a
// character outside the Basic Multilingual Plane (a surrogate pair from U+D800)
// orders before U+E000..U+FFFF, where code points order it after them; without a
// comparer LINQ to Objects compares with the current culture and SQL with the
// collation of the database.
[Collection(SharedDatabase.Name)]
public sealed class TextOrderSemanticsTests(Database database)
{
    // #133: .NET orders "é", "😀", "ａ" (U+00E9, U+D83D U+DE00, U+FF41);
    // COLLATE "C" orders "é", "ａ", "😀".
    [Fact]
    public void OrdersSequenceByUtf16CodeUnits()
    {
        database.AssertSameSequence(
            new[] { "ａ", "😀", "a", "é", "�" }
                .AsQueryable()
                .OrderBy(s => s, StringComparer.Ordinal)
        );
    }

    // #133: the same order over a table column, descending as well.
    [Fact]
    public void OrdersColumnByUtf16CodeUnits()
    {
        database.AssertSameSequence(
            Orders
                .Where(o => o.Note != null)
                .OrderBy(o => o.Id == 1 ? "ａ" : o.Note, StringComparer.Ordinal)
                .ThenBy(o => o.Id)
                .Select(o => o.Id)
        );
        database.AssertSameSequence(
            Orders
                .Where(o => o.Note != null)
                .OrderByDescending(o => o.Id == 1 ? "ａ" : o.Note, StringComparer.Ordinal)
                .ThenBy(o => o.Id)
                .Select(o => o.Id)
        );
    }

    // #133: ROW_NUMBER() OVER (ORDER BY ... COLLATE "C") numbers the emoji after "ａ".
    [Fact]
    public void NumbersRowsInUtf16Order()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Where(o => o.Note != null),
                null,
                rows =>
                    rows.OrderBy(o => o.Id == 1 ? "ａ" : o.Note, StringComparer.Ordinal)
                        .ThenBy(o => o.Id),
                null,
                (o, w) => new { o.Id, Number = w.RowNumber() }
            )
        );
    }

    // #133: MAX(... COLLATE "C") of a sequence is the emoji, .NET's maximum is "ａ".
    [Fact]
    public void TakesOrdinalExtremesOfSequenceInUtf16Order()
    {
        IQueryable<string?> notes = Orders.Select(o => o.Id == 1 ? "ａ" : o.Note);

        database.AssertSameResult(notes, q => q.Max(StringComparer.Ordinal));
        database.AssertSameResult(
            Orders.Select(o =>
                o.Id == 1 ? "ａ"
                : o.Id == 7 ? o.Note
                : null
            ),
            q => q.Min(StringComparer.Ordinal)
        );
    }

    // #133: STRING_AGG(... ORDER BY ... COLLATE "C") joins the emoji after "ａ".
    [Fact]
    public void JoinsGroupInUtf16Order()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.Note != null)
                .GroupBy(o => 1)
                .Select(g => new
                {
                    Notes = string.Join(
                        ",",
                        g.Select(o => o.Id == 1 ? "ａ" : o.Note)
                            .OrderBy(n => n, StringComparer.Ordinal)
                    ),
                })
        );
    }

    // #133: without a comparer LINQ to Objects orders with the current culture, where
    // case and leading white space weigh less than letters, and which differs from
    // one machine to the next. SQL orders text by the collation of its database, as
    // every query over a text column does: here the code point order of C.UTF-8,
    // which orders "MiXeD Case" before "bob". The order of a culture is the
    // database's to give, by a collation of that culture, so the translation neither
    // refuses the most common of orders nor claims the culture's.
    [Fact]
    public void OrdersTextWithoutComparerInDatabaseCollation()
    {
        IQueryable<int> query = Customers
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => c.Id);

        ResultSet result = database.Query(
            new PostgreSqlQuery(query.Expression).TextValue
        );

        Assert.Equal(
            Seed.Customers.OrderBy(c => c.Name, StringComparer.Ordinal)
                .ThenBy(c => c.Id)
                .Select(c => c.Id),
            result.Rows.Select(row => (int)row[0]!)
        );
    }
}
