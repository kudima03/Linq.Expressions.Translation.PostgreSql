namespace Linq.Expressions.Translation.PostgreSql;

// What a frame asks of the ORDER BY of its window: a RANGE with an offset needs
// exactly one key, numeric or temporal by the kind of the offset.
internal enum FrameKind
{
    Rows,
    Range,
    NumericRange,
    IntervalRange,
}
