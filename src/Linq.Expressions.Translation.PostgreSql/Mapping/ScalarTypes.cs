namespace Linq.Expressions.Translation.PostgreSql.Mapping;

internal static class ScalarTypes
{
    private static readonly HashSet<Type> Types =
    [
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
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

    // PostgreSQL has no unsigned integers and no byte: each is the narrowest type
    // that holds every value of it.
    private static readonly Dictionary<Type, string> Numbers = new()
    {
        [typeof(byte)] = "smallint",
        [typeof(sbyte)] = "smallint",
        [typeof(short)] = "smallint",
        [typeof(ushort)] = "integer",
        [typeof(int)] = "integer",
        [typeof(uint)] = "bigint",
        [typeof(long)] = "bigint",
        [typeof(ulong)] = "numeric",
        [typeof(float)] = "real",
        [typeof(double)] = "double precision",
        [typeof(decimal)] = "numeric",
    };

    private static readonly HashSet<Type> Widened =
    [
        typeof(byte),
        typeof(sbyte),
        typeof(ushort),
        typeof(uint),
        typeof(ulong),
    ];

    private static readonly Dictionary<Type, object> Defaults = new()
    {
        [typeof(bool)] = false,
        [typeof(byte)] = (byte)0,
        [typeof(sbyte)] = (sbyte)0,
        [typeof(short)] = (short)0,
        [typeof(ushort)] = (ushort)0,
        [typeof(int)] = 0,
        [typeof(uint)] = 0U,
        [typeof(long)] = 0L,
        [typeof(ulong)] = 0UL,
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

    // A byte[] is a bytea, not an array of a scalar.
    public static bool IsScalar(Type type)
    {
        Type value = Underlying(type);
        return Types.Contains(value)
            || value == typeof(byte[])
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

    // Whether the PostgreSQL type of a number holds more values than the number: a
    // byte or an unsigned integer, which a cast to that type would not confine.
    public static bool IsWidened(Type type)
    {
        return Widened.Contains(Underlying(type));
    }
}
