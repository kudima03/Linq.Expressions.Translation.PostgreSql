using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

// A captured computation that throws is left to the database where it may not be
// reached: the branch of a conditional or the right operand of ??, && and ||. One
// of its constants is a scalar subquery, so that the planner does not compute it
// before any row is read.
public sealed record DeferredEvaluationTests
{
    [Fact]
    public void DefersFailingOperandOfBranchNotTaken()
    {
        decimal one = 1m;
        decimal zero = 0m;

        Expression query = Orders.Select(o => o.Id < 0 ? one / zero : o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Id" < 0 THEN (SELECT 1) / 0 ELSE t0."Total" END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefersFailingOperandOfCoalesce()
    {
        decimal one = 1m;
        decimal zero = 0m;

        Expression query = Orders.Select(o => o.Discount ?? (one / zero)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Discount", (SELECT 1) / 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefersFailingOperandOfConjunction()
    {
        int one = 1;
        int zero = 0;

        Expression query = Orders.Select(o => o.Id < 0 && (one / zero) > 0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Id" < 0) AND (((SELECT 1) / 0) > 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefersFailingOperandOfDisjunction()
    {
        int one = 1;
        int zero = 0;

        Expression query = Orders.Select(o => o.Id > 0 || (one / zero) > 0).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Id" > 0) OR (((SELECT 1) / 0) > 0)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefersOnlyTheFailingOperation()
    {
        decimal one = 1m;
        decimal zero = 0m;

        Expression query = Orders
            .Select(o => o.Id < 0 ? (one / zero) + one : o.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Id" < 0 THEN ((SELECT 1) / 0) + 1 ELSE t0."Total" END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefersFailingOperationNestedInFailingBranch()
    {
        decimal one = 1m;
        decimal zero = 0m;
        bool flag = true;

        Expression query = Orders
            .Select(o => o.Id < 0 ? (flag ? one / zero : one) : o.Total)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Id" < 0 THEN CASE WHEN TRUE THEN (SELECT 1) / 0 ELSE 1 END ELSE t0."Total" END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DefersFirstOperandOfFailingCall()
    {
        string text = "x";

        Expression query = Orders
            .Select(o =>
                o.Id < 0
                    ? long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture)
                    : o.Points
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Id" < 0 THEN CAST((SELECT 'x') AS bigint) ELSE t0."Points" END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EvaluatesOperandsOfBranchThatDoNotFail()
    {
        decimal one = 1m;
        decimal two = 2m;

        Expression query = Orders.Select(o => o.Id < 0 ? one / two : o.Total).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Id" < 0 THEN 0.5 ELSE t0."Total" END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FailsOnFailingOperandThatIsReached()
    {
        decimal one = 1m;
        decimal zero = 0m;

        Expression query = Orders.Where(o => o.Total > one / zero).Expression;

        _ = Assert.Throws<DivideByZeroException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FailsOnFailingTestOfConditional()
    {
        int one = 1;
        int zero = 0;

        Expression query = Orders
            .Select(o => (one / zero) > 0 ? o.Total : o.Quantity)
            .Expression;

        _ = Assert.Throws<DivideByZeroException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FailsOnFailingLeftOperandOfConjunction()
    {
        int one = 1;
        int zero = 0;

        Expression query = Orders.Where(o => (one / zero) > 0 && o.IsPaid).Expression;

        _ = Assert.Throws<DivideByZeroException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FailsOnFailingLeftOperandOfDisjunction()
    {
        int one = 1;
        int zero = 0;

        Expression query = Orders.Where(o => (one / zero) > 0 || o.IsPaid).Expression;

        _ = Assert.Throws<DivideByZeroException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FailsOnFailingLeftOperandOfCoalesce()
    {
        decimal one = 1m;
        decimal zero = 0m;

        Expression query = Orders
            .Select(o => (decimal?)(one / zero) ?? o.Total)
            .Expression;

        _ = Assert.Throws<DivideByZeroException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FailsOnFailingOperandReachedAfterBranch()
    {
        decimal one = 1m;
        decimal zero = 0m;

        Expression query = Orders
            .Select(o => new
            {
                Lazy = o.Id < 0 ? one / zero : o.Total,
                Eager = one / zero,
            })
            .Expression;

        _ = Assert.Throws<DivideByZeroException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }
}
