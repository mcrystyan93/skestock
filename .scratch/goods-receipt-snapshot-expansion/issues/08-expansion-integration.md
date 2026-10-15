# Task 08: Switch receipt expansion to snapshots

Status: Complete — 2026-10-14
Type: task
Blocked by: 07

## Description

Replace StockBatchesTableContainer imports and template usage in both receipt layouts. Export the new container through the goods-receipts barrel if needed.

## Acceptance Criteria

- [x] Desktop and mobile deferred expansion mount the snapshot container with goods receipt ID; expansion no longer loads batches.
- [x] Existing expandedReceiptId behavior, multi-row toggles, downloads, headers and layout dimensions remain functional.
- [x] Operational stock-batch tables/services are unchanged; integrated client builds and existing tests pass.

## Verification

- [x] `cd src/Client && npm test -- --watch=false`
- [x] `cd src/Client && npm run build`

## Files Likely Touched

- `src/Client/src/app/shared/goods-receipts/ui/table/table.ts`
- `src/Client/src/app/shared/goods-receipts/ui/table/table.html`
- `src/Client/src/app/shared/goods-receipts/ui/table/small/goods-receipt-list-small.ts`
- `src/Client/src/app/shared/goods-receipts/ui/table/small/goods-receipt-list-small.html`
- `src/Client/src/app/shared/goods-receipts/index.ts`

Estimated scope: 5 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
