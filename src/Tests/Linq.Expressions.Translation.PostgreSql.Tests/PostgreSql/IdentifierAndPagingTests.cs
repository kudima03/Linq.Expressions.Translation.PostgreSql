using System.ComponentModel.DataAnnotations.Schema;

namespace Linq.Expressions.Translation.PostgreSql.Tests.PostgreSql;

public sealed record IdentifierAndPagingTests
{
    [Table("Order Items", Schema = "sales")]
    public sealed class QuotedRow
    {
        [Column("Quoted \"Name\"")]
        public string Name { get; init; } = string.Empty;

        [Column("select")]
        public int Select { get; init; }

        public int Group { get; init; }

        [NotMapped]
        public int Ignored { get; init; }
    }

    [Table("User")]
    public sealed class KeywordRow
    {
        public int Order { get; init; }

        public string Table { get; init; } = string.Empty;
    }

    public sealed class Ünïcödé
    {
        public int Größe { get; init; }
    }

    public class Entity
    {
        public int Id { get; init; }
    }

    public class AuditedEntity : Entity
    {
        public DateTime CreatedAt { get; init; }
    }

    public sealed class Invoice : AuditedEntity
    {
        public string Number { get; init; } = string.Empty;

        public decimal Amount { get; init; }
    }

    [Fact]
    public void MapsInheritedPropertiesBeforeDeclaredOnes()
    {
        Expression query = new CapturingQueryable<Invoice>().Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."CreatedAt", t0."Number", t0."Amount"
                FROM "Invoice" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QuotesSchemaAndTableName()
    {
        Expression query = new CapturingQueryable<QuotedRow>().Expression;

        Assert.Equal(
            Sql.Valid(
                """"
                SELECT t0."Quoted ""Name""", t0."select", t0."Group"
                FROM "sales"."Order Items" AS t0
                """"
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void DoublesQuotesInsideColumnName()
    {
        Expression query = new CapturingQueryable<QuotedRow>()
            .Select(r => r.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """"
                SELECT t0."Quoted ""Name"""
                FROM "sales"."Order Items" AS t0
                """"
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ExcludesNotMappedProperties()
    {
        Expression query = new CapturingQueryable<QuotedRow>()
            .Select(r => new { r.Group, r.Select })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Group", t0."select" AS "Select"
                FROM "sales"."Order Items" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QuotesReservedWordsInEveryPosition()
    {
        Expression query = new CapturingQueryable<KeywordRow>()
            .Where(r => r.Order > 1)
            .OrderBy(r => r.Table)
            .Select(r => new { r.Order, r.Table })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Order", t0."Table"
                FROM "User" AS t0
                WHERE t0."Order" > 1
                ORDER BY t0."Table"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QuotesReservedWordsAsAliases()
    {
        Expression query = new CapturingQueryable<KeywordRow>()
            .Select(r => new { Select = r.Order, From = r.Table })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Order" AS "Select", t0."Table" AS "From"
                FROM "User" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QuotesUnicodeNames()
    {
        Expression query = new CapturingQueryable<Ünïcödé>()
            .Select(u => new { u.Größe })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Größe"
                FROM "Ünïcödé" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void KeepsCaseOfMixedCaseNames()
    {
        Expression query = Customers.Select(c => new { c.IsActive, c.Region }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."IsActive", t0."Region"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QuotesAliasesOfAnonymousMembers()
    {
        Expression query = Customers
            .Select(c => new { MixedCase = c.Id, lower = c.Name })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "MixedCase", t0."Name" AS "lower"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QuotesNestedAliasesWithDot()
    {
        Expression query = Customers
            .Select(c => new { Outer = new { Inner = c.Id } })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Outer.Inner"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void QualifiesJoinedColumnsWithSchemaTableAliases()
    {
        Expression query = new CapturingQueryable<QuotedRow>()
            .Join(Customers, r => r.Select, c => c.Id, (r, c) => new { r.Group, c.Name })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Group", t1."Name"
                FROM "sales"."Order Items" AS t0
                INNER JOIN "Customer" AS t1 ON t0."select" = t1."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NumbersAliasesInOrderSourcesAreIntroduced()
    {
        Expression query = Customers
            .Join(
                Orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => new { c.Id, OrderId = o.Id }
            )
            .Join(
                OrderLines,
                x => x.OrderId,
                l => l.OrderId,
                (x, l) => new { x.Id, l.ProductId }
            )
            .Join(Products, x => x.ProductId, p => p.Id, (x, p) => p.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t3."Name"
                FROM "Customer" AS t0
                INNER JOIN "Order" AS t1 ON t0."Id" = t1."CustomerId"
                INNER JOIN "OrderLine" AS t2 ON t1."Id" = t2."OrderId"
                INNER JOIN "Product" AS t3 ON t2."ProductId" = t3."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void NumbersTwoDigitAliases()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Concat(Customers.Select(c => c.Id))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                UNION ALL
                SELECT t1."Id"
                FROM "Customer" AS t1
                UNION ALL
                SELECT t2."Id"
                FROM "Customer" AS t2
                UNION ALL
                SELECT t3."Id"
                FROM "Customer" AS t3
                UNION ALL
                SELECT t4."Id"
                FROM "Customer" AS t4
                UNION ALL
                SELECT t5."Id"
                FROM "Customer" AS t5
                UNION ALL
                SELECT t6."Id"
                FROM "Customer" AS t6
                UNION ALL
                SELECT t7."Id"
                FROM "Customer" AS t7
                UNION ALL
                SELECT t8."Id"
                FROM "Customer" AS t8
                UNION ALL
                SELECT t9."Id"
                FROM "Customer" AS t9
                UNION ALL
                SELECT t10."Id"
                FROM "Customer" AS t10
                UNION ALL
                SELECT t11."Id"
                FROM "Customer" AS t11
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IgnoresSkipOfZero()
    {
        Expression query = Customers.OrderBy(c => c.Id).Skip(0).Take(3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesNoRowsForNegativeTake()
    {
        int count = -1;

        Expression query = Customers.Take(count).Select(c => c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                LIMIT 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void IgnoresNegativeSkip()
    {
        int count = -4;

        Expression query = Customers.Skip(count).Select(c => c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersLargestTake()
    {
        Expression query = Customers.Take(int.MaxValue).Select(c => c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                LIMIT 2147483647
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersLargestSkip()
    {
        Expression query = Customers.Skip(int.MaxValue).Select(c => c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                OFFSET 2147483647
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RendersSingleRowPage()
    {
        Expression query = Customers
            .OrderBy(c => c.Id)
            .Skip(41)
            .Take(1)
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                ORDER BY t0."Id"
                LIMIT 1 OFFSET 41
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PutsLimitBeforeOffset()
    {
        Expression query = Customers.Skip(2).Take(3).Select(c => c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                LIMIT 3 OFFSET 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void PagesAfterSetOperation()
    {
        Expression query = Customers
            .Select(c => c.Id)
            .Union(Products.Select(p => p.Id))
            .Skip(5)
            .Take(5)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t2."Id"
                FROM (
                    SELECT t0."Id"
                    FROM "Customer" AS t0
                    UNION
                    SELECT t1."Id"
                    FROM "Product" AS t1
                ) AS t2
                LIMIT 5 OFFSET 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
