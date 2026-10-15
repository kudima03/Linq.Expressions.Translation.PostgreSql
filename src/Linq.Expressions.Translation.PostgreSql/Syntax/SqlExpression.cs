namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal abstract class SqlExpression(Type type, bool nullable)
{
    public Type Type { get; } = type;

    public bool Nullable { get; } = nullable;

    public virtual bool IsCompound(SqlMode mode)
    {
        return false;
    }

    public abstract void Render(SqlWriter writer, SqlMode mode);
}
