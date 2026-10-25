namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal abstract class SqlExpression(Type type)
{
    public Type Type { get; } = type;

    public virtual bool Nullable => false;

    public virtual bool IsCompound(SqlMode mode)
    {
        return false;
    }

    public virtual SqlExpression AsNullable()
    {
        return new SqlNullable(this);
    }

    public abstract void Render(SqlWriter writer, SqlMode mode);
}
