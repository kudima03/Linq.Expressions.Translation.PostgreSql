using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

internal sealed partial class Translator
{
    private static readonly SqlConstant Origin = new(DateTime.MinValue, typeof(DateTime));

    private static SqlBinary Interval(SqlExpression amount, string unit)
    {
        return new SqlBinary(
            "*",
            SqlBinaryKind.Arithmetic,
            amount,
            new SqlFragment($"INTERVAL '1 {unit}'", typeof(TimeSpan)),
            typeof(TimeSpan),
            amount.Nullable
        );
    }

    // PostgreSQL keeps microseconds, so the ticks below one are truncated.
    private static SqlBinary Microseconds(SqlExpression ticks)
    {
        return Interval(Calculate("/", ticks, Integer(10)), "microsecond");
    }

    // Ticks are 100 ns units since 0001-01-01: a duration has them in its seconds.
    private static SqlCast Ticks(SqlExpression duration)
    {
        return new SqlCast(
            Calculate("*", new SqlExtract("EPOCH", duration), Integer(10_000_000)),
            "bigint",
            typeof(long)
        );
    }

    // A timestamp is built from whole days and the microseconds left over, as a
    // single interval of microseconds loses precision past 2^53 of them.
    private SqlExpression Moment(NewExpression creation)
    {
        SqlExpression ticks = Sql(creation.Arguments[0]);
        SqlConstant day = new(TimeSpan.TicksPerDay, typeof(long));
        SqlExpression moment = new SqlBinary(
            "+",
            SqlBinaryKind.Arithmetic,
            Shifted(Origin, Calculate("/", ticks, day), "day"),
            Microseconds(Calculate("%", ticks, day)),
            typeof(DateTime),
            ticks.Nullable
        );
        return Kind(creation) == DateTimeKind.Utc
            ? new SqlBinary(
                "AT TIME ZONE",
                SqlBinaryKind.Arithmetic,
                moment,
                Text("UTC"),
                typeof(DateTime),
                ticks.Nullable
            )
            : moment;
    }

    private static DateTimeKind Kind(NewExpression creation)
    {
        return creation.Arguments.Count == 1 ? DateTimeKind.Unspecified
            : creation.Arguments[1] is ConstantExpression { Value: DateTimeKind kind }
            && kind != DateTimeKind.Local
                ? kind
            : throw new NotSupportedException(
                "A DateTime is constructed from ticks with a constant kind of Utc or Unspecified; a Local one has no PostgreSQL equivalent."
            );
    }
}
