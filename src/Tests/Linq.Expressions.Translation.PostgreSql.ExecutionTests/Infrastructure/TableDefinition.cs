using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// A table as the translator names it: [Table] (with its schema) or the type name,
// [Column] or the property name, [NotMapped] properties left out, and NOT NULL unless
// the property is nullable in the CLR.
public sealed class TableDefinition
{
    private static readonly Dictionary<Type, string> Types = new()
    {
        [typeof(bool)] = "boolean",
        [typeof(short)] = "smallint",
        [typeof(int)] = "integer",
        [typeof(long)] = "bigint",
        [typeof(float)] = "real",
        [typeof(double)] = "double precision",
        [typeof(decimal)] = "numeric",
        [typeof(string)] = "text",
        [typeof(Guid)] = "uuid",
        [typeof(DateTime)] = "timestamp without time zone",
        [typeof(DateTimeOffset)] = "timestamp with time zone",
        [typeof(DateOnly)] = "date",
        [typeof(TimeOnly)] = "time without time zone",
        [typeof(TimeSpan)] = "interval",
    };

    public TableDefinition(Type type, IEnumerable rows)
    {
        TableAttribute? table = type.GetCustomAttribute<TableAttribute>();
        Type = type;
        Rows = rows;
        Schema = table?.Schema;
        Name =
            (Schema is null ? string.Empty : Quoted(Schema) + ".")
            + Quoted(table?.Name ?? type.Name);
        Columns =
        [
            .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property =>
                    property.GetCustomAttribute<NotMappedAttribute>() is null
                    && property.GetIndexParameters().Length == 0
                    && property.GetMethod is { IsPublic: true }
                ),
        ];
    }

    public Type Type { get; }

    public IEnumerable Rows { get; }

    public string? Schema { get; }

    // The quoted, schema-qualified name.
    public string Name { get; }

    public IReadOnlyList<PropertyInfo> Columns { get; }

    public string Create =>
        $"CREATE TABLE {Name} ("
        + string.Join(
            ", ",
            Columns.Select(column => $"{ColumnName(column)} {Definition(column)}")
        )
        + ")";

    public string Insert =>
        $"INSERT INTO {Name} ("
        + string.Join(", ", Columns.Select(ColumnName))
        + ") VALUES ("
        + string.Join(", ", Columns.Select((_, index) => $"${index + 1}"))
        + ")";

    // The values of a row as Npgsql writes them: enums as their numbers, and
    // timestamptz values as UTC instants, the only offset Npgsql accepts.
    public IEnumerable<object> Values(object row)
    {
        foreach (PropertyInfo column in Columns)
        {
            yield return column.GetValue(row) switch
            {
                null => DBNull.Value,
                Enum member => Convert.ChangeType(
                    member,
                    Enum.GetUnderlyingType(member.GetType()),
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                DateTimeOffset moment => moment.ToUniversalTime(),
                object value => value,
            };
        }
    }

    private static string ColumnName(PropertyInfo column)
    {
        return Quoted(column.GetCustomAttribute<ColumnAttribute>()?.Name ?? column.Name);
    }

    private static string Definition(PropertyInfo column)
    {
        bool nullable =
            new NullabilityInfoContext().Create(column).ReadState
            != NullabilityState.NotNull;
        return SqlType(column.PropertyType) + (nullable ? string.Empty : " NOT NULL");
    }

    private static string SqlType(Type type)
    {
        Type value = Nullable.GetUnderlyingType(type) ?? type;
        return value.IsArray ? SqlType(value.GetElementType()!) + "[]"
            : value.IsEnum ? Types[Enum.GetUnderlyingType(value)]
            : Types[value];
    }

    private static string Quoted(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
