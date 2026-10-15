namespace Linq.Expressions.Translation.PostgreSql.Tests.Model;

public sealed record Order
{
    public int Id { get; init; }

    public int CustomerId { get; init; }

    public string Number { get; init; } = string.Empty;

    public string? Note { get; init; }

    public decimal Total { get; init; }

    public decimal? Discount { get; init; }

    public int Quantity { get; init; }

    public int? Priority { get; init; }

    public long Points { get; init; }

    public double Weight { get; init; }

    public bool IsPaid { get; init; }

    public bool? IsGift { get; init; }

    public OrderStatus Status { get; init; }

    public ShippingOptions Options { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTimeOffset? PaidAt { get; init; }

    public DateOnly? ShippedOn { get; init; }

    public TimeOnly DeliveryTime { get; init; }

    public Guid TrackingId { get; init; }

    public string[] Tags { get; init; } = [];
}
