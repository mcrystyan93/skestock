# Task 07: Wire the snapshot container and retry

Status: Complete — 2026-10-14
Type: task
Blocked by: 05, 06

## Description

Provide the new store locally in a receipt-ID container, load when mounted in expansion, and bind presentation inputs. Reuse the existing problem-details UI and provide retry.

## Acceptance Criteria

- [x] Mounted receipt ID triggers loading; input changes update the store and rendered data; independent containers never share row state.
- [x] Container coordinates loading/error/empty success explicitly and provides accessible retry for the current receipt.
- [x] Data is passed to presentation through typed inputs; no batches, filtering, pagination or extraction reconstruction.

## Verification

- [x] `cd src/Client && npm test -- --watch=false`
- [x] `cd src/Client && npm run build`

## Files Likely Touched

- `src/Client/src/app/shared/goods-receipts/ui/import-lines/import-lines-container.ts`
- `src/Client/src/app/shared/goods-receipts/ui/import-lines/import-lines-container.html`
- `src/Client/src/app/shared/goods-receipts/ui/import-lines/import-lines-container.spec.ts`

Estimated scope: 3 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
