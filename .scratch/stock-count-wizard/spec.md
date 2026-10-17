# Spec: Stock count wizard

Status: Approved 2026-10-17 (one reason per count, jump-to-summary added, label "Inventar stoc");
amended the same day with Playwright tests and responsive requirements (story 9).

## Objective

Staff do a physical recount of a class's stock in one guided pass instead of opening the
per-item "Ajustează stoc" modal over and over. The wizard walks every item that has stock at
the selected locations, collects the counted quantity, shows a summary, and saves every change
in one atomic request.

### User stories / acceptance criteria

1. **Entry point.** On the school-class overview, when the **Stoc** tab (index 0) is selected and
   the class is not closed, the header shows an extra button **"Inventar stoc"** (icon button with
   tooltip on compact layouts, same as the other header buttons). It is hidden on every other tab
   and on closed classes.
2. **Modal + steps.** Clicking it opens a modal (`nz-modal`, `nzMaskClosable: false`) with
   `nz-steps` showing three steps: **Locații → Articole → Rezumat**.
3. **Step 1, Locații.** Shows the locations that currently hold stock (quantity > 0) for this
   class, as a checkbox list with "select all". At least one must be selected to continue.
   Also holds one **Motiv ajustare** select (`ADJUSTMENT_REASON_OPTIONS`, default Ajustare)
   that applies to the whole count.
4. **Step 2, Articole.** Walks the items one at a time: every (item, location) row with
   `quantity > 0` at the selected locations, ordered by location name, then category name, then
   item name. For each item it shows:
   - item name, category, location, unit, and position ("3 / 27")
   - the current quantity (read-only)
   - an `nz-input-number` (min 0, integer) prefilled with the current quantity
   - a **warning** alert when the new value is lower ("Cantitatea va fi redusă cu N …") and a
     **success** alert when it is higher ("… crescută cu N …"), with the same wording as the
     existing adjust form; no alert when unchanged
   - **Înapoi** / **Următorul** buttons. Înapoi on the first item returns to step 1. Următorul on
     the last item goes to step 3. Entered values are kept when moving back and forth.
   - **Sari la rezumat** jumps straight to step 3 from any item; untouched items count as
     unchanged.
5. **Step 3, Rezumat.** Table of changed rows only: item, location, current → new, delta (colored
   +/-), plus a line "N articole nemodificate". Clicking a row jumps back to that item in step 2.
   **Finalizează** is disabled when nothing changed.
6. **Save.** Finalizează sends all changed rows in **one** request. On success: a success toast,
   the modal closes (with `true`), the stock list reloads via `StockEvents.refresh` dispatched by
   `HeaderContainer`, and the header summary refreshes through the SignalR `stockAdjusted` event.
   On failure: the problem-details error shows inside the modal, nothing is saved, and the entered
   values are kept so the user can retry.
7. **Atomicity.** Either every line is saved or none is (single `SaveChangesAsync`).
8. **Changing locations** in step 1 after entering values keeps the values entered for rows that
   are still in scope.
9. **Responsive at every breakpoint** (NG-ZORRO xs/sm/md/lg/xl/xxl and Tailwind sm–2xl; checked
   at 360, 576, 768, 992, 1280 and 1600 px wide):
   - The modal is full width below `lg` (`modal-100`) and 75% from `lg` (`modal-lg-75`); there is
     no horizontal scroll on the page or inside the modal body.
   - Below `md`, `nz-steps` uses `nzSize="small"` with the vertical label placement, so all three
     step titles fit without overflow or clipping.
   - On step 2 the current/new quantity sit side by side from `sm` and stack below it; the input is
     full width on mobile; long item, category and location names wrap instead of overflowing.
   - The footer buttons wrap onto new lines on narrow screens, every button stays inside the
     viewport, and tap targets are at least 40 px high on mobile, like the existing header buttons.
   - Step 3 shows a table from `md` and stacked cards below `md`, following the stock list's
     existing table/small-cards pattern.
   - The header button follows the header's compact rule: below `lg` it is an icon-only circle
     button with a tooltip and an `aria-label`; from `lg` it has an icon and text.

## Backend: new endpoint

`POST /api/Stock/class/{classId:guid}/bulk-adjust`

```jsonc
// request
{
  "reason": 1,                       // AdjustmentReason, one for the whole count
  "lines": [
    { "itemId": "…", "locationId": "…", "actualQuantity": 12 }
  ]
}
// 200 OK (empty body) on success, matching RemoveExpired/Move in Stock.cs; ProblemDetails on failure
```

- `BulkAdjustStockCommand : IRequest<Result>, ICacheInvalidation, IRequiresOpenClass`,
  `[Authorize]`, scaffolded with `dotnet new ca-usecase -n BulkAdjustStock -fn Stock -ut command -rt Result`.
  Being `IRequiresOpenClass`, it gets the ClassOpen check and the class-row lock for free.
- **Tags:** `CacheConstants.BuildTag(ClassId, locationId)` for each distinct location in the
  lines, plus `CacheConstants.BuildClassTag(ClassId)`.
- **Handler:** the FIFO draw-down / surplus-batch logic in `AdjustStockCommandHandler` moves into
  one shared static helper that both handlers call, so behavior stays identical (one
  `StockTransaction` per touched batch, `Type = Adjustment`, `StockAdjustedEvent` raised once per touched location, which drives the SignalR refresh of the list and header summary).
  Batches for all lines load in one query. A line whose counted quantity already equals the
  current total is skipped (not an error); the count is absolute, so this is still correct if
  stock changed after the wizard loaded. One `SaveChangesAsync`; a
  `DbUpdateConcurrencyException` returns a new `StockErrors.BulkConcurrencyConflict(classId)` (409, code `stock.bulk_concurrency_conflict`; add it to client `error-codes.ts` and `error-messages.ro.ts`).
- **Validator:** `ClassId` not empty; `Lines` not empty and at most 1000 (`ValidationErrorCodes`
  as in `AdjustStockCommandValidator`); each line: ids not empty, `ActualQuantity >= 0`;
  no duplicate (itemId, locationId); `Reason` in enum; class/items/locations exist, checked
  with one query per entity set (not per line).
- `StockRequests.BulkAdjustStockRequest` + `BulkAdjustStockLine`; endpoint in
  `src/Web/Endpoints/Stock.cs` next to `AdjustStock`.

## Frontend

- **Data for the wizard:** reuse `StockHttp.getClassLocationStock` with no location filter
  (`includeHidden: false`, no other filters) and filter client-side to `quantity > 0`. This also
  gives step 1 its location list, so no location API call and no query change are needed.
- `StockHttp.bulkAdjustStock(classId, request)` + `BulkAdjustStockRequest` model in
  `core/models/stock.ts`.
- `StockCountState` signal store (same shape as `StockAdjustmentState`: `withLoadingFeature`,
  `withProblemDetailsFeature`, `rxMethod` + `mapResponse`), provided by the modal; it loads rows,
  holds `selectedLocationIds`, `counts: Record<rowKey, number>`, `currentIndex`, `step`, and
  exposes computed `rowsInScope`, `currentRow`, `changes`.
- `StockCountModal` in `src/Client/src/app/shared/stock/ui/modals/count/` (one component, three
  `@switch` step blocks; split into child components only if the template passes ~150 lines).
- Header: new `onStockCount` output + button in `header.html`; `HeaderContainer.openStockCount()`
  opens the modal (`modal-100 modal-lg-75`, centered, not mask-closable).
- Romanian UI copy, NG-ZORRO components, Tailwind for layout, signal forms only if a form is
  needed (a bare `nz-input-number` bound to the store is enough).

## Tech stack

.NET 10, Mediator (source-generated), FluentValidation, EF Core / SQL Server, HybridCache.
Angular (standalone, signals, `@ngrx/signals`), NG-ZORRO, Tailwind, Vitest, Playwright.

## Commands

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~BulkAdjustStock"
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~AdjustStock"   # regression after the helper extraction
./run-functional-tests.sh --filter "FullyQualifiedName~BulkAdjustStock"
cd src/Client && npm run build && npm test
cd src/Client && npm run e2e -- stock-count-wizard     # needs the app served (dotnet run --project src/AppHost)
graphify update .
```

## Project structure (touched)

```
src/Application/Features/Stock/Commands/BulkAdjustStock/   command, handler, validator
src/Application/Features/Stock/Commands/AdjustStock/        handler uses the shared helper
src/Application/Features/Stock/Models/StockRequests.cs      request + line
src/Application/Common/Errors/StockErrors.cs                BulkConcurrencyConflict
src/Web/Endpoints/Stock.cs                                  POST class/{classId}/bulk-adjust
src/Client/src/app/core/models/stock.ts                     request types
src/Client/src/app/shared/stock/services/                   stock.http.ts, stock-count.store.ts
src/Client/src/app/shared/stock/ui/modals/count/            wizard modal
src/Client/src/app/features/school-classes/overview/header/ button + open modal
tests/Application.UnitTests/Features/Stock/Commands/BulkAdjustStock/
tests/Application.FunctionalTests/Features/Stock/
src/Client/e2e/specs/stock-count-wizard.spec.ts             Playwright flow + responsive tests
```

## Code style

Follow the neighbours: `AdjustStockCommand*` for the backend, `StockAdjustmentState` /
`StockAdjustmentModal` for the client. Example of the intended command shape:

```csharp
[Authorize]
public class BulkAdjustStockCommand : IRequest<Result>, ICacheInvalidation, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => ClassId;

    public Guid ClassId { get; init; }
    public AdjustmentReason Reason { get; init; }
    public IReadOnlyList<BulkAdjustStockLine> Lines { get; init; } = [];

    public IReadOnlyCollection<string> Tags =>
        [
            .. Lines.Select(l => l.LocationId).Distinct().Select(id => CacheConstants.BuildTag(ClassId, id)),
            CacheConstants.BuildClassTag(ClassId)
        ];
}
```

## Testing strategy

- **Unit (Application.UnitTests):** validator (empty lines, duplicates, negative qty, unknown
  item/location/class, > 1000 lines); handler (decrease across several batches FIFO, increase
  creates a surplus batch, no-op line skipped, mixed lines across two locations, one transaction
  row per touched batch). The existing `AdjustStock*` tests must still pass after the extraction.
- **Functional (TestAppHost):** endpoint happy path persists all lines; closed class is rejected
  (add to `ClosedClassRejectsChangesTests`); a failing line saves nothing.
- **Client (Vitest):** store navigation (next/prev bounds, step transitions), `changes` computed,
  values kept across location changes; header button visible only on tab 0 and open classes.
- **E2E (Playwright)** in `src/Client/e2e/specs/stock-count-wizard.spec.ts`. The API is mocked
  with `page.route('**/api/**', …)` like `goods-receipt-snapshot-expansion.spec.ts`, so no seeded
  database is needed (the app still has to be served). The flow tests run in both existing
  projects (`chromium`, `mobile` / Pixel 7):
  1. The button is shown on the Stoc tab, hidden on other tabs, and hidden for a closed class.
  2. Happy path over two locations: pick the reason; decreasing shows the warning alert and
     increasing shows the success alert; Înapoi keeps the entered value; the summary lists only
     changed rows. Finalizează sends exactly one `POST …/bulk-adjust` whose body is asserted,
     then the modal closes and the stock list reloads (`StockEvents.refresh`).
  3. "Sari la rezumat" from the first item; untouched items count as unchanged.
  4. Finalizează is disabled when nothing changed.
  5. A 409/500 from the bulk endpoint shows the error in the modal and keeps the values; a retry
     succeeds.
  6. Closing with unsaved changes asks for confirmation.
  7. Clicking a summary row returns to that item in step 2.
- **Responsive (Playwright, `chromium` project only, to avoid duplication):** for each width
  360/576/768/992/1280/1600 (height 800), walk all three steps with long names and assert:
  `document.documentElement.scrollWidth <= clientWidth`; the modal body has no horizontal
  overflow; every footer button lies inside the viewport; the steps titles are not clipped; the
  summary shows a table at ≥ 768 and cards below. Each step gets a full-page screenshot attached
  to the report (`testInfo.attach`) for visual review; no pixel-baseline snapshots, because
  baselines are brittle across machines and fonts.

## Boundaries

- **Always:** reuse the shared FIFO helper (no copy of the batch logic); one DB round-trip per
  entity set in the validator; run `dotnet build`, the tests above, and `npm run build` before
  calling a task done; `graphify update .` after code changes.
- **Ask first:** any EF schema/migration change (none expected); changing
  `GetClassLocationStock`; adding npm/NuGet packages; changing the existing single-item adjust
  endpoint's contract.
- **Never:** hand-edit migration designer/snapshot files; partial saves; duplicate
  `skestock.Shared.Services` literals.

## Success criteria

- [ ] Button visible only on the Stoc tab of an open class; opens the 3-step modal.
- [ ] Step 2 iterates exactly the `quantity > 0` rows at the selected locations, in the stated
      order, with working Înapoi/Următorul and the warning/success alerts.
- [ ] Step 3 lists only changed rows; Finalizează disabled with no changes.
- [ ] One `POST …/bulk-adjust` saves all changes atomically; stock list and header summary
      refresh; cache for each touched location and the class is invalidated.
- [ ] Closed class → request rejected by the existing ClassOpen behaviour.
- [ ] Single-item adjust behaves exactly as before (its tests pass unchanged).
- [ ] Layout holds at 360/576/768/992/1280/1600 px: no horizontal scroll, buttons in the
      viewport, the summary switches between table and cards at `md`.
- [ ] `dotnet build`, unit + functional tests, `npm run build`, `npm test`, and
      `npm run e2e -- stock-count-wizard` all green.

## Assumptions (confirmed)

1. "Locations" = the locations that currently hold stock for this class, multi-select.
2. One adjustment reason for the whole count, picked in step 1, default **Ajustare**.
3. Items with zero stock are not in the walk (as you said: quantity > 0), so stock "found" for an
   item at 0 still goes through the existing add/adjust flows.
4. Hidden (`hideWhenZeroStock`) rows only exist at zero stock, so they never appear. The report is requested with `includeHidden: false`, because the API returns only hidden rows for `true`.
5. Expired quantity is not shown separately; the count is the total, FIFO consumes
   oldest-expiry batches first, as single adjust does today.
6. Wizard state is in-memory only; closing the modal loses it (with a confirm if there are
   unsaved changes).

## Open questions

None.
