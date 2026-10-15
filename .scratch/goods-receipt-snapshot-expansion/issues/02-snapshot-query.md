# Task 02: Implement the cached snapshot query

Status: Complete — 2026-10-14
Type: task
Blocked by: 01

## Description

Scaffold the CQRS query using ca-usecase from src/Application, then implement the approved DTO, validator, read-only handler and cache contract. No migration or command behavior changes.

## Acceptance Criteria

- [x] Query validates nonempty receipt ID, returns the existing not-found error or all linked confirmed snapshots; honors cancellation and uses IApplicationDbContext.
- [x] Dedicated DTO includes every approved field; labels use RawItemText and totals use saved values, ordered by CreatedDate then Id.
- [x] Receipt-specific cache keys, receipt/collection/location tags and randomized five-minute expiration follow the approved plan.

## Verification

- [x] `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~GetGoodsReceiptImportLines`
- [x] `dotnet build`

## Files Likely Touched

- `src/Application/Features/GoodsReceipts/Models/GoodsReceiptImportLineDto.cs`
- `src/Application/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/GetGoodsReceiptImportLinesQuery.cs`
- `src/Application/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/GetGoodsReceiptImportLinesQueryValidator.cs`
- `src/Application/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/GetGoodsReceiptImportLinesHandler.cs`
- `src/Application/Features/GoodsReceipts/CacheConstants.cs`

Estimated scope: 5 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
