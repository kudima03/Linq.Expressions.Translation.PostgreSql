using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Mapping;

internal sealed class TableMapping
{
    public const string Trimming =
        "Columns are read from the public properties of the queried element types, "
        + "which trimming removes unless they are preserved.";

    [RequiresUnreferencedCode(Trimming)]
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
                    && IsColumn(property)
                )
                .OrderBy(property => Depth(property.DeclaringType!))
                .Select(property => new ColumnMapping(property)),
        ];
    }

    public string Name { get; }

    public string? Schema { get; }

    public IReadOnlyList<ColumnMapping> Columns { get; }

    // A column is read through a public getter; an indexer and a write-only property
    // have no value to select.
    private static bool IsColumn(PropertyInfo property)
    {
        return property.GetIndexParameters().Length == 0
            && property.GetMethod is { IsPublic: true };
    }

    // Properties come in declaration order, derived types first; the stable sort puts
    // base types first. Metadata tokens would be exact but do not exist under Native AOT.
    private static int Depth(Type type)
    {
        return type.BaseType is Type parent ? Depth(parent) + 1 : 0;
    }
}
