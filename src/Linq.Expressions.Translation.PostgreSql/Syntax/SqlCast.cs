namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlCast(SqlExpression operand, string target, Type type)
    : SqlExpression(type, operand.Nullable)
{
    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer
            .Append("CAST(")
            .Render(operand, SqlMode.Value)
            .Append(" AS " + target + ")");
    }
}
