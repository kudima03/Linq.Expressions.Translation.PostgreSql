namespace Linq.Expressions.Translation.PostgreSql.Syntax;

internal abstract class SqlExpression(Type type)
{
    // The expressions the value is computed from in its SELECT; a subquery is a
    // value of its own.
    public virtual IEnumerable<SqlExpression> Operands => [];

    // An aggregate of the rows of its SELECT, and a function over a window of them.
    public virtual bool IsAggregate => false;

    public virtual bool IsWindow => false;

    // Whether the value, or anything it is computed from, is of a kind.
    public bool Any(Func<SqlExpression, bool> kind)
    {
        return kind(this) || Operands.Any(operand => operand.Any(kind));
    }

    public Type Type { get; } = type;

    public virtual bool Nullable => false;

    // The kind of the DateTime the value is read as.
    public virtual SqlKind TimeKind => SqlKind.Unspecified;

    // The kind of a DateTime that is one of the operands: theirs when they agree. A
    // NULL is of any kind.
    protected SqlKind KindOf(IEnumerable<SqlExpression> operands)
    {
        List<SqlKind> kinds =
        [
            .. operands
                .Where(operand => operand is not SqlConstant { Value: null })
                .Select(operand => operand.TimeKind)
                .Distinct(),
        ];
        return (System.Nullable.GetUnderlyingType(Type) ?? Type) != typeof(DateTime)
            || kinds.Count == 0
                ? SqlKind.Unspecified
            : kinds.Count == 1 ? kinds[0]
            : SqlKind.Mixed;
    }

    public virtual bool IsCompound(SqlMode mode)
    {
        return false;
    }

    public abstract void Render(SqlWriter writer, SqlMode mode);
}
