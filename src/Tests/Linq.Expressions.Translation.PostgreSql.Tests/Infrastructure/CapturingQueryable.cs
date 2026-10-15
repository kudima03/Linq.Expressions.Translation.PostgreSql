using System.Collections;

namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public sealed class CapturingQueryable<T> : IOrderedQueryable<T>
{
    public CapturingQueryable()
    {
        Provider = new CapturingQueryProvider();
        Expression = Expression.Constant(this);
    }

    public CapturingQueryable(CapturingQueryProvider provider, Expression expression)
    {
        Provider = provider;
        Expression = expression;
    }

    public Type ElementType => typeof(T);

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotSupportedException("The test root only captures expression trees.");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
