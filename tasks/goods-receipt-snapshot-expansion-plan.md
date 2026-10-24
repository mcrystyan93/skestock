# Implementation Plan: Goods receipt snapshot expansion

Status: Complete — 2026-10-14; existing baseline limitations recorded in verification.

## Objective

Implement the [approved spec](../.scratch/goods-receipt-snapshot-expansion/spec.md).
The completed previous plan is preserved in
[goods-receipt-import-snapshots-plan.md](goods-receipt-import-snapshots-plan.md).

## Decisions and dependencies

Existing schema → snapshot DTO/query → endpoint → client HTTP → receipt-scoped
store and presentation → expansion integration → verification.

- No migration or backfill. Check receipt existence, then project saved lines from
  confirmed imports linked by ResultingGoodsReceiptId. Missing receipt returns
  404; existing receipt without snapshots returns an empty array.
- Return all rows ordered by CreatedDate, then Id. Use RawItemText as the label,
  nullable current location names, and totals derived from saved scalars.
- Scaffold the query with ca-usecase from src/Application using the approved
  template version. Complete validator and caching manually.
- Cache key includes receipt ID and an import-lines suffix. Use randomized
  five-minute sliding expiration. Tags include GoodsReceiptListTag, a new
  helper-generated receipt-specific tag, and Locations.CacheConstants.LocationListTag.
  Confirmation already invalidates GoodsReceiptListTag; location updates already
  invalidate LocationListTag. Both cover this query without changing commands.
  Test distinct keys and shared invalidation tags.
- Add GET /api/GoodsReceipts/{id:guid}/import-lines to the existing endpoint group,
  retaining authentication, cancellation and ToOk () result mapping.
- Extend the client model and GoodsReceiptsHttp with a dedicated typed method.
- Provide a new store per snapshot container. Compose existing loading and
  problem-details features; use patchState, rxMethod and mapResponse. Receipt
  changes clear old rows/errors and cancel stale requests. Retry uses current ID.
- Presentation receives inputs and supports desktop table/mobile cards. Container
  owns HTTP/store coordination and error/retry UI. Full arrays need no pagination.
- Replace both stock-batch expansion containers in their deferred blocks. Preserve
  controls, downloads, dimensions and formatting. Collapse/re-expand may reuse
  mounted data; errors remain retryable. Operational batch views stay independent.

## Proposed task sequence

Task issues are drafted in the repository Markdown tracker at
`.scratch/goods-receipt-snapshot-expansion/issues/`, with acceptance criteria,
commands, dependencies and file lists. These approved slices are implemented.

| Issue | Deliverable                                                  | Depends on | Expected files                  |
|-------|--------------------------------------------------------------|------------|---------------------------------|
| 01    | Query test context and failing snapshot tests                | —          | 2 test files                    |
| 02    | DTO, query, validator, handler and cache helpers             | 01         | 5 source files                  |
| 03    | Endpoint and HTTP/SQL/cache contract tests                   | 02         | Endpoint and up to 3 test files |
| 04    | Client DTO, HTTP method and request tests                    | 03         | 3 files                         |
| 05    | Receipt-scoped store and lifecycle/error/retry tests         | 04         | 2 files                         |
| 06    | Responsive snapshot presentation and tests                   | 04         | Up to 3 files                   |
| 07    | Snapshot container, loading/error/retry UI and tests         | 05, 06     | Up to 3 files                   |
| 08    | Desktop/mobile expansion integration and export              | 07         | Up to 5 files                   |
| 09    | Expansion regressions, browser verification and final review | 08         | Up to 3 test/verification files |

Split any slice that exceeds approximately five files during task drafting.
Task 01 is intentionally red until 02; subsequent integration boundaries must
build and preserve existing behavior. Store and presentation are independently
implementable after the DTO is fixed, but execution will be sequential.

## Verification checkpoints

- After 02: NUnit tests verify missing/empty receipts, isolation, splits, nullable
  references, deterministic ordering, validator and cache keys. Stock quantity,
  price and expiry edits and catalog item-name changes do not change snapshot
  scalar values or labels. Backend builds.
- After 03: real SQL and authenticated HTTP tests establish projection, 404,
  empty arrays, authentication rejection and serialization. Assert query tags
  overlap confirmation/location invalidation tags and keys isolate receipts.
- After 07: client tests establish URL, input changes, independent store instances,
  cancellation, retry and distinct loading/error/empty states; client builds.
- After 09: browser checks desktop/mobile, concurrent expansions, collapse/reopen,
  long labels, keyboard access, layout and downloads. Expanded rows request no
  stock-batch endpoint. Review every approved success criterion and update graphify.

From repository root:

```bash
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~GetGoodsReceiptImportLines
./run-functional-tests.sh --filter FullyQualifiedName~GoodsReceipts
dotnet test tests/Application.UnitTests
dotnet build
dotnet run --project src/AppHost
graphify update .
```

From src/Client:

```bash
npm run build
npm test -- --watch=false
```

Use npm ci when clean installation is needed. Record environment-blocked checks.

## Risks and mitigations

| Risk                                           | Mitigation                                                            |
|------------------------------------------------|-----------------------------------------------------------------------|
| Old/manual receipts have no snapshots          | Explicit empty state, no fallback or reconstruction                   |
| SQL projection differs from in-memory provider | Functional test through real SQL and HTTP                             |
| Catalog labels appear historical               | Saved raw item text; document current location labels                 |
| Concurrent expansion overwrites rows           | Container-local stores and stale-request cancellation tests           |
| Errors appear as empty success                 | Separate problem details and retry controls                           |
| Nested responsive layout clips rows            | Desktop/mobile browser checks, long labels and multiple expansions    |
| Confirmation leaves stale cached empty lines   | Existing confirmation collection tag covers each snapshot cache entry |

## Review gates and open questions

No unresolved product questions remain. The plan is approved and issue files
are drafted; tasks were approved before implementation, following the invoked
spec-driven-development skill. Implementation is complete.

## Task issue index

- [01: Specify snapshot query regressions](../.scratch/goods-receipt-snapshot-expansion/issues/01-query-regressions.md)
- [02: Implement the cached snapshot query](../.scratch/goods-receipt-snapshot-expansion/issues/02-snapshot-query.md)
- [03: Expose and verify the snapshot endpoint](../.scratch/goods-receipt-snapshot-expansion/issues/03-snapshot-endpoint.md)
- [04: Add the typed snapshot HTTP contract](../.scratch/goods-receipt-snapshot-expansion/issues/04-client-http.md)
- [05: Isolate snapshot loading per receipt](../.scratch/goods-receipt-snapshot-expansion/issues/05-snapshot-store.md)
- [06: Render responsive snapshot rows](../.scratch/goods-receipt-snapshot-expansion/issues/06-snapshot-presentation.md)
- [07: Wire the snapshot container and retry](../.scratch/goods-receipt-snapshot-expansion/issues/07-snapshot-container.md)
- [08: Switch receipt expansion to snapshots](../.scratch/goods-receipt-snapshot-expansion/issues/08-expansion-integration.md)
- [09: Verify the complete expansion flow](../.scratch/goods-receipt-snapshot-expansion/issues/09-expansion-verification.md)

## Checkpoint checklist

- [x] After 02: focused query tests and backend build pass.
- [x] After 03: authenticated HTTP/SQL and cache contracts verified.
- [x] After 07: HTTP/store/presentation/container tests and client build pass.
- [x] After 09: complete regression suite, browser verification, final review and graph update recorded.

## Verification results

See [verification](../.scratch/goods-receipt-snapshot-expansion/verification.md):
786 Application unit tests, 17 receipt functional tests, 17 focused client tests
and 6 desktop/mobile browser cases passed. Solution and Angular builds passed.
Snapshot AXE scans have no violations. Full client suite retains two unrelated
room-section failures; the broader desktop receipt scan retains existing header
metadata contrast issues. Graphify update completed.
