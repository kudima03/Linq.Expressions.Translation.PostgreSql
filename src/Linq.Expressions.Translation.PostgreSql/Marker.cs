namespace Linq.Expressions.Translation.PostgreSql;

// The members of PostgreSqlWindows, PostgreSqlWindow<TSource> and
// PostgreSqlRecursion mark PostgreSQL's SQL in an expression tree; nothing
// computes them in memory. Called, they throw NotSupportedException, which tells a
// provider that evaluates closed subtrees to keep the call in the tree.
internal static class Marker
{
    public static NotSupportedException Called(string member)
    {
        return new NotSupportedException(
            $"'{member}' marks PostgreSQL's SQL in an expression tree that PostgreSqlQuery translates; it cannot be called."
        );
    }
}
