using System.Globalization;
using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal static class SqlLiteral
{
    private const string Seconds = "HH:mm:ss.FFFFFF";

    public static string Render(object? value)
    {
        return value switch
        {
            null => "NULL",
            bool flag => flag ? "TRUE" : "FALSE",
            string text => Text(text),
            char character => Text(character.ToString()),
            Enum member => Convert
                .ChangeType(
                    member,
                    Enum.GetUnderlyingType(member.GetType()),
                    CultureInfo.InvariantCulture
                )
                .ToString()!,
            double number => Floating(number, "double precision"),
            float number => Floating(number, "real"),
            Guid guid => $"UUID '{guid}'",
            DateTime moment => Timestamp(moment),
            DateTimeOffset moment =>
                $"TIMESTAMPTZ '{Precise(moment.DateTime)}{Offset(moment)}'",
            DateOnly day =>
                $"DATE '{day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}'",
            TimeOnly time =>
                $"TIME '{time.ToString(Seconds, CultureInfo.InvariantCulture)}'",
            TimeSpan span => $"INTERVAL '{Duration(span)}'",
            Array items when value is not byte[] => Items(items),
            IFormattable number when value.GetType().IsPrimitive || value is decimal =>
                number.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException(
                $"Constants of type '{value.GetType().Name}' cannot be rendered as SQL literals."
            ),
        };
    }

    private static string Text(string text)
    {
        if (text.Contains('\0', StringComparison.Ordinal))
        {
            throw new NotSupportedException(
                "PostgreSQL text cannot contain the NUL character."
            );
        }
        if (!text.Any(IsInvisible))
        {
            return "'" + text.Replace("'", "''", StringComparison.Ordinal) + "'";
        }
        StringBuilder escaped = new("E'");
        foreach (char character in text)
        {
            _ = escaped.Append(
                character switch
                {
                    '\'' => "''",
                    '\\' => "\\\\",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    // \x writes a byte, which is no UTF-8 character above 0x7f.
                    _ when char.IsControl(character) && character < 0x80 =>
                        $"\\x{(int)character:x2}",
                    _ when IsInvisible(character) => $"\\u{(int)character:x4}",
                    _ => character.ToString(),
                }
            );
        }
        return escaped.Append('\'').ToString();
    }

    // A control character or a white-space character other than the space is written
    // as an escape, so that the statement shows it and stays a single line.
    private static bool IsInvisible(char character)
    {
        return char.IsControl(character)
            || (char.IsWhiteSpace(character) && character != ' ');
    }

    private static string Floating(IFormattable number, string type)
    {
        NumberFormatInfo format = NumberFormatInfo.InvariantInfo;
        string text = number.ToString(null, format);
        return text == format.NaNSymbol ? $"CAST('NaN' AS {type})"
            : text == format.PositiveInfinitySymbol ? $"CAST('Infinity' AS {type})"
            : text == format.NegativeInfinitySymbol ? $"CAST('-Infinity' AS {type})"
            : text;
    }

    private static string Timestamp(DateTime moment)
    {
        return moment.Kind == DateTimeKind.Local
                ? throw new NotSupportedException(
                    "DateTime constants of kind Local have no PostgreSQL equivalent."
                )
            : moment.Kind == DateTimeKind.Utc ? $"TIMESTAMPTZ '{Precise(moment)}+00'"
            : $"TIMESTAMP '{Precise(moment)}'";
    }

    private static string Precise(DateTime moment)
    {
        return moment.Ticks % 10 != 0
            ? throw new NotSupportedException(
                "PostgreSQL timestamps are limited to microsecond precision."
            )
            : moment.ToString("yyyy-MM-dd " + Seconds, CultureInfo.InvariantCulture);
    }

    private static string Offset(DateTimeOffset moment)
    {
        return moment.Offset == TimeSpan.Zero
            ? "+00"
            : moment.ToString("zzz", CultureInfo.InvariantCulture);
    }

    private static string Duration(TimeSpan span)
    {
        string date = span.Days == 0 ? string.Empty : $"{span.Days}D";
        decimal seconds =
            (decimal)(span.Ticks % TimeSpan.TicksPerMinute) / TimeSpan.TicksPerSecond;
        string time =
            (span.Hours == 0 ? string.Empty : $"{span.Hours}H")
            + (span.Minutes == 0 ? string.Empty : $"{span.Minutes}M")
            + (
                seconds == 0
                    ? string.Empty
                    : seconds.ToString(CultureInfo.InvariantCulture) + "S"
            );
        string text = date + (time.Length == 0 ? string.Empty : "T" + time);
        return text.Length == 0 ? "PT0S" : "P" + text;
    }

    // ARRAY[] has no element type; the empty array literal is cast by its context.
    private static string Items(Array items)
    {
        return items.Length == 0
            ? "'{}'"
            : "ARRAY[" + string.Join(", ", items.Cast<object?>().Select(Render)) + "]";
    }
}
