using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private SelectQuery GroupBy(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Opened(source);
        Shape key = Bind(Lambda(call, 1), query.Shape);
        bool selected =
            call.Arguments.Count > 2 && ParameterCount(call.Arguments[2]) == 1;
        Shape element = selected ? Bind(Lambda(call, 2), query.Shape) : query.Shape;
        query.GroupBy = [.. key.Columns().Select(column => column.Value)];
        query.Orderings = [];
        query.Shape = new GroupingShape(key, element, null, false);
        if (call.Arguments.Count > (selected ? 3 : 2))
        {
            query.Shape = Bind(
                Lambda(call, call.Arguments.Count - 1, 2),
                key,
                query.Shape
            );
        }
        return query;
    }

    private static int ParameterCount(Expression argument)
    {
        return ((LambdaExpression)((UnaryExpression)argument).Operand).Parameters.Count;
    }

    private Shape Grouped(GroupingShape group, MethodCallExpression call)
    {
        string name = call.Method.Name;
        bool selected = call.Arguments.Count > 1;
        return name switch
        {
            nameof(Enumerable.Where) => new GroupingShape(
                group.Key,
                group.Element,
                Conjoined(Scalar(Bind(Lambda(call, 1), group.Element)), group.Filter),
                group.Distinct
            ),
            nameof(Enumerable.Select) => new GroupingShape(
                group.Key,
                Bind(Lambda(call, 1), group.Element),
                group.Filter,
                group.Distinct
            ),
            nameof(Enumerable.Distinct) => new GroupingShape(
                group.Key,
                group.Element,
                group.Filter,
                true
            ),
            nameof(Enumerable.Count) or nameof(Enumerable.LongCount) => new ScalarShape(
                Counting(
                    name,
                    group.Distinct ? Scalar(group.Element) : null,
                    selected
                        ? Conjoined(
                            Scalar(Bind(Lambda(call, 1), group.Element)),
                            group.Filter
                        )
                        : group.Filter
                )
            ),
            nameof(Enumerable.Any) or nameof(Enumerable.All) => new ScalarShape(
                Quantified(group, call)
            ),
            nameof(Enumerable.Sum)
            or nameof(Enumerable.Average)
            or nameof(Enumerable.Min)
            or nameof(Enumerable.Max) => new ScalarShape(
                Aggregate(
                    name,
                    Scalar(
                        selected ? Bind(Lambda(call, 1), group.Element) : group.Element
                    ),
                    call.Type,
                    group.Filter
                )
            ),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SqlExpression Quantified(GroupingShape group, MethodCallExpression call)
    {
        bool any = call.Method.Name == nameof(Enumerable.Any);
        SqlExpression aggregate = new SqlAggregate(
            any ? "BOOL_OR" : "BOOL_AND",
            call.Arguments.Count > 1
                ? Scalar(Bind(Lambda(call, 1), group.Element))
                : True,
            false,
            group.Filter,
            typeof(bool)
        );
        return group.Filter is null
            ? aggregate
            : new SqlFunction(
                "COALESCE",
                [aggregate, new SqlConstant(!any, typeof(bool))],
                typeof(bool)
            );
    }
}
