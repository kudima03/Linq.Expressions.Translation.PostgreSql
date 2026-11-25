using System.Linq.Expressions;
using Linq.Expressions.Translation.PostgreSql.Syntax;

namespace Linq.Expressions.Translation.PostgreSql.Translation;

// Dates and times built from their parts fail where .NET throws. MAKE_DATE,
// MAKE_TIME and MAKE_TIMESTAMP reject a month, a day or a minute out of range, but
// take a year before 1 or after 9999, the hour 24 and the second 60, so such a part
// turns the year into 0 or the hour into -1, which they reject in turn. The part is
// chosen with CASE rather than failing in a branch, which PostgreSQL may evaluate
// while it plans the statement.
internal sealed partial class Translator
{
    private const int DaysToMaxDate = 3_652_058;

    // new DateOnly(...), new TimeOnly(...) or new DateTime(...) from parts, or null.
    private SqlExpression? Constructed(NewExpression creation)
    {
        // A struct created without arguments has no constructor.
        if (creation.Constructor is null)
        {
            return null;
        }
        Type[] parameters =
        [
            .. creation
                .Constructor.GetParameters()
                .Select(parameter => parameter.ParameterType),
        ];
        bool parts = parameters.Length > 1 && parameters.All(IsPart);
        return creation.Type == typeof(DateOnly) && parts && parameters.Length == 3
                ? Day(creation.Arguments)
            : creation.Type == typeof(TimeOnly) && parts ? Clock(creation.Arguments)
            : creation.Type == typeof(TimeOnly)
            && parameters is [Type ticks]
            && ticks == typeof(long)
                ? ClockOfTicks(Sql(creation.Arguments[0]))
            : creation.Type == typeof(DateTime) && parameters.Length > 1
                ? Timestamp(creation, parameters)
            : null;
    }

    private static bool IsPart(Type type)
    {
        return type == typeof(int);
    }

    private SqlFunction Day(IReadOnlyList<Expression> arguments)
    {
        SqlExpression year = Sql(arguments[0]);
        return new SqlFunction(
            "MAKE_DATE",
            [
                Guarded(year, [Within(year, 1, 9999)], 0),
                Sql(arguments[1]),
                Sql(arguments[2]),
            ],
            typeof(DateOnly)
        );
    }

    // new TimeOnly(hour, minute[, second[, millisecond[, microsecond]]]).
    private SqlFunction Clock(IReadOnlyList<Expression> arguments)
    {
        List<SqlExpression> parts = [.. arguments.Select(Sql)];
        SqlExpression hour = parts[0];
        return new SqlFunction(
            "MAKE_TIME",
            [
                Guarded(hour, [Within(hour, 0, 23), .. Fraction(parts.Skip(2))], -1),
                parts[1],
                Seconds(parts.Skip(2)),
            ],
            typeof(TimeOnly)
        );
    }

    // The ranges of the second, millisecond and microsecond parts.
    private static IEnumerable<SqlExpression> Fraction(IEnumerable<SqlExpression> parts)
    {
        return parts.Select((part, index) => Within(part, 0, index == 0 ? 59 : 999));
    }

    // The seconds with their fraction, exact in numeric: s + ms * 0.001 + us * 0.000001,
    // the constant parts added up.
    private static SqlExpression Seconds(IEnumerable<SqlExpression> parts)
    {
        decimal[] units = [1m, 0.001m, 0.000001m];
        decimal constant = 0m;
        List<SqlExpression> terms = [];
        foreach ((SqlExpression part, decimal unit) in parts.Zip(units))
        {
            if (part is SqlConstant { Value: int value })
            {
                constant += value * unit;
            }
            else
            {
                terms.Add(
                    unit == 1m
                        ? part
                        : Calculate("*", part, new SqlConstant(unit, typeof(decimal)))
                );
            }
        }
        if (constant != 0m || terms.Count == 0)
        {
            terms.Add(
                constant == decimal.Truncate(constant)
                    ? Integer((int)constant)
                    : new SqlConstant(constant, typeof(decimal))
            );
        }
        return terms.Aggregate((sum, term) => Calculate("+", sum, term));
    }

    // new TimeOnly(ticks): the ticks of a time of day are under a day, and a time
    // plus an interval wraps around midnight.
    private static SqlBinary ClockOfTicks(SqlExpression ticks)
    {
        SqlFunction midnight = new(
            "MAKE_TIME",
            [
                Guarded(Integer(0), [Within(ticks, 0, TimeSpan.TicksPerDay - 1)], -1),
                Integer(0),
                Integer(0),
            ],
            typeof(TimeOnly)
        );
        return new SqlBinary(
            "+",
            SqlBinaryKind.Arithmetic,
            midnight,
            Microseconds(ticks),
            typeof(TimeOnly),
            false
        );
    }

    // new DateTime(year, month, day[, hour, minute, second[, millisecond[,
    // microsecond]]][, kind]) or new DateTime(date, time[, kind]).
    private SqlExpression Timestamp(NewExpression creation, Type[] parameters)
    {
        bool kinded = parameters[^1] == typeof(DateTimeKind);
        Type[] values = kinded ? parameters[..^1] : parameters;
        if (
            values is [Type date, Type time]
            && date == typeof(DateOnly)
            && time == typeof(TimeOnly)
        )
        {
            SqlExpression joined = Joined(
                Sql(creation.Arguments[0]),
                Sql(creation.Arguments[1])
            );
            return kinded ? Kinded(joined, creation.Arguments[^1]) : joined;
        }
        if (values.Length is not (3 or 6 or 7 or 8) || !values.All(IsPart))
        {
            throw new NotSupportedException(
                "A DateTime is constructed from integer parts or from a DateOnly and a TimeOnly, optionally with a constant kind; a Calendar has no PostgreSQL equivalent."
            );
        }
        List<SqlExpression> parts =
        [
            .. creation.Arguments.Take(values.Length).Select(Sql),
        ];
        SqlExpression year = parts[0];
        List<SqlExpression> checks = [Within(year, 1, 9999)];
        List<SqlExpression> clock = [Integer(0), Integer(0), Integer(0)];
        if (parts.Count > 3)
        {
            checks.Add(Within(parts[3], 0, 23));
            checks.AddRange(Fraction(parts.Skip(5)));
            clock = [parts[3], parts[4], Seconds(parts.Skip(5))];
        }
        SqlFunction moment = new(
            "MAKE_TIMESTAMP",
            [Guarded(year, checks, 0), parts[1], parts[2], .. clock],
            typeof(DateTime)
        );
        return kinded ? Kinded(moment, creation.Arguments[^1]) : moment;
    }

    private static SqlBinary Joined(SqlExpression date, SqlExpression time)
    {
        return new SqlBinary(
            "+",
            SqlBinaryKind.Arithmetic,
            date,
            time,
            typeof(DateTime),
            false
        );
    }

    private static SqlExpression Kinded(SqlExpression moment, Expression kind)
    {
        return kind is not ConstantExpression { Value: DateTimeKind value }
            || value == DateTimeKind.Local
                ? throw new NotSupportedException(
                    "A DateTime is constructed with a constant kind of Utc or Unspecified; a Local one has no PostgreSQL equivalent."
                )
            : value == DateTimeKind.Utc
                ? new SqlBinary(
                    "AT TIME ZONE",
                    SqlBinaryKind.Arithmetic,
                    moment,
                    Text("UTC"),
                    typeof(DateTime),
                    false
                )
            : moment;
    }

    // Whether a part is in its inclusive range, TRUE or FALSE for a constant.
    private static SqlExpression Within(SqlExpression part, long low, long high)
    {
        return part is SqlConstant { Value: int constant }
            ? new SqlConstant(constant >= low && constant <= high, typeof(bool))
            : Logical(
                "AND",
                Compare(">=", part, new SqlConstant(low, typeof(long))),
                Compare("<=", part, new SqlConstant(high, typeof(long)))
            );
    }

    // value where every check holds, else the invalid part.
    private static SqlExpression Guarded(
        SqlExpression value,
        IReadOnlyList<SqlExpression> checks,
        int invalid
    )
    {
        if (checks.Any(check => check is SqlConstant { Value: false }))
        {
            return Integer(invalid);
        }
        List<SqlExpression> open =
        [
            .. checks.Where(check => check is not SqlConstant { Value: true }),
        ];
        return open.Count == 0
            ? value
            : new SqlCase(
                [
                    new SqlWhen(
                        open.Aggregate((first, second) => Logical("AND", first, second)),
                        value
                    ),
                ],
                Integer(invalid),
                typeof(int)
            );
    }

    // DateOnly.FromDayNumber(n) counts days from 0001-01-01 up to 9999-12-31.
    private static SqlBinary DayOfNumber(SqlExpression number)
    {
        SqlFunction first = new(
            "MAKE_DATE",
            [
                Guarded(Integer(1), [Within(number, 0, DaysToMaxDate)], 0),
                Integer(1),
                Integer(1),
            ],
            typeof(DateOnly)
        );
        return new SqlBinary(
            "+",
            SqlBinaryKind.Arithmetic,
            first,
            number,
            typeof(DateOnly),
            false
        );
    }

    // DateOnly.ToDateTime(time[, kind]): a date plus a time is a timestamp.
    private SqlExpression DayAt(MethodCallExpression call)
    {
        SqlBinary moment = Joined(Sql(call.Object!), Sql(call.Arguments[0]));
        return call.Arguments.Count == 2 ? Kinded(moment, call.Arguments[1]) : moment;
    }
}
