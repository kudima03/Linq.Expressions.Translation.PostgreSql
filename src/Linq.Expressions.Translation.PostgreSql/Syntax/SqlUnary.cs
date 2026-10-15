namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlUnary(string symbol, SqlExpression operand, Type type)
    : SqlExpression(type)
{
    public override bool Nullable => operand.Nullable;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append(symbol).Operand(operand, SqlMode.Value);
    }
}
