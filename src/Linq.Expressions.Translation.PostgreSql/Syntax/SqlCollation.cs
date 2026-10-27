namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// A text compared or sorted byte-wise, as an ordinal comparison does: the "C"
// collation orders UTF-8 bytes, which is code-point order.
internal sealed class SqlCollation(SqlExpression operand) : SqlExpression(typeof(string))
{
    public override bool Nullable => operand.Nullable;

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        _ = writer.Operand(operand, SqlMode.Value).Append(" COLLATE \"C\"");
    }
}
