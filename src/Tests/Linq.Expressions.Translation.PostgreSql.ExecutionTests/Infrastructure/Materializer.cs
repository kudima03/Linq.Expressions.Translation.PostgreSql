using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// Builds an element of the query from a row the way the translator flattens it: a
// scalar element is the only column, and the columns of an entity or a constructed
// object are named by the path of their member ("Summary.Id"; an entity column by its
// [Column] name). An entity whose columns are all NULL is the null of an outer join.
public static class Materializer
{
    public static object? Element(Type type, ResultSet result, object?[] row)
    {
        if (!IsScalar(type))
        {
            return Composite(type, result, row, null);
        }
        Assert.True(result.Columns.Count == 1, $"One column expected: {result.Sql}");
        return Scalar(type, row[0], result.Sql);
    }

    private static bool IsScalar(Type type)
    {
        Type value = Nullable.GetUnderlyingType(type) ?? type;
        return value.IsPrimitive
            || value.IsEnum
            || value.IsArray
            || value == typeof(string)
            || value == typeof(decimal)
            || value == typeof(Guid)
            || value == typeof(DateTime)
            || value == typeof(DateTimeOffset)
            || value == typeof(DateOnly)
            || value == typeof(TimeOnly)
            || value == typeof(TimeSpan);
    }

    private static object? Composite(
        Type type,
        ResultSet result,
        object?[] row,
        string? path
    )
    {
        List<int> columns = [.. Under(result, path)];
        Assert.True(columns.Count > 0, $"No columns for '{path}': {result.Sql}");
        if (Seed.IsTable(type) && columns.All(index => row[index] is null))
        {
            return null;
        }
        ConstructorInfo constructor = type.GetConstructors()
            .OrderByDescending(candidate => candidate.GetParameters().Length)
            .First(candidate =>
                candidate.GetParameters().All(p => IsMember(type, p.Name!))
            );
        ParameterInfo[] parameters = constructor.GetParameters();
        object instance = constructor.Invoke([
            .. parameters.Select(parameter =>
                Member(parameter.ParameterType, result, row, Path(path, parameter.Name!))
            ),
        ]);
        foreach (PropertyInfo property in type.GetProperties())
        {
            string? name = Initialized(property, result, path);
            if (
                property.SetMethod is { IsPublic: true }
                && name is not null
                && !parameters.Any(parameter => Same(parameter.Name!, property.Name))
            )
            {
                property.SetValue(
                    instance,
                    Member(property.PropertyType, result, row, Path(path, name))
                );
            }
        }
        return instance;
    }

    private static object? Member(Type type, ResultSet result, object?[] row, string path)
    {
        return IsScalar(type)
            ? Scalar(type, row[Find(result, path)], result.Sql)
            : Composite(type, result, row, path);
    }

    // The name a property's columns have: its [Column] name for an entity, else its own.
    private static string? Initialized(
        PropertyInfo property,
        ResultSet result,
        string? path
    )
    {
        string? column = property.GetCustomAttribute<ColumnAttribute>()?.Name;
        return column is not null && Under(result, Path(path, column)).Any() ? column
            : Under(result, Path(path, property.Name)).Any() ? property.Name
            : null;
    }

    private static bool IsMember(Type type, string name)
    {
        return type.GetProperties().Any(property => Same(property.Name, name))
            || type.GetFields().Any(field => Same(field.Name, name));
    }

    private static IEnumerable<int> Under(ResultSet result, string? path)
    {
        return Enumerable
            .Range(0, result.Columns.Count)
            .Where(index =>
                path is null
                || Same(result.Columns[index], path)
                || result
                    .Columns[index]
                    .StartsWith(path + ".", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static int Find(ResultSet result, string path)
    {
        int index = Enumerable
            .Range(0, result.Columns.Count)
            .FirstOrDefault(index => Same(result.Columns[index], path), -1);
        Assert.True(index >= 0, $"No column '{path}': {result.Sql}");
        return index;
    }

    private static string Path(string? path, string name)
    {
        return path is null ? name : $"{path}.{name}";
    }

    private static bool Same(string first, string second)
    {
        return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }

    private static object? Scalar(Type type, object? value, string sql)
    {
        if (value is null)
        {
            Assert.False(
                type.IsValueType && Nullable.GetUnderlyingType(type) is null,
                $"NULL read as {type.Name}: {sql}"
            );
            return null;
        }
        return Converted(value, Nullable.GetUnderlyingType(type) ?? type);
    }

    // PostgreSQL values read back as the CLR types of the query: integer aggregates
    // come wider, dates and times as DateTime and TimeSpan, enums as numbers.
    private static object Converted(object value, Type type)
    {
        if (type.IsInstanceOfType(value))
        {
            return value;
        }
        if (type.IsEnum)
        {
            return Enum.ToObject(
                type,
                Convert.ToInt64(value, CultureInfo.InvariantCulture)
            );
        }
        if (type.IsArray)
        {
            Array items = (Array)value;
            Type element = type.GetElementType()!;
            Array converted = Array.CreateInstance(element, items.Length);
            for (int index = 0; index < items.Length; index++)
            {
                converted.SetValue(
                    items.GetValue(index) is object item
                        ? Converted(item, element)
                        : null,
                    index
                );
            }
            return converted;
        }
        return value switch
        {
            DateTime day when type == typeof(DateOnly) => DateOnly.FromDateTime(day),
            TimeSpan time when type == typeof(TimeOnly) => TimeOnly.FromTimeSpan(time),
            DateTime moment when type == typeof(DateTimeOffset) => new DateTimeOffset(
                moment
            ),
            _ => Convert.ChangeType(value, type, CultureInfo.InvariantCulture),
        };
    }
}
