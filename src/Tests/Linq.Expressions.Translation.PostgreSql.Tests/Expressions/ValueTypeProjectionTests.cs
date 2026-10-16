namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record ValueTypeProjectionTests
{
    public struct Point
    {
        public int X { get; set; }

        public string Y { get; set; }
    }

    public sealed class Renamed(int key)
    {
        public int Id { get; } = key;
    }

    [Fact]
    public void ProjectsValueTupleInitializer()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, string> { Item1 = o.Id, Item2 = o.Number })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Item1", t0."Number" AS "Item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByValueTupleInitializerMember()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, string> { Item1 = o.Id, Item2 = o.Number })
            .Where(t => t.Item1 > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Item1", t0."Number" AS "Item2"
                FROM "Order" AS t0
                WHERE t0."Id" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByStructInitializerMember()
    {
        Expression query = Orders
            .Select(o => new Point { X = o.Id, Y = o.Number })
            .OrderBy(p => p.Y)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "X", t0."Number" AS "Y"
                FROM "Order" AS t0
                ORDER BY t0."Number"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsValueTupleConstructor()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, string>(o.Id, o.Number))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", t0."Number" AS "item2"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByValueTupleConstructorMember()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, string>(o.Id, o.Number))
            .Where(t => t.Item1 > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", t0."Number" AS "item2"
                FROM "Order" AS t0
                WHERE t0."Id" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByTupleConstructorMember()
    {
        Expression query = Orders
            .Select(o => new Tuple<int, string>(o.Id, o.Number))
            .OrderBy(t => t.Item2)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", t0."Number" AS "item2"
                FROM "Order" AS t0
                ORDER BY t0."Number"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PrefersMemberWithExactName()
    {
        Expression query = Orders
            .Select(o => new { a = o.Id, A = o.Quantity })
            .Where(x => x.A > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "a", t0."Quantity" AS "A"
                FROM "Order" AS t0
                WHERE t0."Quantity" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReplacesConstructorArgumentWithInitializer()
    {
        Expression query = Customers
            .Select(c => new CustomerSummary(c.Id, c.Name) { Name = c.Region })
            .Where(s => s.Name == "EU")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Region" AS "Name"
                FROM "Customer" AS t0
                WHERE t0."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReplacesValueTupleConstructorArgumentsWithInitializers()
    {
        Expression query = Orders
            .Select(o => new ValueTuple<int, string?, int>(o.Id, o.Number, o.Quantity)
            {
                Item2 = o.Note,
                Item3 = o.CustomerId,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "item1", t0."Note" AS "Item2", t0."CustomerId" AS "Item3"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RejectsMemberNotPassedByName()
    {
        Expression query = Orders
            .Select(o => new Renamed(o.Id))
            .Where(r => r.Id > 1)
            .Expression;

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
        Assert.Contains("'Renamed.Id'", exception.Message, StringComparison.Ordinal);
    }
}
