namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlInQuery(SqlExpression operand, SqlQuery query, bool nullable)
    : SqlMembership(operand.Nullable || nullable)
{
    public override IEnumerable<SqlExpression> Operands => [operand];

    protected override void RenderMembership(SqlWriter writer)
    {
        _ = writer.Operand(operand, SqlMode.Value).Append(" IN (");
        query.Render(writer);
        _ = writer.Append(")");
    }
}
