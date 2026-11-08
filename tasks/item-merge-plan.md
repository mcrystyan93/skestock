# Implementation Plan: Merge Items

Spec: `tasks/item-merge-spec.md` (approved). Tasks: `tasks/item-merge-todo.md`. Implementer for every task: **sonnet**. Post-change review: **haiku** sub-agent, diff + task goal only.

## Overview
Admins select 2+ items in the items table, pick a primary in a 3-step wizard (primary -> server-side preview of affected data -> confirm) and merge. One command re-points every `ItemId` reference from the secondaries to the primary (combining rows where a unique key would collide), disables the secondaries, in one transaction under an exclusive lock on all school classes. No schema change, no migration.

## Approved answers to the spec's open questions
1. Wizard step 2 uses a server-side preview query.
2. Merge runs under an exclusive lock on all school-class rows.
3. Client selection survives filter changes and resets after a successful merge.

## Architecture decisions

**D1. Mergers use tracked entities, not `ExecuteUpdate`/`ExecuteDelete`.**
The EF in-memory provider used by `Application.UnitTests` (`ItemTestDbContext`) does not support `ExecuteUpdate`/`ExecuteDelete`, and the spec requires TDD unit tests per rule. Tracked loads + mutations + one `SaveChangesAsync` in the handler give: unit-testable rules, `AuditableEntityInterceptor` sets `LastModifiedById` automatically (temporal-table rule satisfied), `StockBatch.Version` concurrency token is checked automatically (no lost updates), and atomicity comes from the single transaction. Cost: rows are loaded into memory. Merges are rare and per-item row counts are small (thousands at most), so this is fine. Leave a `shortcut:` comment on the transaction merger (the biggest table) saying when to switch to `ExecuteUpdate`.

**D2. `IItemReferenceMerger`, one class per table, DI-registered in `Application/DependencyInjection.cs`.**
```csharp
public interface IItemReferenceMerger
{
    ItemMergeKind Kind { get; }                       // enum used as preview row key (client maps it to Romanian labels)
    Task<ItemMergeImpact> PreviewAsync(Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken ct);
    Task MergeAsync(Guid primaryId, IReadOnlyCollection<Guid> secondaryIds, CancellationToken ct); // mutates tracked entities only, never SaveChanges
}
public sealed record ItemMergeImpact(ItemMergeKind Kind, int Relinked, int Combined);
```
The preview query and the command run the same rule code, so the preview counts cannot drift from what the merge does. Location: `src/Application/Features/Items/Commands/MergeItems/Mergers/`.
Plain mergers may share one tiny generic base (`ItemIdReferenceMerger<T>`, selector expression + setter) if it saves code; nothing more.

**D3. Collision rule = group by unique key across primary + all secondaries.**
For each key group: survivor = primary's row if it exists, else the first secondary row (re-pointed). Combine into survivor, delete the rest. This covers secondary-vs-secondary collisions (two secondaries with a row for the same key and no primary row), which a naive "re-point unless the primary has one" would turn into a unique-index violation. Every combining merger gets a test for this case.

| Table | Key | Rule |
|---|---|---|
| StockBatch, StockTransaction, GoodsReceiptImportLine.MatchedItemId, OrderListLine.ItemId (nullable) | none | plain re-point; name snapshots untouched |
| ClassItemStockVisibility | (ClassId, LocationId) | keep survivor, delete rest |
| ItemLocationThreshold | (LocationId) | keep survivor, delete rest |
| SupplyListLine | (SupplyListId) | sum `Quantity`; survivor keeps its Unit/Notes |
| DailyItemConsumption | (Date, ClassId, LocationId) | sum `Quantity`, `TotalValue`; `ComputedAt` = max |
| ItemPurchaseStatistic | (Scope, ClassId) | sum `TotalQuantity`, `TotalValue`, `PurchaseCount`; recompute `AverageQuantity` and `AverageUnitPrice` with the formulas in `PurchaseStatisticsCalculator` (round 2, guard /0); `LastPurchasedAt` = max; `AverageIntervalDays` = survivor's value (the nightly `MaterializePurchaseStatistics` full rebuild recomputes it exactly) |
| **ClassBalance** | (ClassId, LocationId) | sum `OpeningQty`; `ClosingQty` = null if both null else sum of non-null. **Deviation from spec** (spec says plain re-point): there is no unique index, but the domain treats one row per (class, item, location) as the invariant (`CloseSchoolClassCommandHandler.GetOrAddBalance` uses `FirstOrDefault`), so two rows would silently lose quantity on the next close. Combining keeps per-class totals identical. |
| Item | | secondaries `IsActive = false`; primary must be active |

**D4. Locking: add `ClassLocking.ExclusiveLockAllClasses(this IApplicationDbContext)`.**
Verified in source: `ClassLockBehaviour` takes `SharedLock(id)` = `SELECT ... WITH (HOLDLOCK) WHERE Id = @id` for every `IRequiresOpenClass` command inside its transaction; `ExclusiveLock(a, b)` = `WITH (XLOCK, HOLDLOCK)` for two ids (used by `CloseSchoolClass`). New sibling: `SELECT *, [PeriodStart], [PeriodEnd] FROM [SchoolClasses] WITH (XLOCK, HOLDLOCK)` (no WHERE; in-memory fallback `dbContext.SchoolClasses`). It waits for in-flight class-scoped commands, blocks new ones until commit, and blocks `CloseSchoolClass` (deadlock-free by ordering: merge takes all rows up front). `MergeItemsCommand` does **not** implement `IRequiresOpenClass`; the handler wraps its work in `dbContext.InTransactionAsync(...)` and takes the lock first, exactly like `CloseSchoolClassCommandHandler`.
Not covered by the lock: Worker statistics materialization and goods-receipt import matching (not class-locked). Overlap there fails the merge's SaveChanges (unique violation / concurrency) and rolls back atomically; the user retries. Acceptable for a rare admin action.

**D5. Events, cache, realtime: reuse, no new event type.**
Handler raises the existing `ItemDisabledEvent` for each secondary (its handler already invalidates item list/category/stock-coarse tags and sends `RealtimeEvents.ItemDisabled`). `MergeItemsCommand : ICacheInvalidation` lists the rest: `Items.ItemListTag`, `ItemTag(id)` for all ids, `Stock.BuildCoarseTag()`, `StockBatches.StockBatchListTag`, `OrderLists.OrderListListTag`, `SupplyLists.SupplyListListTag`, `GoodsReceipts.GoodsReceiptImportListTag`, `Statistics.PurchaseStatisticsTag`, `Statistics.DailyConsumptionTag`, `Categories.CategoryListTag`. By-id queries for order/supply lists carry their list tag too (verified), so list tags suffice. **Implementer must verify** every stock report query is stamped with `BuildCoarseTag()`; if some use only class tags, also invalidate `Stock` class tags for all classes (handler already has them from the lock). This drops the spec's `ItemsMergedEvent` / `ItemsMergedEventHandler` (YAGNI: no new consumer needs it).

**D6. Command shape.** `[Authorize(Roles = Roles.Administrator)] MergeItemsCommand(Guid PrimaryItemId, IReadOnlyCollection<Guid> ItemIds) : IRequest<Result>, ICacheInvalidation`. `ItemIds` includes the primary. Validator: ids non-empty, >= 2 distinct, primary in set (codes from `ValidationErrorCodes`). Handler: missing id -> `ItemErrors.ItemNotFound`; primary inactive -> new `ItemErrors.MergePrimaryInactive`. Idempotent: rerun on already-merged items finds no references, re-disables, succeeds. Endpoint returns `NoContent` (client refreshes the list; no DTO needed).

**D7. Preview query.** `[Authorize(Roles = Roles.Administrator)] GetItemMergePreviewQuery(PrimaryItemId, ItemIds) : IRequest<Result<ItemMergePreviewDto>>`, same validator rules (shared via a small base validator or duplicated 3 rules, whichever is shorter). Returns `IReadOnlyList<ItemMergeImpact>` (only non-zero rows). **Not** `ICacheableQuery`: it must be exact at confirm time and would need every tag above; it is opened rarely. Endpoints per spec: `POST /api/Items/merge-preview`, `POST /api/Items/merge`.

**D8. No role gating on the client (decision by user).** The Merge button is shown regardless of role; only the backend checks the role (`[Authorize(Roles = Roles.Administrator)]`, a non-admin gets 403 and the UI shows the error). Client roles will be added in the future. No `/api/Users/roles` endpoint, no `isAdministrator`.

**D9. Client structure.**
- `items.http.ts`: `mergePreview(body)`, `merge(body)`.
- `item-list.store.ts`: `selected: Record<string, ItemListRow>` snapshot (rows survive filter changes even when no longer loaded, wizard step 1 needs name/SKU/unit/category), `toggleSelected`, `setPageSelected`, `clearSelection`, computed `selectedCount`; `merge` rxMethod -> on success `clearSelection` + reload + toast; on failure keep selection.
- Large table: `nzShowCheckbox` column + header select-all (pattern: `shared/order-lists/ui/modals/wizard/steps/checked-rows.ts`). Small list (mobile): `nz-checkbox` per row.
- Filter form: **Unește** button (`nz-button`, visible when `selectedCount >= 2`), emits an output; `item-list-tab.ts` opens the wizard via `NzModalService` (already provided in `items.page.ts`).
- Wizard: one component `features/items/list/merge/item-merge-wizard.ts` (inline template like the order-list steps), `nz-steps` with 3 steps, `nz-radio-group` primary choice, preview fetched on entering step 2 (`nz-spin`/`nz-alert` on error), confirm step with `nz-alert` warning. Romanian copy: "Alege item principal", "Date afectate", "Confirmare".

## Dependency graph
```
T1 errors + command + validator + IItemReferenceMerger
 ├─ T2 plain mergers ─┐
 ├─ T3 keep mergers  ─┤
 ├─ T4 combine merg. ─┼─ T6 handler + lock + events/cache + DI ─┬─ T8 endpoints + functional tests
 └─ T5 stats merger  ─┘          │                              │
                                 └─ T7 preview query ───────────┘
(T9 and T10 removed per D8.)
T8 ─ T11 http + store selection ─ T12 table checkboxes ─ T13 filter-bar button ─ T14 wizard ─ T15 e2e ─ T16 review + PR
```
T2-T5 are independent of each other (parallelizable, but they share the DI file only in T6, so no conflicts).

## Phases
- Phase 0: branch `feature/item-merge` (T0).
- Phase 1 backend core: T1-T6. Checkpoint A.
- Phase 2 backend API: T7-T8. Checkpoint B (haiku review of backend diff).
- Phase 3 client: T11-T15. Checkpoint C.
- Phase 4: T16 final verification, graphify, haiku review, PR.

## Risks and mitigations
| Risk | Impact | Mitigation |
|---|---|---|
| Unique-index collision missed (incl. secondary-vs-secondary) | High: merge fails or data lost | D3 group-by-key rule; unit test per merger for primary-vs-secondary and secondary-vs-secondary; functional test on real SQL Server with collisions in every unique table |
| SaveChanges ordering: deleted row and re-pointed row share a unique key in one batch | High | EF Core orders deletes before conflicting updates on unique indexes; prove it in the functional test (T8). Fallback if it fails: `SaveChangesAsync` twice inside the transaction (deletes first) |
| Atomicity | High | Single `InTransactionAsync` + single SaveChanges; functional test forces a failure (e.g. inactive primary after lock, or a concurrency conflict) and asserts nothing changed |
| StockBatch lost update | High | Tracked updates carry `Version`; exclusive lock blocks class-scoped stock writers during merge; never bypass with ExecuteUpdate |
| Stale caches after merge | Medium | D5 tag list; verify stock query tags; functional test reads list before and after merge through the API |
| Temporal tables | Medium | Tracked updates -> interceptor sets `LastModifiedById`; no TRUNCATE/raw SQL writes; no schema change so retention untouched |
| ClassBalance totals | Medium | D3 combine rule; test that per-(class, location) totals are unchanged |
| Lock blocks all class writes | Low | Merge is rare and short; documented |
| Worker (stats job / import matching) concurrent write | Low | Merge rolls back; retry. Import matching may later match to an inactive secondary: out of scope, mention in PR |
| Non-admin sees Merge button and gets 403 | Accepted | Per D8; UI shows a clear error toast |

## Open questions for the user (non-blocking, defaults chosen)
1. D3 ClassBalance combine instead of plain re-point (default: combine, preserves totals).
2. D5 drop `ItemsMergedEvent` and reuse `ItemDisabledEvent` per secondary (default: reuse).
3. D8 resolved by user: no client role check, backend only.
4. Mobile: checkbox selection in the small list too (default: yes, otherwise mobile admins cannot merge).
