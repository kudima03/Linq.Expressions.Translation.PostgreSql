namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlAny(SqlExpression value, SqlExpression array)
    : SqlExpression(typeof(bool))
{
    public override bool IsCompound(SqlMode mode)
    {
        return true;
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Operand(value, SqlMode.Value)
            .Append(" = ANY (")
            .Render(array, SqlMode.Value)
            .Append(")");
    }
}
