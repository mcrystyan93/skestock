# Goods receipts

A goods receipt records a delivery of items into a school class's stock: a header (date, supplier reference, note, total amount) plus one stock batch and one `Order` transaction per line. In the app, receipts are created by uploading a delivery document (invoice, delivery note); an AI step extracts the lines, a person reviews and corrects them, and confirming creates the receipt, its stock, and any items or categories the catalog was missing. The API also has a direct "create receipt" endpoint that the client does not use (see [Open points](#open-points)).

UI copy is Romanian. Everything below was read from source; gaps and oddities are listed under Open points, not fixed. Related: [stock.md](stock.md), [items.md](items.md), [categories.md](categories.md), [locations.md](locations.md) (each on its own PR until merged).

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/GoodsReceipts/` (`GoodsReceipt`, `GoodsReceiptImport`, `GoodsReceiptImportLine`), `Domain/Events/GoodsReceipt/` (import created/completed/failed/confirmed), `Domain/Enums/GoodsReceiptImportStatus.cs` |
| Application | `src/Application/Features/GoodsReceipts/` (commands, queries, event handlers, models, filter/sort/cursor/cache config), errors in `Application/Common/Errors/GoodsReceiptErrors.cs` |
| Infrastructure | `Data/Configurations/GoodsReceipt*Configuration.cs`; extraction via `IStockDocumentExtractionService` (OpenAI) |
| Web | `src/Web/Endpoints/GoodsReceipts.cs` |
| Worker | `src/Worker/Queues/GoodsReceiptImportQueueProcessingService.cs` |
| Client | `src/Client/src/app/shared/goods-receipts` (receipts list, upload modal), `shared/goods-receipt-imports` (import list, review), `core/models/goods-receipt.ts`; mounted in `features/school-classes/overview` |

## Data model

| Entity | Meaning |
|---|---|
| `GoodsReceipt` | Header: `ReceivedAt`, `ClassId`, `SupplierReference` (optional), `Note` (required), `TotalAmount`. `TotalAmount` is the sum of `Quantity * UnitPrice` captured at creation, so later stock changes never alter what was paid. Has `Batches` and `Transactions` navigation collections. |
| `StockBatch` / `StockTransaction` | One of each per line, linked through `GoodsReceiptId` (see [stock.md](stock.md#data-model)). The transaction type is `Order`. |
| `GoodsReceiptImport` | One uploaded document: `BlobPath`, `Status`, `ErrorMessage`, `ClassId` (fixed at upload), `FileMetadataId`, `UploadedByUserId`, `UploadedAt`, `ProcessedAt`, `ExtractedDataJson`, and `ResultingGoodsReceiptId` once confirmed. |
| `GoodsReceiptImportLine` | A snapshot of each reviewed line, written at confirmation: `RawItemText`, `MatchedItemId`, `LocationId`, `Quantity`, `UnitPrice`, `ExpiryDate`. Later stock changes never update it. |

`GoodsReceiptImportStatus` (persisted as int): `Processing = 0`, `PendingReview = 1`, `Confirmed = 2`, `Failed = 3`.

Unlike the category and item imports, a goods receipt import is **one file per import** and has no `ClientRequestId`, no processing lease, no attempt counter, no concurrency stamp and no status-history collection (`ImportBatchHistory`). Its lifecycle is simpler and older; see the process step below. (The table still gets SQL temporal history like every other table.)

## Backend

### Endpoints (`/api/GoodsReceipts`)

The group requires an authenticated user (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`). The three commands also carry `[Authorize]` and `IRequiresOpenClass`.

| Method + route | Use case | Notes |
|---|---|---|
| `POST get-all` | `GetAllGoodsReceiptsQuery` | Keyset-paginated list of `GoodsReceiptListItemDto`. Cached. |
| `GET {id}` | `GetGoodsReceiptByIdQuery` | Returns the header only; see Open points. Not cached. |
| `GET {id}/import-lines` | `GetGoodsReceiptImportLinesQuery` | Confirmed import line snapshots for a receipt; an empty list when the receipt has none. Cached. |
| `POST` | `CreateGoodsReceiptCommand` | Direct creation (201). |
| `POST imports` | `CreateGoodsReceiptImportCommand` | Starts an import for an uploaded file (201). |
| `POST imports/get-all` | `GetAllGoodsReceiptImportsQuery` | Paginated import list. Cached. |
| `GET imports/{id}` | `GetGoodsReceiptImportByIdQuery` | Review payload. Not cached. |
| `POST imports/{id}/confirm` | `ConfirmGoodsReceiptImportCommand` | Creates the receipt (201). |

### Creating a receipt directly

`CreateGoodsReceiptCommand` takes a class, optional supplier reference, a required note and lines of `{ItemId, LocationId, Quantity, ExpiryDate?, UnitPrice}`.

- **Validation:** class must exist; `Note` required, max 500; `SupplierReference` max 200; 1 to 200 lines; per line `Quantity > 0` and `UnitPrice >= 0` (zero allowed, for donated goods); items and locations must exist; no two lines with the same (item, location, expiry date) (`DuplicateReceiptLine`).
- **Expiry rules:** a non-perishable item must not have an expiry date (`ExpiryDateNotAllowed`); a perishable item needs one unless it has `ShelfLifeDays > 0`, in which case the handler derives `ReceivedDate + ShelfLifeDays` (`ExpiryDateRequired` otherwise).
- **Handler:** re-checks that no referenced item is inactive (`items.inactive`, 409, because the validator runs before the class lock), then builds the receipt, a batch and an `Order` transaction per line (received date = today, UTC), adds up `TotalAmount`, and saves everything in one `SaveChanges`. `Source` is not set, so batches take the default `Order`.
- The response is a `GoodsReceiptDto` with its lines (quantities at creation time).

### Import flow (AI-assisted)

```
Client uploads one file ──► POST imports {classId, fileMetadataId}
                                │  one transaction: import (Processing) + OutboxMessage
                                ▼
                    outbox publisher ──► goods-receipt-import queue ──► Worker
                                                                          │ ProcessGoodsReceiptImportCommand
                                                                          ▼
                              download blob → OpenAI extraction → PendingReview (or Failed)
                                                                          │ SignalR: GoodsReceiptImportProcessed
                                ┌─────────────────────────────────────────┘
                                ▼
        GET imports/{id} (extracted lines + SKU matches) ──► person reviews, splits, assigns locations
                                ▼
          POST imports/{id}/confirm ──► items/categories created if missing ──► receipt + batches + transactions
```

**Create import:** the class and the file metadata must exist; the blob path is copied from the file metadata. The outbox row is written in the same save as the import, so a created import always gets queued.

**Process (Worker):**
- A missing import returns `goods_receipt_imports.not_found`.
- Imports already `Confirmed` or `Failed` return OK without work. The handler itself processes `Processing` and `PendingReview` again, which would re-run the extraction and overwrite the stored result. In practice the queue layer prevents this for a message that already committed: `QueueMessageProcessor` skips a message id found in `ProcessedMessages`, and writes that row in the same transaction as the handler's changes. A re-run therefore needs an earlier attempt that failed before commit, or a new message (the only place one is enqueued is import creation).
- `UnprocessableDocumentException` marks the import `Failed` immediately (no retry), but see Open points: the `Failed` event handler is an unimplemented stub. Any other exception, including `TransientExtractionException`, propagates; the queue processor leaves the message undeleted so it reappears after the visibility timeout, and moves it to the poison queue after 5 deliveries. Permanent failures (a failed result such as `goods_receipt_imports.not_found`, validation and unprocessable-document exceptions) go to the poison queue immediately.
- The queue's visibility timeout is 30 seconds (a comment says it "must exceed the processing time of one message"). The handler, including the OpenAI call, runs inside the processor's database transaction.

**Review (`GET imports/{id}`):** returns the extracted supplier reference and date, and one line per extracted item with the raw text, product code, name, unit, category, quantity, unit price and perishable flag. Each line is matched to an **active** catalog item by SKU (trimmed, case- and accent-insensitive) against the line's product code; a line with no match means "create a new item on confirm". A import still `Processing` or one that `Failed` before extraction returns an empty line list instead of an error.

**Confirm (`ConfirmGoodsReceiptImportCommand`):**
- Only the `PendingReview` state can be confirmed (`goods_receipt_imports.not_in_review`, 409). A repeated confirm of an already `Confirmed` import returns the receipt that was created instead of making a second one.
- Each line either references an existing item (`ItemId`) or carries the fields to create one (`Name`, `Sku`, `Unit`, `CategoryName`, `IsPerishable`). New lines need `Name` and `CategoryName`. The new-item path is API-only: the client's `buildConfirmRequest` always sends `itemId` and sets `name`, `sku`, `unit` and `categoryName` to `null`, so an unmatched line cannot be confirmed from the UI. Categories are matched by name (trimmed, accent-insensitive) and created if missing; new items are de-duplicated within the request by SKU, or by name + category. New items are created active with the given unit (default `"unit"`), and have no description, shelf life, default location, image or thresholds.
- Lines carry `LocationId`, `Quantity > 0`, `UnitPrice >= 0`, optional `ExpiryDate` and a `SourceLineIndex` pointing at the extraction line they came from (a split produces several lines sharing an index). Max 200 lines; `Note` required (max 500); `SupplierReference` max 200. The expiry rules are the same as for direct creation, except a new item has no stored shelf life, so a perishable new item always needs an explicit expiry.
- The receipt's date is the extracted `ReceivedAt` (stored as midnight UTC), or today when the document had none. The request's `ReceivedDate` property is never read.
- The snapshot line text is the extracted name for that `SourceLineIndex`, else the resolved item name; it must be non-empty and at most 500 characters, otherwise a validation error is thrown.
- One `SaveChanges` writes the new categories and items, the receipt, the batches, the transactions, the line snapshots and the import's `Confirmed` status. The class is resolved from the import, not from the request.
- The "split quantities must sum to the extracted quantity" rule is **commented out** in the validator, so it is not enforced (see Open points).

### Listing, filtering and sorting

- **Receipts:** filter fields `id`, `classId`, `receivedAt`, `supplierReference`, `createdDate`, `lastModifiedDate`; sort keys `receivedAt`, `createdDate`, `lastModifiedDate`, `id` (each with `Id` as tie-breaker); default `CreatedDate desc, Id desc`. `searchTerm` is an accent-insensitive `Contains` on the note and supplier reference. Each row has `LineCount`, `TotalQuantity` (sum of the batches' **current** quantity), `TotalAmount`, and the `FileMetadataId` of the import that produced it (null for directly created receipts).
- **Imports:** filter fields `id`, `classId`, `status`, `fileMetadataId`, `uploadedAt`, `createdDate`, `lastModifiedDate`; sort keys `uploadedAt`, `createdDate`, `lastModifiedDate`, `id`; default `CreatedDate desc, Id desc`. `searchTerm` matches the class name and the error message.
- Page size is validated by the query validators and the handlers fetch `pageSize + 1` rows to compute `hasNextPage`.

### Caching, events and realtime

- Cached queries: receipts list (tag `goods_receipts`), imports list (tag `goods_receipt_imports`), import lines (tags `goods_receipts`, `goods_receipt:{id}` and the locations list tag). Sliding expiration about 5 minutes with 30 s jitter.
- `CreateGoodsReceiptCommand` invalidates the receipts list tag, the class's goods-receipt-cost statistics tag, the class stock tag and each touched location's stock tag. `ConfirmGoodsReceiptImportCommand` invalidates the same plus the imports list, items list and categories list tags.
- Three of the four import domain events have a working handler that clears the imports list tag and notifies the `goods-receipts-import-list` SignalR group: created (`GoodsReceiptImportCreated`), completed (`GoodsReceiptImportProcessed`) and confirmed (`GoodsReceiptImportConfirmed`; it also clears the receipts list tag and the stock coarse tag). **The failed-event handler (`GoodsReceiptImportFailedEventHandler`) only throws `NotImplementedException`**, so a failed import clears no tag and sends no notification; see Open points.
- Direct creation raises no domain event, so it sends no realtime notification.

### Statistics link

`GetClassGoodsReceiptCostsQuery` (`Features/Statistics`, `[Authorize]`) reports a class's receipt costs, and the purchase-statistics job reads receipts through their transactions (`Features/Statistics/Commands/MaterializePurchaseStatistics`). The school class header shows a small cost chart ("Evolutia comenzilor"). These are documented with statistics, not here.

### Errors

`goods_receipts.not_found` (404), `goods_receipt_imports.not_found` (404), `goods_receipt_imports.not_in_review` (409); also `items.inactive` (409). Validation failures use `ValidationErrorCodes` (for example `DuplicateReceiptLine`, `ExpiryDateRequired`, `ExpiryDateNotAllowed`, `EmptyLines`).

### Tests

- Unit: `tests/Application.UnitTests/Features/GoodsReceipts/` (create receipt, create import, process import, confirm import handlers; the create and create-import validators; the receipts list, imports list, import review and import lines handlers; query validators).
- Functional: `tests/Application.FunctionalTests/Features/GoodsReceipts/` (create import, imports list, import lines endpoint, and an outbox-to-worker integration test).
- Client: colocated specs for the HTTP client, import lines store and components, review lines, import list filter and small list.
- No functional test was found for confirming an import or for creating a receipt directly through the endpoint.

## SQL

All three tables are covered by the temporal-table migrations with 2-year history retention (none is in `TemporalExclusions`).

### `GoodsReceipts`

`Id`, `ReceivedAt` (`datetimeoffset`), `ClassId` (FK `Restrict`), `SupplierReference` (`nvarchar(200)` null), `Note` (`nvarchar(500)`), `TotalAmount` (`decimal(10,2)`), audit columns. There are no custom indexes beyond the foreign-key ones.

### `GoodsReceiptImports`

`Id`, `BlobPath` (`nvarchar(1000)`), `Status` (`int`), `ErrorMessage` (`nvarchar(2000)` null), `ClassId` (FK `Restrict`), `FileMetadataId` (FK `Restrict`), `UploadedByUserId` (FK `Restrict`), `UploadedAt`, `ProcessedAt`, `ResultingGoodsReceiptId` (FK `Restrict`, indexed), `ExtractedDataJson`, audit columns.

### `GoodsReceiptImportLines`

`Id`, `ImportId` (FK, **cascade delete** from the import), `RawItemText` (`nvarchar(500)`), `MatchedItemId` (FK `Restrict`), `LocationId` (FK `Restrict`), `Quantity`, `UnitPrice` (`decimal(10,2)`), `ExpiryDate`, audit columns.

### Links from stock

`StockBatches.GoodsReceiptId` and `StockTransactions.GoodsReceiptId` point at `GoodsReceipts`; their foreign keys have no explicit delete rule (SQL Server's default, no action) and are indexed.

### Migrations

The tables come from `InitialCreate`; later changes are `UseDateTimeOffsetForPersistedInstants`, `AddTemporalTables` and `SetTemporalRetention`. No migration is specific to goods receipts. Schema changes go through `dotnet ef migrations add` (see `CLAUDE.md` section 4).

## Frontend

### Where it lives

Goods receipts have no route of their own. Two tabs of the school class overview (`features/school-classes/overview`) show them: the receipts tab (`GoodsReceiptsTab`, a table or card list by breakpoint, with a row that expands to the saved import lines and a download action for the source file) and the imports tab (`GoodsReceiptImportsTab` with the filterable import list). The overview page joins the `goods-receipts-import-list` SignalR group, and the overview header (`header-container.ts`) opens the upload modal; the cost chart is `goods-receipt-cost-statistic.ts`, rendered from the header component.

### Pieces

| Piece | Role |
|---|---|
| `GoodsReceiptsHttp` | `getAll`, `getById`, `getImportLines`, `create`, `createImport`. Of these, `create` and `getById` have no caller in the client. |
| `GoodsReceiptImportsHttp` | Import `getAll`, `getById` (review) and `confirm`. |
| `AddGoodsReceiptModal` + `GoodsReceiptImportState` | Despite the name, it uploads document files only (via `FileStorageState`, with a progress bar) and creates one import per uploaded file for the current class. |
| `ReviewStore` (`review.store.ts`) | Loads the extraction and builds editable lines: a matched item is pre-selected and its shelf life pre-fills the expiry; **split** halves a row's quantity and adds the remainder as a new row with the same `sourceLineIndex`; **remove** drops a row; **confirm** posts the lines with the supplier reference and note (every line must have an item and a location, since `buildConfirmRequest` reads both without a fallback). |
| Review UI | `review-modal`, `review-lines-table` with a table row (`review-line`) and a small-screen row (`review-line-small`); location pickers use `LocationDropdown` with `prePopulateWithDefault`. |
| Lists | `GoodsReceiptImportsList` (filters, table, small list) and the receipts table. |

Types are in `core/models/goods-receipt.ts`.

## Open points

Not fixed here; each needs a decision.

1. **`GET /api/GoodsReceipts/{id}` returns no lines.** `GetGoodsReceiptByIdHandler` has the `Lines` projection commented out, but the endpoint description says "including its lines". Nothing in the client calls it, so the problem is hidden; direct creation and confirm do return lines.
2. **Direct receipt creation is unused and unobserved.** The client has no screen that calls `POST /api/GoodsReceipts` (the "Add" modal only uploads documents), and no functional test covers the endpoint. It raises no domain event, so other open sessions are not notified of a receipt created this way.
3. **Split-quantity validation is disabled.** The confirm validator's split rule is commented out, yet `SourceLineIndex` is documented as "the server checks their quantities sum back to the original". Today a confirmed split can add up to any quantity.
4. **New-item SKU conflicts are not validated on confirm.** A new-item line whose SKU already exists passes validation; the unique `Items.Sku` index would reject it at save time (not traced to a friendly error). The review step also matches only active items, so an inactive item's SKU would reach this path.
5. **Process step is weaker than the other imports.** No lease, attempt limit or `ClientRequestId`. The handler would re-run the AI extraction for a `PendingReview` import and could re-fire the completed event, though the queue's processed-message check normally prevents that. Whether the 30 s visibility timeout covers a full extraction (which runs inside the database transaction) was not measured.
6. **List `TotalQuantity` is a current figure.** It sums the batches' remaining quantity, so it falls as stock is used, while `TotalAmount` stays at the original value.
7. **Stale comments and unused fields.** The review handler says SKU uniqueness "isn't enforced at the DB level", but `Items.Sku` has a filtered unique index; `ConfirmGoodsReceiptImportCommand.ReceivedDate` is never read; the name `AddGoodsReceiptModal` suggests manual entry.
8. **Receipt batches always take `Source = Order`.** Neither creation path sets a source, so a donation recorded through a receipt cannot be marked as one (the only code that sets `Donation` is `CreateStockBatchCommandHandler`).
9. **A failed import may not stay failed.** `GoodsReceiptImportFailedEventHandler` throws `NotImplementedException`. Read statically (not reproduced): the handler's `MarkAsFailed` and save run inside the Worker's transaction, domain events are published before it commits, so the exception rolls the transaction back, the import stays `Processing`, and the message is retried until it reaches the poison queue after 5 deliveries. The other three import handlers are implemented.
10. **Test gaps** listed under Backend > Tests.
