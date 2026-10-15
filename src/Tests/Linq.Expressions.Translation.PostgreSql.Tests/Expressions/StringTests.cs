namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record StringTests
{
    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsLength()
    {
        Expression query = Orders.Select(o => o.Number.Length).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LENGTH(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsLengthOfNullableColumn()
    {
        Expression query = Orders.Select(o => o.Note!.Length).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LENGTH(t0."Note")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsUpperCase()
    {
        Expression query = Orders.Select(o => o.Number.ToUpper()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UPPER(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsLowerCase()
    {
        Expression query = Orders.Select(o => o.Number.ToLower()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LOWER(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsInvariantUpperCase()
    {
        Expression query = Orders.Select(o => o.Number.ToUpperInvariant()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UPPER(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsInvariantLowerCase()
    {
        Expression query = Orders.Select(o => o.Number.ToLowerInvariant()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LOWER(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsTrimmed()
    {
        Expression query = Orders.Select(o => o.Number.Trim()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT BTRIM(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsTrimmedStart()
    {
        Expression query = Orders.Select(o => o.Number.TrimStart()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LTRIM(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsTrimmedEnd()
    {
        Expression query = Orders.Select(o => o.Number.TrimEnd()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT RTRIM(t0."Number")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsTrimmedOfCharacter()
    {
        Expression query = Orders.Select(o => o.Number.Trim('-')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT BTRIM(t0."Number", '-')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsTrimmedStartOfCharacter()
    {
        Expression query = Orders.Select(o => o.Number.TrimStart('-')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LTRIM(t0."Number", '-')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsTrimmedEndOfCharacter()
    {
        Expression query = Orders.Select(o => o.Number.TrimEnd('-')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT RTRIM(t0."Number", '-')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsSubstringFromConstantStart()
    {
        Expression query = Orders.Select(o => o.Number.Substring(2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 3)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsSubstringFromConstantStartWithLength()
    {
        Expression query = Orders.Select(o => o.Number.Substring(2, 4)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 3, 4)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsSubstringFromColumnStart()
    {
        Expression query = Orders.Select(o => o.Number.Substring(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", t0."Quantity" + 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsSubstringFromColumnStartWithColumnLength()
    {
        Expression query = Orders
            .Select(o => o.Number.Substring(o.Quantity, o.CustomerId))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", t0."Quantity" + 1, t0."CustomerId")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsSubstringFromStart()
    {
        Expression query = Orders.Select(o => o.Number.Substring(0, 3)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 1, 3)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsIndexOfString()
    {
        Expression query = Orders.Select(o => o.Number.IndexOf("-")).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT STRPOS(t0."Number", '-') - 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsIndexOfCharacter()
    {
        Expression query = Orders.Select(o => o.Number.IndexOf('-')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT STRPOS(t0."Number", '-') - 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsIndexOfColumn()
    {
        Expression query = Orders.Select(o => o.Number.IndexOf(o.Note!)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT STRPOS(t0."Number", t0."Note") - 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsReplacedString()
    {
        Expression query = Orders.Select(o => o.Number.Replace("-", "_")).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT REPLACE(t0."Number", '-', '_')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsReplacedCharacter()
    {
        Expression query = Orders.Select(o => o.Number.Replace('-', '_')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT REPLACE(t0."Number", '-', '_')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsReplacedByColumn()
    {
        Expression query = Orders.Select(o => o.Number.Replace("-", o.Note)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT REPLACE(t0."Number", '-', t0."Note")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChainsStringFunctions()
    {
        Expression query = Orders.Select(o => o.Number.Trim().ToUpper()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UPPER(BTRIM(t0."Number"))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChainsSubstringAndUpperCase()
    {
        Expression query = Orders
            .Select(o => o.Number.Substring(0, 3).ToUpper())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UPPER(SUBSTRING(t0."Number", 1, 3))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChainsLengthOfTrimmed()
    {
        Expression query = Orders.Select(o => o.Number.Trim().Length).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LENGTH(BTRIM(t0."Number"))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void AddsToLength()
    {
        Expression query = Orders.Select(o => o.Number.Length + 1).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LENGTH(t0."Number") + 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesColumnAndConstant()
    {
        Expression query = Orders.Select(o => o.Number + "-x").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-x'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesConstantAndColumn()
    {
        Expression query = Orders.Select(o => "x-" + o.Number).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT 'x-' || t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesTwoColumns()
    {
        Expression query = Orders.Select(o => o.Number + o.Number).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesThreeParts()
    {
        Expression query = Orders.Select(o => o.Number + "-" + o.Number).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-' || t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsNullableOperandAsEmptyWhenConcatenating()
    {
        Expression query = Orders.Select(o => o.Number + o.Note).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || COALESCE(t0."Note", '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsNullableOperandOnTheLeftAsEmpty()
    {
        Expression query = Orders.Select(o => o.Note + o.Number).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(t0."Note", '') || t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TreatsNullableOperandInChainAsEmpty()
    {
        Expression query = Orders.Select(o => o.Number + "-" + o.Note).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-' || COALESCE(t0."Note", '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConcatenatesWithStringConcat()
    {
        Expression query = Orders.Select(o => string.Concat(o.Number, "-x")).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-x'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConcatenatesThreeWithStringConcat()
    {
        Expression query = Orders
            .Select(o => string.Concat(o.Number, "-", o.Number))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-' || t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConcatenatesNumberAfterToString()
    {
        Expression query = Orders
            .Select(o => o.Number + o.Quantity.ToString())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || CAST(t0."Quantity" AS text)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesCapturedVariable()
    {
        string suffix = "-end";

        Expression query = Orders.Select(o => o.Number + suffix).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-end'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConcatenatesParenthesizedUpperCase()
    {
        Expression query = Orders.Select(o => "[" + o.Number.ToUpper() + "]").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '[' || UPPER(t0."Number") || ']'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConvertsIntegerToText()
    {
        Expression query = Orders.Select(o => o.Quantity.ToString()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Quantity" AS text)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConvertsLongToText()
    {
        Expression query = Orders.Select(o => o.Points.ToString()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Points" AS text)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConvertsDecimalToText()
    {
        Expression query = Orders.Select(o => o.Total.ToString()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Total" AS text)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConvertsDoubleToText()
    {
        Expression query = Orders.Select(o => o.Weight.ToString()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."Weight" AS text)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConvertsGuidToText()
    {
        Expression query = Orders.Select(o => o.TrackingId.ToString()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(t0."TrackingId" AS text)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ConvertsStringToItself()
    {
        Expression query = Orders.Select(o => o.Number.ToString()).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChecksNullOrEmptyAsValue()
    {
        Expression query = Orders.Select(o => string.IsNullOrEmpty(o.Note)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT (t0."Note" IS NULL) OR (t0."Note" = '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNullOrEmpty()
    {
        Expression query = Orders
            .Where(o => string.IsNullOrEmpty(o.Note))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Note" IS NULL) OR (t0."Note" = '')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNotNullOrEmpty()
    {
        Expression query = Orders
            .Where(o => !string.IsNullOrEmpty(o.Note))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT ((t0."Note" IS NULL) OR (t0."Note" = ''))
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNullOrEmptyOfNonNullableColumn()
    {
        Expression query = Orders
            .Where(o => string.IsNullOrEmpty(o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Number" IS NULL) OR (t0."Number" = '')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CombinesNullOrEmptyWithOtherPredicate()
    {
        Expression query = Orders
            .Where(o => string.IsNullOrEmpty(o.Note) && o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE ((t0."Note" IS NULL) OR (t0."Note" = '')) AND t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByLength()
    {
        Expression query = Orders
            .Where(o => o.Number.Length > 5)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE LENGTH(t0."Number") > 5
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByLengthOfNullableColumnNotNull()
    {
        Expression query = Orders
            .Where(o => (o.Note != null) && (o.Note.Length > 3))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Note" IS NOT NULL) AND (LENGTH(t0."Note") > 3)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByUpperCaseEquality()
    {
        Expression query = Orders
            .Where(o => o.Number.ToUpper() == "A-1")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE UPPER(t0."Number") = 'A-1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByLowerCaseEqualityWithCapturedVariable()
    {
        string term = "abc";

        Expression query = Orders
            .Where(o => o.Number.ToLower() == term)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE LOWER(t0."Number") = 'abc'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByTrimmedEquality()
    {
        Expression query = Orders
            .Where(o => o.Number.Trim() == "A-1")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE BTRIM(t0."Number") = 'A-1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersBySubstringEquality()
    {
        Expression query = Orders
            .Where(o => o.Number.Substring(0, 2) == "A-")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE SUBSTRING(t0."Number", 1, 2) = 'A-'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByIndexOf()
    {
        Expression query = Orders
            .Where(o => o.Number.IndexOf("-") == 1)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (STRPOS(t0."Number", '-') - 1) = 1
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByIndexOfFound()
    {
        Expression query = Orders
            .Where(o => o.Number.IndexOf("-") >= 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (STRPOS(t0."Number", '-') - 1) >= 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByReplacedEquality()
    {
        Expression query = Orders
            .Where(o => o.Number.Replace("-", "") == "A1")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE REPLACE(t0."Number", '-', '') = 'A1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByConcatenationEquality()
    {
        Expression query = Orders
            .Where(o => o.Number + "!" == "A-1!")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Number" || '!') = 'A-1!'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByContains()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains("ab"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%ab%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByContainsOfCharacter()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains('-'))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%-%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByStartsWith()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith("A-"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE 'A-%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByEndsWith()
    {
        Expression query = Orders
            .Where(o => o.Number.EndsWith("-1"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%-1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByNegatedContains()
    {
        Expression query = Orders
            .Where(o => !o.Number.Contains("ab"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE NOT (t0."Number" LIKE '%ab%')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByStartsWithOfNullableColumn()
    {
        Expression query = Orders
            .Where(o => o.Note!.StartsWith("x"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" LIKE 'x%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByContainsOfCapturedVariable()
    {
        string term = "abc";

        Expression query = Orders
            .Where(o => o.Number.Contains(term))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%abc%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByStartsWithOfCharacter()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith('A'))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE 'A%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FiltersByEndsWithOfCharacter()
    {
        Expression query = Orders
            .Where(o => o.Number.EndsWith('1'))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%1'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EscapesPercentInContains()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains("50%"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%50\%%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EscapesUnderscoreInStartsWith()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith("a_b"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE 'a\_b%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EscapesBackslashInEndsWith()
    {
        Expression query = Orders
            .Where(o => o.Number.EndsWith("a\\b"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%a\\b'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void EscapesAllWildcardsTogether()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains("%_\\"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%\%\_\\%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void DoublesQuoteInContains()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains("O'Brien"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%O''Brien%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void MatchesEverythingForEmptyContains()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains(""))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsColumnInsideColumnWithStrpos()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains(o.Note!))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE STRPOS(t0."Number", t0."Note") > 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsColumnPrefixWithStartsWith()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith(o.Note!))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE STARTS_WITH(t0."Number", t0."Note")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsColumnSuffixWithRight()
    {
        Expression query = Orders
            .Where(o => o.Number.EndsWith(o.Note!))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE RIGHT(t0."Number", LENGTH(t0."Note")) = t0."Note"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsComputedValueInsideColumn()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains(o.Note!.ToUpper()))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE STRPOS(t0."Number", UPPER(t0."Note")) > 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void LikesComputedColumnWithConstant()
    {
        Expression query = Orders
            .Where(o => o.Number.ToUpper().StartsWith("A"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE UPPER(t0."Number") LIKE 'A%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CombinesLikeWithOtherPredicate()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith("A") && o.IsPaid)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Number" LIKE 'A%') AND t0."IsPaid"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CombinesTwoLikes()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith("A") || o.Number.EndsWith("Z"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Number" LIKE 'A%') OR (t0."Number" LIKE '%Z')
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsLikeAsBooleanValue()
    {
        Expression query = Orders.Select(o => o.Number.Contains("ab")).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" LIKE '%ab%'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsLikeNextToColumn()
    {
        Expression query = Orders
            .Select(o => new { o.Id, HasDash = o.Number.Contains("-") })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t0."Number" LIKE '%-%' AS "HasDash"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void SelectsConditionalOnLike()
    {
        Expression query = Orders
            .Select(o => o.Number.StartsWith("A") ? 1 : 0)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE WHEN t0."Number" LIKE 'A%' THEN 1 ELSE 0 END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void OrdersByLength()
    {
        Expression query = Orders
            .OrderBy(o => o.Number.Length)
            .ThenBy(o => o.Number)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY LENGTH(t0."Number"), t0."Number"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void OrdersByLowerCase()
    {
        Expression query = Orders
            .OrderByDescending(o => o.Number.ToLower())
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                ORDER BY LOWER(t0."Number") DESC
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByFirstCharacters()
    {
        Expression query = Orders
            .GroupBy(o => o.Number.Substring(0, 1))
            .Select(g => new { g.Key, Count = g.Count() })
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 1, 1) AS "Key",
                CAST(COUNT(*) AS integer) AS "Count"
                FROM "Order" AS t0
                GROUP BY SUBSTRING(t0."Number", 1, 1)
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void GroupsByUpperCase()
    {
        Expression query = Orders
            .GroupBy(o => o.Number.ToUpper())
            .Select(g => g.Key)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UPPER(t0."Number")
                FROM "Order" AS t0
                GROUP BY UPPER(t0."Number")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void JoinsOnUpperCasedKeys()
    {
        Expression query = Customers
            .Join(
                Products,
                c => c.Name.ToUpper(),
                p => p.Name.ToUpper(),
                (c, p) => new { c.Id, ProductId = p.Id }
            )
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id", t1."Id" AS "ProductId"
                FROM "Customer" AS t0
                INNER JOIN "Product" AS t1 ON UPPER(t0."Name") = UPPER(t1."Name")
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsLongestNumber()
    {
        Expression query = Orders.Executed(q => q.Max(o => o.Number.Length));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MAX(LENGTH(t0."Number"))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void FindsMinimumUpperCaseNumber()
    {
        Expression query = Orders.Executed(q => q.Min(o => o.Number.ToUpper()));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT MIN(UPPER(t0."Number"))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void CountsMatchingStrings()
    {
        Expression query = Orders.Executed(q => q.Count(o => o.Number.StartsWith("A")));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CAST(COUNT(*) AS integer)
                FROM "Order" AS t0
                WHERE t0."Number" LIKE 'A%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = Skipped.NotImplemented)]
    public void ChecksAnyMatchingString()
    {
        Expression query = Orders.Executed(q => q.Any(o => o.Number.Contains("ab")));

        Assert.Equal(
            Sql.Valid(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM "Order" AS t0
                    WHERE t0."Number" LIKE '%ab%'
                )
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
