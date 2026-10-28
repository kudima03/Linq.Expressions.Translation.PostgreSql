using System.Collections;
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
        [nameof(Queryable.UnionBy)] = "UNION ALL",
    };

    // The side of UNION ALL a row comes from; a member never starts with $.
    private const string Side = "$side";

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

    // UnionBy keeps the first element of each key, the one of the first sequence when
    // both have the key: DISTINCT ON over the sides combined, each marked with its
    // side, in the order of the key and then of the side.
    private SelectQuery UnionBy(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery left = Sided(source, 0);
        SelectQuery right = Sided(Joined(call), 1);
        SelectQuery query = Wrap(
            new SetOperationQuery(
                SetOperations[call.Method.Name],
                left,
                right,
                Merged(left.Shape, right.Shape)
            )
        );
        List<SqlExpression> keys = Partitioned(Bind(Lambda(call, 2), query.Shape));
        query.DistinctOn = keys;
        query.Orderings =
        [
            .. keys.Select(key => new Ordering(key, false)),
            new Ordering(
                new SqlColumn(query.From!.Alias, Side, typeof(int), false),
                false
            ),
        ];
        query.IsOrdered = false;
        return query;
    }

    private SelectQuery Sided(SqlQuery source, int side)
    {
        SelectQuery query = Unpaged(source) as SelectQuery ?? Wrap(source);
        query.Hidden.Add(new ProjectionColumn(Side, Integer(side)));
        return query;
    }

    // IntersectBy and ExceptBy keep one element per key, as DistinctBy does, of the
    // elements whose key is, or is not, among the keys of the second sequence; a null
    // key matches a null of that sequence, as it does for Contains.
    private SelectQuery Retained(SqlQuery source, MethodCallExpression call)
    {
        SelectQuery query = Restricted(source);
        Shape key = Bind(Lambda(call, 2), query.Shape);
        SqlExpression membership = Membership(call.Arguments[1], Scalar(key));
        Restrict(
            query,
            call.Method.Name == nameof(Queryable.ExceptBy)
                ? new SqlUnary("NOT ", membership, typeof(bool))
                : membership
        );
        return Distinguished(query, key);
    }

    // The second sequence is a query or an in-memory collection of keys.
    private SqlExpression Membership(Expression sequence, SqlExpression key)
    {
        return
            sequence
                is ConstantExpression { Value: IEnumerable values and not IQueryable }
            ? Membership(new SqlConstant(values, sequence.Type), key)
            : Membership(Query(sequence), sequence, key);
    }
}
