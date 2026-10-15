namespace Linq.Expressions.Translation.PostgreSql.Tests.Infrastructure;

public static class Tables
{
    public static IQueryable<Customer> Customers => new CapturingQueryable<Customer>();

    public static IQueryable<Employee> Employees => new CapturingQueryable<Employee>();

    public static IQueryable<Order> Orders => new CapturingQueryable<Order>();

    public static IQueryable<OrderLine> OrderLines => new CapturingQueryable<OrderLine>();

    public static IQueryable<Product> Products => new CapturingQueryable<Product>();
}
