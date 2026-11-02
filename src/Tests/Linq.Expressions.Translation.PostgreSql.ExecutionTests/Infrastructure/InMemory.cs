namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// Rewrites a query over the capturing tables into the same query over the Seed rows,
// which LINQ to Objects runs: a table read as a constant, a static property or a
// captured variable becomes its rows, and a captured composed query its own tree,
// whose tables are rewritten in turn.
public sealed class InMemory : ExpressionVisitor
{
    public static Expression Rewrite(Expression expression)
    {
        return new InMemory().Visit(expression);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        return node.Value is IQueryable query
            ? Source(query) ?? base.VisitConstant(node)
            : base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (typeof(IQueryable).IsAssignableFrom(node.Type) && IsClosed(node))
        {
            object? value = Expression
                .Lambda<Func<object?>>(Expression.Convert(node, typeof(object)))
                .Compile()();
            if (value is IQueryable query && Source(query) is Expression source)
            {
                return source;
            }
        }
        return base.VisitMember(node);
    }

    // A capturing table is its rows; a composed query, over tables or an in-memory
    // root, is its tree; any other root stays what it is.
    private Expression? Source(IQueryable query)
    {
        return query.Expression is ConstantExpression root && root.Value == query
            ? IsCapturing(query)
                ? Expression.Constant(Seed.Source(query.ElementType))
                : null
            : Visit(query.Expression);
    }

    private static bool IsCapturing(IQueryable query)
    {
        Type type = query.GetType();
        return type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(CapturingQueryable<>);
    }

    // A static member or a member of a captured value, not of a lambda parameter.
    private static bool IsClosed(MemberExpression node)
    {
        Expression? owner = node.Expression;
        while (owner is MemberExpression member)
        {
            owner = member.Expression;
        }
        return owner is null or ConstantExpression;
    }
}
