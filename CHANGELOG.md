# Changelog

All notable changes to `Linq.Expressions.Translation.PostgreSql` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Translation of `TakeLast` (a reversed `LIMIT` in a derived table) and `SkipLast` (`LIMIT` by a row-count subquery).
- Translation of `DefaultIfEmpty()` before `Count`/`LongCount`, `Sum`, `Average`, `Min`, `Max` and `Any`, substituting the default for an empty sequence.
- Translation of `Abs`, `Min`, `Max` and `Clamp` on `int`, `long`, `decimal` and `double`, `Math.Clamp`, `Math.BigMul`, `double.IsNaN`/`IsInfinity`/`IsFinite`, and checked subtraction and multiplication.
- Translation of `string.IndexOf` with a start index and count, `LastIndexOf`, `IndexOfAny`, `Remove`, trimming of several characters, and `Contains`/`StartsWith`/`EndsWith` with `StringComparison.Ordinal`.
- Translation of `Millisecond`/`Microsecond`, `AddMilliseconds`, `AddTicks` (whole microseconds), `Add(TimeSpan)`, `DateTime.TimeOfDay`, `DateTimeOffset.Date`/`UtcDateTime`/`Now`, `DateOnly.DayNumber` and `TimeOnly.FromDateTime`.
- `Any()` without a predicate on a group.

### Fixed

- `FirstOrDefault`, `SingleOrDefault`, `LastOrDefault` and `ElementAtOrDefault` of a non-nullable value inside a lambda substitute the type's default instead of `NULL` when no row matches.
- `Any`/`All` on a filtered group are `FALSE`/`TRUE` instead of `NULL` when no row matches the filter.
- Chained `Where` calls on a group combine their predicates instead of keeping only the last one.
- `Cast` to another numeric type converts the values, and `OfType` of an incompatible type returns no rows instead of every row.
- `DateOnly.AddMonths`/`AddYears` return a `date`, and shift counts are masked to the operand width as in C#.
- Struct initializers such as `new ValueTuple<int, string> { Item1 = ..., Item2 = ... }` are translated instead of throwing `NullReferenceException`. Members of constructor projections (`new ValueTuple<,>(...)`, `new Tuple<,>(...)`) resolve to the argument whose name matches ignoring case, an initializer replaces the constructor argument it sets, and an unmatched member throws `NotSupportedException` instead of `KeyNotFoundException`.

## [0.1.0-preview.0.1.0] - 2026-10-15

### Added

- `PostgreSqlQuery` — accepts the `Expression` of an `IQueryable<T>` and exposes the PostgreSQL `SELECT` statement for it as `TextValue`.
- Translation of filtering, projection, ordering and paging: `Where`, `Select`, `OrderBy`/`ThenBy` (and their descending forms), `Skip`, `Take`, `Distinct`, `Reverse`, `Cast` and `OfType`, with derived tables when an operator cannot extend the current `SELECT`.
- Translation of comparison, logical, arithmetic, bitwise and conditional operators, `??`, numeric and enum conversions, string concatenation and `Nullable<T>` members, preserving C# null semantics.
- Translation of `string` members, `Math` functions, date and time parts and arithmetic, `DateTime.Now`/`UtcNow`, `Guid.NewGuid`, `Enum.HasFlag` and `ToString` of numbers and `Guid`.
- Translation of `Contains` on in-memory collections into `IN (...)` (with a separate `IS NULL` check for null elements) and on array columns into `= ANY (...)`.
- Translation of terminal operators — `Count`, `LongCount`, `Sum`, `Average`, `Min`, `Max`, `Any`, `All`, `Contains`, `First`, `Single`, `Last`, `ElementAt` and their `OrDefault` forms — into a single statement, and of the same operators inside lambdas into `EXISTS`, `IN (SELECT ...)` and scalar subqueries.
- Translation of `Join`, `GroupJoin` and `SelectMany` into inner, left, cross and lateral joins, and of aggregates over a `GroupJoin` group into correlated subqueries.
- Translation of `GroupBy` into `GROUP BY` and `HAVING`, with aggregates over groups (`FILTER (WHERE ...)`, `COUNT(DISTINCT ...)`, `BOOL_OR`/`BOOL_AND`); returning `IGrouping` results is rejected.
- Translation of `Concat`, `Union`, `Intersect` and `Except` into `UNION ALL`, `UNION`, `INTERSECT` and `EXCEPT`.
- Inline literals for numbers, strings, booleans, enums, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` and arrays; captured variables and parameter-free subtrees are evaluated in memory.
- Table and column mapping by type and property names, overridable with `[Table]`, `[Column]` and `[NotMapped]`.
- `NotSupportedException` naming the offending member, method or node for constructs that cannot be translated.
