# Order lists

An order list is a class's shopping list: a draft that staff build (by hand, from the suggestions catalog, or from predefined supply lists), approve into a submitted order, and export as an Excel sheet or a PNG image to send to a supplier. It is a planning document only: submitting a list changes no stock, and nothing links an order list to the goods receipt that later records the delivery ([goods-receipts.md](goods-receipts.md)).

UI copy is Romanian. Everything below was read from source; anything not traced is marked as such, and gaps and oddities are listed under [Open points](#open-points), not fixed. Related: [stock.md](stock.md), [items.md](items.md), [goods-receipts.md](goods-receipts.md), [locations.md](locations.md), [categories.md](categories.md), and the supply lists feature (`supply-lists.md`, documented separately), each on its own PR until merged.

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/OrderLists/` (`OrderList`, `OrderListLine`), `Domain/Enums/OrderListStatus.cs`, `Domain/Events/OrderList/` (created, submitted, cancelled, reopened) |
| Application | `src/Application/Features/OrderLists/` (commands, queries, event handlers, models, projection, line mapper and validation, filter/sort/cursor/cache config), errors in `Application/Common/Errors/OrderListErrors.cs`, exporter interfaces in `Application/Common/Interfaces/IOrderListExcelExporter.cs` and `IOrderListImageExporter.cs` |
| Infrastructure | `Data/Configurations/OrderListConfiguration.cs`, `OrderListLineConfiguration.cs`; `Export/OrderListExcelExporter.cs` (ClosedXML), `Export/OrderListImageExporter.cs` (SkiaSharp, embedded Liberation Sans fonts) |
| Web | `src/Web/Endpoints/OrderLists.cs` |
| Client | `src/Client/src/app/shared/order-lists` (HTTP, stores, detail modal, wizard, suggestions, tables), `features/school-classes/overview/tabs/order-lists` (the tab and its filter), `core/models/order-list.ts`; the stock list opens `shared/stock/ui/modals/add-to-order` |

There is no Worker or queue involvement: every operation, including exports, runs synchronously in the request.

## Data model

| Entity | Meaning |
|---|---|
| `OrderList` | `ClassId` (required), optional `Name` and `Note`, `Status`, `SubmittedAt` (set on submit, cleared on reopen), a `Version` rowversion and a `LinesRevision` counter (see [Concurrency](#concurrency)). Owns its `Lines`. |
| `OrderListLine` | `OrderListId`, optional `ItemId`, `ProductName` (always set), `Quantity` (decimal), optional `Unit`, optional `Notes`. A line without `ItemId` is a free-text product that is not in the catalog. |

`OrderListStatus` (`Domain/Enums`): `Draft` (editable), `Submitted` (frozen, "represents an order that has been placed"), `Cancelled` (kept for history). It is **stored as text** (`nvarchar(20)`), not as an int. The client labels them "Ciornă", "Finalizată" and "Anulată" (`ORDER_LIST_STATUS_LABELS`).

Transitions are encoded in the entity and the handlers:

```
            Submit (needs >= 1 line)
   Draft ───────────────────────────► Submitted
     ▲  │                                │
     │  └──────── Cancel ────────────────┤  (Cancel works from Draft or Submitted)
     │                                   ▼
     └────────────── Reopen ───────── Cancelled
```

- `IsEditable` is true only for `Draft`; `IsReopenable` only for `Cancelled`.
- Cancel is refused only when the list is already `Cancelled` (`order_lists.already_cancelled`), so both `Draft` and `Submitted` lists can be cancelled.
- Reopen returns a `Cancelled` list to `Draft` and clears `SubmittedAt`.
- Delete is allowed for `Draft` and `Cancelled` lists only; a `Submitted` list must be cancelled first.

## Backend

### Endpoints (`/api/OrderLists`)

The group requires an authenticated user (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`). Every command also carries `[Authorize]` and `IRequiresOpenClass`; the queries carry no attribute of their own.

| Method + route | Use case | Notes |
|---|---|---|
| `POST get-all` | `GetAllOrderListsQuery` | Keyset-paginated list of `OrderListListItemDto`. Cached. |
| `GET catalog/{classId}` | `GetOrderCatalogQuery` | Catalog for the order wizard: `OrderCatalogItemDto` list (`itemId`, `name`, `sku`, `imageFileId`, `unit`, `lastQuantity`, `purchaseCount`, `averageIntervalDays`, `isFrequent`, `locations[{locationId, locationName, isLowStock}]`). `lastQuantity`, `purchaseCount` (0 when never ordered) and `averageIntervalDays` come from the Last365Days statistics row; the client lists rows by `purchaseCount` descending, then interval ascending, then name; `isFrequent` = weekly or every-two-weeks cadence. |
| `GET {id}` | `GetOrderListByIdQuery` | Full `OrderListDto` with lines. Cached. |
| `GET {id}/export?format=xlsx\|png` | `ExportOrderListQuery` | File download; `Submitted` lists only. Default `xlsx`. |
| `POST` | `CreateOrderListCommand` | Draft with optional initial lines (201). |
| `POST add-item` | `AddItemToOrderListCommand` | Adds one catalog item to a draft, or creates a new draft. |
| `PUT {id}` | `UpdateOrderListCommand` | Replaces name, note and **all lines** of a draft. |
| `POST {id}/submit` | `SubmitOrderListCommand` | Draft → Submitted. |
| `POST {id}/cancel` | `CancelOrderListCommand` | → Cancelled. |
| `POST {id}/reopen` | `ReopenOrderListCommand` | Cancelled → Draft. |
| `DELETE {id}` | `DeleteOrderListCommand` | Hard delete (204). |

### Rules common to all writes

- `IRequiresOpenClass`: `Create` and `AddItem` expose the class directly; `Update`, `Submit`, `Cancel`, `Reopen` and `Delete` expose only the order list id, and `ClassOpenBehaviour` / `ClassLockBehaviour` resolve the class from it (`ClassLocking.ResolveClassIdAsync`). A change to a list of a closed class is rejected with the school-class-closed error, including cancel, reopen and delete. Export is a read and is not blocked.
- All handlers return the same shape on success: the order list is re-read through `OrderListProjection.LoadAsync` (lines ordered by `CreatedDate`).
- The three catalog-aware commands (`Create`, `Update`, `AddItem`) accept only **active** catalog items (`InvalidReference` otherwise).

### Fields and validation

| Field | Rule |
|---|---|
| `Name` | optional, max 200, trimmed, blank becomes `null` |
| `Note` | optional, max 1000, trimmed, blank becomes `null` |
| `ClassId` (create) | required, class must exist |
| Line `Quantity` | `> 0` (decimal, stored as `decimal(18,3)`) |
| Line `ProductName` | required when no `ItemId`; max 500 |
| Line `Unit` | required (non-blank), max 50 |
| Line `Notes` | max 1000 |
| Line `ItemId` | when set, must exist and be active |

The line rules are shared by `Create` and `Update` (`OrderListLineValidationExtensions`). `OrderListLineMapper` builds the lines: for an item line a blank `ProductName` is replaced by the catalog name, and a blank unit would fall back to the item's unit (the validator already requires a unit, so that fallback is not reachable through these two commands); notes are trimmed. `Create` adds the lines to the new list in the same save.

**Add item** (`AddItemToOrderListCommand`): requires the class and an active item, `Quantity > 0`, and **exactly one** of `OrderListId` or `NewOrderListName` (`NewOrderListName` max 200). With a name it creates a new draft with one line (product name and unit taken from the item). With an id it loads the list (which must belong to the given class, else `order_lists.not_found`, and be a draft, else `order_lists.not_editable`) and either adds the quantity to the existing line for that item (the first line with that `ItemId`) or appends a new line.

### Submit, cancel, reopen, delete

- **Submit:** list must exist, be a draft (`order_lists.not_editable`, 409) and have at least one line (`order_lists.empty`, 422). It sets `Status`, `SubmittedAt` (UTC now) and raises `OrderListSubmittedEvent`. It does not re-check that the lines' items are still active.
- **Cancel / Reopen:** see the transitions above (`order_lists.already_cancelled`, `order_lists.not_reopenable`, both 409).
- **Delete:** `order_lists.not_deletable` (409) for a `Submitted` list; otherwise the row is removed and the database cascades to the lines. Delete raises no domain event.

### Concurrency

`OrderList.Version` is a SQL Server `rowversion` and `LinesRevision` is an `int`. Lines are child rows, so edits that only touch lines would not change the parent; `Update` and `AddItem` therefore call `TouchLines()` (increments `LinesRevision`) to make the parent row part of the save, which turns a concurrent line writer into a `DbUpdateConcurrencyException`.

- `Update` maps that exception to `order_lists.concurrency_conflict` (409).
- `AddItem` on an existing list retries up to 3 times: on a conflict it clears the change tracker and re-applies the add on fresh data, and returns `order_lists.concurrency_conflict` only if all attempts fail. Creating a new list never retries.
- `Submit`, `Cancel`, `Reopen` and `Delete` do not catch the exception. Since they change or remove the parent row, the rowversion token should apply to them as well; how an uncaught conflict surfaces to the client was not traced.

### Listing, filtering and sorting

- Filter fields (`OrderListFilterConfiguration`): `id`, `classId`, `status`, `createdDate`, `lastModifiedDate`.
- Sort keys (`OrderListSortConfiguration`): `createdDate`, `lastModifiedDate`, `name`, `status`, `id`, each with `Id` as tie-breaker; default `CreatedDate desc, Id desc`. Because `Status` is stored as text, a status sort orders by the status name (this follows from the column type; it was not run).
- `searchTerm` is an accent-insensitive `Contains` on `Name` only; lists without a name never match a search.
- Page size is validated (1 to the default page size) and the handler fetches `pageSize + 1` rows to compute `hasNextPage`. The cursor carries the sort it was issued for.
- Each row has `LineCount`, `ClassName`, `SubmittedAt` and the creator's name; lines are only in the by-id response.

### Caching, events and realtime

- `GetAllOrderListsQuery` (tag `order_lists`) and `GetOrderListByIdQuery` (tags `order_lists`, `order_list:{id}`) are cached with a sliding expiration of about 5 minutes and 30 s jitter.
- Invalidation per command: `Create` clears `order_lists`; `Update`, `Submit`, `Cancel`, `Reopen` and `Delete` clear `order_lists` and `order_list:{id}`; `AddItem` clears `order_lists`, plus `order_list:{id}` when it targets an existing list.
- The four domain events (`OrderListCreated`, `Submitted`, `Cancelled`, `Reopened`) each have a handler that clears `order_lists` and `order_list:{id}` and notifies the class's SignalR group (`RealtimeGroups.SchoolClass(classId)`) with `OrderListCreated`, `OrderListSubmitted`, `OrderListCancelled` or `OrderListReopened`. `AddItem` to a new list raises `OrderListCreatedEvent` through `OrderList.Create`.
- `Update`, `Delete` and `AddItem` to an existing list raise no event, so other open sessions are not told about line edits or deletions (they only refresh on the four state events).
- The item merge ([items.md](items.md#merging-duplicates)) re-points `OrderListLine.ItemId` to the primary item (`OrderListLineItemMerger`, a plain re-point with no combining) and clears the order-list list tag, so lines for the merged items stay as separate lines, possibly several with the same item on one list. Order lists are not otherwise touched.

### Export

`ExportOrderListQuery` (`format` = `Xlsx` or `Png`, enum validated) turns a **submitted** list into a presentation model and renders it. A list in any other status returns `order_lists.not_exportable` (409).

- **Grouping:** lines are grouped by their item's category name; free-text lines (no item) go into a group named "Alte articole", which is sorted last. Groups are ordered alphabetically (case-insensitive, current culture) and lines within a group alphabetically by product name. The model carries name, note, class name and submit date.
- **File name:** the list name, else `Comanda-<class name or "comanda">-<yyyyMMdd of the submit date>`; invalid file-name characters are replaced with `_`. The extension is `xlsx` or `png`.
- **Excel** (`OrderListExcelExporter`, ClosedXML): one sheet named "Comanda" with an optional merged title row, an optional merged "Notă: ..." row, a header row ("Produs", "Cantitate", "U.M.", "Observații"), one grey merged row per category and one row per line, with a spacer row between groups.
- **PNG** (`OrderListImageExporter`, SkiaSharp): a table drawn 1000 units wide at 3x scale, with word-wrapped cells, using the embedded Liberation Sans fonts because the runtime image ships none (`InformarePdfRenderer`, used by the fruit and dessert feature, reuses the same fonts).
- Both exporters are registered as singletons in `Infrastructure/DependencyInjection.cs`. The client downloads through `OrderListExportService`, which reads the file name from the `Content-Disposition` header.

### Errors

`order_lists.not_found` (404); 409: `not_editable`, `concurrency_conflict`, `not_deletable`, `already_cancelled`, `not_exportable`, `not_reopenable`; `order_lists.empty` (422). Also the school-class-closed error from the open-class behaviours and `ValidationErrorCodes` validation failures.

### Tests

- Domain: `tests/Domain.UnitTests/Entities/OrderListTests.cs` (create, submit, cancel, reopen and their events).
- Unit: `tests/Application.UnitTests/Features/OrderLists/` (add-item handler including both concurrency paths and the add-item validator, create validator, submit and reopen handlers, export handler, get-by-id handler).
- Functional: `tests/Application.FunctionalTests/Features/OrderLists/` (lifecycle: create/update/submit, update after submit, empty submit, cancel then delete, cancel and reopen, reopen while submitted, delete while submitted; export as workbook and as PNG, export of a draft; get-all). The item merge and closed-class functional tests also touch order lists.
- Infrastructure: `tests/Infrastructure.IntegrationTests/OrderListExcelExporterTests.cs` and `OrderListImageExporterTests.cs`.
- Client: colocated specs for the wizard store and container, detail store, list store, export service, suggestions, lines, filter form and small list.
- There are no unit tests for the update, cancel or delete handlers (the functional lifecycle tests do exercise them through the API), and no test for the update-path concurrency conflict; only `AddItemToOrderListCommandHandlerTests` covers `DbUpdateConcurrencyException`. Only Submit has a closed-class rejection test (`ClassFoundThroughTheOrderList_IsRejected` in `ClosedClassRejectsChangesTests.cs`); the other order list commands have none.

## SQL

Both tables are SQL Server temporal tables with 2-year history retention (`SetTemporalRetention` covers `OrderLists` and `OrderListLines`; neither is in `TemporalExclusions`).

### `OrderLists`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `ClassId` | `uniqueidentifier` | FK to `SchoolClasses`, `Restrict`; index `IX_OrderLists_ClassId` |
| `Name` | `nvarchar(200)` null | |
| `Note` | `nvarchar(1000)` null | |
| `Status` | `nvarchar(20)` | stored as text |
| `SubmittedAt` | date-time null | created as `datetime2` by `AddOrderLists`; `UseDateTimeOffsetForPersistedInstants` also references this table |
| `LinesRevision` | `int` | default 0, added by `AddOrderListConcurrencyToken` |
| `Version` | `rowversion` | concurrency token, added by `AddOrderListConcurrencyToken` |
| audit columns | | `CreatedDate`, `LastModifiedDate`, `CreatedById`, `LastModifiedById` (FKs to `UserProfiles.IdentityId`, `Restrict`) |

### `OrderListLines`

`Id`, `OrderListId` (FK, **cascade delete**, indexed), `ItemId` (FK to `Items`, `Restrict`, indexed, nullable), `ProductName` (`nvarchar(500)`), `Quantity` (`decimal(18,3)`), `Unit` (`nvarchar(50)` null), `Notes` (`nvarchar(1000)` null), audit columns.

The only indexes are the foreign-key indexes created by `AddOrderLists`; there are no custom keyset indexes (for example no `(CreatedDate, Id)` on `OrderLists`), unlike items and categories. `Status` has no index.

### Migrations

`AddOrderLists` (both tables), `AddOrderListConcurrencyToken` (`LinesRevision`, `Version`), `UseDateTimeOffsetForPersistedInstants`, `AddTemporalTables`, `SetTemporalRetention`. Schema changes go through `dotnet ef migrations add` (see `CLAUDE.md` section 4).

## Frontend

### Where it lives

Order lists have no route of their own. They appear as a tab of the school class overview (`features/school-classes/overview/tabs/order-lists/order-lists-tab.ts`): a filter form plus a table (large screens) or card list (small screens), where each row can be opened, submitted, cancelled, reopened, or downloaded as Excel or PNG. The tab loads with a `classId` filter. Cancelling and reopening ask for confirmation; submitting from the list does not. The overview header (`header-container.ts`) opens the detail modal for a new list of the current class, and the stock list opens the add-to-order modal.

### State

| Piece | Role |
|---|---|
| `OrderListsHttp` | Wraps every `/api/OrderLists` route. `delete` exists but has no caller in the client (no screen deletes an order list). The export call sends `format=Xlsx` or `Png` (a comment says the API binds that value case-sensitively). |
| `withOrderListCollection()` | Paged list, filters, load more, and `changeStatus` (submit, cancel or reopen one row at a time). |
| `OrderListListStore` | Tab store; reloads on the four realtime events `orderListCreated`, `Submitted`, `Cancelled`, `Reopened`. |
| `OrderListDetailState` | Load, create and update one order list; save and submit results are announced through `orderListApiEvents` with an operation id. |
| `OrderListWizardStore` | State machine for creating a new list (below). |
| `OrderListExportService` | Downloads the file and tracks which ids are downloading. |
| `StockAddToOrderState` (in `shared/stock`) | Modal store: loads the class's **draft** lists (a `classId` equals filter and `status = Draft`) and adds a stock item. |

### Detail modal and wizard

`OrderListDetailModal` handles both a new and an existing list. A new list first goes through the wizard; once saved (or when opening an existing list) it behaves as a plain editor.

- **Editable** only when the class is not read-only (`injectClassReadOnly`) and the status is `Draft`; the approve action ("Aprobați comanda?" confirmation) shows for a saved draft; export shows for a `Submitted` list.
- **Wizard steps** (`OrderListWizardStore`): `source` → `selection` → (`products` for supply lists only) → `details` → `edit`. The source is either "new" (suggestions) or "supplyList". Titles: for a new list "Sursă, Produse, Detalii, Articole comandă"; for a supply list "Sursă, Listă predefinită, Produse, Detalii, Articole comandă".
  - **Suggestions source:** one searchable list built from `GET /api/OrderLists/catalog/{classId}` (`GetOrderCatalogQuery`): every active item, grouped by location (an item stocked in several locations appears in each group; items without class stock fall back to their default location, else "Fără locație" last). Rows show a low-stock icon per location (`qty < threshold`) and are ordered by purchase cadence. Suggested lines default to the item's last received quantity (`LastQuantity`), else 1. Product names are prefixed with the SKU as "(SKU) name".
  - **Supply list source:** picks one or more supply lists, lets the user untick products, and merges the lines by item (quantities add up, distinct notes are joined with " · " within a 1000-character limit). This conversion happens only in the browser; the backend has no link between supply lists and order lists.
  - **Details:** a name is required in the wizard (max 200) and the note is optional (max 1000), although the API treats both as optional.
- **Suggestions panel** (large screens, inside the editor): the same catalog list (`GET /api/OrderLists/catalog/{classId}`, no tabs) as a side panel, with an "add" action per line.
- **Add to order from stock:** `StockAddToOrderModal` (opened from the stock list) asks for a target (an existing draft or a new name) and a quantity (>= 0.01, < 1,000,000), then calls `add-item`.

Types and status labels, icons and colours are in `core/models/order-list.ts`.

## Open points

Not fixed here; each needs a decision.

1. **No way to delete an order list from the UI.** The API supports it and `OrderListsHttp.delete` exists, but no client code calls it, so cancelled lists can only accumulate.
2. **Edits and deletions are invisible to other sessions.** `Update`, `Delete` and `AddItem` to an existing list raise no domain event, so only create, submit, cancel and reopen refresh other open sessions. A second user can keep editing a stale draft until a save conflicts.
3. **Uncaught concurrency on status changes.** `Submit`, `Cancel`, `Reopen` and `Delete` do not catch `DbUpdateConcurrencyException`, unlike `Update` and `AddItem`. The resulting HTTP status was not traced.
4. **Submit does not re-check items.** Items are validated as active when lines are saved, but not when the list is submitted, so a draft whose items were deactivated since (or after a merge) can still be submitted and exported. This is by reading `SubmitOrderListCommandHandler`; no test was found.
5. **Item merge can leave duplicate item lines.** The merge only re-points `ItemId` on order list lines (it does not combine them or touch `ProductName`), so a list can end up with two lines for the same item; `AddItem` then adds quantity only to the first one.
6. **Client and server disagree on the required name.** The wizard requires a name for a new list; the API accepts a list without one (and then search, which matches names only, can never find it).
7. **Table columns the server cannot sort or filter.** The client column set includes `className`, `lineCount`, `submittedAt` and `createdByName`, while the server sorts only on `createdDate`, `lastModifiedDate`, `name`, `status` and `id` and filters on `id`, `classId`, `status`, `createdDate`, `lastModifiedDate`. Whether those headers offer sorting was not traced.
8. **Misleading chart title.** The class header chart titled "Evolutia comenzilor" ("orders over time") is `goods-receipt-cost-statistic.ts` and plots goods receipt totals, not order lists ([goods-receipts.md](goods-receipts.md#statistics-link)).
9. **Client-only `'All'` status.** The client `OrderListStatus` type adds an `'All'` value on top of the three server statuses (it has a label, icon and colour). It is not a server status; how the filter turns it into "no status filter" was not traced.
10. **Test gaps** listed under Backend > Tests.
