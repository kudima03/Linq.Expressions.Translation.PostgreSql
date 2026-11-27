using System.Globalization;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// DateTime.Parse with the invariant culture keeps the fraction of a second the text
// carries, up to the microsecond, read as written or as UTC, and still reads a whole
// second written without a point.
[Collection(SharedDatabase.Name)]
public sealed class ParseFractionSemanticsTests(Database database)
{
    private static IQueryable<Order> Digits =>
        Orders.Where(o => o.Quantity > 0 && o.Quantity < 10).OrderBy(o => o.Id);

    // #146: TO_TIMESTAMP('+00 ' || text, 'TZH MM/DD/YYYY HH24:MI:SS') ignores the
    // fraction of a second.
    [Fact]
    public void ParsesUniversalDateTimeWithFraction()
    {
        database.AssertSameSequence(
            Digits.Select(o => new
            {
                o.Id,
                Time = DateTime.Parse(
                    "01/31/2024 23:59:59.99999" + o.Quantity,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                ),
                Short = DateTime.Parse(
                    "0" + o.Quantity + "/02/2024 10:30:00.5",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                ),
            })
        );
    }

    // #146: the same pattern behind the plain invariant parse.
    [Fact]
    public void ParsesInvariantDateTimeWithFraction()
    {
        database.AssertSameSequence(
            Digits.Select(o => new
            {
                o.Id,
                Time = DateTime.Parse(
                    "01/31/2024 23:59:59.99999" + o.Quantity,
                    CultureInfo.InvariantCulture
                ),
                Short = DateTime.Parse(
                    "0" + o.Quantity + "/02/2024 10:30:00.25",
                    CultureInfo.InvariantCulture
                ),
            })
        );
    }

    // #146: the fraction decides a comparison with a constant and with another
    // parsed instant.
    [Fact]
    public void ComparesParsedInstantsByTheirFraction()
    {
        database.AssertSameSequence(
            Digits.Select(o => new
            {
                o.Id,
                Equal = DateTime.Parse(
                    "01/31/2024 23:59:59.99999" + o.Quantity,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                ) == new DateTime(2024, 1, 31, 23, 59, 59, 999, 995, DateTimeKind.Utc),
                Later = DateTime.Parse(
                    "01/31/2024 23:59:59." + o.Quantity,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                ) > new DateTime(2024, 1, 31, 23, 59, 59, 500, DateTimeKind.Utc),
            })
        );
        database.AssertSameRows(
            Orders
                .Where(o =>
                    DateTime.Parse(
                        "01/31/2024 23:59:59." + o.Quantity,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                    ) > new DateTime(2024, 1, 31, 23, 59, 59, 500, DateTimeKind.Utc)
                )
                .Select(o => o.Id)
        );
    }

    // #146: a difference of parsed instants keeps the fraction.
    [Fact]
    public void SubtractsParsedInstantsWithTheirFraction()
    {
        database.AssertSameSequence(
            Digits.Select(o => new
            {
                o.Id,
                Elapsed = DateTime.Parse(
                    "01/31/2024 23:59:59.99999" + o.Quantity,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                ) - new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            })
        );
    }

    // A text with a fraction and one without, in the same column: the fix must still
    // read a whole second written without a point.
    [Fact]
    public void ParsesTextsWithAndWithoutFraction()
    {
        database.AssertSameSequence(
            Digits.Select(o => new
            {
                o.Id,
                Time = DateTime.Parse(
                    o.Quantity > 3
                        ? "01/31/2024 23:59:5" + o.Quantity
                        : "01/31/2024 23:59:59.12345" + o.Quantity,
                    CultureInfo.InvariantCulture
                ),
                Universal = DateTime.Parse(
                    o.Quantity > 3
                        ? "01/31/2024 23:59:5" + o.Quantity
                        : "01/31/2024 23:59:59.12345" + o.Quantity,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                ),
            })
        );
    }
}
