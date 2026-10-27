namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// A nullable key sorts as in .NET, nulls first ascending and last descending, unless
// its nulls are to sort last whatever the direction.
internal sealed class Ordering(SqlExpression key, bool descending, bool nullsLast)
{
    public Ordering(SqlExpression key, bool descending)
        : this(key, descending, descending) { }

    public SqlExpression Key { get; } = key;

    public bool Descending { get; } = descending;

    public bool NullsLast { get; } = nullsLast;

    public Ordering Inverted()
    {
        return new Ordering(Key, !Descending, !NullsLast);
    }

    public void Render(SqlWriter writer)
    {
        _ = writer.Render(Key, SqlMode.Value).Append(Descending ? " DESC" : string.Empty);
        if (Key.Nullable)
        {
            _ = writer.Append(NullsLast ? " NULLS LAST" : " NULLS FIRST");
        }
    }
}
