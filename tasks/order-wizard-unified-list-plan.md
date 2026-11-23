# Plan: Unified product list in the "Comandă nouă" wizard

Spec: `tasks/order-wizard-unified-list-spec.md` (approved). Todo: `tasks/order-wizard-unified-list-todo.md`.
Branch: `feature/order-wizard-unified-list`. Engineering level: production grade for the migration and the
new read endpoint (auth, caching, query shape); lean on the UI (one shared row, one pure list model).

## Verified facts (code, 2026-11-14)

- `ItemPurchaseStatistic` (`src/Domain/Entities/Statistics/ItemPurchaseStatistic.cs`) has `TotalQuantity`,
  `AverageQuantity`, `AverageIntervalDays`, `LastPurchasedAt`; no last quantity. `PurchaseLine.Quantity` is `int`,
  so `LastQuantity` is `int?` (no column type config needed).
- `PurchaseStatisticsCalculator.Aggregate` builds every row (Last90Days, Last365Days, Class). Receipt = `PurchaseKey`,
  receipt date = earliest `PurchasedAt` per key (same rule as `AverageIntervalDays`).
- `MaterializePurchaseStatisticsCommandHandler` deletes all rows and re-inserts (`ExecuteDeleteAsync` + `AddRange`),
  so the new column needs no update path; existing rows stay `NULL` until the next run (daily job or
  `RecalculatePurchaseStatistics`). Client fallback = 1 covers that window.
- Post-temporal add-column migration (`20261113065804_AddItemDefaultLocation`) is a plain `AddColumn` with no
  versioning toggle, so the retention line should not need re-appending; still checked by `TemporalSchemaTests`.
- `GetPurchaseSegmentsQueryHandler` loads Last365Days + Class rows of active items, builds
  `PurchaseSegmentCandidate`s and calls `PurchaseSegmentClassifier.Classify(candidates, now)` (per-class precedence,
  staleness rule `now - last <= 2 × interval`). "Frequent" must reuse this, not re-derive `interval <= 21`.
- Low stock lives in `GetClassLocationStockHandler`: class batches grouped by (item, location); hidden =
  `ClassItemStockVisibility.HideWhenZeroStock && qty <= 0`; low = `qty < ItemLocationThreshold.MinThreshold`, no
  threshold row = never low. `GET /api/Stock/class/{classId}/low-stock` wraps it. Only inactive-item filter is absent
  there (it lists any item with batches).
- Item default location: `Item.LocationId` / `Item.Location` (nullable FK, shipped).
- Cache tags: `Items.CacheConstants.ItemListTag` (item create/edit/enable/disable/merge, threshold edits via
  `EditItem`, and `UpdateLocation` renames), `Stock.CacheConstants.BuildClassTag(classId)` + `BuildCoarseTag()`
  (stock writes), `Statistics.CacheConstants.PurchaseStatisticsTag` (materialization, item edits).
- Existing indexes cover the reads: `StockBatches(ReceivedClassId, LocationId, ItemId)`,
  `ItemPurchaseStatistics(Scope, ClassId, ItemId)` unique and `(ItemId, Scope)`, `ItemLocationThresholds(ItemId,
  LocationId)`, `ClassItemStockVisibilities(ClassId, ItemId, LocationId)`. No new index expected; the EF review decides.
- Client: wizard store loads `getLowStockItems` + `getPurchaseSegments` (forkJoin) when leaving `source` with
  `new`; `buildPrefill` → `suggestionsToLines`. Step 2 = `steps/suggestions-step.ts` (5 tabs). Last-step panel =
  `suggestions/order-list-suggestions-panel.ts` (3 tabs incl. "Predefinite") rendering
  `order-list-suggestion-groups.ts` (sticky search, `normalize()` accent-insensitive, add button / "Adăugat").
  Panel is shown only at `lg` (`*skeLayoutBreakpoint="'lg'"` in `order-list-detail-form.html`).
- Icons exist in `src/Client/src/assets/icons`: `triangle-exclamation.svg` (stoc redus), `arrows-rotate.svg`
  (comandat frecvent), `magnifying-glass.svg`, `chevrons-right.svg` (already used). Use `nzType="icons:<name>"`.
- E2E: `e2e/specs/order-wizard-segments.spec.ts` (viewports 360/768/1280, `screenshotRoot` helper →
  `screenshot-checks/order-wizard-segments/`); `order-list-detail-submission.spec.ts` also mocks the low-stock and
  segments routes.

## Design

### Backend

1. `ItemPurchaseStatistic.LastQuantity` (`int?`), migration `AddItemPurchaseStatisticLastQuantity` (nullable, no
   default, no index). Calculator: `LastQuantity` = sum of the item's line quantities on its latest receipt (latest
   receipt date; ties broken by max `PurchaseKey` for determinism). Set for every scope (no branching); the catalog
   reads Last365Days only (spec assumption 1).
2. Extract the candidate loading from `GetPurchaseSegmentsQueryHandler` into
   `GetPurchaseSegments/PurchaseSegmentCandidates.cs` (`static Task<List<PurchaseSegmentCandidate>> LoadAsync(
   IApplicationDbContext, CancellationToken)`), used by both handlers. Pure move; existing tests prove it.
3. New slice `src/Application/Features/OrderLists/Queries/GetOrderCatalog/`:
   - `GetOrderCatalogQuery { Guid ClassId }`, `[Authorize]`, `ICacheableQuery`:
     key `$"{OrderLists.CacheConstants.OrderList}:catalog:class:{ClassId}"`, tags `[Items.ItemListTag,
     Stock.BuildClassTag(ClassId), Stock.BuildCoarseTag(), Statistics.PurchaseStatisticsTag]`, sliding 5 min like
     `GetClassLocationStockQuery`. Read only → no `IRequiresOpenClass`.
   - Validator: `ClassId` `NotEmpty` (`ValidationErrorCodes.Required`). Unknown class → `SchoolClassNotFound`.
   - DTO (`OrderLists/Models/OrderCatalogDto.cs`): `OrderCatalogItemDto { ItemId, Name, Sku, ImageFileId, Unit,
     int? LastQuantity, decimal? AverageIntervalDays, bool IsFrequent, List<OrderCatalogLocationDto> Locations }`,
     `OrderCatalogLocationDto { LocationId, LocationName, bool IsLowStock }`. Empty `Locations` = "Fără locație".
     Response = `List<OrderCatalogItemDto>` sorted by name (client re-sorts per group).
   - Handler, all `AsNoTracking`, projections only, a fixed number of round trips (no per-item queries):
     1. class exists (`AnyAsync`);
     2. active items projected with default location name and the Last365Days stat via correlated
        `FirstOrDefault` sub-select (`LastQuantity`, `AverageIntervalDays`) → one SQL with OUTER APPLY/LEFT JOIN;
     3. class stock `GroupBy(ItemId, LocationId, Location.Name)` → `Sum(Quantity)`;
     4. thresholds filtered server-side with `EXISTS` on this class's batches (no large `IN` lists);
     5. `HideWhenZeroStock` visibilities for the class;
     6. `PurchaseSegmentCandidates.LoadAsync` + `Classify` → frequent ids = weekly ∪ everyTwoWeeks.
     Locations per item = non-hidden class stock rows (same rule as the stock report; hidden rows dropped); if none,
     the item's default location (`IsLowStock = false`); else empty. Low = same `qty < threshold` rule.
     Fallback applies to every item without class stock, history or not (user confirmed: whole catalog).
   - Decision: low stock is per location (`Locations[].IsLowStock`), so the icon shows in the group where the item is
     actually low. Spec success criterion 5 still holds.
4. Endpoint `GET /api/OrderLists/catalog/{classId}` in `src/Web/Endpoints/OrderLists.cs`,
   `Results<Ok<List<OrderCatalogItemDto>>, ProblemHttpResult>`, `ToProblemHttpResult()` on failure. Two segments, so
   no clash with `GET {id}`.
5. Remove (user decision): `GET /api/statistics/purchases/segments` with `GetPurchaseSegmentsQuery`/`Handler` and the
   segments query (the segments DTOs stay as the classifier's internal types, moved next to it; the classifier and `PurchaseSegmentCandidates` stay, used by the catalog; its tests move to
   classifier/candidates tests), and `GET /api/Stock/class/{classId}/low-stock` with `LowStockItemDto`.
   `POST statistics/purchases/items-history` stays (editor line hints). Breaking API change: client is the only caller.

### Client

- Models `core/models/order-list.ts` (+ export): `OrderCatalogItemDto`, `OrderCatalogLocationDto`.
  `OrderListsHttp.getCatalog(classId)` → `GET /api/OrderLists/catalog/{classId}`.
- Pure list model `shared/order-lists/services/order-catalog.ts`:
  - `normalize(text)` (moved from `order-list-suggestion-groups.ts`).
  - `catalogGroups(items, query): CatalogGroup[]` with `CatalogGroup { title: string; rows: CatalogRow[] }`,
    `CatalogRow { item, isLowStock }`: one group per `locationId`, titled by name (an item with 2 locations appears in both,
    same `itemId`), groups sorted by `localeCompare('ro')`, "Fără locație" last; rows by `averageIntervalDays` asc,
    nulls last, then name; query filters name/SKU accent-insensitive, empty groups dropped.
  - `catalogLine(item): OrderListLineDto` with `quantity = wholeQuantity(item.lastQuantity ?? 0)` (→ ≥ 1, fallback 1),
    product name via existing `toProductName`; `catalogToLines(items, selectedIds)`.
- Shared UI `shared/order-lists/ui/catalog/`:
  - `order-catalog-row.ts` (`ske-order-catalog-row`): thumbnail, name, `(SKU)`, `icons:triangle-exclamation`
    (aria-label/title "Stoc redus"), `icons:arrows-rotate` ("Comandat frecvent"), `{{ quantity }} {{ unit }}`;
    `mode: 'select' | 'add'`; select → `nz-checkbox` + `toggle` output; add → button / "Adăugat" (today's
    `order-list-suggestion-groups` markup) + `add` output.
  - `order-catalog-list.ts` (`ske-order-catalog-list`): sticky search (Signal Form, existing markup), group headings
    (in `select` mode each has a per-location "Selectează tot", checked/indeterminate, acting only on that group's
    visible rows; no global select-all),
    rows, `nz-empty` ("Niciun rezultat." / empty text). Inputs `items`, `mode`, `selectedIds` or `addedItemIds`.
- Wizard: store state `catalog: OrderCatalogItemDto[]` replaces `suggestions` + `segments`; one `getCatalog` load;
  `buildPrefill` → `catalogToLines`. `suggestions-step.ts` becomes the list in `select` mode (no tabs, per-location select-all only). Selection still starts empty (today's behavior).
- Panel: one `rxResource(getCatalog(classId))`, list in `add` mode; tabs and supply-list tab removed.
- Dead code after both swaps: `groupLowStockItems`, `purchaseSegmentLists`, `suggestionsToLines`, `SuggestedItem`,
  `OrderListSuggestionGroups` (+spec), `StockHttp.getLowStockItems` / `ClassStatisticsHttp.getPurchaseSegments` if
  no other caller. File deletions approved by the user.

## Risks

- `LastQuantity` is NULL until the next materialization → every row shows/uses 1 on deploy day. Mention in PR;
  optionally trigger `RecalculatePurchaseStatistics` after deploy.
- Catalog size: all active items in one response (hundreds, not thousands) — fine; cache per class. If it grows,
  revisit (not now).
- Frequent flag depends on `TimeProvider.GetUtcNow()` inside a 5-minute cache → negligible staleness.
- Location names are not unique (`LocationConfiguration` has no unique index), so groups are keyed by `LocationId`
  and titled by name.
- A catalog failure now fails the whole load (`loadFailed` + retry); the old segments-only tolerance is gone.
- Docs to update: `docs/features/order-lists.md:191,194`, `docs/features/stock.md:41`.
- `ItemPurchaseStatisticItemMerger` must carry `LastQuantity` on merge (T1).
- Removing the endpoints is a breaking API change; grep confirmed only the wizard/panel/e2e mocks call them.
- Removing "Predefinite" tab removes supply-list adds from the editor panel (approved in spec).
- Panel is `lg`-only, so mobile screenshots cover step 2 only.
- `order-list-detail-submission.spec.ts` and `order-list-detail-form.spec.ts` mock the old endpoints; update them
  or they fail/silently stop covering the panel.

## Parallelizable

- T1 (LastQuantity) ∥ T2 (candidate extract). T3 needs both.
- Client T6 can start once the DTO shape in T3 is fixed (parallel with T4/T5).
- T8 (wizard) ∥ T9 (panel) after T7; dead-code cleanup only in T9 to avoid editing `order-list-wizard.utils.ts` twice.

## Verification gates

`dotnet build`; `dotnet test tests/Application.UnitTests`; `./run-functional-tests.sh --filter
"FullyQualifiedName~OrderCatalog"` (+ full run before PR); `dotnet test tests/Infrastructure.IntegrationTests
--filter "FullyQualifiedName~TemporalSchema"`; `has-pending-model-changes`; `cd src/Client && npm run build && npm test
-- --watch=false`; `./run-e2e.sh -- --project=chromium e2e/specs/order-wizard-segments.spec.ts` (+ mobile project);
screenshots reviewed; EF review skill; Haiku review; `graphify update .`.
