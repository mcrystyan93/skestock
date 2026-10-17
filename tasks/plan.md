# Implementation Plan: Stock count wizard

Status: Implemented 2026-10-17; see [verification](../.scratch/stock-count-wizard/verification.md)

## Objective

Implement the [approved spec](../.scratch/stock-count-wizard/spec.md). The previous plan is
preserved in [goods-receipt-snapshot-expansion-plan.md](goods-receipt-snapshot-expansion-plan.md).

## Decisions and dependencies

Shared FIFO helper → bulk command/validator/handler → endpoint → client HTTP/model → store →
modal → header button → verification.

- **Helper first, with no behavior change.** Move the draw-down/surplus body of
  `AdjustStockCommandHandler` into an `internal static` helper in the `AdjustStock` folder
  (e.g. `StockAdjuster.Apply(dbContext, batches, classId, itemId, locationId, actualQuantity,
  reason, identityId)`). The single-item handler keeps its query, SaveChanges, error mapping and
  DTO building. Existing `AdjustStock*` tests are the safety net, so this ships alone.
- **Bulk handler** loads every `Quantity > 0` batch for the class whose (item, location) is in the
  lines with one query (filter by the item ids and location ids, then group in memory by
  (item, location)), orders each group oldest expiry first, skips no-op lines, calls the helper
  per line, and saves once. On `DbUpdateConcurrencyException` it returns
  `StockErrors.BulkConcurrencyConflict`; the client maps that error code to a Romanian message
  the same way it does for `ConcurrencyConflict`.
- **No migration, no change to `GetClassLocationStock`.** The wizard filters the existing report
  client-side.
- **Client store** is provided by the modal, so every opening starts clean. Row key =
  `${itemId}:${locationId}`. `counts` holds only edited values; a missing key means unchanged.
- **Modal:** one component with three `@switch` blocks; split into child components only if the
  template passes about 150 lines. Footer buttons change per step.
- **Refresh (verified by exploration):** `StockEvents.adjustSuccess` only drives the single-item
  modal, and `StockStore` is component-scoped. So the modal closes with `true` on success and
  `HeaderContainer` dispatches `StockEvents.refresh` in `afterClose` (as `closeClass` does). The
  header summary refreshes via SignalR `stockAdjusted`, so the bulk handler raises
  `StockAdjustedEvent` once per touched location.
- **Endpoint returns 200 empty (`Results<Ok, ProblemHttpResult>` + `ToOk()`),** the Stock.cs
  convention, not 204.

## Tasks

- [ ] **1. Extract the shared FIFO helper**
  - Acceptance: `AdjustStockCommandHandler` calls the helper and its behavior is unchanged.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~AdjustStock"`
    is green with the existing tests untouched; `dotnet build`.
  - Files: `AdjustStock/AdjustStockCommandHandler.cs`, new `AdjustStock/StockAdjuster.cs`.

- [ ] **2. Bulk command, validator, error (tests first)**
  - Acceptance: scaffolded with `dotnet new ca-usecase -n BulkAdjustStock -fn Stock -ut command
    -rt Result` from `src/Application`; command shape and tags as in the spec; the validator
    covers empty lines, more than 1000 lines, duplicate (item, location), negative quantity, bad
    enum and missing class/items/locations, with one query per entity set.
  - Verify: new validator tests green.
  - Files: `BulkAdjustStock/BulkAdjustStockCommand.cs`, `…Validator.cs`, `StockRequests.cs`
    (request + line), `StockErrors.cs`, validator test file.

- [ ] **3. Bulk handler (tests first)**
  - Acceptance: FIFO decrease across batches, a surplus batch on increase, no-op lines skipped,
    mixed locations, one transaction row per touched batch, a single SaveChanges, and the
    concurrency failure mapped to `BulkConcurrencyConflict`.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~BulkAdjustStock"`.
  - Files: `BulkAdjustStock/BulkAdjustStockCommandHandler.cs`, handler test file.

- [ ] **4. Endpoint + functional tests**
  - Acceptance: `POST /api/Stock/class/{classId:guid}/bulk-adjust` returns 200 and persists every
    line; a closed class is rejected; an invalid line saves nothing.
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~BulkAdjustStock|FullyQualifiedName~ClosedClassRejectsChanges"`.
  - Files: `src/Web/Endpoints/Stock.cs`, new `tests/Application.FunctionalTests/Features/Stock/BulkAdjustStockTests.cs`,
    `ClosedClassRejectsChangesTests.cs`.

  **Checkpoint A:** `dotnet build` and all Application unit tests green; the backend is complete.

- [ ] **5. Client contract**
  - Acceptance: `BulkAdjustStockRequest`/line types in `core/models/stock.ts`;
    `StockHttp.bulkAdjustStock(classId, request)`; error code mapped in `error-messages.ro.ts`.
  - Verify: `npm run build`.
  - Files: `core/models/stock.ts`, `shared/stock/services/stock.http.ts`,
    `shared/errors/services/error-messages.ro.ts`, `core/models/error-codes.ts` if codes are listed there.

- [ ] **6. `StockCountState` store (tests first)**
  - Acceptance: loads the report (`includeHidden: true`) and keeps rows with `quantity > 0`;
    `locations` derived from rows; `selectedLocationIds`, `reason`, `counts`, `currentIndex`,
    `step`; computed `rowsInScope` (ordered by location, category, item), `currentRow`,
    `changes`; methods `next`/`previous`/`jumpToSummary`/`goToRow`/`setCount`/`submit`.
    `counts` survive location changes; `submit` exposes success so the modal can close with `true`.
  - Verify: `npm test -- --watch=false stock-count`.
  - Files: `shared/stock/services/stock-count.store.ts` + `.spec.ts`.

- [ ] **7. `StockCountModal`**
  - Acceptance: `nz-steps` with three steps per the spec: step 1 location checkboxes with
    select-all + reason select; step 2 the item card, input, warning/success alert,
    Înapoi/Următorul/Sari la rezumat; step 3 the changed-rows table (row click → item), the
    unchanged count, Finalizează disabled with no changes, an inline error alert, and a close
    confirm when there are unsaved changes. Exported from `@ske/shared/stock`.
    Responsive per spec story 9: the steps are small with vertical labels below `md`; the step 2
    quantities stack below `sm`; the footer wraps; tap targets are at least 40 px on mobile;
    the step 3 table becomes cards below `md` (reuse the stock list's small-cards pattern); long
    names wrap. Breakpoints come from `NzBreakpointService` (as in `Header`) or Tailwind
    prefixes, with no hand-written media queries.
  - Verify: `npm run build`; a manual pass at a mobile width and a desktop width.
  - Files: `shared/stock/ui/modals/count/stock-count-modal.ts` + `.html`, `shared/stock/index.ts`.

- [ ] **8. Header button**
  - Acceptance: "Inventar stoc" is visible only when `selectedTabIndex() === 0 && !isClosed()`,
    as a compact icon button with tooltip or a full button; it opens the modal (centered,
    `modal-100 modal-lg-75`, not mask-closable).
  - Verify: header spec (visible only on tab 0 of an open class); `npm run build`.
  - Files: `overview/header/header.ts`, `header.html`, `header-container.ts`,
    `header-container.html`, header spec.

- [ ] **9. Playwright flow tests**
  - Acceptance: `e2e/specs/stock-count-wizard.spec.ts` covers spec E2E cases 1–7 with the API
    mocked through `page.route('**/api/**')`, reusing the `goods-receipt-snapshot-expansion.spec.ts`
    setup (user info, class, summary, stock report fixtures across two locations, one closed-class
    variant). It asserts the bulk-adjust request body and that exactly one request was sent. It
    runs in both the `chromium` and `mobile` projects.
  - Verify: `npm run e2e -- stock-count-wizard` (app served via AppHost).
  - Files: `e2e/specs/stock-count-wizard.spec.ts`.

- [ ] **10. Playwright responsive tests**
  - Acceptance: in the same spec file, a `describe` that runs only in the `chromium` project and
    loops over widths 360/576/768/992/1280/1600 at height 800. It walks all three steps with long
    names and asserts: no page or modal-body horizontal overflow; footer buttons inside the
    viewport; step titles not clipped; the header button is icon-only below 992 and has text
    from 992; the summary shows a table at ≥ 768 and cards below. It attaches a full-page
    screenshot per step and width to the report. Any layout bug found goes back into the
    Task 7/8 files.
  - Verify: `npm run e2e -- stock-count-wizard --project=chromium`; review the screenshots in
    `npm run e2e:report`.
  - Files: `e2e/specs/stock-count-wizard.spec.ts`, plus fixes in the modal/header files if needed.

  **Checkpoint B:** `npm run build`, `npm test -- --watch=false`, and the full
  `npm run e2e -- stock-count-wizard` green.

- [ ] **11. Verification**
  - Acceptance: every spec success criterion checked; full app run (`dotnet run --project
    src/AppHost`) with a count over two locations that includes a decrease, an increase and the
    jump; the list and summary refresh; screenshots reviewed; `graphify update .`.
  - Files: `.scratch/stock-count-wizard/verification.md`.

Tasks 5–8 depend on 4 only for the endpoint shape, so 5–7 can start after Task 2 fixes the
contract. Tasks 9–10 need 7–8. Execution stays sequential.

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Extracting the helper changes single-item behavior | Task 1 ships alone; existing tests stay untouched |
| A large count makes a heavy validator/handler | Set-based queries, a 1000-line cap |
| Stock changes while the wizard is open | The count is absolute; the rowversion conflict fails the whole save and the values are kept for retry |
| The class lock is held for a long save | One SaveChanges, no per-line round-trips |
| List/summary do not refresh after a bulk save | `refresh` after modal close + per-location `StockAdjustedEvent` for SignalR; verified in Task 11 |
| E2E mocks drift from the real API | The fixtures use the TS model types; functional tests (Task 4) cover the real contract |
| Flaky layout checks (fonts, animations) | Assert geometry with tolerances after the modal animation settles; no pixel baselines |
| Responsive bugs found late | Task 7 builds against the breakpoints; Task 10 is the gate, and its fixes stay in scope |

## Commands

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~AdjustStock"
./run-functional-tests.sh --filter "FullyQualifiedName~BulkAdjustStock"
cd src/Client && npm run build && npm test -- --watch=false
cd src/Client && npm run e2e -- stock-count-wizard        # app served via dotnet run --project src/AppHost
graphify update .
```
