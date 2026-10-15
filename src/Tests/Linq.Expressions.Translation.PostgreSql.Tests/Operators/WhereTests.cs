namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record WhereTests
{
    public static TheoryData<Expression<Func<Customer, bool>>, string> Comparisons =>
        new()
        {
            { c => c.Id > 5, ">" },
            { c => c.Id >= 5, ">=" },
            { c => c.Id < 5, "<" },
            { c => c.Id <= 5, "<=" },
            { c => c.Id == 5, "=" },
            { c => c.Id != 5, "<>" },
        };

    [Theory(Skip = Skipped.NotImplemented)]
    [MemberData(nameof(Comparisons))]
    public void FiltersByComparisonOfColumnWithConstant(
        Expression<Func<Customer, bool>> predicate,
        string sqlOperator
    )
    {
        Expression query = Customers.Where(predicate).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" {sqlOperator} 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByStringEquality()
    {
        Expression query = Customers.Where(c => c.Region == "EU").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Region" = 'EU'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByConstantOnTheLeft()
    {
        Expression query = Customers.Where(c => 5 < c.Id).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE 5 < t0."Id"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByBooleanColumn()
    {
        Expression query = Customers.Where(c => c.IsActive).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNegatedBooleanColumn()
    {
        Expression query = Customers.Where(c => !c.IsActive).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE NOT t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Theory(Skip = Skipped.NotImplemented)]
    [InlineData(true, "TRUE")]
    [InlineData(false, "FALSE")]
    public void FiltersByBooleanColumnComparedWithLiteral(bool value, string literal)
    {
        Expression query = Customers.Where(c => c.IsActive == value).Expression;

        Assert.Equal(
            Sql.Valid(
                $"""
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive" = {literal}
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByConstantTrue()
    {
        Expression query = Customers.Where(c => true).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE TRUE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByConstantFalse()
    {
        Expression query = Customers.Where(c => false).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE FALSE
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByConjunction()
    {
        Expression query = Customers
            .Where(c => c.IsActive && (c.Region == "EU"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."IsActive" AND (t0."Region" = 'EU')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByDisjunction()
    {
        Expression query = Customers
            .Where(c => (c.Region == "EU") || (c.Region == "US"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE (t0."Region" = 'EU') OR (t0."Region" = 'US')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByRange()
    {
        Expression query = Customers.Where(c => (c.Id >= 3) && (c.Id <= 9)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE (t0."Id" >= 3) AND (t0."Id" <= 9)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ParenthesizesDisjunctionInsideConjunction()
    {
        Expression query = Customers
            .Where(c => ((c.Region == "EU") || (c.Region == "US")) && c.IsActive)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE ((t0."Region" = 'EU') OR (t0."Region" = 'US')) AND t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ParenthesizesConjunctionInsideDisjunction()
    {
        Expression query = Customers
            .Where(c => (c.IsActive && (c.Id > 1)) || (c.Region == "EU"))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE (t0."IsActive" AND (t0."Id" > 1)) OR (t0."Region" = 'EU')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void NegatesParenthesizedConjunction()
    {
        Expression query = Customers.Where(c => !(c.IsActive && (c.Id > 1))).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE NOT (t0."IsActive" AND (t0."Id" > 1))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void JoinsSeveralWhereClausesWithAnd()
    {
        Expression query = Customers
            .Where(c => c.Region == "EU")
            .Where(c => c.Id > 1)
            .Where(c => c.IsActive)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE (t0."Region" = 'EU') AND (t0."Id" > 1) AND t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNullColumn()
    {
        Expression query = Customers.Where(c => c.Email == null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Email" IS NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNotNullColumn()
    {
        Expression query = Customers.Where(c => c.Email != null).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Email" IS NOT NULL
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ComparesNullableColumnWithNullSafeEquality()
    {
        Expression query = Customers.Where(c => c.Email == "a@b.c").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Email" IS NOT DISTINCT FROM 'a@b.c'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ComparesNullableColumnWithNullSafeInequality()
    {
        Expression query = Customers.Where(c => c.Email != "a@b.c").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Email" IS DISTINCT FROM 'a@b.c'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ComparesTwoColumns()
    {
        Expression query = Orders
            .Where(o => o.Quantity > o.CustomerId)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Quantity" > t0."CustomerId"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersAfterProjectionToScalar()
    {
        Expression query = Customers.Select(c => c.Id).Where(id => id > 3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE t0."Id" > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersAfterProjectionToComputedMember()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Upper = c.Name.ToUpper() })
            .Where(x => x.Upper == "ANN")
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", UPPER(t0."Name") AS "Upper"
                FROM "Customer" AS t0
                WHERE UPPER(t0."Name") = 'ANN'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByCapturedVariable()
    {
        int minimum = 7;

        Expression query = Customers.Where(c => c.Id > minimum).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                WHERE t0."Id" > 7
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
