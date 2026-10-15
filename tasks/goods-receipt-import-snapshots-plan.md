# Plan: Confirmed goods receipt import snapshots

Status: Complete — approved and implemented on 2026-10-14

## Objective
Implement the [approved spec](../.scratch/goods-receipt-import-snapshots/spec.md). The completed previous plan is preserved in [room-configuration-ui-plan.md](room-configuration-ui-plan.md).

## Decisions and Dependencies
1. Map import lines in the existing handler test context, which currently ignores them.
2. Write failing persistence tests, then create a snapshot alongside each batch using the resolved item and effective expiry. Attach through the tracked import collection and retain one SaveChangesAsync.
3. Deserialize extraction once for source names and received date. Include catalog names in the existing item lookup for fallback text. Preserve nonblank source names; use catalog names when extraction or the source index is unavailable. Reject unusable or overlong text before saving without truncation.
4. Update stale entity comments. Verify persisted snapshots remain unchanged after stock edits and repeat confirmation.

Work is sequential because both tasks share the handler tests. No migration or dependency change is needed.

## Task Index
Tasks use the repository Markdown issue tracker:
- [01 Persist snapshots](../.scratch/goods-receipt-import-snapshots/issues/01-persist-snapshots.md)
- [02 Preservation and final verification](../.scratch/goods-receipt-import-snapshots/issues/02-preservation-and-verification.md), depends on 01.

## Checkpoints and Commands
After 01, focused tests verify reloaded snapshots for existing/new items, splits, text fallback and explicit/derived expiry. After 02, run broader verification, review the diff against the spec and update the graph.

```bash
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~ConfirmGoodsReceiptImport
dotnet test tests/Application.UnitTests
dotnet build
graphify update .
```

## Risks and Mitigations
- EF relationship resolution: explicit test mappings and assertions after clearing tracking.
- Missing extraction: catalog-name fallback; keep current received-date fallback.
- Raw text exceeds 500 characters: reject before save, never truncate.
- Mutable stock affects history: independent scalar values and regression tests.
- Repeat confirmation duplicates lines: preserve the existing early return and assert IDs/count/values.
- In-memory tests cannot prove SQL rollback: retain the existing single save operation and report this limitation.

## Open Questions
No unresolved requirements. Plan and tasks approved on 2026-10-14.

## Verification Results
- RED: 11 failing snapshot tests and 9 passing existing tests before implementation.
- GREEN: all 20 initial focused tests passed after snapshot creation was implemented.
- Final Application unit suite: 779 passed, zero failed/skipped; includes four additional snapshot text boundary cases.
- Full solution build: zero warnings and zero errors.
- `git diff --check`: clean.
- `graphify update .`: completed successfully using AST extraction.
- Code review: existing schema and inward dependencies preserved, no extra per-line queries, one save operation, repeat-confirmation early return preserved. No blocking findings.
- Limitation: in-memory persistence tests do not prove SQL rollback; the existing single-save transaction boundary remains unchanged. Immutability is enforced by application behavior, not database write protection.
