using System.Linq.Expressions;
using System.Reflection;
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
        return Valued(Terminal(call), call);
    }

    // In a value position, an ...OrDefault of a non-nullable value falls back to its
    // default where the subquery yields no row.
    private static SqlExpression Valued(SqlExpression result, MethodCallExpression call)
    {
        SqlExpression value = Single(result);
        return call.Method.Name.EndsWith("OrDefault", StringComparison.Ordinal)
            ? Substituted(value, call.Type)
            : value;
    }

    // A subquery in a value position must produce one column; the shape of an element
    // query is the shape of its source, which may be an entity or a projection.
    private static SqlExpression Single(SqlExpression result)
    {
        return result is SqlScalarSubquery { Query.Shape: not ScalarShape }
            ? throw new NotSupportedException(
                "A subquery used as a value must select a single scalar value."
            )
            : result;
    }

    private SqlExpression Terminal(MethodCallExpression call)
    {
        MethodCallExpression? padding = call.Arguments[0] as MethodCallExpression;
        return
            padding
                is { Method.Name: nameof(Queryable.DefaultIfEmpty), Arguments.Count: 1 }
            ? Padded(Operand(padding), call, padding.Method)
            : Terminal(Operand(call), call);
    }

    private SqlExpression Padded(
        SqlQuery source,
        MethodCallExpression call,
        MethodInfo padding
    )
    {
        string name = call.Method.Name;
        return call.Arguments.Count > 1 ? throw Unsupported.Method(padding)
            : name == nameof(Queryable.Any) ? new SqlConstant(true, typeof(bool))
            : name is nameof(Queryable.Count) or nameof(Queryable.LongCount)
                ? Sized(
                    name,
                    new SqlFunction(
                        "GREATEST",
                        [
                            Counted(source, nameof(Queryable.LongCount), typeof(long)),
                            new SqlConstant(1, typeof(int)),
                        ],
                        typeof(long)
                    )
                )
            : name == nameof(Queryable.Sum) ? Aggregated(source, call)
            : name
                is nameof(Queryable.Average)
                    or nameof(Queryable.Min)
                    or nameof(Queryable.Max)
                ? Substituted(Aggregated(source, call), call.Type)
            : throw Unsupported.Method(padding);
    }

    private static SqlExpression Substituted(SqlExpression value, Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) is null
            ? new SqlFunction(
                "COALESCE",
                [value, new SqlConstant(ScalarTypes.Default(type), type)],
                type
            )
            : value;
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
                filtered ? Where(source, Lambda(call, 1)) : source,
                call.Method.Name,
                call.Type
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
                Unordered(Open(source, Barrier.None)),
                Scalar(source.Shape).Nullable
            ),
            nameof(Queryable.Last) or nameof(Queryable.LastOrDefault) => Element(
                Reverse(filtered ? Where(source, Lambda(call, 1)) : source, call.Method),
                1,
                call
            ),
            nameof(Queryable.ElementAt) or nameof(Queryable.ElementAtOrDefault) =>
                Element(Positioned(source, Count(call.Arguments[1])), 1, call),
            nameof(Queryable.First) or nameof(Queryable.FirstOrDefault) => Element(
                filtered ? Where(source, Lambda(call, 1)) : source,
                1,
                call
            ),
            nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault) => Element(
                filtered ? Where(source, Lambda(call, 1)) : source,
                2,
                call
            ),
            _ => throw Unsupported.Method(call.Method),
        };
    }

    private SqlScalarSubquery Counted(SqlQuery source, string name, Type type)
    {
        SelectQuery query = Unordered(
            Open(source, Barrier.Paging | Barrier.Distinct | Barrier.Grouping)
        );
        query.Shape = new ScalarShape(Counting(name, null, null));
        return new SqlScalarSubquery(query, type);
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
            Aggregate(call.Method.Name, value, call.Type, false, null)
        );
        return new SqlScalarSubquery(query, call.Type);
    }

    private static SqlExpression Counting(
        string name,
        SqlExpression? distinct,
        SqlExpression? filter
    )
    {
        return Sized(
            name,
            new SqlAggregate(
                "COUNT",
                distinct,
                distinct is not null,
                filter,
                typeof(long)
            )
        );
    }

    private static SqlExpression Sized(string name, SqlExpression count)
    {
        return name == nameof(Queryable.Count)
            ? new SqlCast(count, "integer", typeof(int))
            : count;
    }

    private static SqlExpression Aggregate(
        string name,
        SqlExpression value,
        Type type,
        bool distinct,
        SqlExpression? filter
    )
    {
        Type result = ScalarTypes.Underlying(type);
        if (name == nameof(Queryable.Sum))
        {
            SqlExpression sum = new SqlFunction(
                "COALESCE",
                [
                    new SqlAggregate("SUM", value, distinct, filter, type),
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
            distinct,
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
