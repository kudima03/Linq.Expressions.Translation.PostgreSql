# Linq.Expressions.Translation.PostgreSql

Translates the expression tree of an `IQueryable<T>` into a single PostgreSQL `SELECT` statement.

[![.NET build & test](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Linq.Expressions.Translation.PostgreSql)](https://www.nuget.org/packages/Linq.Expressions.Translation.PostgreSql)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Status

The translation is not implemented yet.

## Overview

The package exposes exactly one public type, `PostgreSqlQuery`. It accepts a `System.Linq.Expressions.Expression` — the `Expression` of an `IQueryable<T>` — and exposes the PostgreSQL `SELECT` statement for that tree as its `TextValue`.

```csharp
IQueryable<Order> orders = ...; // any IQueryable<T> whose provider builds the tree

string sql = new PostgreSqlQuery(
    orders.Where(o => o.Total > 100).Select(o => o.Id).Expression
).TextValue;

// SELECT t0."Id" FROM "Order" AS t0 WHERE t0."Total" > 100
```

The package does not execute queries, open connections or materialize results; it only produces SQL text.

## Constants are inline literals

The result is a single string, not a command with a parameter collection. Every constant in the tree — literals, captured variables and in-memory collections — is rendered as a safely escaped inline PostgreSQL literal (for example, `'O''Brien'` for the string `O'Brien`). No bind parameters (`$1`, `@p0`) are produced.

## Translation conventions

The output is deterministic: the same tree always produces the same text. The test suite pins every rule below.

### Shape

- One statement, keywords in upper case, single spaces, no trailing `;`.
- A query root is a constant `IQueryable<T>` whose `Expression` is that constant. Its table is the `[Table]` name (with `Schema` when set) or the CLR type name.
- The columns of an entity are its public instance properties of a mapped type, in declaration order; `[Column]` renames a column and `[NotMapped]` excludes a property. Mapped types are `bool`, the integral and floating-point types, `decimal`, `string`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, enums, their `Nullable<T>` forms and one-dimensional arrays of them (PostgreSQL arrays). A property of any other type is a navigation and is not supported.
- Every table, column and output name is a double-quoted identifier, with `"` doubled inside it. Table aliases are `t0`, `t1`, … unquoted, numbered in the order sources are introduced: an operator's source before its arguments, operands left to right, and a derived table after the sources inside it.
- Selecting an entity lists all of its columns. A scalar projection is a single unnamed item. An anonymous type, a record constructor or a member initializer gives one item per member (constructor parameter name for records), followed by `AS "<Member>"` unless the item is a column of the same name. Nested objects are flattened as `"<Outer>.<Inner>"`.
- An operand that is itself an operator expression is parenthesized: `(t0."A" + t0."B") * 2`, `(t0."A" > 1) AND (t0."B" < 2)`. The top-level expression of a clause, select item or function argument is not.

### Constants

- A subtree that does not depend on the query's lambda parameters is evaluated in memory and rendered as one literal: captured variables, `min * 2`, `-min`. `DateTime.Now`, `DateTime.UtcNow` and `Guid.NewGuid()` are not evaluated; they become `LOCALTIMESTAMP`, `NOW()` and `GEN_RANDOM_UUID()`. A captured `IQueryable` becomes a subquery.
- Literals: `TRUE`, `FALSE`, `NULL`; numbers in invariant culture (`-5`, `10.50`, `1.5`, `1E-07`); `CAST('NaN' AS double precision)` and `'Infinity'` / `'-Infinity'` likewise; strings and chars as `'...'` with `'` doubled (`standard_conforming_strings` on, so backslashes are literal); `UUID '...'`; `DATE 'yyyy-MM-dd'`; `TIME 'HH:mm:ss[.ffffff]'`; `TIMESTAMP 'yyyy-MM-dd HH:mm:ss[.ffffff]'` for unspecified `DateTime`, `TIMESTAMPTZ '...+00'` for UTC and `TIMESTAMPTZ '...+hh:mm'` for `DateTimeOffset`; `INTERVAL '<ISO 8601 duration>'`; `ARRAY[...]`. Fractional seconds drop trailing zeros. Enums render as their underlying integer.
- Not supported: a string containing `\0`, a local-kind `DateTime`, a value with sub-microsecond precision, and a constant of an unmapped type.

### Types

Every SQL expression has the PostgreSQL type of the CLR type of its LINQ expression (`int` → `integer`, `long` → `bigint`, `short` → `smallint`, `decimal` → `numeric`, `double` → `double precision`, `float` → `real`, `string` → `text`, `bool` → `boolean`, `Guid` → `uuid`, `DateTime` → `timestamp`, `DateTimeOffset` → `timestamptz`, `DateOnly` → `date`, `TimeOnly` → `time`, `TimeSpan` → `interval`). Where PostgreSQL would produce a different type, a `CAST` restores it: `Count` → `CAST(COUNT(*) AS integer)`, `Sum` of `int` → `CAST(... AS integer)`, `Average` of an integer → `AVG(CAST(x AS double precision))`, date parts → `CAST(EXTRACT(...) AS integer)`. A conversion that only changes nullability or converts an enum to its underlying type renders nothing; a numeric conversion renders `CAST`, and a floating-point to integer conversion truncates first: `CAST(TRUNC(x) AS integer)`.

### NULL semantics

.NET semantics are preserved:

- `x == null` is `x IS NULL`; `x != null` is `x IS NOT NULL`.
- `==` / `!=` with a nullable operand (`Nullable<T>`, or a reference type annotated `?`) is `IS NOT DISTINCT FROM` / `IS DISTINCT FROM`; between non-nullable operands it is `=` / `<>`. Join keys always compare with `=`, as `Join` never matches null keys.
- An ordering comparison with a nullable operand is `false` in .NET when an operand is null. Where SQL `NULL` and `false` differ — under `NOT` or as a value — it is wrapped as `COALESCE(<comparison>, FALSE)`; directly in `WHERE`, `HAVING`, `ON` or `CASE WHEN`, possibly through `AND` / `OR`, it is not.
- String concatenation treats null as empty: a nullable operand becomes `COALESCE(x, '')`.
- `Sum` over no rows is `0`: `COALESCE(SUM(x), 0)`. `Min`, `Max` and `Average` over no rows are `NULL`.
- Nullable ordering keys sort the way .NET does, nulls first: `ORDER BY x NULLS FIRST`, `ORDER BY x DESC NULLS LAST`.

### Operators

| LINQ | SQL |
|------|-----|
| `Where` | `WHERE`; several are joined by `AND` |
| `Select` | the select list; it composes with the operators before it |
| `OrderBy` / `ThenBy` / `…Descending` | `ORDER BY x[, y DESC]`; a later `OrderBy` keeps earlier keys as tie-breakers, as the sort is stable |
| `Skip` / `Take` | `OFFSET` / `LIMIT`, combined arithmetically when chained |
| `Distinct` | `SELECT DISTINCT` |
| `Reverse` | inverts every `ORDER BY` key; requires an ordering |
| `Concat` / `Union` / `Intersect` / `Except` | `UNION ALL` / `UNION` / `INTERSECT` / `EXCEPT`; an operand with `ORDER BY`, `LIMIT` or `OFFSET` is parenthesized |
| `Join` | `INNER JOIN ... ON`; composite keys compare member by member |
| `GroupJoin` + `SelectMany(DefaultIfEmpty())` | `LEFT JOIN ... ON` |
| `GroupJoin` with an aggregate | correlated scalar subquery |
| `SelectMany` | `CROSS JOIN`; `INNER JOIN` / `LEFT JOIN` when correlated by `Where`; `CROSS JOIN LATERAL` / `LEFT JOIN LATERAL ... ON TRUE` otherwise |
| `GroupBy` + projection | `GROUP BY`; predicates on groups become `HAVING`; `g.Count(p)` is `COUNT(*) FILTER (WHERE p)`; `g.Select(x).Distinct().Count()` is `COUNT(DISTINCT x)` |
| `Count` / `LongCount` / `Sum` / `Average` / `Min` / `Max` | aggregates, typed as above |
| `Any` / `All` / `Contains` | `EXISTS (SELECT 1 ...)` / `NOT EXISTS (... WHERE NOT p)` / `x IN (SELECT ...)` |
| `First` / `Single` / `Last` / `ElementAt` and `…OrDefault` | `LIMIT 1` / `LIMIT 2` / inverted ordering and `LIMIT 1` (requires an ordering) / `LIMIT 1 OFFSET n` |
| `Cast` / `OfType` | nothing to the element type or a type it implements; `OfType` of a nullable element is `IS NOT NULL` |

An operator that cannot apply to the current query level — anything but `Select`, `Skip` and `Take` after `LIMIT` / `OFFSET`, anything but `Skip` and `Take` after `DISTINCT`, anything after a set operation — wraps the query so far in a derived table `(...) AS tN`. Predicates and orderings after a grouped projection stay on the grouped level.

### Expressions

| .NET | SQL |
|------|-----|
| `==` `!=` `<` `<=` `>` `>=` | `=` `<>` `<` `<=` `>` `>=` (see NULL semantics) |
| `&&` `\|\|` `!` | `AND` `OR` `NOT`; a `bool` member is a predicate on its own |
| `+` `-` `*` `/` `%`, unary `-` | the same operators; integer division truncates as in .NET |
| `&` `\|` `^` `~` `<<` `>>` | `&` `\|` `#` `~` `<<` `>>`; on `bool`: `AND` `OR` `<>` |
| `c ? a : b` | `CASE WHEN c THEN a ELSE b END`; nested conditionals in the else branch flatten |
| `a ?? b ?? c` | `COALESCE(a, b, c)` |
| `HasValue` / `Value` / `GetValueOrDefault(d)` | `IS NOT NULL` / the value / `COALESCE(x, d)` |
| string `+`, `string.Concat` | `\|\|` |
| `Length`, `ToUpper`, `ToLower`, `Trim`, `TrimStart`, `TrimEnd` | `LENGTH`, `UPPER`, `LOWER`, `BTRIM`, `LTRIM`, `RTRIM` |
| `Contains` / `StartsWith` / `EndsWith` | `LIKE` with `%`, `_` and `\` escaped for a constant; `STRPOS(a, b) > 0` / `STARTS_WITH(a, b)` / `RIGHT(a, LENGTH(b)) = b` otherwise |
| `Substring(s[, n])`, `IndexOf`, `Replace` | `SUBSTRING(a, s + 1[, n])`, `STRPOS(a, b) - 1`, `REPLACE` |
| `string.IsNullOrEmpty(x)` | `(x IS NULL) OR (x = '')` |
| `ToString()` of numbers and `Guid` | `CAST(x AS text)` |
| `Math.Abs`, `Ceiling`, `Floor`, `Truncate`, `Sqrt`, `Pow`, `Exp`, `Log`, `Log10`, `Sign`, `Max`, `Min` | `ABS`, `CEILING`, `FLOOR`, `TRUNC`, `SQRT`, `POWER`, `EXP`, `LN`, `LOG`, `SIGN`, `GREATEST`, `LEAST` |
| `Math.Round(x, [d,] MidpointRounding.AwayFromZero)` on `decimal` | `ROUND(x[, d])` |
| `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `DayOfWeek`, `DayOfYear` | `CAST(EXTRACT(<part> FROM x) AS integer)` |
| `DateTime.Date` | `DATE_TRUNC('day', x)` |
| `AddDays(n)`, `AddMonths(n)`, `AddYears(n)`, `AddHours(n)`, `AddMinutes(n)`, `AddSeconds(n)` | `x + (n * INTERVAL '1 day')` and so on; `DateOnly.AddDays(n)` is `x + n` |
| `DateOnly.FromDateTime(x)` | `CAST(x AS date)` |
| `Enum.HasFlag(f)` | `(x & f) = f` |
| in-memory `collection.Contains(x)` | `x IN (...)`; `FALSE` when empty; a null element adds `OR (x IS NULL)` |
| array column `.Contains(v)` / `.Length` | `v = ANY (x)` / `CARDINALITY(x)` |

### Not supported

Any other method or member — user methods, `Regex`, `string.Format`, culture- or `StringComparison`-sensitive overloads, `ToString()` of enums and dates, `Math.Round` with banker's rounding or on `double`, navigation properties, entity equality, invoking delegates — and operators without a set-based meaning (`GroupBy` returning groupings, `Last` / `Reverse` without an ordering, `Zip`, `Aggregate`, `SkipWhile`, `TakeWhile`, `Prepend`, `Append`, `Chunk`, in-memory query roots) throw `NotSupportedException` whose message names the construct.

## Dependencies

None beyond the .NET base class library.

## Target frameworks

`net8.0`, `net9.0`, `net10.0`.
