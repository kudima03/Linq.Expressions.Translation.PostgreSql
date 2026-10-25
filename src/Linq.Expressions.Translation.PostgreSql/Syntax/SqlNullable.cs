namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// A value that may be NULL where its operands are not: a column of a set operation
// whose other side is nullable, or a computed column of a left-joined source.
internal sealed class SqlNullable(SqlExpression operand) : SqlExpression(operand.Type)
{
    public override bool Nullable => true;

    public override bool IsCompound(SqlMode mode)
    {
        return operand.IsCompound(mode);
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        operand.Render(writer, mode);
    }
}
