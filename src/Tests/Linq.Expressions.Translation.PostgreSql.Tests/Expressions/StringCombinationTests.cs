#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet
#pragma warning disable CA1861 // the arrays are the arguments under test
#pragma warning disable CA1870 // SearchValues would hide the IndexOfAny translation

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record StringCombinationTests
{
    [Fact(Skip = "A nullable integer is coalesced with '' without a cast to text.")]
    public void ConcatenatesNullableIntegerAsText()
    {
        Expression query = Orders.Select(o => o.Number + o.Priority).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || COALESCE(CAST(t0."Priority" AS text), '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ConcatenatesNonNullableIntegerDirectly()
    {
        Expression query = Orders.Select(o => o.Number + "-" + o.Quantity).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-' || t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "A boolean is concatenated as 'true'/'false' instead of 'True'/'False'."
    )]
    public void RejectsConcatenatingBoolean()
    {
        Expression query = Orders.Select(o => o.Number + o.IsPaid).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "An enum is concatenated as its number instead of its name.")]
    public void RejectsConcatenatingEnum()
    {
        Expression query = Orders.Select(o => o.Number + o.Status).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "A DateTime is concatenated in the PostgreSQL format, not the .NET one."
    )]
    public void RejectsConcatenatingDateTime()
    {
        Expression query = Orders.Select(o => o.Number + o.CreatedAt).Expression;

        _ = Assert.Throws<NotSupportedException>(() =>
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.Concat of a string array is not translated.")]
    public void ConcatenatesStringArray()
    {
        Expression query = Orders
            .Select(o => string.Concat(new[] { o.Number, "/", o.Note }))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '/' || COALESCE(t0."Note", '')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(
        Skip = "Interpolated strings compile to String.Format, which is not translated."
    )]
    public void InterpolatesStrings()
    {
        Expression query = Orders.Select(o => $"{o.Number}-{o.Note}").Expression;

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

    [Fact(
        Skip = "Interpolated strings compile to String.Format, which is not translated."
    )]
    public void InterpolatesInteger()
    {
        Expression query = Orders.Select(o => $"#{o.Quantity}").Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT '#' || t0."Quantity"
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TrimsWhitespaceForEmptyCharacterArray()
    {
        Expression query = Orders.Select(o => o.Number.Trim(new char[0])).Expression;

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

    [Fact]
    public void EscapesEveryWildcardOfCapturedFragment()
    {
        string part = "50%_\\";
        Expression query = Orders
            .Where(o => o.Number.Contains(part))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%50\%\_\\%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesQuoteOfCapturedPrefix()
    {
        string prefix = "O'B";
        Expression query = Customers
            .Where(c => c.Name.StartsWith(prefix))
            .Select(c => c.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Customer" AS t0
                WHERE t0."Name" LIKE 'O''B%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void EscapesUnderscoreCharacter()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains('_'))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%\_%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByPrefixFromColumn()
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

    [Fact]
    public void FiltersBySuffixFromColumn()
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

    [Fact]
    public void FiltersByFragmentFromColumn()
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

    [Fact]
    public void MatchesFragmentOfUpperCasedText()
    {
        Expression query = Orders
            .Where(o => o.Number.ToUpper().Contains("AB"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE UPPER(t0."Number") LIKE '%AB%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesLowerCasedTextWithEvaluatedCapture()
    {
        string number = "AbC";
        Expression query = Orders
            .Where(o => o.Number.ToLower() == number.ToLower())
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

    [Fact]
    public void FiltersByLengthOfTrimmedText()
    {
        Expression query = Orders
            .Where(o => o.Number.Trim().Length > 3)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE LENGTH(BTRIM(t0."Number")) > 3
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ProjectsLengthOfReplacedText()
    {
        Expression query = Orders
            .Select(o => o.Number.Replace("-", "").Length)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LENGTH(REPLACE(t0."Number", '-', ''))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void UpperCasesSubstring()
    {
        Expression query = Orders
            .Select(o => o.Number.Substring(1, 2).ToUpper())
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT UPPER(SUBSTRING(t0."Number", 2, 2))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByPositionOfFragment()
    {
        Expression query = Orders
            .Where(o => o.Number.IndexOf("-") > 0)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (STRPOS(t0."Number", '-') - 1) > 0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSubstringFromComputedStart()
    {
        Expression query = Orders
            .Select(o => o.Number.Substring(o.Quantity + 1))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", (t0."Quantity" + 1) + 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void OrdersByLengthThenText()
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

    [Fact]
    public void FiltersByNegatedIsNullOrEmpty()
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

    [Fact]
    public void ProjectsIsNullOrEmpty()
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

    [Fact]
    public void ComparesNullableStringsWithStaticEquals()
    {
        Expression query = Orders
            .Where(o => string.Equals(o.Note, o.Number))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Note" IS NOT DISTINCT FROM t0."Number"
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesPrefixOfCoalescedText()
    {
        Expression query = Orders
            .Where(o => (o.Note ?? "").StartsWith("A"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE COALESCE(t0."Note", '') LIKE 'A%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ComparesConcatenation()
    {
        Expression query = Orders
            .Where(o => o.Number + "-" == "A-")
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE (t0."Number" || '-') = 'A-'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FiltersByLastPositionOfCharacter()
    {
        Expression query = Orders
            .Where(o => o.Number.LastIndexOf('-') > 2)
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE CASE
                WHEN STRPOS(t0."Number", '-') = 0 THEN -1
                ELSE LENGTH(t0."Number") - STRPOS(REVERSE(t0."Number"), '-')
                END > 2
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FindsSingleListedCharacter()
    {
        Expression query = Orders
            .Select(o => o.Number.IndexOfAny(new[] { '-' }))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT COALESCE(LEAST(NULLIF(STRPOS(t0."Number", '-') - 1, -1)), -1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RemovesTailFromColumnPosition()
    {
        Expression query = Orders.Select(o => o.Number.Remove(o.Quantity)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 1, t0."Quantity")
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void RemovesMiddleFromColumnPosition()
    {
        Expression query = Orders.Select(o => o.Number.Remove(o.Quantity, 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 1, t0."Quantity") || SUBSTRING(t0."Number", (t0."Quantity" + 2) + 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesSuffixAfterTrimmingStart()
    {
        Expression query = Orders
            .Where(o => o.Number.TrimStart('0').EndsWith("9"))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE LTRIM(t0."Number", '0') LIKE '%9'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void ReplacesCharacter()
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

    [Fact]
    public void MeasuresConcatenationWithNullablePart()
    {
        Expression query = Orders.Select(o => (o.Number + o.Note).Length).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LENGTH(t0."Number" || COALESCE(t0."Note", ''))
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
