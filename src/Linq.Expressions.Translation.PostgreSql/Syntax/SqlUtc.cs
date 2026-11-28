namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// A timestamp that is a DateTime of the kind Utc. It renders as the timestamp it is,
// so that its members, its arithmetic and its comparisons read its ticks as they
// are written, and is converted to the UTC offset only where it is read.
internal sealed class SqlUtc(SqlExpression operand) : SqlExpression(operand.Type)
{
    public override IEnumerable<SqlExpression> Operands => [Operand];

    public SqlExpression Operand { get; } = operand;

    public override bool Nullable => Operand.Nullable;

    public override SqlKind TimeKind => SqlKind.Utc;

    public override bool IsCompound(SqlMode mode)
    {
        return Operand.IsCompound(mode);
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        Operand.Render(writer, mode);
    }
}
