namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

[Collection(SharedDatabase.Name)]
public sealed class StringSemanticsTests(Database database)
{
    [Fact]
    public void TrimsEveryWhiteSpaceCharacter()
    {
        database.AssertSameSequence(
            Customers
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    Both = c.Name.Trim(),
                    Start = c.Name.TrimStart(),
                    End = c.Name.TrimEnd(),
                })
        );
    }

    [Fact]
    public void MatchesWildcardsAndBackslashLiterally()
    {
        database.AssertSameSequence(
            Customers
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    Percent = c.Name.Contains("%"),
                    Underscore = c.Name.Contains("_"),
                    Backslash = c.Name.Contains("\\"),
                    Prefix = c.Name.StartsWith("50%"),
                    Suffix = c.Name.EndsWith("_score"),
                    Quote = c.Name.StartsWith("O'"),
                    Escaped = c.Name.EndsWith("\\slash"),
                })
        );
    }

    [Fact]
    public void MatchesColumnPatternsLiterally()
    {
        database.AssertSameSequence(
            Customers
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    Containing = Customers.Count(d => d.Name.Contains(c.Name)),
                    Starting = Customers.Count(d => d.Name.StartsWith(c.Name)),
                    Ending = Customers.Count(d => d.Name.EndsWith(c.Name)),
                })
        );
    }

    [Fact]
    public void CountsPositionsFromZero()
    {
        char[] separators = ['_', '-'];

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new
                {
                    o.Id,
                    Character = o.Number.IndexOf('-'),
                    Text = o.Number.IndexOf("-"),
                    Missing = o.Number.IndexOf("x"),
                    Last = o.Number.LastIndexOf("0"),
                    Any = o.Number.IndexOfAny(separators),
                    Tail = o.Number.Substring(2),
                    Middle = o.Number.Substring(1, 3),
                    Removed = o.Number.Remove(1, 1),
                })
        );
    }

    [Fact(
        Skip = "#62: LENGTH and STRPOS count code points, .NET counts UTF-16 code units"
    )]
    public void CountsUtf16CodeUnits()
    {
        database.AssertSameSequence(
            Customers
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Name.Length,
                    Space = c.Name.IndexOf(' '),
                })
        );
    }

    [Fact]
    public void ComparesWithEscapedLiterals()
    {
        string quoted = "O'Brien";

        database.AssertSameRows(
            Customers
                .Where(c =>
                    c.Name == quoted
                    || c.Name == "back\\slash"
                    || c.Name == "  padded\t "
                    || c.Name == "\u00a0nbsp\u2003"
                    || c.Name == "\U0001F600 smile"
                    || c.Name == "Zo\u00eb\u0085"
                )
                .Select(c => c.Id)
        );
    }

    [Fact]
    public void ProjectsEscapedLiterals()
    {
        database.AssertSameRows(
            Customers
                .Where(c => c.Id == 1)
                .Select(c => new
                {
                    Quotes = "'O''Brien' \"x\"",
                    Controls = "tab\tnew\nline\r\u0001\u001f",
                    Backslash = "\\n is not a newline",
                    Wide = "\U0001F600\u00a0\u2028",
                })
        );
    }
}
