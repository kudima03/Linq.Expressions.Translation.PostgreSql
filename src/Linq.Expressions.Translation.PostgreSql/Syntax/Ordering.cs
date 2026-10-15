namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal sealed class Ordering(SqlExpression key, bool descending)
{
    public SqlExpression Key { get; } = key;

    public bool Descending { get; } = descending;

    public Ordering Inverted()
    {
        return new Ordering(Key, !Descending);
    }

    public void Render(SqlWriter writer)
    {
        _ = writer.Render(Key, SqlMode.Value).Append(Descending ? " DESC" : string.Empty);
        if (Key.Nullable)
        {
            _ = writer.Append(Descending ? " NULLS LAST" : " NULLS FIRST");
        }
    }
}
