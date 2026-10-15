namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

public sealed record OrderLine
{
    public int OrderId { get; init; }

    public int ProductId { get; init; }

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}
