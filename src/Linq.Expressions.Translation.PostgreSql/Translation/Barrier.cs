namespace Linq.Expressions.Translation.PostgreSql.Translation;

[Flags]
internal enum Barrier
{
    None = 0,
    Paging = 1,
    Distinct = 2,
    Grouping = 4,
    Projection = 8,
}
