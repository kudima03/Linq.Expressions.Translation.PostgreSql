namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// A nullable value read where C# requires one, Nullable<T>.Value or a cast to T: it
// is never NULL where the C# evaluates, so it compares with = and keeps the type of
// the value. The operand renders as it is.
internal sealed class SqlRequired(SqlExpression operand, Type type) : SqlExpression(type)
{
    public SqlExpression Operand { get; } = operand;

    public override bool Nullable => false;

    public override bool IsCompound(SqlMode mode)
    {
        return Operand.IsCompound(mode);
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        Operand.Render(writer, mode);
    }
}
