namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlIsNull(SqlExpression operand, bool negated)
    : SqlExpression(typeof(bool), false)
{
    public override bool IsCompound(SqlMode mode)
    {
        return true;
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Operand(operand, SqlMode.Value)
            .Append(negated ? " IS NOT NULL" : " IS NULL");
    }
}
