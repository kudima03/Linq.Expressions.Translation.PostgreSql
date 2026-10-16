namespace Linq.Expressions.Translation.PostgreSql.Mapping;

internal static class ScalarTypes
{
    private static readonly HashSet<Type> Types =
    [
        typeof(bool),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(char),
        typeof(string),
        typeof(Guid),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(TimeSpan),
    ];

    private static readonly Dictionary<Type, string> Numbers = new()
    {
        [typeof(short)] = "smallint",
        [typeof(int)] = "integer",
        [typeof(long)] = "bigint",
        [typeof(float)] = "real",
        [typeof(double)] = "double precision",
        [typeof(decimal)] = "numeric",
    };

    public static Type Underlying(Type type)
    {
        Type value = Nullable.GetUnderlyingType(type) ?? type;
        return value.IsEnum ? Enum.GetUnderlyingType(value) : value;
    }

    public static bool IsScalar(Type type)
    {
        Type value = Underlying(type);
        return Types.Contains(value)
            || (value.IsArray && IsScalar(value.GetElementType()!));
    }

    public static bool IsNumber(Type type)
    {
        return Numbers.ContainsKey(Underlying(type));
    }

    public static string Number(Type type)
    {
        return Numbers[type];
    }
}
