using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class EntityColumn(
    string member,
    string name,
    SqlExpression value,
    bool required
)
{
    public string Member { get; } = member;

    public string Name { get; } = name;

    public SqlExpression Value { get; } = value;

    // The table does not store NULL in the column.
    public bool Required { get; } = required;
}
