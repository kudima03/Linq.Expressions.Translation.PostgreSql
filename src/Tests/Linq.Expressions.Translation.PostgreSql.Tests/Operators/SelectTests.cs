namespace Linq.Expressions.Translation.PostgreSql.Tests.Operators;

public sealed record SelectTests
{
    [Fact]
    public void ProjectsAnonymousTypeOfColumns()
    {
        Expression query = Customers.Select(c => new { c.Id, c.Name }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AliasesRenamedAnonymousMember()
    {
        Expression query = Customers.Select(c => new { Key = c.Id, c.Name }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" AS "Key", t0."Name"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AliasesComputedAnonymousMember()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Label = c.Name + "!" })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name" || '!' AS "Label"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsRecordThroughConstructor()
    {
        Expression query = Customers
            .Select(c => new CustomerSummary(c.Id, c.Name))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void AliasesRecordConstructorArgumentsByParameterName()
    {
        Expression query = Customers
            .Select(c => new CustomerSummary(c.Id + 1, c.Name.ToUpper()))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" + 1 AS "Id", UPPER(t0."Name") AS "Name"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsMemberInitializer()
    {
        Expression query = Customers
            .Select(c => new CustomerCard { Id = c.Id, Title = c.Name })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name" AS "Title"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FlattensNestedAnonymousType()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Info = new { c.Name, c.Region } })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name" AS "Info.Name", t0."Region" AS "Info.Region"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsEntityIdentity()
    {
        Expression query = Customers.Select(c => c).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Name", t0."Email", t0."Region", t0."IsActive"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsNullableColumn()
    {
        Expression query = Customers.Select(c => c.Email).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Email"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsConstant()
    {
        Expression query = Customers.Select(c => 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 1
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsStringConstantNextToColumn()
    {
        Expression query = Customers
            .Select(c => new { c.Id, Kind = "customer" })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", 'customer' AS "Kind"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsCapturedVariable()
    {
        string tag = "vip";

        Expression query = Customers.Select(c => new { c.Id, Tag = tag }).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", 'vip' AS "Tag"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsArithmeticWithoutOuterParentheses()
    {
        Expression query = Orders.Select(o => o.Total * o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Total" * CAST(t0."Quantity" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ParenthesizesNestedArithmetic()
    {
        Expression query = Orders
            .Select(o => (o.Total + o.Discount) * o.Quantity)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Total" + t0."Discount") * CAST(t0."Quantity" AS numeric)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsPredicateAsBooleanValue()
    {
        Expression query = Customers.Select(c => c.Id > 3).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id" > 3
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsPredicateAlongsideColumn()
    {
        Expression query = Customers
            .Select(c => new { c.Name, Big = c.Id > 3 })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name", t0."Id" > 3 AS "Big"
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComposesConsecutiveSelectsIntoOne()
    {
        Expression query = Customers
            .Select(c => new { c.Id, c.Name })
            .Select(x => x.Id)
            .Expression;

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
    public void InlinesComputedMemberOfEarlierSelect()
    {
        Expression query = Customers.Select(c => c.Id * 2).Select(x => x + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Id" * 2) + 1
                FROM "Customer" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComposesWhereBeforeSelect()
    {
        Expression query = Customers
            .Where(c => c.IsActive)
            .Select(c => c.Name)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Name"
                FROM "Customer" AS t0
                WHERE t0."IsActive"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsColumnsOfEveryMappedType()
    {
        Expression query = Orders
            .Select(o => new
            {
                o.Id,
                o.Note,
                o.Total,
                o.Discount,
                o.Points,
                o.Weight,
                o.IsPaid,
                o.IsGift,
                o.Status,
                o.Options,
                o.CreatedAt,
                o.PaidAt,
                o.ShippedOn,
                o.DeliveryTime,
                o.TrackingId,
                o.Tags,
            })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Note", t0."Total", t0."Discount", t0."Points",
                t0."Weight", t0."IsPaid", t0."IsGift", t0."Status", t0."Options",
                t0."CreatedAt", t0."PaidAt", t0."ShippedOn", t0."DeliveryTime",
                t0."TrackingId", t0."Tags"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
