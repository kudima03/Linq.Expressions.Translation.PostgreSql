# Linq.Expressions.Translation.PostgreSql

Translates the expression tree of an `IQueryable<T>` into a single PostgreSQL `SELECT` statement.

[![.NET build & test](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Linq.Expressions.Translation.PostgreSql)](https://www.nuget.org/packages/Linq.Expressions.Translation.PostgreSql)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## What is translated

- `Where`, `Select`, `OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`, `Order`/`OrderDescending`, `Skip`, `Take`, `TakeLast`, `SkipLast`, `Distinct`, `DistinctBy`, `Reverse`, `Cast` and `OfType`; an operator that cannot extend the current `SELECT` (for example `Where` after `Take`) wraps it into a derived table, which exposes ordering keys the projection hides as extra `"$n"` columns so that the order survives (also for `Reverse`, `Last` and `TakeLast`). The ordering operators accept `StringComparer.Ordinal` (`ORDER BY ... COLLATE "C"`, the byte-wise order of UTF-8, which is code-point order). `DistinctBy` is `SELECT DISTINCT ON (keys)`, whose `ORDER BY` has to start with the keys: a source order that does not (`OrderByDescending(o => o.PlacedAt).DistinctBy(o => o.UserId)`, the latest order per user) is restored outside a derived table, into which every later operator other than a projection or paging also wraps the kept rows.
- Comparison, logical, arithmetic, bitwise and conditional operators, `??`, conversions (a `DateTime` compares with a `DateTimeOffset` through PostgreSQL's session time zone), string concatenation, `Nullable<T>` members and constructed objects (`new Summary(c.Id, c.Name)`, also of constants only), which are projected member by member, as is a constant tuple (`default(ValueTuple<int?, string?>)` or a captured one, whose members become literals, `NULL` for a default). `string.CompareOrdinal(a, b)` compared with `0` is `a COLLATE "C" < b` (`<=`, `>`, `>=`, `=`, `<>`), ranking a null operand below every string as .NET does (`a IS NULL OR a COLLATE "C" < b`).
- `string` members (`Length`, case conversion, trimming (`Trim()` removes every .NET white-space character, not only spaces), `Substring`, `Remove`, `IndexOf`/`LastIndexOf`/`IndexOfAny`, `Replace`, `Contains`/`StartsWith`/`EndsWith` as `LIKE` with escaped wildcards, `IsNullOrEmpty`, `Concat`, `Format` and interpolated strings without alignment or format strings), `Math` functions (`Round` half to even, .NET's default, or away from zero, of decimals and doubles, also to digits) and their `int`/`long`/`decimal`/`double` counterparts, `double.IsNaN`/`IsInfinity`/`IsFinite`, date and time parts and arithmetic (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`), `TimeSpan` fields and totals (`Days`, `Hours`, ..., `TotalDays`, ..., `Ticks`), `DateTime.Now`/`UtcNow`/`Today`, `DateTimeOffset.Now`/`UtcNow`, `Guid.NewGuid`, `Enum.HasFlag` and `ToString` of numbers and `Guid`. Ticks are 100 ns since `0001-01-01`: `DateTime.Ticks` and `TimeOnly.Ticks` come from `EXTRACT(EPOCH FROM ...)`, and `TimeSpan.FromTicks`, `AddTicks` and `new DateTime(ticks[, kind])` of a computed count become interval arithmetic in microseconds (PostgreSQL keeps no finer unit; the kind has to be a constant other than `Local`, and `Utc` adds `AT TIME ZONE 'UTC'`). `Parse` of `bool` and `Guid`, and of numbers, `DateOnly`, `TimeOnly` and `DateTime` with `CultureInfo.InvariantCulture` (plus constant `NumberStyles`/`DateTimeStyles`), is `CAST(... AS boolean | uuid | bigint | numeric | ... | date | time | timestamp)`: the text has to be in PostgreSQL's input syntax, which also accepts forms .NET rejects, and a `timestamp` cast drops an explicit offset; the current-culture overloads are rejected.
- `Contains` (and `Any(x => x == value)`) on in-memory collections (`IN (...)`) and on array columns (`= ANY (...)`), plus array `Length`, `Count()` and `Any()`.
- Terminal operators: `Count`/`LongCount`, `Sum`, `Average`, `Min`, `Max` (of booleans as `BOOL_AND`/`BOOL_OR`; `Min`/`Max` of `Guid` is rejected, as PostgreSQL orders uuids differently), `Any`, `All`, `Contains`, `First`/`Single`/`Last`/`ElementAt` (and their `OrDefault` forms, which substitute the default of a value type inside a lambda), and aggregates after `DefaultIfEmpty()`.
- The same operators inside lambdas, as correlated or uncorrelated subqueries (`EXISTS`, `IN (SELECT ...)`, scalar subqueries).
- `Join` (`INNER JOIN`; two nullable keys match with `IS NOT DISTINCT FROM`, as null keys match in C#), `GroupJoin` (aggregated groups as correlated subqueries, also after `Where`, `Select`, `OrderBy`, `Distinct` or paging of the group, or `LEFT JOIN` with `SelectMany`/`DefaultIfEmpty`, where a `Where` on the group before `DefaultIfEmpty()`, possibly chained, becomes part of the `ON` condition instead of dropping the unmatched rows) and `SelectMany` (`CROSS JOIN`, correlated `INNER`/`LEFT JOIN`, or `JOIN LATERAL` when the inner source is paged).
- `GroupBy` (with element and result selectors): `GROUP BY` the flattened key, `HAVING` for filters on groups, and aggregates over the group — `Count` (also with a predicate, as `FILTER (WHERE ...)`), `Sum`, `Average`, `Min`, `Max`, `Any`/`All` (`BOOL_OR`/`BOOL_AND`), `Where(...)` (possibly chained) before an aggregate and `Select(...).Distinct()` before `Count`, `Sum` or `Average` (`COUNT(DISTINCT ...)`, `SUM(DISTINCT ...)`, `AVG(DISTINCT ...)`; `Select` after `Distinct` is rejected, as the aggregate would apply to the distinct projected values).
- `Concat`, `Union`, `Intersect` and `Except` as `UNION ALL`, `UNION`, `INTERSECT` and `EXCEPT`, parenthesizing operands that are paged or a different set operation.
- Captured variables and parameter-free subtrees, which are evaluated in memory and rendered as literals.
- One row of aggregates over tables: a constant one-element sequence as the root (`new[] { 0 }.AsQueryable().Select(_ => new { Orders = orders.Count(), Revenue = orders.Sum(o => o.Total) })`) is a `SELECT` of scalar subqueries without `FROM`. The element itself has no column and cannot be read.

Anything else — user-defined methods, culture- or comparison-specific overloads, navigation properties, in-memory sources (other than the one-element root above), operators such as `Zip`, `Aggregate` or `SkipWhile` — throws `NotSupportedException` naming the offending member, method or expression node.

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

## Mapping

A table is named after the element type and its columns after its public properties with a public getter (indexers and write-only properties are skipped), in declaration order. `[Table]` (including its `Schema`), `[Column]` and `[NotMapped]` from `System.ComponentModel.DataAnnotations.Schema` override the defaults. Identifiers are always double-quoted, so their case is kept. A property whose type is not a scalar (or an array of scalars) is rejected, so navigation properties are not supported.

Nullability follows the CLR: `Nullable<T>`, nullable reference types and references without nullable annotations are nullable columns, and so are a conditional with a nullable branch, a scalar subquery or `Min`/`Max` of a reference type, a column of a set operation that is nullable on either side and every column of a left-joined source. C# semantics are preserved where SQL differs — equality with a nullable operand uses `IS [NOT] DISTINCT FROM` (a correlation key may still be compared with `=`, which is false for NULL in both languages), a nullable comparison used as a value becomes `COALESCE(..., FALSE)`, nullable keys sort as `NULLS FIRST` ascending, membership tests (`IN`, `= ANY`) of a nullable value used as a value or under `NOT` become `COALESCE(..., FALSE)`, a nullable string or number is concatenated as `COALESCE(..., '')`, a lifted `^` of booleans stays NULL, `Contains` of a nullable item against a subquery or an array column also matches a NULL of the sequence (`Contains(null)` is true for a sequence holding a null), a left-joined entity compared with `null` tests a column its table requires (or every column), `Select(...).Distinct().Count()` over nullable group values counts NULL as one of them, and `OfType<T>()` drops the nulls of a nullable sequence. Concatenating a `bool`, an enum or a date or time value is rejected, as PostgreSQL formats them differently from .NET.

## Constants are inline literals

The result is a single string, not a command with a parameter collection. Every constant in the tree — literals, captured variables and in-memory collections — is rendered as a safely escaped inline PostgreSQL literal (for example, `'O''Brien'` for the string `O'Brien`; control and non-space white-space characters become `E''` escapes such as `E'a\u00a0b'`). No bind parameters (`$1`, `@p0`) are produced.

## Native AOT and trimming

The package is marked `IsAotCompatible` and builds without trimming or AOT warnings. Translation reads the public properties of the queried element types to find their columns, so `TextValue` is annotated `[RequiresUnreferencedCode]`: in a trimmed or Native AOT application a property that the code never references is removed, and its column silently disappears from `SELECT`. Keep the properties of every mapped type and then acknowledge the `IL2026` warning at the call site, for example:

```csharp
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Order))]
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Order properties are preserved.")]
static string Sql(IQueryable<Order> orders)
{
    return new PostgreSqlQuery(orders.Expression).TextValue;
}
```
