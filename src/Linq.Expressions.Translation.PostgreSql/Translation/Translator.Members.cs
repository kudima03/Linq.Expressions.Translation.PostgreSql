using System.Reflection;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly HashSet<Type> Temporal =
    [
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
    ];

    private static readonly Dictionary<string, string> DateParts = new()
    {
        [nameof(DateTime.Year)] = "YEAR",
        [nameof(DateTime.Month)] = "MONTH",
        [nameof(DateTime.Day)] = "DAY",
        [nameof(DateTime.Hour)] = "HOUR",
        [nameof(DateTime.Minute)] = "MINUTE",
        [nameof(DateTime.Second)] = "SECOND",
        [nameof(DateTime.DayOfYear)] = "DOY",
        [nameof(DateTime.DayOfWeek)] = "DOW",
    };

    // Seconds of a duration; 0 is .NET's own unit.
    private static readonly Dictionary<string, int> Totals = new()
    {
        [nameof(TimeSpan.TotalDays)] = 86400,
        [nameof(TimeSpan.TotalHours)] = 3600,
        [nameof(TimeSpan.TotalMinutes)] = 60,
        [nameof(TimeSpan.TotalSeconds)] = 1,
    };

    private static readonly Dictionary<string, string> Durations = new()
    {
        [nameof(TimeSpan.Days)] = "DAY",
        [nameof(TimeSpan.Hours)] = "HOUR",
        [nameof(TimeSpan.Minutes)] = "MINUTE",
        [nameof(TimeSpan.Seconds)] = "SECOND",
    };

    private static readonly Dictionary<string, string> Fractions = new()
    {
        [nameof(DateTime.Millisecond)] = "MILLISECONDS",
        [nameof(DateTime.Microsecond)] = "MICROSECONDS",
    };

    private static SqlExpression ScalarMember(SqlExpression owner, MemberInfo member)
    {
        Type type = ((PropertyInfo)member).PropertyType;
        return member switch
        {
            { Name: nameof(Nullable<>.Value) } when IsNullable(member) => owner,
            { Name: nameof(Nullable<>.HasValue) } when IsNullable(member) =>
                new SqlIsNull(owner, true),
            { Name: nameof(string.Length) } when member.DeclaringType == typeof(string) =>
                new SqlFunction("LENGTH", [owner], type),
            { Name: nameof(DateTime.Date) } => new SqlFunction(
                "DATE_TRUNC",
                [Text("day"), owner],
                type
            ),
            { Name: nameof(DateTime.TimeOfDay) } => new SqlBinary(
                "-",
                SqlBinaryKind.Arithmetic,
                owner,
                new SqlFunction("DATE_TRUNC", [Text("day"), owner], owner.Type),
                type,
                owner.Nullable
            ),
            { Name: nameof(DateTimeOffset.UtcDateTime) } => new SqlBinary(
                "AT TIME ZONE",
                SqlBinaryKind.Arithmetic,
                owner,
                Text("UTC"),
                type,
                owner.Nullable
            ),
            { Name: nameof(DateOnly.DayNumber) } => Calculate(
                "-",
                owner,
                new SqlConstant(DateOnly.MinValue, typeof(DateOnly))
            ),
            { Name: nameof(DateTime.Ticks) }
                when member.DeclaringType == typeof(DateTime) => Ticks(
                Calculate("-", owner, Origin)
            ),
            { Name: nameof(TimeOnly.Ticks) }
                when member.DeclaringType == typeof(TimeOnly) => Ticks(owner),
            _ when member.DeclaringType == typeof(TimeSpan) => Duration(
                owner,
                member,
                type
            ),
            _ when Temporal.Contains(member.DeclaringType!)
                    && DateParts.TryGetValue(member.Name, out string? part) =>
                new SqlCast(Extracted(part, owner), "integer", type),
            _ when Temporal.Contains(member.DeclaringType!)
                    && Fractions.TryGetValue(member.Name, out string? fraction) =>
                Calculate(
                    "%",
                    new SqlCast(Extracted(fraction, owner), "integer", type),
                    Integer(1000)
                ),
            _ => throw Unsupported.Member(member),
        };
    }

    // The whole fields of an interval come out once its hours are carried into
    // days, as .NET always keeps them; the totals come from its seconds.
    private static SqlExpression Duration(
        SqlExpression owner,
        MemberInfo member,
        Type type
    )
    {
        SqlExpression seconds = new SqlExtract("EPOCH", owner);
        SqlExpression justified = new SqlFunction("JUSTIFY_HOURS", [owner], owner.Type);
        return member.Name switch
        {
            _ when Totals.TryGetValue(member.Name, out int unit) => new SqlCast(
                unit == 1 ? seconds : Calculate("/", seconds, Integer(unit)),
                "double precision",
                type
            ),
            nameof(TimeSpan.TotalMilliseconds) => new SqlCast(
                Calculate("*", seconds, Integer(1000)),
                "double precision",
                type
            ),
            nameof(TimeSpan.Ticks) => Ticks(owner),
            _ when Durations.TryGetValue(member.Name, out string? field) => new SqlCast(
                Extracted(field, justified),
                "integer",
                type
            ),
            nameof(TimeSpan.Milliseconds) => Calculate(
                "%",
                new SqlCast(Extracted("MILLISECONDS", justified), "integer", type),
                Integer(1000)
            ),
            _ => throw Unsupported.Member(member),
        };
    }

    private static SqlExpression Extracted(string field, SqlExpression owner)
    {
        SqlExtract extract = new(field, owner);
        // The fraction below a second or millisecond is truncated away in C#.
        return field is "SECOND" or "MILLISECONDS"
            ? new SqlFunction("TRUNC", [extract], typeof(decimal))
            : extract;
    }

    // Only the clocks reach here; everything else static is evaluated.
    private static SqlExpression StaticMember(MemberInfo member)
    {
        SqlFragment local = new("LOCALTIMESTAMP", typeof(DateTime));
        return member.DeclaringType != typeof(DateTime)
            ? Now(member)
            : member.Name switch
            {
                nameof(DateTime.Now) => local,
                nameof(DateTime.Today) => new SqlFunction(
                    "DATE_TRUNC",
                    [Text("day"), local],
                    typeof(DateTime)
                ),
                _ => Now(member),
            };
    }

    private static SqlFunction Now(MemberInfo member)
    {
        return new SqlFunction("NOW", [], ((PropertyInfo)member).PropertyType);
    }

    private static SqlConstant Text(string text)
    {
        return new SqlConstant(text, typeof(string));
    }

    private static SqlConstant Integer(int value)
    {
        return new SqlConstant(value, typeof(int));
    }

    private static bool IsNullable(MemberInfo member)
    {
        return Nullable.GetUnderlyingType(member.DeclaringType!) is not null;
    }
}
