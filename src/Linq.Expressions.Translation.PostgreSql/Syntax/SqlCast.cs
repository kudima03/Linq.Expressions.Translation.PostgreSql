namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class SqlCast(SqlExpression operand, string target, Type type)
    : SqlExpression(type)
{
    public override IEnumerable<SqlExpression> Operands => [Operand];

    public SqlExpression Operand { get; } = operand;

    public override bool Nullable => Operand.Nullable;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Append("CAST(").Cast(Operand).Append(" AS " + target + ")");
    }
}
