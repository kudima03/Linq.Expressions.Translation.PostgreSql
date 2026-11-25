namespace Linq.Expressions.Translation.PostgreSql;

// What a frame asks of the ORDER BY of its window: GROUPS needs one, a RANGE with an
// offset exactly one key, numeric or temporal by the kind of the offset.
internal enum FrameKind
{
    Rows,
    Groups,
    Range,
    NumericRange,
    IntervalRange,
}
