# Verification: Goods receipt snapshot expansion

Date: 2026-10-14
Status: Implemented and verified; existing unrelated client baseline issues recorded below.

## Result

GET `/api/GoodsReceipts/{id:guid}/import-lines` returns saved lines from confirmed
imports linked to the receipt. The Application query validates receipt ID, distinguishes
404 from an existing receipt with no snapshots, projects nullable references, calculates
totals from saved scalars and orders by CreatedDate then Id. No batch or extraction
reconstruction is used.

Desktop and mobile expansion use a dedicated HTTP method, locally provided snapshot
store and responsive presentation. Store instances isolate concurrent receipts, cancel
obsolete requests and expose retry. RawItemText is the label, location names are current
catalog values, and missing snapshots have the approved Romanian empty state. Desktop
expansion uses a labeled native button with aria-expanded/aria-controls. Long labels
wrap despite the existing global table ellipsis/nowrap rule.

## Checks

| Check | Result |
|---|---|
| Application unit suite | 786 passed, 0 failed/skipped |
| Focused query/validator/cache tests | 7 passed |
| GoodsReceipts functional suite with Podman, SQL and HTTP | 17 passed |
| Focused client receipt suite | 17 passed across 5 files |
| Desktop/mobile Playwright expansion scenarios | 6 passed |
| Full solution build | Passed, 0 warnings/errors |
| Production Angular build | Passed, 4 existing unused-import warnings |
| Full client suite | 247 passed, 2 unrelated room-section failures |
| Snapshot AXE WCAG A/AA scan, desktop and mobile | 0 violations |
| Browser page errors during snapshot audit | None |
| AST-only graphify update | Completed after allowing local worker IPC |
| git diff --check | Passed |

Backend regressions were red against an empty handler before implementation. Client
HTTP/store tests were red before their methods/store existed; presentation/container
tests were red against CLI scaffolds. The long-label browser regression reproduced
clipped desktop text before the targeted whitespace override and passes afterward.

## Boundary evidence

- SQL/HTTP tests establish authenticated access, anonymous rejection, validation, 404,
  empty array, split-row isolation, decimal totals, DateOnly serialization and null references.
- The functional test reads cached snapshot rows, renames a location through its normal
  Mediator command, and reads the new label without changing saved scalar values. This
  verifies real HybridCache tag invalidation through the existing location collection tag.
- Unit tests mutate stock quantity/price/expiry and catalog item name and reload snapshots;
  saved text/scalars remain unchanged. Another fixture excludes other receipts and
  unconfirmed imports and verifies deterministic order and nullable references.
- Browser tests mock API responses and exercise the actual client through the existing
  AppHost-served dev client. They establish lazy receipt-ID requests, no stock-batch calls,
  concurrent expansion, keyboard activation, collapse/reopen, empty state and retry.
  HTTP functional tests separately verify the actual backend/SQL behavior.
- A controlled delayed browser response verifies loading before saved rows arrive.
- Desktop/mobile screenshots were visually inspected:
  [desktop](verification/desktop.png), [mobile](verification/mobile.png).

## Existing baseline limitations

The full client suite fails two tests in
`src/Client/src/app/features/class-configuration/rooms/rooms-section.spec.ts`:

- `submits all three values through the configuration store`: the expected numeric input
  is absent from the rendered fixture.
- `does not send an invalid configuration to the store`: saveRooms is called with the
  fixture's existing valid values.

These failures also reproduce when running only that untouched fixture. `git diff HEAD`
for the room components/tests is empty. No room code or tests were changed for this work.

The Angular build reports existing unused imports in LoginPage, the category-import
large table, ItemImportTable and the operational stock-batch table.

An AXE scan of the entire desktop receipt tab additionally reports three color-contrast
findings in existing receipt header metadata (`ant-typography-secondary`, contrast 4.12
instead of 4.5). The new snapshot panels have no findings, and the full mobile receipt
scan has none. Header metadata styles were preserved. These baseline issues are not
silenced, and this report does not claim the entire page or client suite passes.

No database migration, backfill or project dependency changes. Manual and legacy receipts
without saved snapshots show the empty state. Original document order and independently
historical location/catalog labels remain outside the approved schema scope.

## Final code review

Reviewed correctness, readability, architecture, security and performance against the
approved spec. Query uses two bounded-by-receipt database reads, no tracking and cancellation;
no per-line queries. Endpoint retains group authorization and existing result mapping.
Client templates render text through Angular interpolation; state and HTTP stay out of
presentation. Snapshot response is a complete receipt array as approved, with no generic
pagination or batch abstractions. Cache keys isolate receipts and tags cover confirmation
and location edits. No suppressions, skipped/deleted tests, generated migration edits or
unrelated working-tree changes were introduced. No blocking findings in the implemented scope.
