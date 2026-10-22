using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Translation;

namespace Linq.Expressions.Translation.PostgreSql;

public sealed record PostgreSqlQuery
{
    private readonly Expression _expression;

    public PostgreSqlQuery(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        _expression = expression;
    }

    public string TextValue
    {
        [RequiresUnreferencedCode(TableMapping.Trimming)]
        get => new Translator().Translate(_expression);
    }

    // The generated description would translate the query, which needs reflection.
    public override string ToString()
    {
        return "PostgreSqlQuery { Expression = " + _expression + " }";
    }
}
