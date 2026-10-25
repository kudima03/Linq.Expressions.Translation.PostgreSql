using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;
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
        SqlQuery left = Unpaged(source);
        SqlQuery right = Unpaged(Joined(call));
        return new SetOperationQuery(
            SetOperations[call.Method.Name],
            left,
            right,
            Merged(left.Shape, right.Shape)
        );
    }

    // A column of the result is nullable when it is nullable on either side.
    private static Shape Merged(Shape left, Shape right)
    {
        List<ProjectionColumn> others = right.Columns();
        int index = 0;
        return left.Map(
            (value, name) => others[index++].Value.Nullable ? value.AsNullable() : value,
            null
        );
    }

    private static SqlQuery Unpaged(SqlQuery query)
    {
        return query is SelectQuery select ? Unordered(select) : query;
    }
}
