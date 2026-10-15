namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlInQuery(SqlExpression operand, SqlQuery query)
    : SqlExpression(typeof(bool))
{
    public override bool IsCompound(SqlMode mode)
    {
        return true;
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Operand(operand, SqlMode.Value).Append(" IN (");
        query.Render(writer);
        _ = writer.Append(")");
    }
}
