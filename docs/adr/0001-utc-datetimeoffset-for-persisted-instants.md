# ADR 0001: UTC `DateTimeOffset` for persisted instants

- **Status:** Accepted
- **Date:** 2026-09-20

## Context

Persisted activity and audit values represent instants, but `DateTime` and SQL Server
`datetime2` do not carry an offset. Calendar values such as school terms and stock
expiry dates are business dates and must not acquire a time zone.

## Decision

Use `DateTimeOffset` end-to-end for persisted instants, including DTOs, queries,
queue/outbox timestamps, and EF SQL Server columns (`datetimeoffset`). Create and
normalize those values in UTC (`DateTimeOffset.UtcNow` or `TimeProvider.GetUtcNow()`).
Use `DateOnly` for business calendar dates and construct date-range boundaries at
`00:00:00 +00:00` when querying instant columns.

`StockTransaction.CreatedAt` remains a distinct activity timestamp; stock reporting
uses it for `LastUpdatedAt` and it is not replaced by `BaseAuditableEntity.CreatedDate`.

## Consequences

The offset is explicit at every boundary and instants survive round trips without
depending on server local time. `DateTimeOffset` does not itself force UTC, so
callers and persistence code must continue to normalize or create values in UTC.
Existing `datetime2` values are migrated as UTC, and the migration's reverse path
converts offsets back to UTC `datetime2` values.
