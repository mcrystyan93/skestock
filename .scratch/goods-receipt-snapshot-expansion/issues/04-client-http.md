# Task 04: Add the typed snapshot HTTP contract

Status: Complete — 2026-10-14
Type: task
Blocked by: 03

## Description

Mirror the dedicated snapshot DTO in goods-receipt.ts and extend GoodsReceiptsHttp. Existing model barrel already exports goods-receipt.

## Acceptance Criteria

- [x] Client DTO matches nullable IDs/labels/expiry and all approved scalar fields; no StockBatchId dependency.
- [x] HTTP method requests the exact receipt-ID GET route and returns a typed array through existing credential handling.
- [x] HTTP tests establish URL/method, successful array payload, empty payload and error propagation.

## Verification

- [x] `cd src/Client && npm test -- --watch=false`
- [x] `cd src/Client && npm run build`

## Files Likely Touched

- `src/Client/src/app/core/models/goods-receipt.ts`
- `src/Client/src/app/shared/goods-receipts/services/goods-receipts.http.ts`
- `src/Client/src/app/shared/goods-receipts/services/goods-receipts.http.spec.ts`

Estimated scope: 3 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
