#pragma warning disable CA1861 // the constant arrays are the roots and values under test

using System.Text;

namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// string.Join, the first and last value of an ordered group, GREATEST and LEAST of a
// few values, Min and Max with StringComparer.Ordinal and Normalize give .NET's
// results: a null value joins as the empty string, an empty sequence as the empty
// string, NULLs are skipped by Max and Min, strings compare by code point.
[Collection(SharedDatabase.Name)]
public sealed class StringAggregationSemanticsTests(Database database)
{
    [Fact]
    public void JoinsGroupsInTheirOrder()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Notes = string.Join(", ", g.OrderBy(o => o.Id).Select(o => o.Note)),
                    Numbers = string.Join(
                        "/",
                        g.OrderByDescending(o => o.Number, StringComparer.Ordinal)
                            .Select(o => o.Number)
                    ),
                    Paid = string.Join(
                        "",
                        g.Where(o => o.IsPaid).OrderBy(o => o.Id).Select(o => o.Number)
                    ),
                    Kept = string.Join(
                        "|",
                        g.OrderBy(o => o.Id).Select(o => o.Note).Where(v => v != null)
                    ),
                })
        );
    }

    [Fact]
    public void JoinsDistinctValuesOfGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    g.Key,
                    Up = string.Join(
                        ",",
                        g.OrderBy(o => o.Number, StringComparer.Ordinal)
                            .Select(o => o.Number)
                            .Distinct()
                    ),
                    Down = string.Join(
                        ",",
                        g.OrderByDescending(o => o.Note, StringComparer.Ordinal)
                            .Select(o => o.Note)
                            .Where(v => v != null)
                            .Distinct()
                    ),
                })
        );
    }

    [Fact]
    public void JoinsCorrelatedSubqueries()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Numbers = string.Join(
                    "; ",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Number)
                ),
                Notes = string.Join(
                    ",",
                    Orders
                        .Where(o => o.CustomerId == c.Id)
                        .OrderByDescending(o => o.Id)
                        .Take(2)
                        .Select(o => o.Note)
                ),
            })
        );
    }

    [Fact]
    public void JoinsGroupsOfGroupJoin()
    {
        database.AssertSameRows(
            Customers.GroupJoin(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) =>
                    new
                    {
                        c.Id,
                        Numbers = string.Join(
                            ",",
                            os.OrderBy(o => o.Number, StringComparer.Ordinal)
                                .Select(o => o.Number)
                        ),
                    }
            )
        );
    }

    [Fact]
    public void JoinsEveryRowOnce()
    {
        database.AssertSameRows(
            new[] { 0 }
                .AsQueryable()
                .Select(_ =>
                    string.Join(
                        " + ",
                        Orders.OrderByDescending(o => o.Id).Select(o => o.Note)
                    )
                )
        );
    }

    [Fact]
    public void JoinsArraysWithNulls()
    {
        string? none = null;

        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Values = string.Join(", ", new[] { o.Number, o.Note, "x" }),
                Glued = string.Join(none, new[] { o.Note, o.Number }),
                ByNote = string.Join(o.Note, new[] { o.Number, o.Number }),
                Tags = string.Join(",", o.Tags),
                Piped = string.Join('|', o.Tags),
            })
        );
    }

    [Fact]
    public void ReadsFirstAndLastOfOrderedGroups()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    First = g.OrderBy(o => o.CreatedAt)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Total)
                        .First(),
                    Last = g.OrderBy(o => o.Priority)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Note)
                        .LastOrDefault(),
                    Highest = g.OrderByDescending(o => o.Priority)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Priority)
                        .First(),
                    Ordinal = g.OrderBy(o => o.Note, StringComparer.Ordinal)
                        .ThenBy(o => o.Id)
                        .Select(o => o.Id)
                        .First(),
                    Paid = g.Where(o => o.IsPaid)
                        .OrderBy(o => o.Id)
                        .Select(o => o.Quantity)
                        .FirstOrDefault(),
                    Gift = g.OrderBy(o => o.Id)
                        .Select(o => o.Note)
                        .FirstOrDefault(n => n == "x"),
                })
        );
    }

    [Fact]
    public void OrdersAndFiltersGroupsOnFirstValues()
    {
        database.AssertSameSequence(
            Orders
                .GroupBy(o => o.CustomerId)
                .Where(g => g.OrderBy(o => o.Id).Select(o => o.Quantity).First() > 0)
                .OrderBy(
                    g => g.OrderByDescending(o => o.Id).Select(o => o.Number).First(),
                    StringComparer.Ordinal
                )
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void TakesGreatestAndLeastOfValues()
    {
        database.AssertSameRows(
            Orders.Select(o => new
            {
                o.Id,
                Most = new[] { o.Quantity, o.Priority ?? -1, 3 }.Max(),
                Least = new[] { o.Priority, null }.Min(),
                Nullable = new[] { o.Priority, o.Quantity }.Min(),
                Total = new[] { o.Total, o.Discount ?? 0 }.Max(),
                Day = new[] { o.ShippedOn, new DateOnly(2024, 6, 3) }.Max(),
                Time = new[] { o.DeliveryTime, new TimeOnly(12, 0) }.Min(),
                Paid = new[] { o.PaidAt, null }.Min(),
                Flag = new[] { o.IsPaid, o.IsGift ?? false }.Max(),
                Text = new[] { o.Number, o.Note, "a" }.Min(StringComparer.Ordinal),
                Top = new[] { o.Note, null }.Max(StringComparer.Ordinal),
            })
        );
    }

    [Fact]
    public void TakesOrdinalExtremesOfSequences()
    {
        database.AssertSameRows(
            Orders
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    g.Key,
                    Min = g.Select(o => o.Note).Min(StringComparer.Ordinal),
                    Max = g.Where(o => o.IsPaid)
                        .Select(o => o.Number)
                        .Max(StringComparer.Ordinal),
                })
        );
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                Max = Orders
                    .Where(o => o.CustomerId == c.Id)
                    .Select(o => o.Note)
                    .Max(StringComparer.Ordinal),
            })
        );
    }

    [Fact]
    public void NormalizesText()
    {
        database.AssertSameRows(
            Customers.Select(c => new
            {
                c.Id,
                C = c.Name.Normalize(),
                D = c.Name.Normalize(NormalizationForm.FormD),
                KC = c.Name.Normalize(NormalizationForm.FormKC),
                KD = c.Name.Normalize(NormalizationForm.FormKD),
                Same = c.Name.Normalize(NormalizationForm.FormD) == c.Name,
            })
        );
        database.AssertSameRows(
            Orders
                .Where(o => o.Note != null)
                .Select(o => new
                {
                    o.Id,
                    D = o.Note!.Normalize(NormalizationForm.FormD),
                    KC = o.Note!.Normalize(NormalizationForm.FormKC),
                })
        );
    }
}
