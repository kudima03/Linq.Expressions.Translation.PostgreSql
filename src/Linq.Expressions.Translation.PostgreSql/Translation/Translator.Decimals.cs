using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// System.Decimal against numeric. A decimal holds 96 bits of digits and up to 28
// decimal places; a numeric has no such limit, so its sums, differences and
// products are exact where a decimal rounds or overflows, and they are left exact.
// A quotient is not exact in either: numeric cuts it at a scale of its own, about
// 16 significant digits or the scale of the operands, where a decimal keeps as many
// decimal places as its 96 bits hold. A quotient and an average are therefore
// computed to 30 places and rounded to the places of a decimal.
internal sealed partial class Translator
{
    // The places a quotient is computed to, past the 28 of a decimal.
    private const int QuotientScale = 30;

    private const int DecimalScale = 28;

    // A tenth of 7.9228162514264337593543950335, the digits of decimal.MaxValue,
    // which has a place more than a decimal holds.
    private static readonly SqlFragment Bound = new(
        "0.79228162514264337593543950335",
        typeof(decimal)
    );

    private static bool IsDecimal(Type type)
    {
        return ScalarTypes.Underlying(type) == typeof(decimal);
    }

    // left / right of decimals: the dividend padded to 30 places, which is then the
    // scale of the quotient.
    private static SqlFunction Quotient(
        SqlExpression left,
        SqlExpression right,
        Type type
    )
    {
        return Fitted(
            new SqlBinary(
                "/",
                SqlBinaryKind.Arithmetic,
                Padded(left),
                right,
                type,
                left.Nullable || right.Nullable
            )
        );
    }

    private static SqlFunction Padded(SqlExpression value)
    {
        return new SqlFunction(
            "ROUND",
            [value, Integer(QuotientScale)],
            value.Type,
            value.Nullable
        );
    }

    // A value rounded to the decimal places of a decimal: 28, less one for every
    // digit of the value above 7.9228162514264337593543950335, where 29 digits no
    // longer fit the 96 bits. The whole part of the value over a tenth of that
    // bound has one digit up to the bound and one more for each power of ten above
    // it. A decimal keeps no more places than the value needs, and neither does
    // this; a tie is rounded away from zero, where a decimal rounds it to even.
    private static SqlFunction Fitted(SqlExpression value)
    {
        SqlBinary scaled = new(
            "/",
            SqlBinaryKind.Arithmetic,
            new SqlFunction("ABS", [value], typeof(decimal), value.Nullable),
            Bound,
            typeof(decimal),
            value.Nullable
        );
        SqlBinary places = Calculate(
            "-",
            Integer(DecimalScale + 1),
            new SqlFunction(
                "LENGTH",
                [
                    new SqlCast(
                        new SqlFunction("TRUNC", [scaled], typeof(decimal)),
                        "text",
                        typeof(string)
                    ),
                ],
                typeof(int)
            )
        );
        return new SqlFunction(
            "TRIM_SCALE",
            [new SqlFunction("ROUND", [value, places], value.Type, value.Nullable)],
            value.Type,
            value.Nullable
        );
    }
}
