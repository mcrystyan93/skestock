# Spec: Merge Items

## Objective
Admins can select 2+ near-duplicate items in the items table, pick one to keep, and merge. All data tied to the other items is re-pointed to the kept item; the others are soft-disabled (`IsActive = false`). Merge is global (all classes, including closed ones) and irreversible from the UI.

### User flow
1. Items table (nz-table) gets a checkbox column (NG-ZORRO `nzShowCheckbox`, select-all in header). Selection persists while filtering/scrolling the current list.
2. With >= 2 selected, a **Merge** button appears in the filter bar (Administrator only).
3. Modal wizard (nz-steps), Romanian copy:
   1. **Alege item principal** – radio list of selected items (name, SKU, unit, category).
   2. **Date afectate** – counts per kind of data that will be re-linked (batches, transactions, class balances, order list lines, supply list lines, goods-receipt import lines, thresholds, statistics) + conflict notes (e.g. "2 linii din liste de aprovizionare vor fi însumate").
   3. **Confirmare** – warning text, names of kept/disabled items, confirm button.
4. On success: modal closes, list refreshes, secondary items shown as inactive, toast.

### Decisions (confirmed by user)
- Only `Administrator` role may merge (`[Authorize(Roles = Roles.Administrator)]`, button hidden otherwise).
- Closed-class history is re-pointed too (ClassBalance, StockTransaction, statistics).
- Unique-constraint collisions: primary wins, rest combine (see table).
- Item fields (name, unit, category, image, perishability) stay as the primary's; nothing copied.

### Re-pointing rules (secondary -> primary)
| Table | Rule |
|---|---|
| StockBatch, StockTransaction, ClassBalance, GoodsReceiptImportLine.MatchedItemId, OrderListLine.ItemId | Plain `ItemId` update (keep name snapshots as-is) |
| ClassItemStockVisibility (ClassId, ItemId, LocationId unique) | On collision keep primary's row, delete secondary's |
| ItemLocationThreshold (ItemId, LocationId unique) | On collision keep primary's, delete secondary's |
| SupplyListLine (SupplyListId, ItemId unique) | On collision add quantity into primary's line, delete secondary's |
| DailyItemConsumption (Date, ItemId, ClassId, LocationId unique) | On collision sum quantities into primary's row, delete secondary's |
| ItemPurchaseStatistic (Scope, ClassId, ItemId unique) | On collision sum values into primary's row, delete secondary's (exact columns confirmed in the plan phase) |
| Item | Secondary `IsActive = false`; primary must be active |

## Tech Stack
.NET 10 minimal API, Mediator (source-generated), FluentValidation, FluentResults, EF Core 10 / SQL Server (temporal tables), HybridCache tags, SignalR notifier. Angular 22, NG-ZORRO 22, `@ngrx/signals`, Vitest, Playwright.

## Commands
```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~MergeItems"
./run-functional-tests.sh --filter "FullyQualifiedName~MergeItems"
cd src/Client && npm run build && npm test -- --watch=false
./run-e2e.sh -- --project=chromium e2e/specs/item-merge.spec.ts
```

## Project Structure
```
src/Application/Features/Items/Commands/MergeItems/   MergeItemsCommand, Handler, Validator
src/Application/Features/Items/Queries/GetItemMergePreview/   counts + conflicts for wizard step 2
src/Application/Features/Items/EventHandlers/ItemsMergedEventHandler.cs   cache + realtime
src/Application/Common/Errors/ItemErrors.cs   new merge errors
src/Web/Endpoints/Items.cs   POST /api/Items/merge, POST /api/Items/merge-preview
src/Domain/Events/Items/ItemsMergedEvent.cs
src/Client/src/app/features/items/...   selection in table, filter-bar button, merge wizard modal, store + http methods
tests/Application.UnitTests/Features/Items/..., tests/Application.FunctionalTests/..., src/Client/e2e/specs/item-merge.spec.ts
```
No schema change expected -> no migration.

## Code Style
Follow `DisableItem`: command `IRequest<Result<...>>, ICacheInvalidation`, `Result` with typed `ItemErrors`, validator with `ValidationErrorCodes`, domain event -> event handler for cache tags + `IRealtimeNotifier`. SOLID: the handler orchestrates; per-table re-pointing lives in small single-purpose strategies behind one interface (`IItemReferenceMerger`, one per table, DI-registered in `Application/DependencyInjection.cs`), so adding a new ItemId-bearing table = adding one class. Client: standalone components, signals, `@ngrx/signals`, Romanian strings, no Prettier.

## Backend rules
- Validation: >= 2 distinct ids, primary in the id set, all items exist, primary active (secondaries may already be inactive).
- Single transaction; `ExecuteUpdate` on temporal tables also sets `LastModifiedById`; respect `StockBatch.Version` (no lost updates: lock/re-check inside transaction).
- Idempotent on retry: merging already-merged (inactive, no remaining references) items succeeds as no-op.
- Cache: invalidate Item list/entity tags, Stock coarse tag, Category list, order/supply list and statistics tags.
- Does not implement `IRequiresOpenClass` (global, like DisableItem) because it also touches closed classes; takes `ClassLocking` exclusive-style protection decision in plan phase (open question 2).

## Testing Strategy
- **TDD on backend**: failing test first per rule. Unit tests: validator; handler (happy path; each collision rule; primary inactive; unknown id; idempotent retry; non-admin rejected via authorization); one test per `IItemReferenceMerger`. Preview query tests. Functional test for endpoints (auth, success, collision, closed class data).
- Client: colocated Vitest specs for store (selection, merge flow) and wizard steps; Playwright spec with mocked API (selection -> button appears at 2 -> wizard -> confirm) saving screenshots to `screenshot-checks/item-merge/`.
- UI tasks must use skills `frontend-ui-engineering` and `angular-developer`; NG-ZORRO APIs checked in `.github/instructions/llms-full.txt` (`nz-table` row selection, `nz-steps`, `nz-modal`). Screenshots at desktop and mobile widths reviewed before done.

## Boundaries
- Always: TDD on backend, `dotnet build` + tests, `npm run build` + tests, `graphify update .`, feature branch + PR, Romanian copy, `@ske/...` aliases.
- Ask first: any migration/index change, new dependency, changing the pipeline order, extending to a client-visible "undo".
- Never: commit to master, hand-edit migrations, run Prettier on client, merge without the Administrator attribute, copy secrets.

## Success Criteria
1. Selecting >= 2 rows shows **Merge** in the filter bar for admins only; hidden for others and with < 2 selected.
2. Wizard has exactly 3 steps (primary -> affected data -> confirm); cannot proceed without a primary.
3. After merge, no row in any table references a secondary `ItemId` (except order-list name snapshots and history tables), secondaries `IsActive = false`, totals of stock quantity per class/location are unchanged.
4. All collision rules above verified by tests; whole merge is atomic (failure leaves data untouched).
5. Non-admin call returns 403; invalid sets return typed validation/problem results.
6. Build + unit + functional + client + e2e green; screenshots reviewed.

## Execution model (per user)
Research: haiku (done). Plan and tasks (`tasks/item-merge-plan.md`, `tasks/item-merge-todo.md`): opus. Implementation: sonnet. Post-change independent review: haiku sub-agent with the diff only.

## Open Questions
1. Should the preview (step 2) be a server query (accurate counts, recommended) rather than client-side estimation?
2. Concurrency: acceptable to run merge under an exclusive lock that briefly blocks stock writes on all open classes, or prefer per-batch optimistic retries? (Plan phase will propose; default: exclusive lock, merge is rare.)
3. Selection scope: selection survives search/filter changes but resets after a successful merge. OK?
