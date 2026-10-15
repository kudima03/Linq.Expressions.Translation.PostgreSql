using PgSqlParser;

namespace Linq.Expressions.Translation.PostgreSql.Tests;

public sealed record ExpectedStatementTests
{
    [Fact]
    public void EveryTestAssertsExactlyOneOutcome()
    {
        IReadOnlyList<RecordedTest> tests =
        [
            .. new RecordedTests(typeof(ExpectedStatementTests)),
        ];
        Assert.NotEmpty(tests);
        Assert.All(tests, test => Assert.Single(test.Outcomes));
    }

    [Fact]
    public void EveryExpectedStatementIsSinglePostgreSqlSelect()
    {
        IReadOnlyList<string> statements =
        [
            .. new RecordedTests(typeof(ExpectedStatementTests))
                .SelectMany(test => test.Outcomes)
                .Select(outcome => outcome.Sql)
                .OfType<string>(),
        ];
        Assert.NotEmpty(statements);
        Assert.All(
            statements,
            sql =>
            {
                Assert.Equal(sql.Trim(), sql);
                Assert.DoesNotContain(sql, char.IsControl);
                Assert.False(sql.EndsWith(';'), sql);
                Result<ParseResult> result = Parser.Parse(sql);
                Assert.True(result.IsSuccess, $"{result.Error?.Message}: {sql}");
                RawStmt statement = Assert.Single(result.Value.Stmts);
                _ = Assert.IsType<SelectStmt>(statement.Stmt.Unwrap());
            }
        );
    }
}
