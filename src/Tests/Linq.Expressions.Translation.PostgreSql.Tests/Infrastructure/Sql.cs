using System.Text;
using System.Text.RegularExpressions;
using PgSqlParser;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public static partial class Sql
{
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
