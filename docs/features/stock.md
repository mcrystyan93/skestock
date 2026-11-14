# Stock

Stock is how much of each item a school class holds at each location, and the operations that change it by hand: a recount (single or bulk), moving stock between locations, removing or re-dating expired stock, and hiding an item once it reaches zero. The data model is batch-based: quantity lives in `StockBatch` rows, and every change is mirrored by a `StockTransaction` row.

This document covers `src/Application/Features/Stock` and the entities it works on. Creating batches is a different feature: goods receipts and the manual batch editor (`Features/GoodsReceipts`, `Features/StockBatches`) add stock, and closing a class (`Features/SchoolClasses`) rolls it over. Both are only described here where they touch stock rules.

UI copy is Romanian. Everything below was read from source; gaps and oddities are listed under [Open points](#open-points), not fixed. Related: [items.md](items.md) and [locations.md](locations.md) (each on its own PR until merged).

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/Stock/` (`StockBatch`, `StockTransaction`, `ClassBalance`, `ClassItemStockVisibility`), `Domain/Events/Stock/`, enums `StockTransactionType`, `StockBatchSource`, `AdjustmentReason` |
| Application | `src/Application/Features/Stock/` (commands, one query, event handlers, models, cache constants), errors in `Application/Common/Errors/StockErrors.cs` |
| Infrastructure | `Data/Configurations/StockBatchConfiguration.cs`, `StockTransactionConfiguration.cs`, `ClassBalanceConfiguration.cs`, `ClassItemStockVisibilityConfiguration.cs` |
| Web | `src/Web/Endpoints/Stock.cs` |
| Client | `src/Client/src/app/shared/stock` (HTTP, stores, list, modals), `core/models/stock.ts`; mounted in `features/school-classes/overview` |

There is no Worker or queue involvement; all operations are synchronous.

## Data model

| Entity | Meaning |
|---|---|
| `StockBatch` | One delivery of an item to a location for a class: `Quantity` (remaining), `ExpiryDate` (null for non-perishables), `ReceivedDate`, `UnitPrice`, `Source` (`Order` or `Donation`), optional `GoodsReceiptId`, and a database `rowversion` (`Version`). The stock total is the sum of `Quantity` over batches. |
| `StockTransaction` | The audit trail: `Type`, signed `QuantityChange`, free-text `Reason` (max 250), `UserId`, `ClassId`, `CreatedAt`, optional `BatchId` and `GoodsReceiptId`. |
| `ClassBalance` | Per class, item and location: `OpeningQty` and `ClosingQty` (null until the class is closed). Written when a class is closed (`CloseSchoolClass`) and re-pointed by the item merge ([items.md](items.md#merging-duplicates)); the Stock feature does not touch it. |
| `ClassItemStockVisibility` | Per class, item and location: `HideWhenZeroStock`. |

"Stock of a class" means batches whose `ReceivedClassId` is that class. `StockTransactionType` (persisted as int, append only): `Order`, `Usage`, `Adjustment`, `Transfer`, `ExpiryWriteoff`, `Rollover`, `Donation`. In `src/Application` the values written are `Order` and `Donation` (batch creation, chosen by the batch's `Source`), `Adjustment`, `Transfer` and `Rollover`. `Usage` is written only by the demo-data seeder in `Infrastructure`, and `ExpiryWriteoff` is written nowhere.

## Backend

### Endpoints (`/api/Stock`)

The group requires an authenticated user (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`). Every command below also carries `[Authorize]` and `IRequiresOpenClass` (see [Rules common to all writes](#rules-common-to-all-writes)).

| Method + route | Use case | Notes |
|---|---|---|
| `POST class/{classId}` | `GetClassLocationStockQuery` | The stock report. Body: `searchTerm`, `filters`, `includeHidden`, `lowStockOnly`, `expiredOnly`. |
| `POST adjust` | `AdjustStockCommand` | Recount of one (class, item, location). Returns a `StockItemDto`. |
| `POST class/{classId}/bulk-adjust` | `BulkAdjustStockCommand` | Recount of many lines, all or nothing. |
| `POST remove-expired` | `RemoveExpiredStockCommand` | Zeroes expired batches of one (class, item, location). |
| `POST extend-expiry` | `ExtendExpiredStockExpiryCommand` | Re-dates expired batches to today + N days. |
| `POST move` | `MoveStockCommand` | Moves quantity to another location. |
| `PATCH class/{classId}/item/{itemId}/location/{locationId}/visibility` | `SetClassItemStockVisibilityCommand` | Body: `hideWhenZeroStock`. |

### The stock report

`GetClassLocationStockQuery` returns `StockReportDto { Items, HasExpiredItems }`, one `StockItemDto` per (item, location) pair that has at least one batch row for the class.

- Quantity is the sum of `StockBatch.Quantity`; `ExpiredQuantity` sums batches with `Quantity > 0` and `ExpiryDate <= today` (UTC date, so a batch counts as expired on its expiry date). `IsExpired` and `ExpiredQuantity` are only set for perishable items.
- `IsLowStock` is `Quantity < MinThreshold` from the item's per-location threshold ([items.md](items.md#thresholds)). No threshold row means never low.
- `LastUpdatedAt` is the latest `StockTransaction.CreatedAt` for the class, item and location.
- Rows are sorted by item name then location name. Unknown class → `school_classes.not_found`.
- **Filters:** only `locationId` and `categoryId` entries in `filters` are applied; any other field is ignored without an error. `searchTerm` is an accent-insensitive `Contains` on the item name.
- **Hiding:** a row is "hidden" when its `ClassItemStockVisibility.HideWhenZeroStock` is set and its quantity is `<= 0`. With `includeHidden = false` (default) hidden rows are left out; with `includeHidden = true` the report returns **only** the hidden rows, not everything.
- `lowStockOnly` and `expiredOnly` restrict the result after the hidden-row step.

Caching: tags are the class tag and the coarse tag; sliding expiration about 5 minutes with 30 s jitter. The cache key contains today's UTC date, so a cached report never crosses midnight.

### Recount (single and bulk)

Both commands share `StockAdjuster.Apply`, which compares the counted quantity with the sum of the class's batches (only `Quantity > 0`) for the item and location, ordered oldest expiry first (batches without an expiry last), then by id:

- **Shortfall:** the difference is drawn down batch by batch (FIFO); each touched batch gets one `Adjustment` transaction with a negative change and `Reason = <AdjustmentReason name>`.
- **Surplus:** one new batch is created for the difference with no expiry date, `UnitPrice = 0`, received today, no goods receipt, and `Source` left at its default `Order`; one positive `Adjustment` transaction records it.
- **No difference:** nothing is written. For the single `adjust` call this is a validation error (`NoAdjustmentNeeded`); the bulk call skips such lines.

`AdjustmentReason` (persisted as text in `StockTransaction.Reason`): `Adjustment`, `Miscount`, `Damaged`, `Expired`, `Found`, `Other`. The single call accepts any defined value; the bulk call accepts only `Adjustment` or `Miscount`.

**Single (`AdjustStockCommand`):** counted quantity must be `>= 0`; class, item and location must exist. A surplus against an inactive item fails with `items.inactive` (re-checked in the handler because the validator runs before the class lock; drawing down an inactive item stays allowed). It raises `StockAdjustedEvent`. A concurrent change to one of the batches trips the `rowversion` token and returns `stock.concurrency_conflict` (409).

**Bulk (`BulkAdjustStockCommand`):** up to 1000 lines, no two lines for the same (item, location), all items and locations must exist. Each line may carry `ExpectedQuantity`, the total the counter saw: if any line's live total differs, the whole call fails with `stock.bulk_concurrency_conflict` (409); a `null` skips the check. A surplus on any inactive item fails the whole call. One `SaveChanges` makes it atomic, and one `StockAdjustedEvent` is raised per distinct location touched.

### Move

`MoveStockCommand` moves `Quantity > 0` of an item from a source location to a different destination, within one class. Source batches are consumed FIFO; for each one a **new destination batch** is created with the same expiry date, received date, unit price and goods receipt, plus a paired `Transfer` transaction (negative on the source batch, positive on the destination batch). More than the source total → `stock.insufficient_quantity` (409); a concurrent change → `stock.concurrency_conflict`. It raises `StockMovedEvent` and invalidates the stock batch list tag in addition to the stock tags.

### Expired stock

An expired batch is a perishable item's batch with `Quantity > 0` and `ExpiryDate <= today` (UTC).

- **`RemoveExpiredStockCommand`** sets each such batch's `Quantity` to 0 and records an `Adjustment` transaction with `Reason = "Expired"` and the negative quantity.
- **`ExtendExpiredStockExpiryCommand`** sets each such batch's `ExpiryDate` to today + `ExtensionDays` (1 to 365) and records an `Adjustment` transaction with `QuantityChange = 0` and `Reason = "ExpiryExtended: <old> -> <new>"`. The item's `ShelfLifeDays` is not changed.
- Both fail with `stock.no_expired_quantity` (409) when nothing qualifies, and with `stock.concurrency_conflict` on a concurrent change. Both raise `StockAdjustedEvent`.

### Zero-stock visibility

`SetClassItemStockVisibilityCommand` upserts the `ClassItemStockVisibility` row (unique per class, item, location). Setting `false` when no row exists is a no-op; setting the same value again is a no-op. A real change raises `ClassItemStockVisibilityChangedEvent`.

### Rules common to all writes

- Every command is `[Authorize]` (an authenticated user; it is required because `StockTransaction.UserId` is the actor) and `IRequiresOpenClass`, so `ClassOpenBehaviour` rejects it while the class is closed. They carry no role attribute: stock is class-scoped data, not global data.
- Concurrency: `StockBatch.Version` is a `rowversion`; a stale write affects zero rows and surfaces as the conflict errors above instead of a lost update.
- Everything a command writes is saved in a single `SaveChanges`.

### Cache invalidation, events and realtime

- Tags (`Stock/CacheConstants.cs`): coarse `stock:class:all:location:all`, per class `stock:class:{classId}:location:all`, per class and location `stock:class:{classId}:location:{locationId}`.
- `StockAdjustedEventHandler` removes the coarse, class and class-location tags plus the stock batch list tag, then sends `StockAdjusted` to the class's SignalR group. `StockMovedEventHandler` does the same with both locations and sends `StockMoved`. `ClassItemStockVisibilityChangedEventHandler` removes the coarse and class tags and sends `ClassItemStockVisibilityChanged`.
- The commands also declare their tags through `ICacheInvalidation`. Move and the visibility command declare the coarse tag; recount, remove and extend do not (their event handler does).

### Errors

`stock.no_expired_quantity` (409), `stock.insufficient_quantity` (409), `stock.concurrency_conflict` (409), `stock.bulk_concurrency_conflict` (409); also `items.inactive` (409) and `school_classes.not_found`. Validation failures use `ValidationErrorCodes`.

### Tests

- Unit: `tests/Application.UnitTests/Features/Stock/` (adjust, bulk adjust, move, remove expired, extend expiry: handlers and validators; visibility command; the report handler; a `ConcurrencyThrowingDbContext` helper).
- Functional: `tests/Application.FunctionalTests/Features/Stock/` (`BulkAdjustStockTests`, `Queries/GetClassLocationStock`).
- Client: colocated specs for the count store, add-to-order store, preferences service, filter form and the small category items list.
- No functional test exercises move, remove-expired or extend-expiry. Single adjust is only sent through the pipeline in the closed-class and class-lock tests (`ClosedClassRejectsChangesTests`, `ClassLockTests`), not as a happy-path endpoint test. No unit test references the three event handlers.

## SQL

All four tables are SQL Server temporal tables with 2-year history retention (none is in `TemporalExclusions`). Every foreign key is configured `Restrict` (the code comments say cascade deletes would create multiple cascade paths), except the two `GoodsReceiptId` foreign keys on `StockBatches` and `StockTransactions`, which have no explicit delete rule (SQL Server's default, no action, applies).

### `StockBatches`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `ItemId`, `LocationId`, `ReceivedClassId` | `uniqueidentifier` | FKs to `Items`, `Locations`, `SchoolClasses` |
| `Quantity` | `int` | remaining quantity |
| `ExpiryDate` | `date` null | null for non-perishables and surplus batches |
| `ReceivedDate` | `date` | |
| `UnitPrice` | `decimal(10,2)` | |
| `Source` | `nvarchar(20)` | stored as text (`Order`/`Donation`), default `Order`; added by `AddStockBatchSource` |
| `GoodsReceiptId` | `uniqueidentifier` null | |
| `Version` | `rowversion` | concurrency token, added by `AddStockBatchConcurrencyToken` |
| audit columns | | `CreatedDate`, `LastModifiedDate`, `CreatedById`, `LastModifiedById` |

Index `IX_StockBatches_ReceivedClassId_LocationId_ItemId` includes `Quantity` and `ExpiryDate`, so the report's grouping and sums are served from the index.

### `StockTransactions`

`Id`, `ItemId`, `LocationId`, `BatchId` null, `ClassId`, `UserId` (FK to `UserProfiles.IdentityId`, not `UserProfiles.Id`), `Type` (`int`), `QuantityChange`, `Reason` (`nvarchar(250)` null), `CreatedAt` (`datetimeoffset`), `GoodsReceiptId` null, audit columns. Index `IX_StockTransactions_ClassId_ItemId_LocationId_CreatedAt` (`CreatedAt` descending) backs the report's latest-update lookup. Both indexes came from `AddStockReportQueryIndexes`.

### `ClassBalances`

`ClassId`, `ItemId`, `LocationId`, `OpeningQty`, `ClosingQty` (null until the class closes), audit columns. No unique index on (class, item, location).

### `ClassItemStockVisibilities`

`ClassId`, `ItemId`, `LocationId`, `HideWhenZeroStock`, audit columns, with a unique index on (`ClassId`, `ItemId`, `LocationId`). Created by `AddClassItemStockVisibility`; `AddLocationToClassItemStockVisibility` added the location to its key.

### Migrations

`InitialCreate`, `AddClassItemStockVisibility`, `AddLocationToClassItemStockVisibility`, `AddStockBatchConcurrencyToken`, `UseDateTimeOffsetForPersistedInstants`, `AddStockReportQueryIndexes`, `AddStockBatchSource`, `AddTemporalTables`, `SetTemporalRetention`, `AddItemDefaultLocation`. Schema changes go through `dotnet ef migrations add` (see `CLAUDE.md` section 4); never reorder `StockTransactionType`.

## Frontend

### Where it lives

Stock has no route of its own. `ClassStock` (`shared/stock/ui/list/class-stock.ts`) is rendered inside the school class overview page (`features/school-classes/overview/page/school-class-overview.page.ts`), and the overview header (`header-container.ts`) opens the count modal and dispatches `StockEvents` (`addProduct`, `refresh`, `adjustSuccess`). The class page, not this feature, decides which class is shown.

### State

| Piece | Role |
|---|---|
| `StockHttp` | Wraps every `/api/Stock` route. |
| `withStockCollection()` | The report state and filter; `setClassItemStockVisibility`; reloads on the SignalR events `goodsReceiptImportConfirmed`, `stockAdjusted`, `stockMoved`, `stockBatchCreated` and `classItemStockVisibilityChanged`. |
| `StockStore` | Adds `categoryId` and `locationId` query-string sync on top of the collection, so filters survive reloads and links. |
| `StockPreferencesService` | Remembers "show hidden products" in `localStorage` (`stock.showHiddenProducts`). |
| `StockAdjustmentState`, `StockMoveState`, `StockCountState`, `StockAddToOrderState` | Per-modal stores. |

### Screens

- **List:** `StockListContainer` renders items grouped by category in a table or card layout depending on the breakpoint; the filter form offers search, category, location, low-stock, expired and "show hidden".
- **Adjustment modal:** single recount with a reason from `ADJUSTMENT_REASON_OPTIONS` (Romanian labels, e.g. "Numărătoare greșită" for `Miscount`).
- **Stock count modal:** a three-step wizard (`locations` → `items` → `summary`). It collects counts per row, shows only changed rows in the summary, and submits through `bulkAdjustStock` with one reason (default `Adjustment`) and each line's `expectedQuantity` set to the quantity the row showed. On a conflict it keeps the user's counts and refreshes the rows so the next submit sends current expectations.
- **Move modal:** `LocationDropdown` for the destination, excluding the item's current location.
- **Extend-expiry modal** and the remove-expired action work on the expired quantity of one row; **add-to-order** adds a stock row to an order list.

## Open points

Not fixed here; each needs a decision.

1. **Moving stock drops `Source`.** `MoveStockCommandHandler` builds the destination batch without copying `Source`, so a donated batch moved to another location becomes `Order` (the default). Expiry, dates, price and goods receipt are copied.
2. **Surplus batches are priced 0 and never expire.** A recount surplus has no expiry date and `UnitPrice = 0` (an intentional default per the code comment), so surplus stock of a perishable item never shows as expired, and anything that values stock by `UnitPrice` sees it as free (the statistics code reads `UnitPrice`; the exact valuation path was not traced). Its `Source` is also left at `Order`.
3. **`includeHidden` means "only hidden".** With `true` the report returns only the hidden rows, which may surprise API consumers expecting "everything". The client labels it as showing hidden products.
4. **Unknown report filters are ignored silently.** Only `locationId` and `categoryId` filters are honoured; `filters` entries with other fields do nothing and return no error.
5. **Unused transaction types.** `ExpiryWriteoff` is never written and `Usage` only by the demo-data seeder; expiry removal records `Adjustment` with reason `Expired` instead. Consumption statistics therefore cannot rely on a `Usage` type in real data (how they derive consumption was not traced).
6. **Inconsistent clock use.** `ExtendExpiredStockExpiryCommandHandler` uses the injected `TimeProvider`; `RemoveExpiredStockCommandHandler`, the report handler and the surplus batch use `DateTimeOffset.UtcNow` directly, which makes them harder to test.
7. **No uniqueness on `ClassBalances`** (class, item, location), unlike `ClassItemStockVisibilities`. `CloseSchoolClass` looks the row up before adding one, so duplicates would need a concurrent writer.
8. **Test gaps** listed under Backend > Tests.
