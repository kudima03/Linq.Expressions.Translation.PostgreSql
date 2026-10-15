namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record ElementOperatorTests
{
    [Fact]
    public void TakesFirstRow()
    {
        Expression query = Customers.Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowMatchingPredicate()
    {
        Expression query = Customers.Executed(q => q.First(c => c.Id == 5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" = 5
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowAfterFilterAndOrdering()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Name"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowOfProjection()
    {
        Expression query = Customers.Select(c => c.Name).Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstOrDefaultRow()
    {
        Expression query = Customers.Executed(q => q.FirstOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstOrDefaultRowMatchingPredicate()
    {
        Expression query = Customers.Executed(q => q.FirstOrDefault(c => c.Id == 5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" = 5
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstOrDefaultRowAfterFilterAndOrdering()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Executed(q => q.FirstOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Name"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstOrDefaultRowOfProjection()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Executed(q => q.FirstOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleRow()
    {
        Expression query = Customers.Executed(q => q.Single());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleRowMatchingPredicate()
    {
        Expression query = Customers.Executed(q => q.Single(c => c.Id == 5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" = 5
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleRowAfterFilterAndOrdering()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Executed(q => q.Single());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Name"
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleRowOfProjection()
    {
        Expression query = Customers.Select(c => c.Name).Executed(q => q.Single());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleOrDefaultRow()
    {
        Expression query = Customers.Executed(q => q.SingleOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleOrDefaultRowMatchingPredicate()
    {
        Expression query = Customers.Executed(q => q.SingleOrDefault(c => c.Id == 5));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" = 5
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleOrDefaultRowAfterFilterAndOrdering()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Executed(q => q.SingleOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Name"
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSingleOrDefaultRowOfProjection()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Executed(q => q.SingleOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                LIMIT 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowCombinedWithFilterAndPredicate()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Executed(q => q.First(c => c.Region == "EU"));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive" AND (t0."Region" = 'EU')
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowOfCapturedPredicate()
    {
        int wanted = 7;

        Expression query = Customers.Executed(q => q.First(c => c.Id == wanted));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" = 7
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowSkippingRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).Skip(3).Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowOfTake()
    {
        Expression query = Customers.OrderBy(c => c.Id).Take(5).Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowOfDistinct()
    {
        Expression query = Customers
            .Select(c => c.Region)
            .Distinct()
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t0."Region"
                FROM "Customer" AS t0
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void TakesFirstRowOfGroups()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId", CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                ORDER BY CAST(COUNT(*) AS integer) DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void TakesFirstRowOfJoin()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => new { c.Name, o.Total })
            .OrderByDescending(x => x.Total)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t1."Total"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                ORDER BY t1."Total" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstRowAfterWrappedOperator()
    {
        Expression query = Customers
            .Take(5)
            .Where(c => c.IsActive)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    LIMIT 5
                ) AS t1
                WHERE t1."IsActive"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowOfOrderedRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastOrDefaultRowOfOrderedRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).Executed(q => q.LastOrDefault());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowMatchingPredicate()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Executed(q => q.Last(c => c.IsActive));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Id" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InvertsEveryKeyForLast()
    {
        Expression query = Customers
            .OrderBy(c => c.Region)
            .ThenByDescending(c => c.Name)
            .Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Region" DESC, t0."Name"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void InvertsNullablePlacementForLast()
    {
        Expression query = Customers.OrderBy(c => c.Email).Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Email" DESC NULLS LAST
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowOfDescendingRows()
    {
        Expression query = Customers.OrderByDescending(c => c.Id).Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowOfProjection()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Select(c => c.Name)
            .Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY t0."Id" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowOfReversedRows()
    {
        Expression query = Customers.OrderBy(c => c.Id).Reverse().Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesLastRowOfTake()
    {
        Expression query = Customers.OrderBy(c => c.Id).Take(5).Executed(q => q.Last());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM (
                    SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 5
                ) AS t1
                ORDER BY t1."Id" DESC
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesElementAtPosition()
    {
        Expression query = Customers.OrderBy(c => c.Id).Executed(q => q.ElementAt(4));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesElementAtOrDefaultPosition()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Executed(q => q.ElementAtOrDefault(4));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 4
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesElementAtPositionWithoutOrdering()
    {
        Expression query = Customers.Executed(q => q.ElementAt(0));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                LIMIT 1 OFFSET 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesElementAtCapturedPosition()
    {
        int index = 9;

        Expression query = Customers.OrderBy(c => c.Id).Executed(q => q.ElementAt(index));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 9
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesElementAtPositionAfterSkip()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip(2)
            .Executed(q => q.ElementAt(3));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesElementAtPositionOfProjection()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Select(c => c.Name)
            .Executed(q => q.ElementAt(2));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesElementAtPositionOfFilteredRows()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Executed(q => q.ElementAt(1));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
