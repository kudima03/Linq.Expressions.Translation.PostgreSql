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
        nameof(Queryable.MinBy),
        nameof(Queryable.MaxBy),
    ];

    // StringComparer.Ordinal compares UTF-16 code units. The "C" collation compares
    // the bytes of UTF-8, which is the order of code points: the same order, except
    // that a character above U+FFFF, a surrogate pair from U+D800 in UTF-16, comes
    // before U+E000..U+FFFF there and after them here. The key of the ordinal order
    // is therefore the text with U+D7FF, the last character before the surrogates,
    // put before every such character, in the "C" collation: it then sorts after
    // every character below U+D800, and after U+D7FF itself by the character that
    // follows, and before U+E000. Taking the marker away from before those
    // characters gives the text back, as an extreme of the keys needs.
    private static readonly SqlConstant Astral = Text("([\\U00010000-\\U0010FFFF])");

    private static readonly SqlFragment Prefixed = new("E'\\ud7ff\\\\1'", typeof(string));

    private static readonly SqlFragment Prefix = new(
        "E'\\ud7ff([\\\\U00010000-\\\\U0010FFFF])'",
        typeof(string)
    );

    private static SqlCollation CodeUnits(SqlExpression text)
    {
        return new SqlCollation(Key(text));
    }

    // The key without its collation, which the other side of a comparison gives it.
    private static SqlExpression Key(SqlExpression text)
    {
        return text switch
        {
            SqlConstant { Value: string value } => Text(Keyed(value)),
            SqlConstant => text,
            _ => Replaced(text, Astral, Prefixed),
        };
    }

    // The key of a constant text: U+D7FF before every surrogate pair.
    private static string Keyed(string text)
    {
        return string.Concat(
            text.Select(character =>
                char.IsHighSurrogate(character)
                    ? "\ud7ff" + character
                    : character.ToString()
            )
        );
    }

    // The text of a key of the ordinal order, in the collation of the database.
    private static SqlCollation Restored(SqlExpression key, bool nullable)
    {
        return new SqlCollation(
            new SqlFunction(
                "REGEXP_REPLACE",
                [key, Prefix, Text("\\1"), Text("g")],
                typeof(string),
                nullable
            ),
            "default"
        );
    }

    private static SqlFunction Replaced(
        SqlExpression text,
        SqlExpression pattern,
        SqlExpression replacement
    )
    {
        return new SqlFunction(
            "REGEXP_REPLACE",
            [text, pattern, replacement, Text("g")],
            typeof(string),
            text.Nullable
        );
    }

    // Any other comparer stays rejected.
    private static bool IsOrdinal(MethodCallExpression call)
    {
        return Sorts.Contains(call.Method.Name)
            && call.Arguments[^1] is ConstantExpression { Value: object comparer }
            && ReferenceEquals(comparer, StringComparer.Ordinal);
    }

    // CompareOrdinal(a, b) compared with 0 is an ordinal comparison of a with b.
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
            CodeUnits(left),
            Key(right),
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
