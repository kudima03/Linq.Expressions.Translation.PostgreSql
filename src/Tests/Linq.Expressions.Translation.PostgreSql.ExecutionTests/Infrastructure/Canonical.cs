using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// A value as text that is equal for equal values of both sides: objects member by
// member, sequences item by item, instants in UTC, timestamps without their kind.
// Decimals compare to 10 places and floating-point numbers to 12 significant digits,
// as numeric division and AVG keep fewer digits than .NET and PostgreSQL may sum
// floating-point numbers in another order.
public static class Canonical
{
    private const string Seconds = "HH:mm:ss.fffffff";

    public static string Describe(object? value)
    {
        return value switch
        {
            null => "null",
            string text => JsonSerializer.Serialize(text),
            char character => JsonSerializer.Serialize(character.ToString()),
            bool flag => flag ? "true" : "false",
            decimal number => Decimal(number),
            double number => Floating(number),
            float number => Floating(number),
            Enum member => member.ToString(),
            DateTime moment => moment.ToString(
                "yyyy-MM-dd'T'" + Seconds,
                CultureInfo.InvariantCulture
            ),
            DateTimeOffset moment => moment.UtcDateTime.ToString(
                "yyyy-MM-dd'T'" + Seconds + "'Z'",
                CultureInfo.InvariantCulture
            ),
            DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString(Seconds, CultureInfo.InvariantCulture),
            TimeSpan span => span.ToString("c", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString(),
            IFormattable number when value.GetType().IsPrimitive => number.ToString(
                null,
                CultureInfo.InvariantCulture
            ),
            ITuple tuple => "("
                + string.Join(
                    ", ",
                    Enumerable
                        .Range(0, tuple.Length)
                        .Select(index => Describe(tuple[index]))
                )
                + ")",
            IEnumerable items => "["
                + string.Join(", ", items.Cast<object?>().Select(Describe))
                + "]",
            _ => "{"
                + string.Join(
                    ", ",
                    value
                        .GetType()
                        .GetProperties()
                        .Where(property => property.GetIndexParameters().Length == 0)
                        .Select(property =>
                            $"{property.Name}={Describe(property.GetValue(value))}"
                        )
                )
                + "}",
        };
    }

    private static string Decimal(decimal number)
    {
        decimal rounded = Math.Round(number, 10);
        return rounded == 0m
            ? "0"
            : rounded.ToString("0.##########", CultureInfo.InvariantCulture);
    }

    private static string Floating(double number)
    {
        return number == 0D ? "0" : number.ToString("G12", CultureInfo.InvariantCulture);
    }
}
