using PgSqlParser;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public static class Sql
{
    public static string Valid(string statement)
    {
        string single = string.Join(' ', statement.ReplaceLineEndings("\n").Split('\n'));
        Assert.Equal(single.Trim(), single);
        Assert.DoesNotContain("  ", single, StringComparison.Ordinal);
        Assert.False(single.EndsWith(';'), single);
        Result<ParseResult> result = Parser.Parse(single);
        Assert.True(result.IsSuccess, $"{result.Error?.Message}: {single}");
        RawStmt parsed = Assert.Single(result.Value.Stmts);
        _ = Assert.IsType<SelectStmt>(parsed.Stmt.Unwrap());
        return single;
    }
}
