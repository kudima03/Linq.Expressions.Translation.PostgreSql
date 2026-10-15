#pragma warning disable xUnit1004 // skipped tests record SQL the translator does not produce yet

namespace Linq.Expressions.Translation.PostgreSql.Tests.Expressions;

public sealed record StringSemanticsTests
{
    [Fact]
    public void ConcatenatesCharacter()
    {
        Expression query = Orders.Select(o => o.Number + '-').Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Number" || '-'
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void FindsEmptyStringAtTheStart()
    {
        Expression query = Orders.Select(o => o.Number.IndexOf(string.Empty)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT STRPOS(t0."Number", '') - 1
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesEmptyPrefix()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith(string.Empty))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void MatchesEmptySuffix()
    {
        Expression query = Orders
            .Where(o => o.Number.EndsWith(string.Empty))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact]
    public void TakesSuffixAfterSeparator()
    {
        Expression query = Orders
            .Select(o => o.Number.Substring(o.Number.IndexOf("-") + 1))
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", ((STRPOS(t0."Number", '-') - 1) + 1) + 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.IndexOf with a start index is not translated.")]
    public void FindsStringFromStartIndex()
    {
        Expression query = Orders.Select(o => o.Number.IndexOf("-", 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE
                WHEN STRPOS(SUBSTRING(t0."Number", 3), '-') = 0 THEN -1
                ELSE (STRPOS(SUBSTRING(t0."Number", 3), '-') - 1) + 2
                END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.IndexOf with a start index is not translated.")]
    public void FindsCharacterFromStartIndex()
    {
        Expression query = Orders.Select(o => o.Number.IndexOf('-', 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE
                WHEN STRPOS(SUBSTRING(t0."Number", 3), '-') = 0 THEN -1
                ELSE (STRPOS(SUBSTRING(t0."Number", 3), '-') - 1) + 2
                END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.LastIndexOf is not translated.")]
    public void FindsLastIndexOfString()
    {
        Expression query = Orders.Select(o => o.Number.LastIndexOf("AB")).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE
                WHEN STRPOS(t0."Number", 'AB') = 0 THEN -1
                ELSE LENGTH(t0."Number") - LENGTH('AB') + 1 - STRPOS(REVERSE(t0."Number"), 'BA')
                END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.LastIndexOf is not translated.")]
    public void FindsLastIndexOfCharacter()
    {
        Expression query = Orders.Select(o => o.Number.LastIndexOf('-')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT CASE
                WHEN STRPOS(t0."Number", '-') = 0 THEN -1
                ELSE LENGTH(t0."Number") - STRPOS(REVERSE(t0."Number"), '-')
                END
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.Remove is not translated.")]
    public void RemovesTail()
    {
        Expression query = Orders.Select(o => o.Number.Remove(1)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 1, 1)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "String.Remove is not translated.")]
    public void RemovesMiddle()
    {
        Expression query = Orders.Select(o => o.Number.Remove(1, 2)).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT SUBSTRING(t0."Number", 1, 1) || SUBSTRING(t0."Number", 4)
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "Trim of several characters is passed as an array.")]
    public void TrimsSeveralCharacters()
    {
        Expression query = Orders.Select(o => o.Number.Trim('-', '_')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT BTRIM(t0."Number", '-_')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "TrimStart of several characters is passed as an array.")]
    public void TrimsSeveralCharactersFromTheStart()
    {
        Expression query = Orders.Select(o => o.Number.TrimStart('-', '_')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT LTRIM(t0."Number", '-_')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "TrimEnd of several characters is passed as an array.")]
    public void TrimsSeveralCharactersFromTheEnd()
    {
        Expression query = Orders.Select(o => o.Number.TrimEnd('-', '_')).Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT RTRIM(t0."Number", '-_')
                FROM "Order" AS t0
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }

    [Fact(Skip = "StartsWith with an ordinal comparison is not translated.")]
    public void MatchesOrdinalPrefix()
    {
        Expression query = Orders
            .Where(o => o.Number.StartsWith("A-", StringComparison.Ordinal))
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

    [Fact(Skip = "Contains with an ordinal comparison is not translated.")]
    public void MatchesOrdinalFragment()
    {
        Expression query = Orders
            .Where(o => o.Number.Contains("a_b", StringComparison.Ordinal))
            .Select(o => o.Id)
            .Expression;

        Assert.Equal(
            Sql.Valid(
                """
                SELECT t0."Id"
                FROM "Order" AS t0
                WHERE t0."Number" LIKE '%a\_b%'
                """
            ),
            new PostgreSqlQuery(query).TextValue
        );
    }
}
