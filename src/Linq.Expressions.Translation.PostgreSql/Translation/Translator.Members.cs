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
            { Name: nameof(DateTime.Date) }
                when member.DeclaringType == typeof(DateTime) => new SqlFunction(
                "DATE_TRUNC",
                [Text("day"), owner],
                type
            ),
            _ when Temporal.Contains(member.DeclaringType!)
                    && DateParts.TryGetValue(member.Name, out string? part) =>
                new SqlCast(new SqlExtract(part, owner), "integer", type),
            _ => throw Unsupported.Member(member),
        };
    }

    private static SqlExpression StaticMember(MemberInfo member)
    {
        return member.Name == nameof(DateTime.UtcNow)
                ? new SqlFunction("NOW", [], ((PropertyInfo)member).PropertyType)
            : member.Name == nameof(DateTime.Now)
            && member.DeclaringType == typeof(DateTime)
                ? new SqlFragment("LOCALTIMESTAMP", typeof(DateTime))
            : throw Unsupported.Member(member);
    }

    private static SqlConstant Text(string text)
    {
        return new SqlConstant(text, typeof(string));
    }

    private static bool IsNullable(MemberInfo member)
    {
        return Nullable.GetUnderlyingType(member.DeclaringType!) is not null;
    }
}
