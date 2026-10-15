# Linq.Expressions.Translation.PostgreSql

Translates the expression tree of an `IQueryable<T>` into a single PostgreSQL `SELECT` statement.

[![.NET build & test](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/build-and-test.yml)
[![Build and Deploy](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Linq.Expressions.Translation.PostgreSql/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Linq.Expressions.Translation.PostgreSql)](https://www.nuget.org/packages/Linq.Expressions.Translation.PostgreSql)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Status

Translated so far:

- `Where`, `Select`, `OrderBy`/`OrderByDescending`/`ThenBy`/`ThenByDescending`, `Skip`, `Take`, `Distinct`, `Reverse`, `Cast` and `OfType`; an operator that cannot extend the current `SELECT` (for example `Where` after `Take`) wraps it into a derived table.
- Comparison, logical, arithmetic, bitwise and conditional operators, `??`, conversions, string concatenation and `Nullable<T>` members.
- `string` members (`Length`, case conversion, trimming, `Substring`, `IndexOf`, `Replace`, `Contains`/`StartsWith`/`EndsWith` as `LIKE` with escaped wildcards, `IsNullOrEmpty`, `Concat`), `Math` functions, date and time parts and arithmetic (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`), `DateTime.Now`/`UtcNow`, `Guid.NewGuid`, `Enum.HasFlag` and `ToString` of numbers and `Guid`.
- `Contains` on in-memory collections (`IN (...)`) and on array columns (`= ANY (...)`), plus array `Length`.
- Terminal operators: `Count`/`LongCount`, `Sum`, `Average`, `Min`, `Max`, `Any`, `All`, `Contains`, `First`/`Single`/`Last`/`ElementAt` (and their `OrDefault` forms).
- The same operators inside lambdas, as correlated or uncorrelated subqueries (`EXISTS`, `IN (SELECT ...)`, scalar subqueries).
- Captured variables and parameter-free subtrees, which are evaluated in memory and rendered as literals.

Joins, grouping and set operations are not translated yet; they throw `NotSupportedException`.

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

A table is named after the element type and its columns after its public properties, in declaration order. `[Table]` (including its `Schema`), `[Column]` and `[NotMapped]` from `System.ComponentModel.DataAnnotations.Schema` override the defaults. Identifiers are always double-quoted, so their case is kept. A property whose type is not a scalar (or an array of scalars) is rejected, so navigation properties are not supported.

Nullability follows the CLR: `Nullable<T>` and nullable reference types are nullable columns. C# semantics are preserved where SQL differs — equality with a nullable operand uses `IS [NOT] DISTINCT FROM`, a nullable comparison used as a value becomes `COALESCE(..., FALSE)`, nullable keys sort as `NULLS FIRST` ascending, and a nullable string is concatenated as `COALESCE(..., '')`.

## Constants are inline literals

The result is a single string, not a command with a parameter collection. Every constant in the tree — literals, captured variables and in-memory collections — is rendered as a safely escaped inline PostgreSQL literal (for example, `'O''Brien'` for the string `O'Brien`). No bind parameters (`$1`, `@p0`) are produced.

## Dependencies

None beyond the .NET base class library.

## Target frameworks

`net8.0`, `net9.0`, `net10.0`.
