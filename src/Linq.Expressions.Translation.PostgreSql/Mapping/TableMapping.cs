using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Mapping;

internal sealed class TableMapping
{
    public TableMapping(Type type)
    {
        TableAttribute? table = type.GetCustomAttribute<TableAttribute>();
        Name = table?.Name ?? type.Name;
        Schema = table?.Schema;
        Columns =
        [
            .. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property =>
                    property.GetCustomAttribute<NotMappedAttribute>() is null
                )
                .OrderBy(property => property.MetadataToken)
                .Select(property => new ColumnMapping(property)),
        ];
    }

    public string Name { get; }

    public string? Schema { get; }

    public IReadOnlyList<ColumnMapping> Columns { get; }
}
