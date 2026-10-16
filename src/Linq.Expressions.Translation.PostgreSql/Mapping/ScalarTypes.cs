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

    private static readonly Dictionary<Type, object> Defaults = new()
    {
        [typeof(bool)] = false,
        [typeof(short)] = (short)0,
        [typeof(int)] = 0,
        [typeof(long)] = 0L,
        [typeof(float)] = 0F,
        [typeof(double)] = 0D,
        [typeof(decimal)] = 0M,
        [typeof(char)] = '\0',
        [typeof(Guid)] = Guid.Empty,
        [typeof(DateTime)] = default(DateTime),
        [typeof(DateTimeOffset)] = default(DateTimeOffset),
        [typeof(DateOnly)] = default(DateOnly),
        [typeof(TimeOnly)] = default(TimeOnly),
        [typeof(TimeSpan)] = TimeSpan.Zero,
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

    // An enum defaults to the zero of its underlying type, which renders the same.
    public static object Default(Type type)
    {
        return Defaults[Underlying(type)];
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
