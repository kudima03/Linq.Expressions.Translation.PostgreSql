using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private SelectQuery GroupBy(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Open(
            source,
            Barrier.Paging | Barrier.Distinct | Barrier.Grouping,
            false
        );
        Shape key = Bind(Lambda(call, 1), query.Shape);
        bool selected =
            call.Arguments.Count > 2 && ParameterCount(call.Arguments[2]) == 1;
        Shape element = selected ? Bind(Lambda(call, 2), query.Shape) : query.Shape;
        query.GroupBy = Partitioned(key);
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

    // A constant key puts every row into one group. A bare literal in GROUP BY is a
    // positional reference or an error, and without a GROUP BY an empty source would
    // still produce a group.
    private static List<SqlExpression> Partitioned(Shape key)
    {
        List<SqlExpression> keys =
        [
            .. key.Columns()
                .Select(column => column.Value)
                .Where(value => value is not SqlConstant),
        ];
        return keys.Count > 0 ? keys : [new SqlCast(Integer(1), "integer", typeof(int))];
    }

    private static int ParameterCount(Expression argument)
    {
        return ((LambdaExpression)((UnaryExpression)argument).Operand).Parameters.Count;
    }

    private Shape Grouped(GroupingShape group, MethodCallExpression call)
    {
        string name = call.Method.Name;
        bool selected = call.Arguments.Count > 1;
        Shape element =
            group.Element
            ?? throw new NotSupportedException(
                "The elements of a group are not available once the groups have been paged, made distinct or combined; aggregate them before that."
            );
        return name switch
        {
            nameof(Enumerable.Where) => new GroupingShape(
                group.Key,
                element,
                Conjoined(Scalar(Bind(Lambda(call, 1), element)), group.Filter),
                group.Distinct
            ),
            // A projection of the distinct elements is not the distinct projection,
            // which is what the aggregate of a DISTINCT group would compute.
            nameof(Enumerable.Select) when group.Distinct =>
                throw new NotSupportedException(
                    "'Select' after 'Distinct' over group elements is not supported; select the value before Distinct."
                ),
            nameof(Enumerable.Select) => new GroupingShape(
                group.Key,
                Bind(Lambda(call, 1), element),
                group.Filter,
                group.Distinct
            ),
            nameof(Enumerable.Distinct) => new GroupingShape(
                group.Key,
                element,
                group.Filter,
                true
            ),
            nameof(Enumerable.Count) or nameof(Enumerable.LongCount) => new ScalarShape(
                Counting(
                    name,
                    group.Distinct ? Scalar(element) : null,
                    selected
                        ? Conjoined(Scalar(Bind(Lambda(call, 1), element)), group.Filter)
                        : group.Filter
                )
            ),
            nameof(Enumerable.Any) or nameof(Enumerable.All) => new ScalarShape(
                Quantified(group, element, call)
            ),
            // Distinct entities summed by a selector are not the distinct values of the
            // selector, which is what SUM(DISTINCT ...) adds up.
            nameof(Enumerable.Sum)
            or nameof(Enumerable.Average) when group.Distinct && selected =>
                throw new NotSupportedException(
                    $"'{name}' with a selector over distinct group elements is not supported; select the value before Distinct."
                ),
            nameof(Enumerable.Sum)
            or nameof(Enumerable.Average)
            or nameof(Enumerable.Min)
            or nameof(Enumerable.Max) => new ScalarShape(
                Aggregate(
                    name,
                    Scalar(selected ? Bind(Lambda(call, 1), element) : element),
                    call.Type,
                    group.Distinct
                        && name is not (nameof(Enumerable.Min) or nameof(Enumerable.Max)),
                    group.Filter
                )
            ),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SqlExpression Quantified(
        GroupingShape group,
        Shape element,
        MethodCallExpression call
    )
    {
        bool any = call.Method.Name == nameof(Enumerable.Any);
        SqlExpression aggregate = new SqlAggregate(
            any ? "BOOL_OR" : "BOOL_AND",
            call.Arguments.Count > 1 ? Scalar(Bind(Lambda(call, 1), element)) : True,
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
