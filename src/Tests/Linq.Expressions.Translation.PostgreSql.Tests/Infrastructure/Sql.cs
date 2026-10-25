using System.Text;
using System.Text.RegularExpressions;
using PgSqlParser;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public static partial class Sql
{
    // Every white-space character of .NET, as Trim() removes it.
    public const string WhiteSpace =
        "E'\\t\\n\\x0b\\f\\r \\u0085\\u00a0\\u1680\\u2000\\u2001\\u2002\\u2003\\u2004\\u2005\\u2006\\u2007\\u2008\\u2009\\u200a\\u2028\\u2029\\u202f\\u205f\\u3000'";

    public static string Valid(string statement)
    {
        string single = Joined(statement);
        Assert.Equal(single.Trim(), single);
        Assert.DoesNotContain("  ", WithoutLiterals(single), StringComparison.Ordinal);
        Assert.False(single.EndsWith(';'), single);
        Result<ParseResult> result = Parser.Parse(single);
        Assert.True(result.IsSuccess, $"{result.Error?.Message}: {single}");
        RawStmt parsed = Assert.Single(result.Value.Stmts);
        _ = Assert.IsType<SelectStmt>(parsed.Stmt.Unwrap());
        return single;
    }

    private static string WithoutLiterals(string statement)
    {
        return Literal().Replace(statement, "''");
    }

    [GeneratedRegex("'(?:[^']|'')*'")]
    private static partial Regex Literal();

    private static string Joined(string statement)
    {
        StringBuilder result = new();
        foreach (string line in statement.ReplaceLineEndings("\n").Split('\n'))
        {
            string trimmed = line.Trim();
            bool glued =
                result.Length == 0 || result[^1] == '(' || trimmed.StartsWith(')');
            _ = result.Append(glued ? trimmed : ' ' + trimmed);
        }
        return result.ToString();
    }
}
