namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The overloads of the Queryable operators beside their common forms: DefaultIfEmpty
// as a sequence operator, the element operators with a default value and ElementAt
// of an Index counted from the end.
[Collection(SharedDatabase.Name)]
public sealed class OperatorOverloadSemanticsTests(Database database)
{
    // #141: DefaultIfEmpty of a non-empty query is the query.
    [Fact]
    public void KeepsRowsOfNonEmptyQueryThroughDefaultIfEmpty()
    {
        database.AssertSameRows(Orders.Select(o => o.Quantity).DefaultIfEmpty());
        database.AssertSameRows(Orders.Select(o => o.Note).DefaultIfEmpty("none"));
    }

    // #141: DefaultIfEmpty of an empty query is one default element.
    [Fact]
    public void YieldsDefaultOfEmptyQuery()
    {
        database.AssertSameRows(
            Orders.Where(o => o.Id > 100).Select(o => o.Quantity).DefaultIfEmpty()
        );
        database.AssertSameRows(
            Orders.Where(o => o.Id > 100).Select(o => o.Quantity).DefaultIfEmpty(-1)
        );
        database.AssertSameRows(
            Orders.Where(o => o.Id > 100).Select(o => o.Priority).DefaultIfEmpty()
        );
        database.AssertSameRows(
            Customers.Where(c => c.Id > 100).Select(c => c.Name).DefaultIfEmpty("none")
        );
    }

    // #141: the operators after DefaultIfEmpty see its default element.
    [Fact]
    public void ComposesAfterDefaultIfEmpty()
    {
        database.AssertSameRows(
            Orders
                .Where(o => o.Id > 100)
                .Select(o => o.Quantity)
                .DefaultIfEmpty(-1)
                .Select(q => q * 2)
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 100).Select(o => o.Quantity).DefaultIfEmpty(),
            q => q.Count()
        );
    }

    // #141: DefaultIfEmpty as the source of Over.
    [Fact]
    public void WindowsDefaultIfEmpty()
    {
        database.AssertSameRows(
            Trees.Over(
                Orders.Where(o => o.Id > 100).Select(o => o.Quantity).DefaultIfEmpty(),
                null,
                null,
                null,
                (q, w) => new { Quantity = q, Count = w.Rows.Count() }
            )
        );
        database.AssertSameRows(
            Trees.Over(
                Orders.Select(o => o.Quantity).DefaultIfEmpty(-1),
                null,
                rows => rows.OrderBy(q => q),
                null,
                (q, w) => new { Quantity = q, Number = w.RowNumber() }
            )
        );
    }

    // #141: FirstOrDefault(source, defaultValue) is the first element of a non-empty
    // query, also where that element is NULL, and the given default of an empty one.
    [Fact]
    public void TakesFirstOrGivenDefault()
    {
        database.AssertSameResult(
            Orders.OrderBy(o => o.Id).Select(o => o.Quantity),
            q => q.FirstOrDefault(-1)
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 100).Select(o => o.Quantity),
            q => q.FirstOrDefault(-1)
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id == 2).Select(o => o.Note),
            q => q.FirstOrDefault("none")
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 100).Select(o => o.Note),
            q => q.FirstOrDefault("none")
        );
    }

    // #141: LastOrDefault(source, defaultValue) of an ordered query.
    [Fact]
    public void TakesLastOrGivenDefault()
    {
        database.AssertSameResult(
            Orders.OrderBy(o => o.Id).Select(o => o.Quantity),
            q => q.LastOrDefault(-1)
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 100).OrderBy(o => o.Id).Select(o => o.Quantity),
            q => q.LastOrDefault(-1)
        );
        database.AssertSameResult(
            Orders.OrderByDescending(o => o.Id).Select(o => o.Note),
            q => q.LastOrDefault("none")
        );
    }

    // #141: SingleOrDefault(source, defaultValue) is the only element, the default of
    // no element, and throws for more than one.
    [Fact]
    public void TakesSingleOrGivenDefault()
    {
        database.AssertSameResult(
            Orders.Where(o => o.Id == 3).Select(o => o.Quantity),
            q => q.SingleOrDefault(-1)
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 100).Select(o => o.Quantity),
            q => q.SingleOrDefault(-1)
        );
        database.AssertSameResult(
            Orders.Where(o => o.Id > 10).Select(o => o.Quantity),
            q => q.SingleOrDefault(-1)
        );
    }

    // #141: ElementAt(^n) is the n-th element from the end; out of range it throws.
    [Fact]
    public void TakesElementFromEnd()
    {
        database.AssertSameResult(
            Orders.OrderBy(o => o.Id).Select(o => o.Id),
            q => q.ElementAt(^2)
        );
        database.AssertSameResult(
            Orders.OrderByDescending(o => o.Quantity).ThenBy(o => o.Id).Select(o => o.Id),
            q => q.ElementAt(^1)
        );
        database.AssertSameResult(
            Orders.OrderBy(o => o.Id).Select(o => o.Id),
            q => q.ElementAt(^13)
        );
    }

    // #141: ElementAtOrDefault(^n) is the default where n is beyond the count.
    [Fact]
    public void TakesElementFromEndOrDefault()
    {
        database.AssertSameResult(
            Orders.OrderBy(o => o.Id).Select(o => o.Id),
            q => q.ElementAtOrDefault(^3)
        );
        database.AssertSameResult(
            Orders.OrderBy(o => o.Id).Select(o => o.Id),
            q => q.ElementAtOrDefault(^13)
        );
    }
}
