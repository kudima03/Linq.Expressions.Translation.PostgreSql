#pragma warning disable CA1861 // constant arrays are the subject
#pragma warning disable IDE0300 // the collection types are the subject
#pragma warning disable IDE0028 // the collection types are the subject
#pragma warning disable IDE0120 // the operator order is the subject

namespace Linq.Expressions.Translation.PostgreSql.Tests.Unsupported;

public sealed record UnsupportedOperatorTests
{
    private static int Helper(int value)
    {
        return value;
    }

    public sealed class Bucket
    {
        public int Key { get; init; }

        public IEnumerable<Order> Items { get; init; } = [];
    }

    [Fact]
    public void RejectsGroupByReturningGroupings()
    {
        Expression query = Customers.GroupBy(c => c.Region).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GroupBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGroupByWithElementSelectorReturningGroupings()
    {
        Expression query = Customers.GroupBy(c => c.Region, c => c.Name).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GroupBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsFirstElementOfGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.First())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("First", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMaterializingGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.ToList())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ToList", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectingTheGroupItself()
    {
        Expression query = Orders.GroupBy(o => o.CustomerId).Select(g => g).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("IGrouping", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsLastWithoutOrdering()
    {
        Expression query = Customers.Executed(q => q.Last());

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Last", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsLastOrDefaultWithoutOrdering()
    {
        Expression query = Customers.Executed(q => q.LastOrDefault());

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("LastOrDefault", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsLastWithPredicateWithoutOrdering()
    {
        Expression query = Customers.Executed(q => q.Last(c => c.IsActive));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Last", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsReverseWithoutOrdering()
    {
        Expression query = Customers.Reverse().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Reverse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsReverseAfterProjectionWithoutOrdering()
    {
        Expression query = Customers.Select(c => c.Id).Reverse().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Reverse", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsZip()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Zip(Products.Select(p => p.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Zip", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsZipWithResultSelector()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Zip(Products.Select(p => p.Id), (a, b) => a + b)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Zip", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAggregate()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Executed(q => q.Aggregate((a, b) => a + b));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Aggregate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAggregateWithSeed()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Executed(q => q.Aggregate(0, (a, b) => a + b));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Aggregate", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSkipWhile()
    {
        Expression query = Customers.SkipWhile(c => c.IsActive).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SkipWhile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTakeWhile()
    {
        Expression query = Customers.TakeWhile(c => c.IsActive).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("TakeWhile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsPrepend()
    {
        Expression query = Customers.Select(c => c.Id).Prepend(0).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Prepend", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAppend()
    {
        Expression query = Customers.Select(c => c.Id).Append(0).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Append", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDistinctBy()
    {
        Expression query = Customers.DistinctBy(c => c.Region).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DistinctBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnionBy()
    {
        Expression query = Customers.UnionBy(Customers, c => c.Region).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("UnionBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsIntersectBy()
    {
        Expression query = Customers
            .IntersectBy(Customers.Select(c => c.Region), c => c.Region)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("IntersectBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsExceptBy()
    {
        Expression query = Customers
            .ExceptBy(Customers.Select(c => c.Region), c => c.Region)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ExceptBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMinBy()
    {
        Expression query = Customers.Executed(q => q.MinBy(c => c.Id));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("MinBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMaxBy()
    {
        Expression query = Customers.Executed(q => q.MaxBy(c => c.Id));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("MaxBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsWhereWithIndex()
    {
        Expression query = Customers.Where((c, i) => i > 1).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Where", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectWithIndex()
    {
        Expression query = Customers.Select((c, i) => i).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Select", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectManyWithIndex()
    {
        Expression query = Customers
            .SelectMany((c, i) => Orders.Where(o => o.CustomerId == c.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SelectMany", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSkipWhileWithIndex()
    {
        Expression query = Customers.SkipWhile((c, i) => i < 2).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SkipWhile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTakeWithRange()
    {
        Expression query = Customers.Take(1..3).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Take", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDistinctWithComparer()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Distinct", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnionWithComparer()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name), StringComparer.OrdinalIgnoreCase)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Union", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsContainsWithComparer()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Executed(q => q.Contains("a", StringComparer.OrdinalIgnoreCase));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Contains", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsJoinWithComparer()
    {
        Expression query = Customers
            .Join(
                Products,
                c => c.Name,
                p => p.Name,
                (c, p) => c.Id,
                StringComparer.OrdinalIgnoreCase
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Join", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGroupByWithComparer()
    {
        Expression query = Customers
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GroupBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsFirstOrDefaultWithDefaultValue()
    {
        Expression query = Customers.Executed(q => q.FirstOrDefault(new Customer()));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("FirstOrDefault", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSingleOrDefaultWithDefaultValue()
    {
        Expression query = Customers.Executed(q => q.SingleOrDefault(new Customer()));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SingleOrDefault", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsElementAtFromEnd()
    {
        Expression query = Customers.OrderBy(c => c.Id).Executed(q => q.ElementAt(^1));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ElementAt", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDefaultIfEmptyOnRoot()
    {
        Expression query = Customers.DefaultIfEmpty().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DefaultIfEmpty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDefaultIfEmptyWithDefaultValue()
    {
        Expression query = Customers.DefaultIfEmpty(new Customer()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DefaultIfEmpty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSequenceEqual()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Executed(q => q.SequenceEqual(new[] { 1, 2 }));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SequenceEqual", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectManyOverString()
    {
        Expression query = Customers.SelectMany(c => c.Name).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SelectMany", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectManyOverArrayColumnWithoutUnnestSupport()
    {
        Expression query = Orders
            .SelectMany(o => o.Tags.Select(t => t.Length))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SelectMany", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsInMemoryRoot()
    {
        Expression query = new[] { 1, 2, 3 }.AsQueryable().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsFilterOverInMemoryRoot()
    {
        Expression query = new[] { 1, 2, 3 }.AsQueryable().Where(i => i > 1).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnionWithInMemoryCollection()
    {
        Expression query = Customers.Select(c => c.Id).Union(new[] { 1, 2 }).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Union", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsConcatWithInMemoryCollection()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Concat(new List<int> { 1, 2 })
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Concat", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsJoinWithInMemoryCollection()
    {
        Expression query = Customers
            .Join(new[] { 1, 2 }, c => c.Id, i => i, (c, i) => c.Name)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Join", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsIntersectWithInMemoryCollection()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Intersect(new[] { 1, 2 })
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Intersect", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsExceptWithInMemoryCollection()
    {
        Expression query = Customers.Select(c => c.Id).Except(new[] { 1, 2 }).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Except", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsInMemoryCollectionAsSubquerySource()
    {
        Expression query = Customers
            .Where(c => new[] { 1, 2 }.AsQueryable().Any(i => i == c.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("EnumerableQuery", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsOrderByWithComparer()
    {
        Expression query = Customers
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("OrderBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsThenByWithComparer()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("ThenBy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnsupportedOperatorAfterSupportedChain()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Take(5)
            .SkipWhile(c => c.IsActive)
            .Select(c => c.Name)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("SkipWhile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnsupportedExpressionAfterSupportedChain()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => Helper(c.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Helper", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnsupportedExpressionDeepInsideNestedSubqueries()
    {
        Expression query = Customers
            .Where(c =>
                Orders.Any(o =>
                    (o.CustomerId == c.Id) && OrderLines.Any(l => Helper(l.OrderId) > 0)
                )
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Helper", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnsupportedExpressionInsideSetOperationOperand()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Union(Products.Select(p => Helper(p.Id)))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Helper", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsUnsupportedExpressionInsideJoinedSource()
    {
        Expression query = Customers
            .Join(
                Orders.Where(o => Helper(o.Id) > 0),
                c => c.Id,
                o => o.CustomerId,
                (c, o) => o.Id
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Helper", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectorAfterDefaultIfEmpty()
    {
        Expression query = Orders
            .Select(o => o.Total)
            .DefaultIfEmpty()
            .Executed(q => q.Max(t => t * 2));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DefaultIfEmpty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsElementAfterDefaultIfEmpty()
    {
        Expression query = Orders
            .Select(o => o.Total)
            .DefaultIfEmpty()
            .Executed(q => q.First());

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DefaultIfEmpty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDefaultIfEmptyValueBeforeAggregate()
    {
        Expression query = Orders
            .Select(o => o.Total)
            .DefaultIfEmpty(5m)
            .Executed(q => q.Max());

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("DefaultIfEmpty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSumWithSelectorOverDistinctGroupElements()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Distinct().Sum(o => o.Total))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Sum", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAverageWithSelectorOverDistinctGroupElements()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Distinct().Average(o => o.Quantity))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Average", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCountingDistinctEntitiesOfGroup()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Distinct().Count())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("single value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMaximumOfGroupEntities()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Max())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("single value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMaximumOfEntities()
    {
        Expression query = Orders.Executed(q => q.Max());

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("single value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMinimumOfProjections()
    {
        Expression query = Orders
            .Select(o => new { o.Id, o.Total })
            .Executed(q => q.Min());

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("single value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCollectionValuedProjection()
    {
        Expression query = Customers
            .Select(c => Orders.Where(o => o.CustomerId == c.Id))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Where", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsProjectingGroupJoinGroupElements()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, g) => new { c, g })
            .Select(x => x.g.Select(o => o.Total))
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Select", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsEntityValuedSubquery()
    {
        Expression query = Customers.Select(c => Orders.First()).Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(
            "single scalar value",
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RejectsProjectionValuedSubquery()
    {
        Expression query = Customers
            .Select(c => Orders.Select(o => new { o.Id, o.Total }).First())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(
            "single scalar value",
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RejectsFirstEntityOfGroupJoinGroup()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, g) => new { c, g })
            .Select(x => x.g.First())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains(
            "single scalar value",
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void RejectsContainsOfEntity()
    {
        Order order = new();
        Expression query = Orders.Executed(q => q.Contains(order));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("single value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsContainsOfProjection()
    {
        Expression query = Orders
            .Select(o => new { o.Id })
            .Executed(q => q.Contains(new { Id = 1 }));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("single value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsNullCheckOfLeftJoinedEntity()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, g) => new { c, g })
            .SelectMany(x => x.g.DefaultIfEmpty(), (x, o) => o == null ? 0m : o.Total)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("single value", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsExpressionThatIsNotAQuery()
    {
        Expression query = Expression.Constant(5);

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Constant", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectingGroupJoinGroup()
    {
        Expression query = Customers
            .GroupJoin(Orders, c => c.Id, o => o.CustomerId, (c, g) => g)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GroupJoin", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsLeftJoiningGroupJoinGroup()
    {
        Expression query = Customers
            .SelectMany(
                c =>
                    Customers
                        .GroupJoin(
                            Orders,
                            x => x.Id,
                            o => o.CustomerId,
                            (x, g) => new { x, g }
                        )
                        .DefaultIfEmpty(),
                (c, y) => c.Name
            )
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("GroupJoin", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGroupNestedInProjection()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Items = g })
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("IGrouping", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGroupElementsNestedInProjection()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Totals = g.Select(o => o.Total) })
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("IGrouping", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsGroupAssignedInInitializer()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new Bucket { Key = g.Key, Items = g })
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("IGrouping", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsCastOfEntitiesToAnotherEntityType()
    {
        Expression query = Customers.Cast<Employee>().Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Employee", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsMaximumOfGuid()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.TrackingId));

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Max", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsSelectAfterDistinctOverGroupElements()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Distinct().Select(o => o.Total).Sum())
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("Distinct", exception.Message, StringComparison.Ordinal);
    }
}
