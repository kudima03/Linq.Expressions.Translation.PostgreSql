namespace Linq.Expressions.Translation.PostgreSql.ExecutionTests;

// The order LINQ to Objects keeps: a later OrderBy sorts stably, and Distinct,
// GroupBy and Concat yield first occurrences, groups and sides in the order of their
// sources.
[Collection(SharedDatabase.Name)]
public sealed class OrderingSemanticsTests(Database database)
{
    [Fact]
    public void KeepsEarlierOrderAfterThenByKeys()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.CustomerId)
                .Where(o => o.Quantity < 10)
                .Select(o => new { o.Id, o.Quantity })
                .OrderBy(x => x.Quantity)
                .ThenByDescending(x => x.Id)
        );
    }

    [Fact]
    public void KeepsPagedOrderAfterThenByKeys()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.CustomerId)
                .Take(12)
                .Select(o => new { o.Id, o.Quantity })
                .OrderBy(x => x.Quantity)
                .ThenByDescending(x => x.Id)
        );
    }

    [Fact]
    public void KeepsFirstOccurrenceOrderOfDistinctValues()
    {
        database.AssertSameSequence(
            Customers.OrderByDescending(c => c.Id).Select(c => c.Region).Distinct()
        );
    }

    [Fact]
    public void KeepsFirstOccurrenceOrderOfDistinctValuesOfPagedSource()
    {
        database.AssertSameSequence(
            Customers.OrderBy(c => c.Id).Skip(2).Select(c => c.Region).Distinct()
        );
    }

    [Fact]
    public void KeepsFirstOccurrenceOrderOfDistinctProjections()
    {
        database.AssertSameSequence(
            Orders
                .OrderByDescending(o => o.Id)
                .Select(o => new { o.CustomerId, o.IsPaid })
                .Distinct()
        );
    }

    [Fact]
    public void PagesDistinctValuesInFirstOccurrenceOrder()
    {
        database.AssertSameSequence(
            Customers
                .OrderByDescending(c => c.Id)
                .Select(c => c.Region)
                .Distinct()
                .Take(2)
        );
    }

    [Fact]
    public void ReversesDistinctValuesInFirstOccurrenceOrder()
    {
        database.AssertSameSequence(
            Orders.OrderBy(o => o.Id).Select(o => o.CustomerId).Distinct().Reverse()
        );
    }

    [Fact]
    public void KeepsOrderOfDistinctValuesOrderedByThemselves()
    {
        database.AssertSameSequence(
            Customers
                .OrderBy(c => c.Region)
                .ThenBy(c => c.Id)
                .Select(c => c.Region)
                .Distinct()
        );
    }

    [Fact]
    public void KeepsFirstOccurrenceOrderOfGroups()
    {
        database.AssertSameSequence(
            Orders
                .OrderByDescending(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .Select(g => new { g.Key, Count = g.Count() })
        );
    }

    [Fact]
    public void KeepsFirstOccurrenceOrderOfGroupsByNullableKey()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Priority)
                .ThenBy(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void KeepsFirstOccurrenceOrderOfGroupsBySeveralKeys()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Quantity)
                .ThenByDescending(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void KeepsFirstOccurrenceOrderOfGroupsOfPagedSource()
    {
        database.AssertSameSequence(
            Orders
                .OrderByDescending(o => o.Id)
                .Select(o => new { o.CustomerId, o.Quantity })
                .Take(6)
                .GroupBy(x => x.CustomerId)
                .Select(g => new { g.Key, Quantity = g.Sum(x => x.Quantity) })
        );
    }

    [Fact]
    public void KeepsOrderOfGroupsOrderedByTheirKey()
    {
        database.AssertSameSequence(
            Orders
                .OrderByDescending(o => o.CustomerId)
                .GroupBy(o => o.CustomerId)
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void SortsGroupsBeforeFirstOccurrenceOrder()
    {
        database.AssertSameSequence(
            Orders
                .OrderBy(o => o.Id)
                .GroupBy(o => o.CustomerId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
        );
    }

    [Fact]
    public void KeepsOrderOfBothSidesOfConcat()
    {
        database.AssertSameSequence(
            Customers
                .OrderByDescending(c => c.Id)
                .Select(c => c.Id)
                .Concat(Orders.OrderBy(o => o.Id).Select(o => o.Id))
        );
    }

    [Fact]
    public void KeepsOrderOfPagedSidesOfConcat()
    {
        database.AssertSameSequence(
            Customers
                .OrderBy(c => c.Id)
                .Skip(10)
                .Select(c => c.Name)
                .Concat(
                    Employees.OrderByDescending(e => e.Id).Skip(4).Select(e => e.Name)
                )
        );
    }

    [Fact]
    public void KeepsOrderOfSecondSideOfConcat()
    {
        database.AssertSameSequence(
            Customers
                .Where(c => c.Id == 1)
                .Select(c => c.Name)
                .Concat(
                    Employees.OrderBy(e => e.Name).ThenBy(e => e.Id).Select(e => e.Name)
                )
        );
    }

    [Fact]
    public void KeepsOrderOfOrderedSideAfterUnorderedConcat()
    {
        database.AssertSameSequence(
            Customers
                .Where(c => c.Id == 1)
                .Select(c => c.Id)
                .Concat(Employees.Where(e => e.Id == 7).Select(e => e.Id))
                .Concat(Orders.OrderByDescending(o => o.Id).Select(o => o.Id))
        );
    }

    [Fact]
    public void MakesValuesOfUnionByDistinct()
    {
        database.AssertSameRows(
            Customers
                .Select(c => new { c.Region, c.Id })
                .UnionBy(Employees.Select(e => new { Region = e.Name, e.Id }), x => x.Id)
                .Select(x => x.Region)
                .Distinct()
        );
    }
}
