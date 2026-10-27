using System.ComponentModel.DataAnnotations.Schema;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests.Model;

// A table whose names come from the mapping attributes rather than from the type.
[Table("order_events", Schema = "audit")]
public sealed record OrderEvent
{
    [Column("event_id")]
    public long Id { get; init; }

    [Column("order_id")]
    public int? OrderId { get; init; }

    [Column("kind")]
    public string Kind { get; init; } = string.Empty;

    public short Severity { get; init; }

    public float Score { get; init; }

    public TimeSpan Elapsed { get; init; }

    public TimeSpan? Timeout { get; init; }

    [NotMapped]
    public string Label => $"{Kind}#{Id}";
}
