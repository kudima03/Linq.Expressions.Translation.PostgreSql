using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// NaN in .NET against PostgreSQL. PostgreSQL takes NaN for equal to itself and
// greater than every number, so that it can be sorted and indexed; in .NET the
// operators ==, <, <=, > and >= are false for it and != is true, while a sort, Min,
// Max, MinBy and MaxBy rank it below every number (Comparer<double>) and Equals,
// Distinct, GroupBy, Join and Contains take it for equal to itself, as PostgreSQL
// does. So a comparison of floating-point values that PostgreSQL would decide by
// NaN being the greatest is guarded, the keys of an order put NaN first, and Max
// and Min are taken of the negated values.
internal sealed partial class Translator
{
    // The text PostgreSQL reads NaN from, of the type of what it is compared with.
    private static readonly SqlFragment NaN = new("'NaN'", typeof(double));

    // Whether a value may be NaN: a floating-point one that is no other constant,
    // no number converted from an integer or a decimal, and no rank or distribution
    // of a window.
    private static bool MayBeNaN(SqlExpression value)
    {
        return IsFloating(value.Type)
            && value switch
            {
                SqlConstant { Value: double number } => double.IsNaN(number),
                SqlConstant { Value: float number } => float.IsNaN(number),
                SqlCast conversion => IsFloating(conversion.Operand.Type),
                SqlWindowFunction function => function.IsRead,
                SqlRequired required => MayBeNaN(required.Operand),
                _ => true,
            };
    }

    // left < right and the like, false where PostgreSQL finds NaN the greater: on
    // the left of > and >=, on the right of < and <=.
    private static SqlExpression Ordered(SqlBinary comparison)
    {
        SqlExpression greater = comparison.Symbol is ">" or ">="
            ? comparison.Left
            : comparison.Right;
        return MayBeNaN(greater)
            ? Logical("AND", comparison, Compare("<>", greater, NaN))
            : comparison;
    }

    // left = right, false for NaN on both sides, and left <> right, true for it. A
    // constant other than NaN on either side already decides it so.
    private static SqlExpression Unequal(
        SqlExpression comparison,
        SqlExpression left,
        SqlExpression right,
        bool negated
    )
    {
        if (!MayBeNaN(left) || !MayBeNaN(right))
        {
            return comparison;
        }
        return left.Nullable
                ? Logical(
                    negated ? "OR" : "AND",
                    comparison,
                    Distinct(left, NaN, !negated)
                )
            : negated ? Logical("OR", comparison, Compare("=", left, NaN))
            : Logical("AND", comparison, Compare("<>", left, NaN));
    }

    // The keys that sort a value as .NET does. NaN comes before every number
    // ascending and after them descending: a value that is never NULL is sorted as
    // NULL where it is NaN, and one that may be NULL by whether it is NaN first, its
    // nulls first ascending and last descending as elsewhere, or last when asked.
    private static IEnumerable<Ordering> Keys(
        SqlExpression value,
        bool descending,
        bool nullsLast
    )
    {
        if (!MayBeNaN(value))
        {
            return [new Ordering(value, descending, nullsLast)];
        }
        if (!value.Nullable)
        {
            return
            [
                new Ordering(
                    new SqlFunction("NULLIF", [value, NaN], value.Type, true),
                    descending,
                    descending
                ),
            ];
        }
        return
        [
            new Ordering(
                new SqlBinary(
                    "=",
                    SqlBinaryKind.Arithmetic,
                    value,
                    NaN,
                    typeof(bool?),
                    true
                ),
                !descending,
                nullsLast
            ),
            new Ordering(value, descending, nullsLast),
        ];
    }

    // Max of floating-point values is NaN only when every value is, and Min is NaN
    // when one of them is, the reverse of MAX and MIN: -MIN(-x) and -MAX(-x).
    private static SqlUnary Negated(SqlExpression value)
    {
        return new SqlUnary("-", value, value.Type);
    }
}
