namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal abstract class SqlMembership(bool nullable) : SqlExpression(typeof(bool))
{
    public override bool Nullable => nullable;

    public override bool IsCompound(SqlMode mode)
    {
        return !IsCoalesced(mode);
    }

    public override void Render(SqlWriter writer, SqlMode mode)
    {
        if (IsCoalesced(mode))
        {
            _ = writer.Append("COALESCE(");
            RenderMembership(writer);
            _ = writer.Append(", FALSE)");
            return;
        }
        RenderMembership(writer);
    }

    protected abstract void RenderMembership(SqlWriter writer);

    private bool IsCoalesced(SqlMode mode)
    {
        return mode == SqlMode.Value && Nullable;
    }
}
