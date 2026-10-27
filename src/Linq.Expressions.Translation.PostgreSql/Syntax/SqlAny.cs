namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// An array may hold NULL elements whatever the CLR element type: = ANY is NULL, not
// FALSE, when no element matches and one is NULL, so the test is always nullable.
internal sealed class SqlAny(SqlExpression value, SqlExpression array)
    : SqlMembership(true)
{
    protected override void RenderMembership(SqlWriter writer)
    {
        _ = writer
            .Operand(value, SqlMode.Value)
            .Append(" = ANY (")
            .Render(array, SqlMode.Value)
            .Append(")");
    }
}
