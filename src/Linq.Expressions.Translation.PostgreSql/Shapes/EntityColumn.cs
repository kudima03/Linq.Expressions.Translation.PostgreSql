using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Shapes;

internal sealed class EntityColumn(string member, string name, SqlExpression value)
{
    public string Member { get; } = member;

    public string Name { get; } = name;

    public SqlExpression Value { get; } = value;
}
