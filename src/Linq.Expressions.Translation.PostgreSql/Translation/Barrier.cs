namespace Linq.Expressions.Translation.PostgreSql.Translation;

[Flags]
internal enum Barrier
{
    None = 0,
    Paging = 1,
    Distinct = 2,
    Grouping = 4,
    Projection = 8,
    Bound = 16,
    Window = 32,

    // The one row of aggregates without FROM, which a join has nothing to attach to.
    OneRow = 64,

    // What an aggregate over the rows of a query cannot read through.
    Aggregation = Paging | Distinct | Grouping | Window,
}
