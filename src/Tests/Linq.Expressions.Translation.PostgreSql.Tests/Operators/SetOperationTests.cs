namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record SetOperationTests
{
    [Fact]
    public void ConcatsScalarSequences()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Products.Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                UNION ALL
                SELECT t1."Name"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsScalarSequences()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                UNION
                SELECT t1."Name"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IntersectsScalarSequences()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Intersect(Products.Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                INTERSECT
                SELECT t1."Name"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExceptsScalarSequences()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Except(Products.Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                EXCEPT
                SELECT t1."Name"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatsFilteredEntities()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Concat(Customers.Where(c => c.Region == "EU"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                UNION ALL
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM "Customer" AS t1
                WHERE t1."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsFilteredEntities()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Union(Customers.Where(c => c.Region == "EU"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                UNION
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM "Customer" AS t1
                WHERE t1."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IntersectsFilteredEntities()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Intersect(Customers.Where(c => c.Region == "EU"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                INTERSECT
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM "Customer" AS t1
                WHERE t1."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExceptsFilteredEntities()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Except(Customers.Where(c => c.Region == "EU"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                EXCEPT
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM "Customer" AS t1
                WHERE t1."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsAnonymousProjections()
    {
        Expression query = Customers
            .Select(c => new { c.Id, c.Name })
            .Union(Products.Select(p => new { p.Id, p.Name }))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name"
                FROM "Customer" AS t0
                UNION
                SELECT t1."Id", t1."Name"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatsProjectionsWithConstantDiscriminator()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Kind = "customer" })
            .Concat(Products.Select(p => new { p.Id, Kind = "product" }))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", 'customer' AS "Kind"
                FROM "Customer" AS t0
                UNION ALL
                SELECT t1."Id", 'product' AS "Kind"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatsEntitiesOfSameTable()
    {
        Expression query = Customers.Concat(Customers).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                UNION ALL
                SELECT t1."Id", t1."Name", t1."Email", t1."Region", t1."IsActive"
                FROM "Customer" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatsThreeSequences()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Products.Select(p => p.Name))
            .Concat(Employees.Select(e => e.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                UNION ALL
                SELECT t1."Name"
                FROM "Product" AS t1
                UNION ALL
                SELECT t2."Name"
                FROM "Employee" AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsThreeSequences()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Union(Employees.Select(e => e.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                UNION
                SELECT t1."Name"
                FROM "Product" AS t1
                UNION
                SELECT t2."Name"
                FROM "Employee" AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesLeftOperandOfDifferentSetOperation()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Intersect(Employees.Select(e => e.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Product" AS t1
                )
                INTERSECT
                SELECT t2."Name"
                FROM "Employee" AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesRightOperandThatIsSetOperation()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name).Intersect(Employees.Select(e => e.Name)))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                UNION
                (
                    SELECT t1."Name"
                    FROM "Product" AS t1
                    INTERSECT
                    SELECT t2."Name"
                    FROM "Employee" AS t2
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesOrderedAndLimitedOperand()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Take(3)
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    LIMIT 3
                )
                UNION
                SELECT t1."Name"
                FROM "Product" AS t1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesLimitedRightOperand()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Except(Products.OrderBy(p => p.Id).Take(2).Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                EXCEPT
                (
                    SELECT t1."Name"
                    FROM "Product" AS t1
                    ORDER BY t1."Id"
                    LIMIT 2
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesOffsetOperandsOnBothSides()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip(1)
            .Select(c => c.Name)
            .Concat(Products.OrderBy(p => p.Id).Skip(1).Select(p => p.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    ORDER BY t0."Id"
                    OFFSET 1
                )
                UNION ALL
                (
                    SELECT t1."Name"
                    FROM "Product" AS t1
                    ORDER BY t1."Id"
                    OFFSET 1
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsGroupedOperands()
    {
        Expression query = Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Key)
            .Union(OrderLines.GroupBy(l => l.OrderId).Select(g => g.Key))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."CustomerId"
                FROM "Order" AS t0
                GROUP BY t0."CustomerId"
                UNION
                SELECT t1."OrderId"
                FROM "OrderLine" AS t1
                GROUP BY t1."OrderId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsJoinedOperand()
    {
        Expression query = Customers
            .Join(Orders, c => c.Id, o => o.CustomerId, (c, o) => o.Id)
            .Union(OrderLines.Select(l => l.OrderId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t1."Id"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                UNION
                SELECT t2."OrderId"
                FROM "OrderLine" AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsFilterAfterUnion()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Where(n => n != "x")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                WHERE t2."Name" <> 'x'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsOrderingAfterUnion()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .OrderBy(n => n)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                ORDER BY t2."Name"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesConstantColumnOfUnionWithNullableSideNullSafe()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Note = (string?)"none" })
            .Union(Orders.Select(o => new { o.Id, o.Note }))
            .Where(x => x.Note != "x")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id", t2."Note"
                FROM (
                    SELECT t0."Id", 'none' AS "Note"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Id", t1."Note"
                    FROM "Order" AS t1
                ) AS t2
                WHERE t2."Note" IS DISTINCT FROM 'x'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesColumnOfUnionWithNullableLeftSideNullSafe()
    {
        Expression query = Orders
            .Select(o => o.Note)
            .Union(Customers.Select(c => (string?)c.Name))
            .Where(n => n != "x")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Note"
                FROM (
                    SELECT t0."Note"
                    FROM "Order" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Customer" AS t1
                ) AS t2
                WHERE t2."Note" IS DISTINCT FROM 'x'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DropsNullsOfUnionWithNullableRightSideForOfType()
    {
        Expression query = Customers
            .Select(c => (string?)c.Name)
            .Union(Orders.Select(o => o.Note))
            .OfType<string>()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Note"
                    FROM "Order" AS t1
                ) AS t2
                WHERE t2."Name" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesNullItemWithNullOfUnionWithNullableRightSide()
    {
        Expression query = Customers
            .Where(c =>
                Customers
                    .Select(x => (string?)x.Name)
                    .Union(Orders.Select(o => o.Note))
                    .Contains(c.Email)
            )
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE (t0."Email" IN (
                    SELECT t3."Name"
                    FROM (
                        SELECT t1."Name"
                        FROM "Customer" AS t1
                        UNION
                        SELECT t2."Note"
                        FROM "Order" AS t2
                    ) AS t3
                )) OR ((t0."Email" IS NULL) AND EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t4."Name"
                        FROM "Customer" AS t4
                        UNION
                        SELECT t5."Note"
                        FROM "Order" AS t5
                    ) AS t6
                    WHERE t6."Name" IS NULL
                ))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsPagingAfterConcat()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Products.Select(p => p.Name))
            .Take(10)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                LIMIT 10
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsOrderingAndPagingAfterConcat()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Products.Select(p => p.Name))
            .OrderBy(n => n)
            .Skip(5)
            .Take(10)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                ORDER BY t2."Name"
                LIMIT 10 OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsProjectionAfterUnion()
    {
        Expression query = Customers
            .Select(c => new { c.Id, c.Name })
            .Union(Products.Select(p => new { p.Id, p.Name }))
            .Select(x => x.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Id", t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Id", t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void WrapsDistinctAfterConcat()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Products.Select(p => p.Name))
            .Distinct()
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT DISTINCT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UnionsAfterWrappedOperand()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Where(n => n != "x")
            .Concat(Employees.Select(e => e.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                WHERE t2."Name" <> 'x'
                UNION ALL
                SELECT t3."Name"
                FROM "Employee" AS t3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void CountsUnionedRows()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .Executed(q => q.Count());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ChecksAnyUnionedRow()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Products.Select(p => p.Name))
            .Executed(q => q.Any());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM (
                        SELECT t0."Name"
                        FROM "Customer" AS t0
                        UNION ALL
                        SELECT t1."Name"
                        FROM "Product" AS t1
                    ) AS t2
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesFirstOfUnion()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Products.Select(p => p.Name))
            .OrderBy(n => n)
            .Executed(q => q.First());

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Name"
                    FROM "Product" AS t1
                ) AS t2
                ORDER BY t2."Name"
                LIMIT 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersComputedValuesAfterUnion()
    {
        Expression query = Orders
            .Select(o => o.Total * 2)
            .Union(Orders.Select(o => o.Total))
            .Where(x => x > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Value"
                FROM (
                    SELECT t0."Total" * 2 AS "Value"
                    FROM "Order" AS t0
                    UNION
                    SELECT t1."Total"
                    FROM "Order" AS t1
                ) AS t2
                WHERE t2."Value" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersComputedValuesAfterConcat()
    {
        Expression query = Orders
            .Select(o => o.Quantity + 1)
            .Concat(Orders.Select(o => o.Quantity))
            .OrderBy(x => x)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Value"
                FROM (
                    SELECT t0."Quantity" + 1 AS "Value"
                    FROM "Order" AS t0
                    UNION ALL
                    SELECT t1."Quantity"
                    FROM "Order" AS t1
                ) AS t2
                ORDER BY t2."Value"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NamesComputedValuesOfLeftmostOperandOfNestedSetOperations()
    {
        Expression query = Orders
            .Select(o => o.Quantity + 1)
            .Concat(Orders.Select(o => o.Quantity))
            .Union(Orders.Select(o => o.Quantity * 2))
            .Where(x => x > 1)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Value"
                FROM (
                    (
                        SELECT t0."Quantity" + 1 AS "Value"
                        FROM "Order" AS t0
                        UNION ALL
                        SELECT t1."Quantity"
                        FROM "Order" AS t1
                    )
                    UNION
                    SELECT t2."Quantity" * 2
                    FROM "Order" AS t2
                ) AS t3
                WHERE t3."Value" > 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesColumnNullableOnTheRightSideNullSafe()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Customers.Select(c => c.Email))
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Email"
                    FROM "Customer" AS t1
                ) AS t2
                WHERE t2."Name" IS DISTINCT FROM 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesComputedColumnNullableOnTheRightSideNullSafe()
    {
        Expression query = Customers
            .Select(c => c.Name.ToUpper())
            .Concat(Customers.Select(c => c.Email))
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Value"
                FROM (
                    SELECT UPPER(t0."Name") AS "Value"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Email"
                    FROM "Customer" AS t1
                ) AS t2
                WHERE t2."Value" IS DISTINCT FROM 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesColumnNotNullableOnEitherSideDirectly()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Concat(Customers.Select(c => c.Region))
            .Where(n => n != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION ALL
                    SELECT t1."Region"
                    FROM "Customer" AS t1
                ) AS t2
                WHERE t2."Name" <> 'a'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MergesNullabilityOfProjectionMembersByPosition()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Text = (string?)c.Name })
            .Union(Customers.Select(c => new { c.Id, Text = c.Email }))
            .Where(x => x.Id != 1 && x.Text != "a")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id", t2."Text"
                FROM (
                    SELECT t0."Id", t0."Name" AS "Text"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Id", t1."Email" AS "Text"
                    FROM "Customer" AS t1
                ) AS t2
                WHERE (t2."Id" <> 1) AND (t2."Text" IS DISTINCT FROM 'a')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersNullsFirstByColumnNullableOnTheRightSide()
    {
        Expression query = Customers
            .Select(c => c.Name)
            .Union(Customers.Select(c => c.Email))
            .OrderBy(n => n)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Name"
                FROM (
                    SELECT t0."Name"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Email"
                    FROM "Customer" AS t1
                ) AS t2
                ORDER BY t2."Name" NULLS FIRST
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
