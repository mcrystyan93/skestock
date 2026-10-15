# Task 06: Render responsive snapshot rows

Status: Complete — 2026-10-14
Type: task
Blocked by: 04

## Description

Create an input-driven presentation component with desktop table and mobile cards, selecting layout through existing responsive conventions. Reuse NG-ZORRO and Romanian formatting.

## Acceptance Criteria

- [x] Displays RawItemText, destination, quantity, unit price and line total; distinct snapshot IDs preserve split rows.
- [x] Null destination uses a neutral accessible placeholder; loading and approved Romanian empty state are distinct; expiry stays in DTO without a new column.
- [x] Component owns no HTTP/store side effects; semantic markup, long labels and desktop/mobile layout retain accessibility and keyboard readability.

## Verification

- [x] `cd src/Client && npm test -- --watch=false`
- [x] `cd src/Client && npm run build`

## Files Likely Touched

- `src/Client/src/app/shared/goods-receipts/ui/import-lines/import-lines.ts`
- `src/Client/src/app/shared/goods-receipts/ui/import-lines/import-lines.html`
- `src/Client/src/app/shared/goods-receipts/ui/import-lines/import-lines.spec.ts`

Estimated scope: 3 source/test/verification files.

## Comments

Spec, plan and tasks approved on 2026-10-14. Implemented and verified; see [verification](../verification.md) for results and existing baseline limitations.
