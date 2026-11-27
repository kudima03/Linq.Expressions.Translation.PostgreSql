using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// The kind of a DateTime. C# compares DateTime values, computes with them and reads
// their members by their ticks, whatever their kind, so a DateTime is a timestamp
// in the statement; one of the kind Utc (SqlUtc, or a column of them) is converted
// to the UTC offset, a timestamptz, only where it leaves the statement as a result
// or becomes a DateTimeOffset, so that it is read with the kind Utc.
internal sealed partial class Translator
{
    // A timestamp at the UTC offset, or an instant as the timestamp of its UTC time.
    private static SqlBinary Zoned(SqlExpression value, Type type)
    {
        return new SqlBinary(
            "AT TIME ZONE",
            SqlBinaryKind.Arithmetic,
            value,
            Text("UTC"),
            type,
            value.Nullable
        );
    }

    // The instant a Utc timestamp stands for; one that was taken from an instant is
    // that instant.
    private static SqlExpression Instant(SqlExpression value, Type type)
    {
        return value is SqlUtc { Operand: SqlBinary { Symbol: "AT TIME ZONE" } zoned }
            ? zoned.Left
            : Zoned(value, type);
    }

    private static NotSupportedException Unkinded()
    {
        return new NotSupportedException(
            "A DateTime that is of the kind Utc on some rows and Unspecified on others cannot be read: a PostgreSQL column is a timestamp or a timestamptz. Give both the same kind."
        );
    }

    // The columns of the result are read by the client: a Utc DateTime as an instant.
    // The sides of a set operation yield one column, so they have to agree.
    private static void Read(SqlQuery query, IReadOnlyList<SqlKind> kinds)
    {
        if (kinds.Contains(SqlKind.Mixed))
        {
            throw Unkinded();
        }
        if (query is SetOperationQuery operation)
        {
            Read(operation.Left, kinds);
            Read(operation.Right, kinds);
            return;
        }
        SelectQuery select = (SelectQuery)query;
        if (!kinds.Contains(SqlKind.Utc))
        {
            return;
        }
        // SELECT DISTINCT is ordered by what it selects, the instants.
        Dictionary<string, SqlExpression> instants = [];
        int index = 0;
        select.Shape = select.Shape.Map(
            (value, _) =>
            {
                if (kinds[index++] != SqlKind.Utc)
                {
                    return value;
                }
                SqlExpression instant = Instant(value, value.Type);
                instants[SqlWriter.Text(value)] = instant;
                return instant;
            },
            null
        );
        if (select.Distinct)
        {
            select.Orderings =
            [
                .. select.Orderings.Select(ordering =>
                    instants.TryGetValue(
                        SqlWriter.Text(ordering.Key),
                        out SqlExpression? instant
                    )
                        ? new Ordering(instant, ordering.Descending, ordering.NullsLast)
                        : ordering
                ),
            ];
        }
    }
}
