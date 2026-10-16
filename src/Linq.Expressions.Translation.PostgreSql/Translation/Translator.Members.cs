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
            _ when Temporal.Contains(member.DeclaringType!)
                    && DateParts.TryGetValue(member.Name, out string? part) =>
                new SqlCast(new SqlExtract(part, owner), "integer", type),
            _ when Temporal.Contains(member.DeclaringType!)
                    && Fractions.TryGetValue(member.Name, out string? fraction) =>
                Calculate(
                    "%",
                    new SqlCast(new SqlExtract(fraction, owner), "integer", type),
                    Integer(1000)
                ),
            _ => throw Unsupported.Member(member),
        };
    }

    private static SqlExpression StaticMember(MemberInfo member)
    {
        return member.Name == nameof(DateTime.Now)
            && member.DeclaringType == typeof(DateTime)
                ? new SqlFragment("LOCALTIMESTAMP", typeof(DateTime))
            : member.Name != nameof(DateTime.Today)
                ? new SqlFunction("NOW", [], ((PropertyInfo)member).PropertyType)
            : throw Unsupported.Member(member);
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
