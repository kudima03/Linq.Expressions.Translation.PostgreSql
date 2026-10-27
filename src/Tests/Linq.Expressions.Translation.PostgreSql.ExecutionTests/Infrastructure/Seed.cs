namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Infrastructure;

// The rows every execution test reads, in memory and in PostgreSQL: NULL in every
// nullable column, empty strings, LIKE wildcards, escapes and quotes, non-BMP and
// white-space characters, mixed case, duplicate keys, rows without a join partner,
// midpoint decimals and dates around month, year and leap-day boundaries.
public static class Seed
{
    public static IReadOnlyList<Customer> Customers { get; } =
    [
        Customer(1, "Ann", "ann@example.com", "EU", true),
        Customer(2, "bob", null, "US", false),
        Customer(3, "O'Brien", string.Empty, "EU", true),
        Customer(4, "50% off", "sale@example.com", "US", true),
        Customer(5, "under_score", null, "APAC", false),
        Customer(6, "back\\slash", "back\\slash@example.com", "EU", true),
        Customer(7, "  padded\t ", "padded@example.com", "APAC", true),
        Customer(8, "\u00a0nbsp\u2003", null, "EU", false),
        Customer(9, "\U0001F600 smile", "smile@example.com", "US", true),
        Customer(10, string.Empty, "empty@example.com", "EU", true),
        Customer(11, "MiXeD Case", "MIXED@example.com", "US", false),
        Customer(12, "Ann", "ann2@example.com", "EU", true),
        Customer(13, "Zo\u00eb\u0085", null, string.Empty, true),
    ];

    public static IReadOnlyList<Employee> Employees { get; } =
    [
        new() { Id = 1, Name = "Root" },
        new()
        {
            Id = 2,
            Name = "Lead A",
            ManagerId = 1,
        },
        new()
        {
            Id = 3,
            Name = "Lead B",
            ManagerId = 1,
        },
        new()
        {
            Id = 4,
            Name = "Dev",
            ManagerId = 2,
        },
        new()
        {
            Id = 5,
            Name = "Dev",
            ManagerId = 2,
        },
        new()
        {
            Id = 6,
            Name = "Orphan",
            ManagerId = 99,
        },
        new() { Id = 7, Name = "Solo" },
    ];

    public static IReadOnlyList<Order> Orders { get; } =
    [
        new()
        {
            Id = 1,
            CustomerId = 1,
            Number = "A-001",
            Note = "leave at door",
            Total = 100.00m,
            Discount = 10.00m,
            Quantity = 2,
            Priority = 1,
            Points = 1000,
            Weight = 2.5,
            IsPaid = true,
            IsGift = true,
            Status = OrderStatus.Paid,
            Options = ShippingOptions.Express,
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0),
            PaidAt = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.FromHours(3)),
            ShippedOn = new DateOnly(2024, 1, 16),
            DeliveryTime = new TimeOnly(9, 0),
            TrackingId = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"),
            Tags = ["gift", "fragile"],
        },
        new()
        {
            Id = 2,
            CustomerId = 1,
            Number = "A-002",
            Total = 2.5m,
            Quantity = 1,
            Weight = 0.5,
            Status = OrderStatus.Created,
            Options = ShippingOptions.None,
            CreatedAt = new DateTime(2024, 2, 29, 23, 59, 59).AddTicks(9_999_990),
            DeliveryTime = new TimeOnly(18, 30, 15),
            TrackingId = new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7"),
            Tags = [],
        },
        new()
        {
            Id = 3,
            CustomerId = 2,
            Number = "B-001",
            Note = string.Empty,
            Total = 3.5m,
            Discount = 0.5m,
            Quantity = 3,
            Priority = 2,
            Points = -50,
            Weight = -1.5,
            IsPaid = true,
            IsGift = false,
            Status = OrderStatus.Shipped,
            Options = ShippingOptions.Express | ShippingOptions.Insured,
            CreatedAt = new DateTime(2023, 12, 31, 23, 0, 0),
            PaidAt = new DateTimeOffset(2024, 1, 1, 1, 0, 0, TimeSpan.Zero),
            ShippedOn = new DateOnly(2024, 1, 2),
            DeliveryTime = new TimeOnly(0, 0),
            TrackingId = new Guid("a8098c1a-f86e-11da-bd1a-00112444be1e"),
            Tags = ["bulk"],
        },
        new()
        {
            Id = 4,
            CustomerId = 3,
            Number = "C_001",
            Note = "50% done",
            Total = -2.5m,
            Discount = 2.5m,
            Quantity = 0,
            Priority = 3,
            Points = 7,
            Weight = 1.25,
            IsPaid = true,
            IsGift = true,
            Status = OrderStatus.Cancelled,
            Options = ShippingOptions.Fragile,
            CreatedAt = new DateTime(2024, 3, 10, 8, 15, 30, 500),
            PaidAt = new DateTimeOffset(2024, 3, 9, 20, 0, 0, TimeSpan.FromHours(-5)),
            DeliveryTime = new TimeOnly(23, 59, 59).Add(TimeSpan.FromTicks(9_999_990)),
            TrackingId = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8"),
            Tags = ["gift", null!],
        },
        new()
        {
            Id = 5,
            CustomerId = 4,
            Number = "D-001",
            Note = "it's",
            Total = 0.125m,
            Quantity = 5,
            Points = 123_456_789_012,
            Weight = 6.5,
            Status = OrderStatus.Created,
            Options = ShippingOptions.Express | ShippingOptions.Fragile,
            CreatedAt = new DateTime(2000, 1, 1, 0, 0, 0),
            ShippedOn = new DateOnly(2000, 1, 3),
            DeliveryTime = new TimeOnly(12, 0),
            TrackingId = new Guid("16fd2706-8baf-433b-82eb-8c7fada847da"),
            Tags = ["x%y", "a_b"],
        },
        new()
        {
            Id = 6,
            CustomerId = 6,
            Number = "E-001",
            Note = "back\\slash",
            Total = 10.005m,
            Discount = 0.005m,
            Quantity = 4,
            Priority = 1,
            Points = 5,
            Weight = 1e10,
            IsPaid = true,
            IsGift = false,
            Status = OrderStatus.Paid,
            Options = ShippingOptions.Insured,
            CreatedAt = new DateTime(1999, 12, 31, 23, 59, 59),
            PaidAt = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ShippedOn = new DateOnly(1999, 12, 31),
            DeliveryTime = new TimeOnly(6, 45),
            TrackingId = new Guid("886313e1-3b8a-5372-9b90-0c9aee199e5d"),
            Tags = ["fragile"],
        },
        new()
        {
            Id = 7,
            CustomerId = 9,
            Number = "F-001",
            Note = "\U0001F600",
            Total = 1.15m,
            Discount = 1.15m,
            Quantity = 7,
            Priority = 2,
            Points = 1,
            Weight = 3.75,
            IsPaid = true,
            IsGift = true,
            Status = OrderStatus.Shipped,
            Options =
                ShippingOptions.Express
                | ShippingOptions.Insured
                | ShippingOptions.Fragile,
            CreatedAt = new DateTime(2024, 6, 30, 12, 0, 0),
            PaidAt = new DateTimeOffset(2024, 7, 1, 0, 30, 0, TimeSpan.FromHours(2)),
            ShippedOn = new DateOnly(2024, 7, 2),
            DeliveryTime = new TimeOnly(13, 13, 13),
            TrackingId = new Guid("c9bf9e57-1685-4c89-bafb-ff5af830be8a"),
            Tags = ["gift"],
        },
        new()
        {
            Id = 8,
            CustomerId = 99,
            Number = "G-001",
            Note = "orphan",
            Total = 7.5m,
            Quantity = 1,
            Points = 2,
            Weight = double.PositiveInfinity,
            Status = OrderStatus.Created,
            Options = ShippingOptions.None,
            CreatedAt = new DateTime(2024, 12, 31, 23, 59, 59),
            DeliveryTime = new TimeOnly(0, 0, 1),
            TrackingId = new Guid("2c1d3f5e-0000-4000-8000-000000000008"),
            Tags = [],
        },
        new()
        {
            Id = 9,
            CustomerId = 1,
            Number = "a-003",
            Note = "Lower",
            Total = 120.00m,
            Discount = 0m,
            Quantity = 6,
            Priority = 1,
            Points = 30,
            Weight = 2.5,
            IsPaid = true,
            IsGift = true,
            Status = OrderStatus.Paid,
            Options = ShippingOptions.Express,
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0),
            PaidAt = new DateTimeOffset(2024, 1, 15, 15, 30, 0, TimeSpan.Zero),
            ShippedOn = new DateOnly(2024, 1, 21),
            DeliveryTime = new TimeOnly(9, 0),
            TrackingId = new Guid("9a7b8c6d-1111-4222-8333-944455556666"),
            Tags = ["gift", "bulk"],
        },
        new()
        {
            Id = 10,
            CustomerId = 11,
            Number = "H-001",
            Total = 99.99m,
            Discount = 9.995m,
            Quantity = 2,
            Priority = 3,
            Points = 10,
            Weight = double.NaN,
            IsGift = false,
            Status = OrderStatus.Cancelled,
            Options = ShippingOptions.Insured,
            CreatedAt = new DateTime(2025, 2, 28, 0, 0, 0),
            ShippedOn = new DateOnly(2025, 3, 1),
            DeliveryTime = new TimeOnly(17, 45),
            TrackingId = new Guid("e1d2c3b4-a596-4877-8899-aabbccddeeff"),
            Tags = ["express", null!],
        },
        new()
        {
            Id = 11,
            CustomerId = 2,
            Number = "B-002",
            Note = "\u00dcn\u00efc\u00f6d\u00e9",
            Total = 0.5m,
            Discount = 0.25m,
            Quantity = 10,
            Points = -1,
            Weight = 1.5,
            IsPaid = true,
            Status = OrderStatus.Paid,
            Options = ShippingOptions.None,
            CreatedAt = new DateTime(2024, 2, 28, 12, 0, 0),
            PaidAt = new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero),
            ShippedOn = new DateOnly(2024, 2, 29),
            DeliveryTime = new TimeOnly(8, 0),
            TrackingId = new Guid("00000000-0000-0000-0000-000000000011"),
            Tags = [],
        },
        new()
        {
            Id = 12,
            CustomerId = 3,
            Number = "C-002",
            Note = "trailing ",
            Total = -0.5m,
            Discount = 0.5m,
            Quantity = 3,
            Priority = 2,
            Points = 0,
            Weight = -2.5,
            IsPaid = false,
            IsGift = true,
            Status = OrderStatus.Created,
            Options = ShippingOptions.Fragile | ShippingOptions.Insured,
            CreatedAt = new DateTime(2024, 11, 3, 12, 0, 0),
            ShippedOn = new DateOnly(2024, 12, 31),
            DeliveryTime = new TimeOnly(23, 0),
            TrackingId = new Guid("ffffffff-ffff-4fff-bfff-ffffffffffff"),
            Tags = ["bulk", "gift", "gift"],
        },
    ];

    public static IReadOnlyList<OrderEvent> OrderEvents { get; } =
    [
        new()
        {
            Id = 1,
            OrderId = 1,
            Kind = "created",
            Severity = 1,
            Score = 0.5F,
            Elapsed = TimeSpan.Zero,
            Timeout = TimeSpan.FromDays(1),
        },
        new()
        {
            Id = 2,
            OrderId = 1,
            Kind = "paid",
            Severity = 2,
            Score = 1.5F,
            Elapsed = new TimeSpan(1, 2, 30, 15, 500),
        },
        new()
        {
            Id = 3,
            Kind = "orphan",
            Score = -2.5F,
            Elapsed = TimeSpan.FromTicks(10),
            Timeout = new TimeSpan(1, 12, 0, 0),
        },
        new()
        {
            Id = 4,
            OrderId = 99,
            Kind = "lost",
            Severity = 3,
            Score = 3.25F,
            Elapsed = new TimeSpan(-1, -30, 0),
        },
        new()
        {
            Id = 5,
            OrderId = 3,
            Kind = "shipped",
            Severity = 1,
            Score = 0F,
            Elapsed = new TimeSpan(400, 23, 59, 59).Add(TimeSpan.FromTicks(9_999_990)),
            Timeout = TimeSpan.FromMinutes(45),
        },
    ];

    // Each table with its rows, in the order the tables are created.
    public static IReadOnlyList<TableDefinition> Tables { get; } =
    [
        new(typeof(Customer), Customers),
        new(typeof(Employee), Employees),
        new(typeof(Order), Orders),
        new(typeof(OrderEvent), OrderEvents),
    ];

    public static bool IsTable(Type type)
    {
        return Tables.Any(table => table.Type == type);
    }

    // The rows of a table as a LINQ to Objects source.
    public static IQueryable Source(Type type)
    {
        return Tables.Single(table => table.Type == type).Rows.AsQueryable();
    }

    private static Customer Customer(
        int id,
        string name,
        string? email,
        string region,
        bool active
    )
    {
        return new()
        {
            Id = id,
            Name = name,
            Email = email,
            Region = region,
            IsActive = active,
        };
    }
}
