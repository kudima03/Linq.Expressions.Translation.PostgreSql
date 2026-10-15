using System.Linq.Expressions;

namespace Linq.Expressions.Translation.PostgreSql;

public sealed record PostgreSqlQuery
{
    private readonly Expression _expression;

    public PostgreSqlQuery(Expression expression)
    {
        _expression = expression;
    }

    public string TextValue =>
        throw new NotImplementedException(
            $"Translation of {_expression.NodeType} expressions is not implemented."
        );
}
