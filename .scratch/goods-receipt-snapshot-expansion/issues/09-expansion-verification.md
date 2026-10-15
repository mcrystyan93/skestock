# Task 09: Verify the complete expansion flow

Status: Complete — 2026-10-14
Type: task
Blocked by: 08

## Description

Extend expansion regressions and add browser coverage using the existing Playwright configuration. Use controlled snapshot responses for UI failures/empty states; functional tests cover actual SQL. Record final evidence and review the diff.

## Acceptance Criteria

- [x] Desktop/mobile tests prove lazy snapshot requests by receipt ID, saved values and no stock-batch request, including concurrent expansion and collapse/reopen.
- [x] Loading, error/retry, manual/legacy empty state, long labels, downloads and keyboard behavior are verified; run AXE checks using available tooling and record any tooling limitation.
- [x] Relevant backend/client tests and builds pass, final review traces every spec criterion, graphify update completes; environment-blocked checks are explicit.

## Verification

- [x] `dotnet test tests/Application.UnitTests --filter FullyQualifiedName~GetGoodsReceiptImportLines`
- [x] `./run-functional-tests.sh --filter FullyQualifiedName~GoodsReceipts`
- [x] `dotnet test tests/Application.UnitTests`
- [x] `dotnet build`
- [x] `cd src/Client && npm test -- --watch=false`
- [x] `cd src/Client && npm run build`
- [x] `dotnet run --project src/AppHost (start stack for browser verification)`
- [x] `cd src/Client && npm run e2e -- e2e/specs/goods-receipt-snapshot-expansion.spec.ts --project=chromium --project=mobile`
- [x] `graphify update .`
- [x] `git diff --check`

## Files Likely Touched

- `src/Client/src/app/shared/goods-receipts/ui/table/small/goods-receipt-list-small.spec.ts`
- `src/Client/e2e/specs/goods-receipt-snapshot-expansion.spec.ts`
- `.scratch/goods-receipt-snapshot-expansion/verification.md`

Estimated scope: 3 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
