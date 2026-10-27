using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly HashSet<string> Sorts =
    [
        nameof(Queryable.Order),
        nameof(Queryable.OrderDescending),
        nameof(Queryable.OrderBy),
        nameof(Queryable.OrderByDescending),
        nameof(Queryable.ThenBy),
        nameof(Queryable.ThenByDescending),
    ];

    // StringComparer.Ordinal asks for the byte-wise order, which the "C" collation
    // gives; any other comparer stays rejected.
    private static bool IsOrdinal(MethodCallExpression call)
    {
        return Sorts.Contains(call.Method.Name)
            && call.Arguments[^1] is ConstantExpression { Value: object comparer }
            && ReferenceEquals(comparer, StringComparer.Ordinal);
    }

    // CompareOrdinal(a, b) compared with 0 is a byte-wise comparison of a with b.
    private static MethodCallExpression? Ordinal(Expression left, Expression right)
    {
        return
            left is MethodCallExpression { Arguments.Count: 2 } call
            && call.Method.Name == nameof(string.CompareOrdinal)
            && call.Method.DeclaringType == typeof(string)
            && right is ConstantExpression { Value: 0 }
            ? call
            : null;
    }

    // CompareOrdinal ranks null below every string: a null lesser side makes < and <=
    // true, unless the greater side is null too and the comparison is strict.
    private SqlExpression Ranked(MethodCallExpression call, string symbol)
    {
        SqlExpression left = Sql(call.Arguments[0]);
        SqlExpression right = Sql(call.Arguments[1]);
        SqlExpression comparison = new SqlBinary(
            symbol,
            SqlBinaryKind.Comparison,
            new SqlCollation(left),
            right,
            typeof(bool),
            left.Nullable || right.Nullable
        );
        bool less = symbol is "<" or "<=";
        SqlExpression lesser = less ? left : right;
        SqlExpression greater = less ? right : left;
        if (!lesser.Nullable)
        {
            return comparison;
        }
        SqlExpression missing = new SqlIsNull(lesser, false);
        return Logical(
            "OR",
            symbol is "<" or ">" && greater.Nullable
                ? Logical("AND", missing, new SqlIsNull(greater, true))
                : missing,
            comparison
        );
    }
}
