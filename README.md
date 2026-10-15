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

## Dependencies

None beyond the .NET base class library.

## Target frameworks

`net8.0`, `net9.0`, `net10.0`.
