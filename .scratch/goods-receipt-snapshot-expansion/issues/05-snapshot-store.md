# Task 05: Isolate snapshot loading per receipt

Status: Complete — 2026-10-14
Type: task
Blocked by: 04

## Description

Create a container-scoped SignalStore using existing loading/problem-details features, patchState, rxMethod and mapResponse. Keep current receipt ID for retry.

## Acceptance Criteria

- [x] Separate store instances retain independent rows, IDs, loading and errors during simultaneous receipt requests.
- [x] Receipt-ID change clears previous data/errors, cancels stale requests and prevents obsolete responses from overwriting the current receipt.
- [x] Success, empty success and failure are distinct; retry clears the old error and reloads the current ID without any batch request.

## Verification

- [x] `cd src/Client && npm test -- --watch=false`
- [x] `cd src/Client && npm run build`

## Files Likely Touched

- `src/Client/src/app/shared/goods-receipts/services/goods-receipt-import-lines.store.ts`
- `src/Client/src/app/shared/goods-receipts/services/goods-receipt-import-lines.store.spec.ts`

Estimated scope: 2 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
