namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlAny(SqlExpression value, SqlExpression array)
    : SqlMembership(value.Nullable || array.Nullable)
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
