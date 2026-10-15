# Changelog

All notable changes to `Linq.Expressions.Translation.PostgreSql` are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `PostgreSqlQuery` — accepts the `Expression` of an `IQueryable<T>` and exposes the PostgreSQL `SELECT` statement for it as `TextValue`. The translation is not implemented yet: `TextValue` throws `NotImplementedException`.
