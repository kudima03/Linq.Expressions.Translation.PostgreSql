namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// #152: a null captured value where nothing beside it types the NULL: the argument
// of an aggregate or of a window function, the branches of a conditional that are
// all null, the operand of a negation and a value read through a subquery. An
// untyped NULL is a text to PostgreSQL there, or of no function at all, and the
// statement fails before it reads a row.
[Collection(SharedDatabase.Name)]
public sealed class NullConstantSemanticsTests(Database database)
{
    // ERROR: function sum(unknown) is not unique.
    [Fact]
    public void SumsANullCapturedValue()
    {
        long? none = null;
        decimal? money = null;

        database.AssertSameResult(Orders, q => q.Sum(o => none));
        database.AssertSameResult(Orders, q => q.Sum(o => money));
    }

    // ERROR: function avg(unknown) is not unique; operator is not unique: - unknown
    // (the Max of doubles is -MIN(-x)).
    [Fact]
    public void AveragesAndTakesTheExtremesOfANullCapturedValue()
    {
        long? none = null;
        double? real = null;
        bool? flag = null;
        string? text = null;

        database.AssertSameResult(Orders, q => q.Average(o => none));
        database.AssertSameResult(Orders, q => q.Max(o => none));
        database.AssertSameResult(Orders, q => q.Max(o => real));
        database.AssertSameResult(Orders, q => q.Min(o => flag));
        database.AssertSameResult(Orders, q => q.Max(o => text));
    }

    // ERROR: operator does not exist: text >= integer.
    [Fact]
    public void ComparesAConditionalOfANullCapturedValue()
    {
        long? none = null;

        database.AssertSameRows(
            Orders.Select(o => new { o.Id, Compared = (o.IsPaid ? none : null) >= 0 })
        );
    }

    // ERROR: operator does not exist: text + integer, text > timestamp without time
    // zone, text = uuid, text = boolean; function round(text, integer) does not
    // exist.
    [Fact]
    public void ComputesWithConditionalsOfNullsOfEveryType()
    {
        int? number = null;
        decimal? money = null;
        double? real = null;
        string? text = null;
        bool? flag = null;
        DateTime? moment = null;
        Guid? guid = null;
        TimeSpan? span = null;

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Sum = (o.IsPaid ? number : null) + o.Quantity,
                Quotient = (o.IsPaid ? money : null) / 3,
                Above = (o.IsPaid ? real : null) > 1.5,
                Text = (o.IsPaid ? text : null) + "x",
                Flag = (o.IsPaid ? flag : null) == true,
                Later = (o.IsPaid ? moment : null) > o.CreatedAt,
                Same = (o.IsPaid ? guid : null) == o.TrackingId,
                Long = (o.IsPaid ? span : null) > TimeSpan.Zero,
                Nested = (
                    o.IsPaid ? number
                    : o.IsGift == true ? number
                    : null
                ) >= 0,
            })
        );
    }

    [Fact]
    public void AggregatesAConditionalOfNulls()
    {
        long? none = null;

        database.AssertSameResult(Orders, q => q.Sum(o => o.IsPaid ? none : null));
        database.AssertSameResult(Orders, q => q.Average(o => o.IsPaid ? none : null));
    }

    [Fact]
    public void SumsANullCapturedValueOverGroupsSubqueriesAndEveryRow()
    {
        long? none = null;

        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Sum = g.Sum(o => none),
                    Paid = g.Where(o => o.IsPaid).Average(o => none),
                })
        );
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Sum = Orders.Where(o => o.CustomerId == c.Id).Sum(o => none),
            })
        );
        database.AssertSameRows(
            Orders.Select(o => new { o.Id, Sum = Orders.Sum(x => none) })
        );
    }

    // ERROR: could not determine polymorphic type because input has type unknown.
    [Fact]
    public void ReadsANullCapturedValueOverAWindow()
    {
        long? none = null;

        database.AssertSameRows(
            Trees.Over(
                Orders,
                null,
                rows => rows.OrderBy(o => o.Id),
                PostgreSqlFrame.Rows(-1, 0),
                (o, w) =>
                    new
                    {
                        o.Id,
                        Sum = w.Rows.Sum(x => none),
                        Mean = w.Rows.Average(x => none),
                        First = w.FirstValue(x => none),
                        Previous = w.Lag(x => none, 1, none),
                        Next = w.Lead(x => none, 1, o.Points),
                    }
            )
        );
    }

    // ERROR: operator is not unique: - unknown.
    [Fact]
    public void NegatesANullCapturedValueProjectedBefore()
    {
        long? none = null;
        bool? flag = null;

        database.AssertSameRows(
            Orders
                .Select(o => new
                {
                    o.Id,
                    None = none,
                    Flag = flag,
                })
                .Select(x => new
                {
                    x.Id,
                    Negated = -x.None,
                    Complement = ~x.None,
                    Not = !x.Flag,
                })
        );
    }

    // The Value of a null captured nullable in a branch no row reaches is read
    // through a subquery. ERROR: function abs(text) does not exist.
    [Fact]
    public void ReadsTheValueOfANullCapturedValueInABranchNoRowReaches()
    {
        long? none = null;

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Value = o.Id < 0 ? Math.Abs(none!.Value) : o.Points,
            })
        );
    }
}
