namespace Linq.Expressions.Translation.PostgreSql.Syntax;

// The kind of the DateTime a value is read as. A DateTime is a timestamp in the
// statement whatever its kind, as C# compares and computes with DateTime values by
// their ticks; the kind tells how the value is read: a Utc one at the UTC offset, a
// timestamptz. A value that is of one kind on some rows and of another on others
// has no PostgreSQL type to be read as.
internal enum SqlKind
{
    Unspecified,
    Utc,
    Mixed,
}
