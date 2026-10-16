# Implementation Plan: Close a school class with stock rollover

Status: Complete — 2026-10-16.

## Overview

Backend for closing a school class: the remaining positive stock moves to an open class through `Rollover`
transactions, `ClassBalance` is filled in, statistics ignore `Rollover`, and `PUT` can no longer change the `Closed`
status. Source: [approved spec](../.scratch/class-closing/spec.md). Tasks live in `.scratch/class-closing/issues/`.

## Technical decisions

- One new use case `CloseSchoolClass` (generated with `ca-usecase`, then adapted), modeled on `MoveStockCommandHandler`:
  FIFO is not needed (everything moves), but the same split pattern is used: a new batch + a zeroed source + paired transactions + one save.
- No migration and no new dependency. `ClassBalance` is upserted in the handler (no unique index exists).
- Concurrency relies on the existing `StockBatch.Version` rowversion. The class has no token, but a second close running at the same time
  writes the same source batches, so it fails with `ConcurrencyConflict`.
- The status guard for `UpdateSchoolClass` goes in the handler (it needs the persisted status), not the validator.
- The statistics fix ships first and alone: it is a no-op until `Rollover` data exists.

## Dependencies and order

```text
01 statistics ignore Rollover ─┐
                               ├─► 03 CloseSchoolClass command ─► 04 ClassBalance ─┐
02 UpdateSchoolClass guard ────┼───────────────────────────────────────────────────┴─► 05 HTTP + functional ─► 06 verification
```

01 and 02 are independent of each other.

## Tasks

- [x] 01. **Statistics ignore Rollover** — two `Where` filters + unit tests (XS, ~4 files).
- [x] 02. **UpdateSchoolClass closed guard** — new error + handler check + unit tests (S, 3 files).
- [x] 03. **CloseSchoolClass command** — errors, command, validator, handler, unit tests (M, 5 files).
- [x] 04. **ClassBalance on close** — upsert in the same save + unit tests (S, 2 files).
- [x] 05. **HTTP + functional tests** — `POST {id}/close`, functional tests for close and PUT guard (S, ~4 files).
- [x] 06. **Final verification** — build, all Application unit tests, focused functional tests, no pending model changes, graphify.

## Checkpoints

- After 01–02: focused unit tests and the build are green. Existing statistics and UpdateSchoolClass tests are unchanged.
- After 03–04: every `CloseSchoolClass` unit test passes; read the handler diff against the spec's Behavior and ClassBalance sections.
- After 05–06: every spec success criterion is checked. Environment-blocked functional tests are reported as blocked, not as passed.

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Another statistics query treats `Rollover` as consumption | High | Grep showed only the two `Type != Transfer` filters; the task 06 diff review re-checks `StockTransactionType` usages. |
| Duplicate `ClassBalance` rows (no unique index) | Med | Read the existing rows before inserting; the functional test repeats the close (409) and checks there is no double row. |
| The in-memory unit test DB does not check the rowversion | Med | The concurrency path is covered by the functional tests (SQL Server), following the `StockBatchConcurrencyTests` pattern. |
| A large class loads many batches into memory | Low | Acceptable at this school's scale; one query for each set (batches, balances). |

## Open questions

None.
