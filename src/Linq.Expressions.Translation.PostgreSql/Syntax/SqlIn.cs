namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlIn(SqlExpression operand, IReadOnlyList<SqlExpression> values)
    : SqlExpression(typeof(bool))
{
    public override bool IsCompound(SqlMode mode)
    {
        return true;
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Operand(operand, SqlMode.Value)
            .Append(" IN (")
            .Join(values, value => writer.Render(value, SqlMode.Value))
            .Append(")");
    }
}
