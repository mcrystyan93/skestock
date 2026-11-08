# Todo: Merge Items

Plan: `tasks/item-merge-plan.md` (decisions D1-D9 referenced below). Spec: `tasks/item-merge-spec.md`.
Implementer for **every** task: **sonnet**. Backend tasks are TDD: write the failing test first, run it red, then implement.
UI line (applies where marked **[UI]**): *Use skills frontend-ui-engineering and angular-developer; check nz-* APIs in .github/instructions/llms-full.txt; take screenshots (desktop+mobile) to screenshot-checks/item-merge/.*
Commit after each task on the feature branch. Never Prettier client files. Romanian UI copy.

---

## Phase 0

### T0: Feature branch — implementer: sonnet
- [ ] `git checkout -b feature/item-merge` from up-to-date `master`; do not commit the unrelated `deploy-mini-pc.sh` change.
- Verify: `git branch --show-current` prints `feature/item-merge`.

---

## Phase 1: Backend core

### T1: Errors, command, validator, merger contract — implementer: sonnet
Tests first: `MergeItemsCommandValidatorTests` (empty ids, 1 id, duplicate ids collapsing to 1, primary not in set, valid set).
- [x] `ItemErrors.MergePrimaryInactive` (code `items.merge_primary_inactive`, metadata like siblings).
- [x] `MergeItemsCommand` per D6 (with `[Authorize(Roles = Roles.Administrator)]`, `ICacheInvalidation` tags per D5), validator with `ValidationErrorCodes`.
- [x] `IItemReferenceMerger`, `ItemMergeImpact`, `ItemMergeKind` per D2 (no implementations yet). Handler not yet written (T6).
- Files: `src/Application/Common/Errors/ItemErrors.cs`, `src/Application/Features/Items/Commands/MergeItems/{MergeItemsCommand,MergeItemsCommandValidator}.cs`, `.../MergeItems/Mergers/IItemReferenceMerger.cs`, `tests/Application.UnitTests/Features/Items/Commands/MergeItems/MergeItemsCommandValidatorTests.cs`
- Verify: `dotnet build && dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~MergeItems"`
- Note: the source-generated Mediator needs a handler for every request to build; if the build fails, add a handler stub returning `Result.Ok()` and replace it in T6.

### T2: Plain re-point mergers — implementer: sonnet
Tests first (in-memory; reuse the public `ItemTestDbContext` from the DisableItem test namespace, do not copy it again): each merger re-points only secondary rows, leaves other items' rows alone, `PreviewAsync` counts match, `OrderListLine` with null `ItemId` and name snapshots untouched.
- [x] `StockBatch`, `StockTransaction` (add `shortcut:` comment per D1), `GoodsReceiptImportLine.MatchedItemId`, `OrderListLine.ItemId` mergers (optional tiny shared generic base, D2).
- Files: `.../MergeItems/Mergers/{StockBatch,StockTransaction,GoodsReceiptImportLine,OrderListLine}ItemMerger.cs` (+ base if used), `tests/.../MergeItems/Mergers/PlainItemMergerTests.cs`
- Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~MergeItems"`

### T3: Keep-survivor mergers — implementer: sonnet
Tests first: no collision -> re-pointed; primary-vs-secondary collision -> primary row kept, secondary deleted; secondary-vs-secondary collision (no primary row) -> one survivor re-pointed, other deleted; preview reports `Combined`.
- [x] `ClassItemStockVisibility` (key ClassId, LocationId), `ItemLocationThreshold` (key LocationId) per D3.
- Files: `.../Mergers/{ClassItemStockVisibility,ItemLocationThreshold}ItemMerger.cs`, `tests/.../Mergers/KeepSurvivorItemMergerTests.cs`
- Verify: same filter as T2.

### T4: Combining mergers — implementer: sonnet
Tests first: same three collision cases as T3, asserting summed values; ClassBalance `ClosingQty` null/non-null combinations; per-(class, location) balance totals unchanged.
- [x] `SupplyListLine` (sum Quantity), `DailyItemConsumption` (sum Quantity, TotalValue; max ComputedAt), `ClassBalance` (D3 deviation: sum OpeningQty, ClosingQty).
- Files: `.../Mergers/{SupplyListLine,DailyItemConsumption,ClassBalance}ItemMerger.cs`, `tests/.../Mergers/CombiningItemMergerTests.cs`
- Verify: same filter as T2.

### T5: Purchase statistic merger — implementer: sonnet
Tests first: collision on (Scope, ClassId) incl. null ClassId; sums; averages recomputed with `PurchaseStatisticsCalculator` formulas incl. zero-quantity guard; LastPurchasedAt = max; AverageIntervalDays = survivor's.
- [ ] `ItemPurchaseStatisticItemMerger` per D3.
- Files: `.../Mergers/ItemPurchaseStatisticItemMerger.cs`, `tests/.../Mergers/ItemPurchaseStatisticItemMergerTests.cs`
- Verify: same filter as T2.

### T6: Handler orchestration, lock, events, DI — implementer: sonnet
Tests first (`MergeItemsCommandHandlerTests`, in-memory, real mergers from DI list): happy path (every table re-pointed, secondaries inactive, `ItemDisabledEvent` per secondary); unknown id -> `ItemNotFound`; inactive primary -> `MergePrimaryInactive` and nothing changed; already-inactive secondary accepted; idempotent second run succeeds with no changes; `AuthorizationBehaviourTests`-style check that a non-admin is rejected (or assert the attribute is present if that is the existing pattern).
- [ ] `ClassLocking.ExclusiveLockAllClasses` per D4.
- [ ] Handler: `InTransactionAsync` -> lock -> load items -> checks -> each merger `MergeAsync` -> disable secondaries + raise events -> one `SaveChangesAsync`.
- [ ] Register all mergers in `src/Application/DependencyInjection.cs` (do not touch pipeline order).
- [ ] Verify stock query cache tags (D5) and extend `MergeItemsCommand.Tags` if needed.
- Files: `src/Application/Common/Behaviours/ClassLocking.cs`, `.../MergeItems/MergeItemsCommandHandler.cs`, `src/Application/DependencyInjection.cs`, `.../MergeItems/MergeItemsCommand.cs`, `tests/.../MergeItems/MergeItemsCommandHandlerTests.cs`
- Verify: `dotnet build && dotnet test tests/Application.UnitTests`

### Checkpoint A (after T1-T6)
- [ ] `dotnet build` clean (warnings are errors); `dotnet test tests/Application.UnitTests` green.
- [ ] Every row of the D3 table has a merger and tests, incl. secondary-vs-secondary.
- [ ] Commit; report status to the user before Phase 2.

---

## Phase 2: Backend API

### T7: Preview query — implementer: sonnet
Tests first: counts per kind equal what T6 merge changes for the same seeded data (seed once, preview, merge, compare); zero-count kinds omitted; same validation and `ItemNotFound` as the command; nothing is written.
- [ ] `GetItemMergePreviewQuery` (+ handler, validator, `ItemMergePreviewDto`) per D7; not cacheable.
- Files: `src/Application/Features/Items/Queries/GetItemMergePreview/{GetItemMergePreviewQuery,GetItemMergePreviewQueryHandler,GetItemMergePreviewQueryValidator,ItemMergePreviewDto}.cs`, `tests/Application.UnitTests/Features/Items/Queries/GetItemMergePreview/GetItemMergePreviewQueryHandlerTests.cs`
- Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~ItemMerge"`

### T8: Endpoints + functional tests — implementer: sonnet
Tests first (`tests/Application.FunctionalTests/Features/Items/Commands/MergeItems/`, pattern from `DisableItem`): admin success on real SQL Server with collisions in every unique table in one merge (proves EF delete/update ordering, plan risk table); data in a closed class re-pointed; per-class/location stock totals unchanged; non-admin -> 403 for both endpoints; invalid set -> validation problem; atomicity (forced failure leaves data untouched); preview counts match.
- [ ] `POST /api/Items/merge` (`NoContent` / `ToProblemHttpResult()`), `POST /api/Items/merge-preview` in `src/Web/Endpoints/Items.cs`.
- Files: `src/Web/Endpoints/Items.cs`, `tests/Application.FunctionalTests/Features/Items/Commands/MergeItems/MergeItemsCommandTests.cs`, `tests/Application.FunctionalTests/Features/Items/Queries/GetItemMergePreview/GetItemMergePreviewQueryTests.cs`
- Verify: `dotnet build && ./run-functional-tests.sh --filter "FullyQualifiedName~ItemMerge|FullyQualifiedName~MergeItems"` (start AppHost first if "Seed administrator not found").

### T9: removed per D8 (no roles endpoint)

### Checkpoint B (after T7-T8)
- [ ] `dotnet build`; `dotnet test tests/Application.UnitTests`; `./run-functional-tests.sh --filter "FullyQualifiedName~ItemMerge|FullyQualifiedName~MergeItems"` green.
- [ ] `dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext` reports none.
- [ ] Haiku review sub-agent (`model: "haiku"`) with `git diff master...HEAD -- src tests` and the spec goal only; fix real issues, queue the rest.
- [ ] Report to user before client work.

---

## Phase 3: Client

### T10: removed per D8 (no client role check)

### T11: Items http + store selection and merge flow — implementer: sonnet
- [ ] `items.http.ts`: `mergePreview`, `merge` (+ request/response models in the items models file).
- [ ] `item-list.store.ts` per D9: selection snapshot survives filter changes; `merge` clears selection, reloads list, shows toast (Romanian, e.g. "Itemele au fost unite."); failure keeps selection and surfaces the problem.
- [ ] Spec: toggle, page select-all, survives filter change, reset after merge success, kept on failure.
- Files: `src/Client/src/app/shared/items/services/items.http.ts`, the items model file, `src/Client/src/app/features/items/services/{item-list.store.ts,item-list.store.spec.ts}`
- Verify: `cd src/Client && npm run build && npm test -- --watch=false`

### T12: Row selection in table and mobile list [UI] — implementer: sonnet
Use skills frontend-ui-engineering and angular-developer; check nz-* APIs in .github/instructions/llms-full.txt; take screenshots (desktop+mobile) to screenshot-checks/item-merge/.
- [ ] Large table: `nzShowCheckbox` column, header select-all with indeterminate state (pattern `shared/order-lists/ui/modals/wizard/steps/checked-rows.ts`), bound to the store.
- [ ] Small list: `nz-checkbox` per row with accessible label (item name).
- [ ] Specs for checkbox -> store toggle.
- Files: `src/Client/src/app/features/items/list/tables/regular/large/{table.ts,table.html}`, `.../small/{item-list-small.ts,item-list-small.html}`, one colocated spec
- Verify: `cd src/Client && npm run build && npm test -- --watch=false`; screenshots reviewed.

### T13: Merge button in filter bar (admin only) [UI] — implementer: sonnet
Use skills frontend-ui-engineering and angular-developer; check nz-* APIs in .github/instructions/llms-full.txt; take screenshots (desktop+mobile) to screenshot-checks/item-merge/.
- [ ] `nz-button` "Unește (N)" shown when `selectedCount() >= 2` (no role check, see D8); emits `merge` output; `item-list-tab.ts` wires it (wizard open lands in T14).
- [ ] Spec: hidden at 0/1 selected, visible at 2.
- Files: `src/Client/src/app/features/items/list/filter/regular/{filter-form.ts,filter-form.html,filter-form.spec.ts}`, `src/Client/src/app/features/items/page/tabs/item-list-tab.ts`
- Verify: `cd src/Client && npm run build && npm test -- --watch=false`; screenshots reviewed.

### T14: Merge wizard modal, 3 steps [UI] — implementer: sonnet
Use skills frontend-ui-engineering and angular-developer; check nz-* APIs in .github/instructions/llms-full.txt; take screenshots (desktop+mobile) to screenshot-checks/item-merge/.
- [ ] `nz-steps` "Alege item principal" (`nz-radio-group`: name, SKU, unit, category) -> "Date afectate" (preview fetched on entry, Romanian label per `ItemMergeKind`, conflict note when `Combined > 0`, `nz-spin` loading, error alert) -> "Confirmare" (warning `nz-alert`, kept vs disabled names, confirm button with loading).
- [ ] "Înainte" disabled until a primary is chosen; on success modal closes (store handles reload/toast).
- [ ] Opened from `item-list-tab.ts` via `NzModalService`, following the order-list wizard container pattern.
- [ ] Spec: exactly 3 steps, cannot advance without primary, preview requested with chosen primary, confirm calls store merge.
- Files: `src/Client/src/app/features/items/list/merge/{item-merge-wizard.ts,item-merge-wizard.spec.ts}`, `src/Client/src/app/features/items/page/tabs/item-list-tab.ts`, `src/Client/src/app/features/items/index.ts` (only if export needed)
- Verify: `cd src/Client && npm run build && npm test -- --watch=false`; screenshots reviewed.

### T15: E2E spec + screenshots [UI] — implementer: sonnet
Use skills frontend-ui-engineering and angular-developer; check nz-* APIs in .github/instructions/llms-full.txt; take screenshots (desktop+mobile) to screenshot-checks/item-merge/.
- [ ] `src/Client/e2e/specs/item-merge.spec.ts` with `page.route` mocks (items list, merge-preview, merge): select 1 -> no button; select 2 -> button; wizard 3 steps; confirm -> list reloaded, toast; merge returns 403 -> error toast, modal stays open. Screenshot each step, chromium + mobile projects.
- Files: `src/Client/e2e/specs/item-merge.spec.ts`
- Verify: `./run-e2e.sh -- --project=chromium e2e/specs/item-merge.spec.ts` and the mobile project; screenshots in `screenshot-checks/item-merge/` reviewed.

### Checkpoint C (after T11-T15)
- [ ] `cd src/Client && npm run build && npm test -- --watch=false` green; e2e green on chromium + mobile.
- [ ] Screenshots reviewed (desktop + mobile, every wizard step).

---

## Phase 4: Finish

### T16: Final verification, review, PR — implementer: sonnet
- [ ] `dotnet build`; `dotnet test tests/Application.UnitTests`; `dotnet test tests/Worker.UnitTests`; functional filter from Checkpoint B; client build + tests; e2e spec. Report all results incl. failures.
- [ ] `graphify update .` from repo root.
- [ ] Haiku review sub-agent (`model: "haiku"`) given only `git diff master...HEAD` and the spec objective; fix real findings, list the rest in the PR.
- [ ] Push `feature/item-merge`, open PR to `master` (summary, D3 ClassBalance deviation, D5/D8 decisions, known limits: Worker not locked, import matching may target inactive items; test results). End with the required attribution line.
- Verify: `gh pr view` shows the PR.
