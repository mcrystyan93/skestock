# Items

An item is a stockable product: a name, an optional SKU, a unit, a required category, an optional default location, per-location minimum thresholds, perishability and an optional image. Stock batches, stock transactions, order lists, supply lists, goods receipts and class statistics all reference items.

Staff create and edit items by hand, switch them active/inactive, upload an image, and (administrators only) merge duplicates. A separate AI-assisted "item import" matches documents to existing items; it does **not** create items (see [Item import](#item-import)).

UI copy is Romanian. Everything below was read from source; gaps and oddities are listed under [Open points](#open-points), not fixed. Categories are documented in [categories.md](categories.md) (also on an open PR if not yet merged).

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/Items/` (`Item`, `ItemLocationThreshold`), `Domain/Events/Items/` (`ItemCreated/Updated/Enabled/Disabled`), `Domain/Enums/ItemImportBatchStatus.cs` |
| Application | `src/Application/Features/Items/` (commands, queries, merge mergers, event handlers, image helpers, models), errors in `Application/Common/Errors/ItemErrors.cs` |
| Infrastructure | `Data/Configurations/ItemConfiguration.cs`, `ItemLocationThresholdConfiguration.cs`, thumbnails via `IImageThumbnailService`, `AI/OpenAiItemDocumentExtractionService.cs` |
| Web | `src/Web/Endpoints/Items.cs`, `ItemImportBatches.cs` |
| Worker | `src/Worker/Queues/ItemImageThumbnailQueueProcessingService.cs`, `ItemImportQueueProcessingService.cs` |
| Client | `src/Client/src/app/features/items` (page, tabs, tables, merge wizard, import lists), `shared/items` (HTTP, stores, form modal, image upload, thumbnail, dropdown, autocomplete), `core/models/item*.ts` |

## Backend

### Endpoints (`/api/Items`)

The group requires an authenticated user (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`; `IEndpointGroup.RequiresAuthorization` defaults to true and no Items endpoint overrides it).

| Method + route | Use case | Notes |
|---|---|---|
| `POST get-all` | `GetAllItemsQuery` | Keyset-paginated, filter + sort + search. |
| `GET {id}` | `GetItemByIdQuery` | Includes `locationThresholds`; 404 `items.not_found`. |
| `GET {id}/image?size=thumb\|full` | `GetItemImageQuery` | Streams the image; see [Images](#images). |
| `POST` | `CreateItemCommand` | 201 with `ItemDto`. |
| `PUT {id}` | `EditItemCommand` | Replaces the item's fields. |
| `PATCH {id}/enable`, `PATCH {id}/disable` | `EnableItemCommand`, `DisableItemCommand` | Return the updated `ItemDto`. |
| `POST merge` | `MergeItemsCommand` | `[Authorize(Roles = Roles.Administrator)]`. 204. |
| `POST merge-preview` | `GetItemMergePreviewQuery` | Administrator only. Counts rows, changes nothing. |

There is no delete; deactivation (`IsActive = false`) is the removal mechanism, and `Item.Category`/`Location` use `DeleteBehavior.Restrict`.

### Fields and validation

Create and Edit share the same rules (separate validators, kept in sync by hand with `ItemConfiguration`):

| Field | Rule |
|---|---|
| `Name` | required, max 100, trimmed |
| `Sku` | optional, max 50, trimmed; blank becomes `null`; must be unique (trimmed, case- and accent-insensitive, own id excluded on edit) → `DuplicateSku` |
| `Description` | optional, max 500; blank becomes `null` |
| `Unit` | required, max 20, trimmed (default `"unit"`) |
| `CategoryId` | required and must exist (`InvalidReference`) |
| `LocationId` | optional default location; must exist when given |
| `IsPerishable` | bool |
| `ShelfLifeDays` | optional; if given must be > 0. A goods-receipt line without an expiry date derives `ReceivedDate + ShelfLifeDays` for perishable items (see the comment on `Item.ShelfLifeDays`). |
| `LocationThresholds` | list of `{LocationId, MinThreshold}`: no nulls, each `MinThreshold >= 0`, one entry per location, every location must exist |
| `ImageFileId` | optional; see [Images](#images) |

### Thresholds

Per-location minimums live in `ItemLocationThreshold` (unique `(ItemId, LocationId)`). Create inserts the list as sent. **Edit is add-or-update only**: locations present in the request are updated or inserted, locations omitted keep their stored value, and an empty list changes nothing. There is no way to delete a threshold through Edit. The list endpoint does not return thresholds (`ItemDto.LocationThresholds` is empty there); only `GET {id}` and the create/edit responses fill it.

### Listing, filtering and sorting

- Filter fields (`ItemFilterConfiguration`): `id`, `sku`, `name`, `unit`, `categoryId`, `isPerishable`, `isActive`, `createdDate`, `lastModifiedDate`.
- Sort keys (`ItemSortConfiguration`): `name`, `sku`, `unit`, `createdDate`, `lastModifiedDate`, `id`, each with `Id` as tie-breaker. Default: `CreatedDate desc, Id desc`.
- `searchTerm` matches `Name` or `Sku` with `Contains`. The accent-insensitive collation is set on those two columns, so the query needs no per-row `Collate` (unlike categories).
- Page size is validated by `GetAllItemsQueryValidator`; the handler fetches `pageSize + 1` rows to compute `hasNextPage`. The cursor encodes the sort it was issued for.
- The item row carries `CategoryName`, `CreatedByName`, `LastModifiedByName`.

### Caching, events and realtime

- `GetAllItemsQuery` (tag `items`) and `GetItemByIdQuery` (tags `items` and `item:{id}`) are `ICacheableQuery`, sliding expiration about 5 minutes with 30 s jitter. `GetItemImageQuery` is deliberately not cached (it streams a blob; the endpoint sets HTTP cache headers instead).
- Invalidation tags per command:

  | Command | Tags |
  |---|---|
  | Create | `items` |
  | Edit, Enable, Disable | `items`, `item:{id}`, purchase statistics |
  | Merge | `items`, `item:{id}` for every id, stock coarse tag, stock batches, order lists, supply lists, goods-receipt imports, goods receipts, purchase statistics, daily consumption, categories |

- `ItemCreated/Updated/Enabled/Disabled` event handlers also remove the categories list tag and the stock coarse tag (category `ItemCount` and stock listings embed item data), then push the matching `RealtimeEvents.Item*` to the `items-list` SignalR group.
- Enable/Disable are idempotent but still emit their event on each request (noted in `DisableItemCommandHandler`; Enable has the same shape).

### Images

An item holds at most one image, referenced by `ImageFileId` (a confirmed `FileMetadata.FileId`). The client uploads the file first, then sends the id on create/edit.

- **Validation** (`ItemImageRules`): the file must exist with status `Completed`, be owned by the caller (`FileNotOwned`), be `image/jpeg|png|webp|gif`, be at most 5 MB, not be used by another item (`DuplicateFile`), and decode as an image (`InvalidImage`). Cheap database checks run before the blob is downloaded. An edit that keeps the current image skips all of these.
- **Thumbnail**: create/edit with a new image writes an outbox message for `GenerateItemThumbnailCommand` on the `item-image-thumbnail` queue. The handler writes a WebP thumbnail to a fixed path (`thumbnails/{imageFileId}.webp`, 256 px per the client comment). It is safe to repeat; a job whose item is gone or whose image has since changed is a no-op, and an undecodable image returns `items.invalid_image` (400).
- **Serving**: `size=thumb` (default) returns the thumbnail, or the original if the thumbnail does not exist yet; `size=full` returns the original. The value is parsed case-insensitively; anything that is not `thumb` or `full` is a 400. The thumbnail fallback is sent `no-cache`; real responses are `private, max-age=31536000, immutable`, so clients bust caches with `?v=<imageFileId>`. `X-Content-Type-Options: nosniff` is always set.
- **Replacing or clearing**: when `ImageFileId` changes on edit, the previous file's metadata is marked `Deleted` and its original and thumbnail blobs are deleted best-effort after the save (a failure is only logged, so a leftover blob is possible).

### Merging duplicates

Administrators can fold several items into one primary item.

- **Rules** (`MergeItemsCommandValidator`): at least two distinct ids, the primary id must be among them. Handler: every id must exist (`items.not_found`) and the primary must be active (`items.merge_primary_inactive`, 409).
- **Execution**: one transaction (`InTransactionAsync`) that first takes an exclusive lock over all classes. It is intentionally not `IRequiresOpenClass`, since closed classes are rewritten too. Every `IItemReferenceMerger` runs, then each active secondary is set inactive and raises `ItemDisabledEvent`, then a single `SaveChanges` commits. A concurrent stock draw-down trips the `StockBatch` rowversion and rolls the whole merge back.
- **Mergers**: one per table that references an item, auto-registered by reflection (`DependencyInjection.cs`). `PlainItemMerger` re-points every secondary row; `KeyedItemMerger` handles tables with a unique key that includes the item: per key, the primary's row survives (otherwise the first secondary's, re-pointed) and the others are `Combine`d into it and deleted. Preview and merge share the same plan, so the counts match. The ten kinds (`ItemMergeKind`): stock batches, stock transactions, goods-receipt import lines, order list lines, class item stock visibilities, item location thresholds, supply list lines, daily item consumptions, class balances, item purchase statistics. On a key collision, quantities and values are summed for class balances, daily consumption, purchase statistics and supply list lines; thresholds and class stock visibilities keep the survivor unchanged.
- **Safety net**: `ItemMergerCoverageTests` fails if any entity with an `ItemId`, `MatchedItemId` or an FK to `Item` lacks a merger, so a new item-referencing table must get one.
- Only references move. The secondary items keep their own fields and stay in the table, inactive. Merge is not undoable from the app; the temporal history tables are the only record.

### Item import

The Items page has a third tab, **Importuri articole**, with the same lifecycle as the category import (see [categories.md](categories.md#import-flow-ai-assisted)): upload files → batch + outbox message → Worker extracts with OpenAI on the `item-import` queue → `PendingReview` → the person reviews → confirm. Endpoints are under `/api/ItemImportBatches` (create, get-all, get by id, `{id}/confirm`). Statuses (`ItemImportBatchStatus`, persisted as int): `Processing = 0`, `PendingReview = 1`, `Confirmed = 2`, `Failed = 3`.

What differs from categories:

- **Confirm does not create items or categories.** Each reviewed line must pick an *existing* item (`ItemId` required; unknown ids → `item_import_batches.items_not_found`, duplicates in one request → `item_import_batches.duplicate_items`, both 400). The stored result echoes the selected catalog items, with `Created` and `CategoryCreated` always `false`. Max 500 items per confirm; per-line limits mirror the item field limits.
- Review suggestions (`ItemImportSuggestionResolver`) are de-duplicated by SKU, or by name + category when there is no SKU, and enriched with matches against existing items and categories (accent-insensitive).
- Idempotency, lease and concurrency fields exist on `ItemImportBatch` exactly as on `CategoryImportBatch` (`ClientRequestId`, `ProcessingLeaseUntilUtc`, `ConcurrencyStamp`, stored `ConfirmationResultJson`). The process handler was not re-read for this document.

### Errors

`items.not_found` (404), `items.inactive` (409, raised by stock, goods receipt and stock batch writes against an inactive item; supply lists use their own `supply_lists.item_inactive`, 400), `items.merge_primary_inactive` (409), `items.image_not_found` (404), `items.invalid_image` (400); import: `item_import_batches.not_found`, `.not_in_review`, `.items_not_found`, `.duplicate_items`, `.idempotency_conflict`.

### Tests

- Unit: `tests/Application.UnitTests/Features/Items/` (create, edit, enable, disable, merge handler/validator/coverage, thumbnail, get-by-id, get-image, merge preview, image validation, image outbox, import handlers) and `Documents/OpenAiItemDocumentExtractionServiceTests.cs`.
- Functional: `tests/Application.FunctionalTests/Features/Items/` (create, edit, enable, disable, merge, get-all, get-by-id, image endpoint, `ItemHistoryTests`).
- Client: colocated `*.spec.ts` for the list store, filter form, small list, merge wizard and steps, image upload, thumbnail, hero, form, dropdown, autocomplete; Playwright `e2e/specs/item-merge.spec.ts` and `seeded-items.spec.ts`.

## SQL

The item tables are SQL Server temporal tables with 2-year history retention, except `DailyItemConsumptions` and `ItemPurchaseStatistics`, which are in `TemporalExclusions` (`ApplicationDbContext.cs`) because they are derived and rebuilt daily.

### `Items`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK; `Create` sets it with `Guid.CreateVersion7()` up front so the thumbnail job message can carry it |
| `Sku` | `nvarchar(50)` null | accent-insensitive collation; **filtered unique index** `WHERE [Sku] IS NOT NULL` (also serves the `sku` sort) |
| `Name` | `nvarchar(100)` | accent-insensitive collation |
| `Description` | `nvarchar(500)` null | |
| `Unit` | `nvarchar(20)` | |
| `MinThreshold` | `int` | **legacy**, not read at runtime (the seeder `ApplicationDbContextInitialiser.cs` still sets it; see Open points) |
| `IsPerishable` | `bit` | |
| `ShelfLifeDays` | `int` null | |
| `IsActive` | `bit` | |
| `ImageFileId` | `uniqueidentifier` null | added by `AddItemImage`; filtered unique index `WHERE [ImageFileId] IS NOT NULL` |
| `CategoryId` | `uniqueidentifier` | FK to `Categories`, `Restrict` |
| `LocationId` | `uniqueidentifier` null | default location, added by `AddItemDefaultLocation` (that migration also backfills it from existing stock batch locations); FK to `Locations`, `Restrict` |
| `CreatedDate`, `LastModifiedDate` | `datetimeoffset` | UTC |
| `CreatedById`, `LastModifiedById` | `uniqueidentifier` null | FK to `UserProfiles.IdentityId`, `Restrict` |

Indexes (named for the keyset paths): `IX_Items_CreatedDate_Id` (desc), `IX_Items_LastModifiedDate_Id` (desc), `IX_Items_Name_Id`, `IX_Items_IsActive_CreatedDate_Id` (active first, then newest), `IX_Items_CategoryId_CreatedDate_Id` (also covers the category FK).

### `ItemLocationThresholds`

`Id`, `ItemId` (FK `Restrict`), `LocationId` (FK `Restrict`), `MinThreshold`. Unique `IX_ItemLocationThresholds_ItemId_LocationId`, plus an index on `LocationId`. Created by `AddItemLocationThresholds`, which also contains a data-copy `INSERT` into the new table.

### Related tables

`StockBatches`, `StockTransactions`, `ClassBalances`, `ClassItemStockVisibilities`, `OrderListLines`, `SupplyListLines`, `GoodsReceiptImportLines`, `DailyItemConsumptions` and `ItemPurchaseStatistics` reference items and are the tables the merge re-points. `ItemImportBatches`, `ItemImportBatchFiles` and `ItemImportBatchHistory` hold the import lifecycle.

### Migrations

`InitialCreate`, `AddClassItemStockVisibility`, `AddOrderLists`, `AddItemListingIndexesAndCollation`, `AddDailyItemConsumption`, `AddItemPurchaseStatistics`, `AddSupplyLists`, `AddItemImage`, `AddItemLocationThresholds`, `AddTemporalTables`, `SetTemporalRetention`, `AddItemDefaultLocation`. Schema changes go through `dotnet ef migrations add` (see `CLAUDE.md` section 4).

## Frontend

### Page

`itemsRoutes` is lazy-loaded at `/items` (`core/layouts/full/full.routes.ts`). `ItemsPage` provides `ItemListState`, `ItemImportState`, `SupplyListListState`, `NzModalService` and `FileStorageState`, and joins the `items-list`, `item-import-batches-list` and `supply-lists-list` SignalR groups for its lifetime. Three tabs (labels defined in the header component; the tab bar itself is hidden): **Articole**, **Liste** (supply lists, documented with their own feature) and **Importuri articole**. Header actions: **Adaugă**, **Adaugă listă**, **Importă**.

Tables switch by breakpoint like categories (large table, small card list).

### State

| Store | File | Role |
|---|---|---|
| `withItemCollection()` | `shared/items/services/item-collection.feature.ts` | Paged item list, filter, load more; shared by the page and the dropdown. |
| `ItemListState` | `features/items/services/item-list.store.ts` | Page store: enable/disable (`toggleActive`), row selection that survives filter changes, `patchItem`, `setItemImage`, merge state. |
| `ItemDetailState` | `shared/items/services/item-detail.store.ts` | Load/create/update one item. |
| `ItemDropdownStore`, `ItemAutocomplete` | `shared/items/` | Reusable item pickers used by stock, stock batches, order lists, supply lists, goods-receipt review and class statistics. `item-autocomplete.ts` is on the project's fragile-area list: test before refactoring. |
| `ItemImportState`, `ItemImportReviewState` | `shared/items/services/` | Import list and review. |

`ItemsHttp` (`shared/items/services/items.http.ts`) wraps the JSON `/api/Items` routes (the image URL is built directly in `item-thumbnail.ts`); `getByIdCached` and the create/edit/enable/disable calls also feed a client-side cache (not traced in detail).

### Behaviour worth knowing

- **Form** (`shared/items/ui/modals/detail/form.ts`): signal form with the same limits as the backend (messages in Romanian). It lists *every* location with a threshold input defaulting to 0, so an Edit from the form sends a threshold for each location. Shelf life is hidden unless the item is perishable.
- **Active toggle**: disabling asks for confirmation ("Confirma dezactivarea articolului?"); enabling does not.
- **Image upload** (`ItemImageUpload`): client-side check of type and 5 MB before upload, mirroring the server. Each instance has its own `FileStorageState`. On a list row, `setItemImage` re-sends the row's current fields through the edit endpoint with the new `imageFileId`.
- **Saving**: after a modal edit the list patches only that row (`patchItem`) to keep the scroll position; after a create it reloads, since the new item's place depends on sort and filter.
- **Merge wizard** (`features/items/list/merge/`): shown when two or more rows are selected. Three steps: choose the primary, review affected data (fetched from `merge-preview` on entering step 2, dropped when going back), confirm. Row labels for each `ItemMergeKind` are in `core/models/item.ts`.

## Open points

Not fixed here; each needs a decision.

1. **Authorization on writes.** Create, Edit, Enable and Disable carry no role attribute, while only Merge and merge-preview are administrator-only. `CLAUDE.md` section 10 asks for `[Authorize(Roles = Roles.Administrator)]` on commands that write global/shared data; items are global.
2. **Merge button visible to everyone.** The client shows it whenever two or more rows are selected; no role check was found under `features/items` or `shared/items`. A non-admin gets a server rejection after going through the wizard.
3. **Legacy `Items.MinThreshold`.** The column and the `Item.MinThreshold` property remain (a code comment says to drop the column in a later migration) and are not read at runtime; the seeder still writes it.
4. **Thresholds cannot be removed via Edit** (add-or-update only). Whether that is intended for locations the form always lists is a product question.
5. **Item import never creates items**, yet the `ItemImportBatchStatus.Confirmed` comment says "items (and any missing categories) were created", and the result DTO keeps `Created`/`CategoryCreated` fields that are always `false`. The comment looks stale.
6. **Dead code on the page.** `items.page.ts` `onImport()` has a commented-out `afterClose` block.
7. **Leftover image blobs** are possible when an image is replaced (the blob delete is best-effort and only logged).
