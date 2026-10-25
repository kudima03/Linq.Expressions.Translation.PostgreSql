using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Linq.Expressions.Translation.PostgreSql.Mapping;

internal sealed class ColumnMapping
{
    public ColumnMapping(PropertyInfo property)
    {
        if (!ScalarTypes.IsScalar(property.PropertyType))
        {
            throw new NotSupportedException(
                $"The property '{property.DeclaringType!.Name}.{property.Name}' cannot be mapped to a column."
            );
        }
        Member = property.Name;
        Name = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;
        Type = property.PropertyType;
        // Without nullable annotations a reference may be null as well.
        Nullable =
            new NullabilityInfoContext().Create(property).ReadState
            != NullabilityState.NotNull;
    }

    public string Member { get; }

    public string Name { get; }

    public Type Type { get; }

    public bool Nullable { get; }
}
