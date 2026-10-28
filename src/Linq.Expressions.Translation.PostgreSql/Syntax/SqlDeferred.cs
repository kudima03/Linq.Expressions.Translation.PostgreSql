namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// A value wrapped in a scalar subquery: the planner reads it as a parameter when a
// row reaches it, instead of folding the operation around it with the statement.
internal sealed class SqlDeferred(SqlExpression value) : SqlExpression(value.Type)
{
    public override bool Nullable => value.Nullable;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append("(SELECT ").Render(value, SqlMode.Value).Append(")");
    }
}
