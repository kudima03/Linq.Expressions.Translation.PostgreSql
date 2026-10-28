namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// LINQ to Objects evaluates only the branch a conditional takes and the right operand
// of ??, && and || only when it is reached, so an operand that would throw is
// harmless when no row reaches it.
[Collection(SharedDatabase.Name)]
public sealed class EvaluationSemanticsTests(Database database)
{
    [Fact]
    public void LeavesBranchThatIsNotTakenUnevaluated()
    {
        decimal one = 1m;
        decimal zero = 0m;

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, Ratio = o.Id < 0 ? one / zero : o.Total })
        );
    }

    [Fact]
    public void LeavesCoalesceOperandThatIsNotReachedUnevaluated()
    {
        decimal one = 1m;
        decimal zero = 0m;

        database.AssertSameSequence(
            Orders
                .Where(o => o.Discount != null)
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, Discount = o.Discount ?? (one / zero) })
        );
    }

    [Fact]
    public void LeavesConjunctionOperandThatIsNotReachedUnevaluated()
    {
        int one = 1;
        int zero = 0;

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, Both = o.Id < 0 && (one / zero) > 0 })
        );
    }

    [Fact]
    public void LeavesDisjunctionOperandThatIsNotReachedUnevaluated()
    {
        int one = 1;
        int zero = 0;

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, Either = o.Id > 0 || (one / zero) > 0 })
        );
    }

    [Fact]
    public void EvaluatesCapturedOperandsOfBranchThatIsTaken()
    {
        decimal one = 1m;
        decimal two = 2m;

        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .Select(o => new { o.Id, Ratio = o.Id > 0 ? one / two : o.Total })
        );
    }
}
