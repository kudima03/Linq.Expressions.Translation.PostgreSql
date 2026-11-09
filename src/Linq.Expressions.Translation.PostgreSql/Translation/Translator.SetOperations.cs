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

    private SqlQuery Combined(SqlQuery source, MethodCallExpression call)
    {
        SqlQuery right = Joined(call);
        return
            call.Method.Name == nameof(Queryable.Concat)
            && (IsOrdered(source) || IsOrdered(right))
            ? Concatenated(source, right)
            : new SetOperationQuery(
                SetOperations[call.Method.Name],
                Unpaged(source),
                Unpaged(right),
                source.Shape
            );
    }

    private static SqlQuery Unpaged(SqlQuery query)
    {
        return query is SelectQuery select ? Unordered(select) : query;
    }

    // Whether the rows of a query come in an order, which a set operation drops.
    private static bool IsOrdered(SqlQuery query)
    {
        return query is SelectQuery { IsOrdered: true, Orderings.Count: > 0 };
    }

    // Concat yields the elements of the first sequence in their order, then those of
    // the second in theirs: each side is marked with its side and exposes its
    // ordering keys as hidden columns, NULL on the other side, and the sides combined
    // are read from a derived table in the order of the side and then of the keys.
    private SelectQuery Concatenated(SqlQuery source, SqlQuery joined)
    {
        List<Ordering> first = Order(source);
        List<Ordering> second = Order(joined);
        SelectQuery left = Sided(source, 0);
        SelectQuery right = Sided(joined, 1);
        SelectQuery query = Wrap(
            new SetOperationQuery(
                SetOperations[nameof(Queryable.Concat)],
                left,
                right,
                left.Shape
            )
        );
        string alias = query.From!.Alias;
        query.Orderings =
        [
            new Ordering(new SqlColumn(alias, Side, typeof(int), false), false),
            .. Exposed(left, right, first, alias),
            .. Exposed(right, left, second, alias),
        ];
        return query;
    }

    private static List<Ordering> Order(SqlQuery query)
    {
        return IsOrdered(query) ? [.. ((SelectQuery)query).Orderings] : [];
    }

    // The ordering keys of one side, as hidden columns of both sides of UNION ALL.
    private static List<Ordering> Exposed(
        SelectQuery own,
        SelectQuery other,
        List<Ordering> order,
        string alias
    )
    {
        List<Ordering> exposed = [];
        foreach (Ordering ordering in order)
        {
            string name = "$" + own.Hidden.Count;
            own.Hidden.Add(new ProjectionColumn(name, ordering.Key));
            other.Hidden.Add(
                new ProjectionColumn(name, new SqlConstant(null, ordering.Key.Type))
            );
            exposed.Add(
                new Ordering(
                    new SqlColumn(alias, name, ordering.Key.Type, ordering.Key.Nullable),
                    ordering.Descending,
                    ordering.NullsLast
                )
            );
        }
        return exposed;
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
                left.Shape
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
