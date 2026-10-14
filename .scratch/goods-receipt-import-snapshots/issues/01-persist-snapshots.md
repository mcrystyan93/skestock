# Task 01: Persist confirmed import snapshots

Status: Complete — 2026-10-14
Type: task
Blocked by: None

## Description
Map import lines in the test context, write failing tests, then implement snapshot creation in the existing confirmation save graph and update stale entity comments.

## Acceptance Criteria
- Reloaded rows match reviewed values and resolved existing/new item references, including split rows and deduplicated new items.
- Extracted names or catalog fallback are saved; invalid snapshot text is rejected before persistence.
- Explicit/derived expiry matches batches and all writes retain one save operation.

## Verification
Run `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~ConfirmGoodsReceiptImport`. Observe new persistence tests fail before implementation and pass afterward.

## Files Likely Touched
- Confirmation handler and validator (validator only if needed for text validation).
- GoodsReceiptImportLine entity comments.
- ConfirmImportTestDbContext.
- ConfirmGoodsReceiptImportCommandHandlerTests.

Estimated scope: Medium, at most five source/test files.

## Comments
Spec approved on 2026-10-14.

Verification: 779 Application unit tests passed; full solution build passed with zero warnings/errors; graph update completed. See `tasks/plan.md` for the complete verification record.
