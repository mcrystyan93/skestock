# Task 03: Expose and verify the snapshot endpoint

Status: Complete — 2026-10-14
Type: task
Blocked by: 02

## Description

Add the constrained GUID GET route to GoodsReceipts and verify real HTTP/SQL projection, validation and cache contracts. Reuse functional test setup and reset helpers.

## Acceptance Criteria

- [x] Authenticated GET /api/GoodsReceipts/{id:guid}/import-lines dispatches Mediator with cancellation and existing result/problem mapping; unauthenticated requests are rejected.
- [x] Functional tests verify 404, 200 empty array, receipt isolation, nullable fields and correctly serialized saved quantity/price/expiry/text through real SQL.
- [x] Focused tests establish nonempty-ID validation, distinct cache keys, receipt-level tags, and overlap with existing confirmation/location invalidation tags.

## Verification

- [x] `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~GetGoodsReceiptImportLines`
- [x] `./run-functional-tests.sh --filter FullyQualifiedName~GoodsReceipts`
- [x] `dotnet build`

## Files Likely Touched

- `src/Web/Endpoints/GoodsReceipts.cs`
- `tests/Application.FunctionalTests/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/GetGoodsReceiptImportLinesEndpointTests.cs`
- `tests/Application.UnitTests/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/GetGoodsReceiptImportLinesQueryTests.cs`

Estimated scope: 3 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
