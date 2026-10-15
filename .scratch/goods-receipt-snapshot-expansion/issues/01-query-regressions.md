# Task 01: Specify snapshot query regressions

Status: Complete — 2026-10-14
Type: task
Blocked by: None

## Description

Create a dedicated EF test context and query regression fixture without changing the existing receipt-list context. Map imports, lines, receipts, locations, items and batches required for the scenarios.

## Acceptance Criteria

- [x] Tests distinguish absent receipts from existing receipts with no lines and isolate confirmed lines by resulting receipt ID.
- [x] Tests cover split rows, nullable references, CreatedDate/Id ordering and saved quantity/price/expiry/text after batch and item-name edits.
- [x] New query tests are intentionally red until task 02; no existing tests or assertions are weakened.

## Verification

- [x] `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~GetGoodsReceiptImportLines (record expected pre-implementation failure; rerun after 02).`

## Files Likely Touched

- `tests/Application.UnitTests/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/GoodsReceiptImportLinesTestDbContext.cs`
- `tests/Application.UnitTests/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/GetGoodsReceiptImportLinesHandlerTests.cs`

Estimated scope: 2 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
