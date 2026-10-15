using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly Dictionary<string, string> SetOperations = new()
    {
        [nameof(Queryable.Concat)] = "UNION ALL",
        [nameof(Queryable.Union)] = "UNION",
        [nameof(Queryable.Intersect)] = "INTERSECT",
        [nameof(Queryable.Except)] = "EXCEPT",
    };

    private SetOperationQuery Combined(SqlQuery source, MethodCallExpression call)
    {
        return new SetOperationQuery(
            SetOperations[call.Method.Name],
            Unpaged(source),
            Unpaged(Joined(call))
        );
    }

    private static SqlQuery Unpaged(SqlQuery query)
    {
        return query is SelectQuery select ? Unordered(select) : query;
    }
}
