namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public sealed class CapturingQueryProvider : IQueryProvider
{
    public Expression Executed
    {
        get =>
            field
            ?? throw new InvalidOperationException("No terminal operator was applied.");
        private set;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        throw new NotSupportedException("Only the generic CreateQuery is supported.");
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new CapturingQueryable<TElement>(this, expression);
    }

    public object? Execute(Expression expression)
    {
        Executed = expression;
        return null;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        Executed = expression;
        return default!;
    }
}
