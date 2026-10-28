namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

[Collection(SharedDatabase.Name)]
public sealed class KeyedOperatorSemanticsTests(Database database)
{
    [Fact]
    public void ExceptsByKeyMatchingNullWithNullOfSubquery()
    {
        IQueryable<string?> emails = Customers.Select(c => c.Email);
        IQueryable<string?> present = Customers
            .Where(c => c.Email != null)
            .Select(c => c.Email);

        database.AssertSameRows(Orders.ExceptBy(emails, o => o.Note).Select(o => o.Note));
        database.AssertSameRows(
            Orders.ExceptBy(present, o => o.Note).Select(o => o.Note)
        );
    }

    [Fact]
    public void ExceptsByKeyMatchingNullWithNullOfCollection()
    {
        string?[] withNull = ["orphan", null];
        string?[] withoutNull = ["orphan"];

        database.AssertSameRows(
            Orders.ExceptBy(withNull, o => o.Note).Select(o => o.Note)
        );
        database.AssertSameRows(
            Orders.ExceptBy(withoutNull, o => o.Note).Select(o => o.Note)
        );
    }

    [Fact]
    public void IntersectsByKeyMatchingNullWithNull()
    {
        int?[] firstOrNone = [1, null];

        database.AssertSameRows(
            Orders
                .IntersectBy(Customers.Select(c => c.Email), o => o.Note)
                .Select(o => o.Note)
        );
        database.AssertSameRows(
            Orders.IntersectBy(firstOrNone, o => o.Priority).Select(o => o.Priority)
        );
        database.AssertSameRows(
            Customers
                .IntersectBy(Orders.Select(o => o.CustomerId), c => c.Id)
                .Select(c => c.Id)
        );
    }

    [Fact]
    public void UnionsByKeyKeepingElementOfFirstSequence()
    {
        database.AssertSameRows(
            Customers
                .Where(c => !c.IsActive)
                .UnionBy(Customers.Where(c => c.IsActive), c => c.Region)
                .Select(c => new { c.Region, c.IsActive })
        );
        database.AssertSameRows(
            Orders
                .Select(o => new { o.CustomerId, Side = 1 })
                .UnionBy(
                    Customers.Select(c => new { CustomerId = c.Id, Side = 2 }),
                    x => x.CustomerId
                )
        );
    }

    [Fact]
    public void TakesExtremesByKeySkippingNullKeys()
    {
        IQueryable<Order> byId = Orders.OrderBy(o => o.Id);

        database.AssertSameResult(Orders, q => q.MinBy(o => o.Discount));
        database.AssertSameResult(Orders, q => q.MaxBy(o => o.Discount));
        database.AssertSameResult(byId, q => q.MinBy(o => o.Priority));
        database.AssertSameResult(byId, q => q.MaxBy(o => o.Priority));
    }

    [Fact]
    public void TakesFirstElementWhenEveryKeyIsNull()
    {
        IQueryable<Order> unprioritized = Orders
            .Where(o => o.Priority == null)
            .OrderBy(o => o.Id);

        database.AssertSameResult(unprioritized, q => q.MinBy(o => o.Priority));
        database.AssertSameResult(unprioritized, q => q.MaxBy(o => o.Priority));
    }

    [Fact]
    public void TakesNoExtremeOfNoElements()
    {
        IQueryable<Order> none = Orders.Where(o => o.Id < 0);

        database.AssertSameResult(none, q => q.MinBy(o => o.Total));
        database.AssertSameResult(none.Select(o => o.Total), q => q.MaxBy(t => t));
    }

    [Fact]
    public void BreaksKeyTiesBySourceOrder()
    {
        IQueryable<Order> byId = Orders.OrderBy(o => o.Id);

        database.AssertSameResult(
            Orders.Where(o => o.CustomerId == 1),
            q => q.MaxBy(o => o.CreatedAt)
        );
        database.AssertSameResult(
            byId,
            q => q.MinBy(o => new ValueTuple<int?, decimal>(o.Priority, o.Total))
        );
        database.AssertSameResult(
            byId,
            q => q.MaxBy(o => new ValueTuple<int?, decimal>(o.Priority, o.Total))
        );
    }

    [Fact]
    public void ComparesStringKeysOrdinally()
    {
        database.AssertSameResult(
            Customers,
            q => q.MinBy(c => c.Name, StringComparer.Ordinal)
        );
        database.AssertSameResult(
            Customers,
            q => q.MaxBy(c => c.Name, StringComparer.Ordinal)
        );
    }
}
