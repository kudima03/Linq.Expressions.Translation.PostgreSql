using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Mapping;
using Linq.Expressions.Translation.PostgreSql.Shapes;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly HashSet<string> Terminals =
    [
        nameof(Queryable.Count),
        nameof(Queryable.LongCount),
        nameof(Queryable.Sum),
        nameof(Queryable.Average),
        nameof(Queryable.Min),
        nameof(Queryable.Max),
        nameof(Queryable.Any),
        nameof(Queryable.All),
        nameof(Queryable.Contains),
        nameof(Queryable.First),
        nameof(Queryable.FirstOrDefault),
        nameof(Queryable.Single),
        nameof(Queryable.SingleOrDefault),
        nameof(Queryable.Last),
        nameof(Queryable.LastOrDefault),
        nameof(Queryable.ElementAt),
        nameof(Queryable.ElementAtOrDefault),
    ];

    private static readonly HashSet<Type> Integral = [typeof(int), typeof(long)];

    private static SelectQuery Statement(SqlExpression result)
    {
        return result is SqlScalarSubquery subquery
            ? subquery.Query
            : new SelectQuery(null, new ScalarShape(result));
    }

    private SqlExpression Subquery(MethodCallExpression call)
    {
        return Terminal(Operand(call), call);
    }

    private SqlQuery Operand(MethodCallExpression call)
    {
        return call.Method.GetParameters().Any(IsRejected)
            ? throw Unsupported.Method(call.Method)
            : Query(call.Arguments[0]);
    }

    private SqlExpression Terminal(SqlQuery source, MethodCallExpression call)
    {
        bool filtered = call.Arguments.Count > 1;
        return call.Method.Name switch
        {
            nameof(Queryable.Count) or nameof(Queryable.LongCount) => Counted(
                source,
                call
            ),
            nameof(Queryable.Sum)
            or nameof(Queryable.Average)
            or nameof(Queryable.Min)
            or nameof(Queryable.Max) => Aggregated(source, call),
            nameof(Queryable.Any) => new SqlExists(
                Existing(
                    filtered ? Where(source, Lambda(call, 1)) : Open(source, Barrier.None)
                )
            ),
            nameof(Queryable.All) => new SqlUnary(
                "NOT ",
                new SqlExists(Unmatched(source, call)),
                typeof(bool)
            ),
            nameof(Queryable.Contains) => new SqlInQuery(
                Sql(call.Arguments[1]),
                Unordered(Open(source, Barrier.None))
            ),
            nameof(Queryable.Last) or nameof(Queryable.LastOrDefault) => Element(
                Reverse(filtered ? Where(source, Lambda(call, 1)) : source, call.Method),
                1,
                call
            ),
            nameof(Queryable.ElementAt) or nameof(Queryable.ElementAtOrDefault) =>
                Element(Positioned(source, Count(call.Arguments[1])), 1, call),
            _ => Element(
                filtered ? Where(source, Lambda(call, 1)) : source,
                call.Method.Name.StartsWith(
                    nameof(Queryable.Single),
                    StringComparison.Ordinal
                )
                    ? 2
                    : 1,
                call
            ),
        };
    }

    private SqlScalarSubquery Counted(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Unordered(
            Open(
                call.Arguments.Count > 1 ? Where(source, Lambda(call, 1)) : source,
                Barrier.Paging | Barrier.Distinct | Barrier.Grouping
            )
        );
        query.Shape = new ScalarShape(Counting(call.Method.Name, null, null));
        return new SqlScalarSubquery(query, call.Type);
    }

    private SqlScalarSubquery Aggregated(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Unordered(
            Open(source, Barrier.Paging | Barrier.Distinct | Barrier.Grouping)
        );
        SqlExpression value =
            call.Arguments.Count > 1
                ? Scalar(Bind(Lambda(call, 1), query.Shape))
                : Scalar(query.Shape);
        query.Shape = new ScalarShape(
            Aggregate(call.Method.Name, value, call.Type, null)
        );
        return new SqlScalarSubquery(query, call.Type);
    }

    private static SqlExpression Counting(
        string name,
        SqlExpression? distinct,
        SqlExpression? filter
    )
    {
        SqlExpression count = new SqlAggregate(
            "COUNT",
            distinct,
            distinct is not null,
            filter,
            typeof(long)
        );
        return name == nameof(Queryable.Count)
            ? new SqlCast(count, "integer", typeof(int))
            : count;
    }

    private static SqlExpression Aggregate(
        string name,
        SqlExpression value,
        Type type,
        SqlExpression? filter
    )
    {
        Type result = ScalarTypes.Underlying(type);
        if (name == nameof(Queryable.Sum))
        {
            SqlExpression sum = new SqlFunction(
                "COALESCE",
                [
                    new SqlAggregate("SUM", value, false, filter, type),
                    new SqlConstant(0, typeof(int)),
                ],
                result
            );
            return Integral.Contains(result)
                ? new SqlCast(sum, ScalarTypes.Number(result), result)
                : sum;
        }
        SqlExpression argument =
            name == nameof(Queryable.Average)
            && Integral.Contains(ScalarTypes.Underlying(value.Type))
                ? new SqlCast(value, "double precision", typeof(double))
                : value;
        return new SqlAggregate(
            name == nameof(Queryable.Average) ? "AVG" : name.ToUpperInvariant(),
            argument,
            false,
            filter,
            type
        );
    }

    private SelectQuery Unmatched(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Restricted(source);
        query.Where = And(
            query.Where,
            new SqlUnary("NOT ", Scalar(Bind(Lambda(call, 1), query.Shape)), typeof(bool))
        );
        return Existing(query);
    }

    private static SelectQuery Existing(SelectQuery query)
    {
        query.Shape = new ScalarShape(new SqlConstant(1, typeof(int)));
        return Unordered(query);
    }

    private static SelectQuery Unordered(SelectQuery query)
    {
        if (!query.IsPaged)
        {
            query.Orderings = [];
        }
        return query;
    }

    private SelectQuery Positioned(SqlQuery source, int index)
    {
        SelectQuery query = Open(Skip(source, index), Barrier.None);
        query.Offset ??= 0;
        return query;
    }

    private SqlScalarSubquery Element(
        SqlQuery source,
        int count,
        MethodCallExpression call
    )
    {
        return new SqlScalarSubquery(Take(source, count), call.Type);
    }

    private bool IsCorrelation(params Expression[] operands)
    {
        Parameters parameters = new();
        foreach (Expression operand in operands)
        {
            _ = parameters.Visit(operand);
        }
        return parameters
                .Found.Select(parameter => _bindings[parameter].Scope)
                .Distinct()
                .Count() > 1;
    }

    private sealed class Parameters : ExpressionVisitor
    {
        public List<ParameterExpression> Found { get; } = [];

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found.Add(node);
            return node;
        }
    }
}
