# Category document import

An import is a **batch of one or more uploaded files**. Document reading proposes
category names; it never creates categories until a user confirms reviewed
names. This page describes the flow as currently implemented; see
[Categories](README.md) for ordinary reads and direct create/update.

```text
CategoriesPage -> CategoryImportModal -> FileStorageState -> StorageHttp
  -> POST /api/Storage/request-upload -> FileMetadata (pending) + upload SAS
  -> PUT blob via SAS (app-files) -> POST /api/Storage/confirm-upload
  -> FileMetadata (completed)
  -> CategoryImportState -> POST /api/CategoryImportBatches
  -> CategoryImportBatch + files/history + OutboxMessage (same SaveChanges)
  -> Web OutboxPublisherService -> Azure category-import queue
  -> Worker QueueMessageProcessor -> ProcessCategoryImportBatchCommand
  -> download blobs -> OpenAI extraction -> stored suggestions (pending review)
  -> GET /api/CategoryImportBatches/{id} -> review UI
  -> POST /api/CategoryImportBatches/{id}/confirm
  -> reuse/create Categories + confirm batch (one SaveChanges)
```

## 1. Upload and schedule

1. The [categories page](../../../src/Client/src/app/features/categories/list/categories.page.ts)
   opens the [import modal](../../../src/Client/src/app/shared/categories/ui/modals/import/category-import-modal.ts).
   Its multi-file picker queues local files (it does not upload on selection).
   **Upload and process** invokes
   [`FileStorageState.uploadFiles`](../../../src/Client/src/app/shared/storage/services/file-storage.store.ts)
   sequentially. For each file,
   [`StorageHttp`](../../../src/Client/src/app/shared/storage/services/storage.http.ts)
   requests a SAS, PUTs the file directly to Blob Storage, then confirms it.
2. [`POST /api/Storage/request-upload`](../../../src/Web/Endpoints/Storage.cs)
   creates a `FileMetadata` record (`Pending`, original name, MIME type,
   `app-files` container and a file-ID-prefixed blob path) and returns a
   **10-minute write SAS**. `POST /api/Storage/confirm-upload` checks that the
   blob exists, records its size and ETag, and changes the metadata to
   `Completed` ([handlers](../../../src/Application/Storage/Commands/RequestUpload/RequestUploadCommandHandler.cs),
   [confirmation](../../../src/Application/Storage/Commands/ConfirmUpload/ConfirmUploadCommandHandler.cs)).
   The upload/confirmation pair uses `FileMetadata.FileId`; the batch later
   references the separate `FileMetadata.Id` returned by confirmation.
3. After successful uploads, the storage store dispatches `uploadSuccess`
   with confirmed metadata IDs. The modal creates a batch only when at least
   one upload succeeded **and none failed**; a partial failure leaves the modal
   displaying failed file names. It sends
   `{ "fileMetadataIds": ["..."], "clientRequestId": "..." }` using
   [`CategoryImportState`](../../../src/Client/src/app/shared/categories/services/category-import.store.ts)
   and [`CategoryImportsHttp`](../../../src/Client/src/app/shared/categories/services/category-imports.http.ts).
4. [`POST /api/CategoryImportBatches`](../../../src/Web/Endpoints/CategoryImportBatches.cs)
   dispatches `CreateCategoryImportBatchCommand` and returns 201 with
   `{id, status}` (initially `processing`). The
   [validator](../../../src/Application/Features/Categories/Commands/CreateCategoryImportBatch/CreateCategoryImportBatchCommandValidator.cs)
   requires distinct, completed files owned by the caller, rejects duplicate
   ETags within a batch, and enforces configured MIME, file count/size,
   combined size, and encoded payload limits. Defaults are 10 files, 25 MiB
   each, 100 MiB total and 140 MiB estimated encoded payload; see
   [`ImportBatchOptions`](../../../src/Application/Common/Models/Options/ImportBatchOptions.cs)
   for MIME types and overridable limits.
5. The [create handler](../../../src/Application/Features/Categories/Commands/CreateCategoryImportBatch/CreateCategoryImportBatchCommandHandler.cs)
   persists `CategoryImportBatch`, ordered `CategoryImportBatchFile` links,
   a `Created` history entry and an `OutboxMessage` for
   `ProcessCategoryImportBatchCommand` in one save. Repeating the same
   `clientRequestId` for the same user and ordered files returns the existing
   batch; reusing it for different files returns 409
   (`category_import_batches.idempotency_conflict`). A unique filtered
   `(UploadedByUserId, ClientRequestId)` index backs this behavior.

## 2. Read files and extract suggestions in the Worker

[`OutboxPublisherService`](../../../src/Web/BackgroundJobs/OutboxPublisherService.cs)
polls the outbox, sends an envelope containing the original user and command
to the category-import Azure Queue, then marks it published. The
[`CategoryImportBatchQueueProcessingService`](../../../src/Worker/Queues/CategoryImportBatchQueueProcessingService.cs)
uses a 20-minute visibility timeout; the common
[`QueueMessageProcessor`](../../../src/Worker/Queues/QueueMessageProcessor.cs)
restores that user, checks `ProcessedMessages` for repeat delivery, dispatches
through Mediator, and records processed-message state. Transport is at-least-once;
failed deliveries can retry or go to a `-poison` queue.

The [processing handler](../../../src/Application/Features/Categories/Commands/ProcessCategoryImportBatch/ProcessCategoryImportBatchCommandHandler.cs)
loads the batch, its ordered files, their `FileMetadata`, and history. It
skips already terminal/pending-review batches and claims a concurrency-stamped
lease before external I/O (15 minutes by default). It downloads each blob in
file order and passes the streams, MIME types and file names to
[`OpenAiCategoryDocumentExtractionService`](../../../src/Infrastructure/AI/OpenAiCategoryDocumentExtractionService.cs).
The [Responses API client](../../../src/Infrastructure/AI/OpenAiDocumentExtractionClient.cs)
sends the files together as base64 `input_file` items with one extraction prompt
and a strict JSON schema. The
[schema factory](../../../src/Infrastructure/AI/Schemas/CategoryExtractionSchemaFactory.cs)
includes current category names and asks for distinct **new Romanian**
category suggestions. It returns `{ "categories": [{ "name": "..." }] }`.
On success, the handler saves `ExtractedDataJson`, changes status to
`PendingReview`, stamps `ProcessedAt`, and records `Completed` history. It
does **not** insert `Category` rows.

An unprocessable document marks the batch `Failed` with an error and history;
other failures release the lease and retry until the configured attempt limit
(default five), after which the batch is marked failed. The processing lease,
attempt count, status, extraction JSON and error are persisted on the
[`CategoryImportBatch`](../../../src/Domain/Entities/CategoryImportBatch.cs).

## 3. List, read and review an import

The page's import tab uses
[`CategoryImportState`](../../../src/Client/src/app/shared/categories/services/category-import.store.ts)
to call `POST /api/CategoryImportBatches/get-all`. The
[`list handler`](../../../src/Application/Features/Categories/Queries/GetAllCategoryImportBatches/GetAllCategoryImportBatchesHandler.cs)
returns keyset-paginated batch summaries (status, dates, uploader, error and
file metadata), with search across error text and file names/blob paths,
filtering and sorting. The UI has status/search filters and appends more pages.
Batch-created/processed/confirmed SignalR events reload this list
([event handlers](../../../src/Application/Features/Categories/EventHandlers/CategoryImportBatchCompletedEventHandler.cs)).

Clicking a batch opens the
[review modal](../../../src/Client/src/app/shared/categories/ui/modals/review/category-import-review-modal.ts).
[`CategoryImportReviewState`](../../../src/Client/src/app/shared/categories/services/category-import-review.store.ts)
calls `GET /api/CategoryImportBatches/{id}` every two seconds while status is
`processing`, then stops. The
[`detail handler`](../../../src/Application/Features/Categories/Queries/GetCategoryImportBatchById/GetCategoryImportBatchByIdHandler.cs)
returns status, attempts, error, ordered files, history and deduplicated,
trimmed suggestions read from `ExtractedDataJson`. It compares names to the
**current** `Categories` table and supplies `alreadyExists`/`matchedCategory`
for each suggestion; extraction output is not the final choice. The modal
shows summary and history tabs. Imported files can also be downloaded from the
batch list via `GET /api/Storage/{fileId}/download`, which returns a short-lived
read SAS ([download handler](../../../src/Application/Storage/Queries/GetFileDownload/GetFileDownloadQueryHandler.cs)).
Despite the route parameter's name, this download lookup uses `FileMetadata.Id`
(the `fileMetadataId` from the batch file), not `FileMetadata.FileId`.

**Visibility boundary:** the batch list query has no uploader predicate,
while the batch detail and confirmation handlers restrict access to
`UploadedByUserId == current user` (returning 404 otherwise). Storage's
download handler looks up completed metadata by ID; it does not check
category-batch ownership. These are descriptions of the present code, not
guarantees of per-user isolation for the list/download endpoints.

## 4. Confirm reviewed names and create categories

In the [review table](../../../src/Client/src/app/shared/categories/ui/modals/review/category-import-review-table.ts),
each suggestion must have a category selected. A match can be selected from
existing categories; **Create** on an unmatched row opens the ordinary
[category creation modal](../../../src/Client/src/app/shared/categories/ui/dropdown/category-dropdown.ts)
prefilled with the suggestion. That creates the category immediately through
`POST /api/Categories`, before batch confirmation.

The review store sends only the selected/entered **names**, not category IDs
or the original extraction JSON:
`POST /api/CategoryImportBatches/{id}/confirm` with
`{ "names": ["...", "..."] }`. The
[confirmation validator](../../../src/Application/Features/Categories/Commands/ConfirmCategoryImportBatch/ConfirmCategoryImportBatchCommandValidator.cs)
allows at most 200 nonblank names, each at most 100 characters. The
[handler](../../../src/Application/Features/Categories/Commands/ConfirmCategoryImportBatch/ConfirmCategoryImportBatchCommandHandler.cs)
requires the caller to own a `PendingReview` batch, trims and deduplicates
names case-insensitively, reuses matching current categories and creates
unmatched categories with no icon. It saves new `Categories`, changes the
batch to `Confirmed`, adds `Confirmed` history and stores
`ConfirmationResultJson` (category IDs/names and `created` flags) together.
The HTTP response is only `{batchId, status}`. A repeat confirmation returns
the stored result's status without creating more categories; a batch in any
other state returns 409 (`category_import_batches.not_in_review`).
The form's confirm button is disabled until suggestions exist and status is
`pendingReview`; the backend request validator does not require a nonempty
`names` list.

The confirm command invalidates category-list, import-list and coarse stock
cache tags; [`CategoryImportBatchConfirmedEventHandler`](../../../src/Application/Features/Categories/EventHandlers/CategoryImportBatchConfirmedEventHandler.cs)
also broadcasts a confirmation. The page reloads both lists when the review
modal closes. Unlike direct creation, import-created categories need no
individual create HTTP requests.

## Persistent records and status

| Store | Data written/read |
|-------|-------------------|
| SQL Server `Categories` | Names and optional owned JSON icon; direct create/update or reviewed import confirmation. [Category configuration](../../../src/Infrastructure/Data/Configurations/CategoryConfiguration.cs) |
| SQL Server `FileMetadata` and Azure Blob `app-files` | Upload status, path, type, size and ETag in SQL; original bytes in Blob. [Metadata](../../../src/Domain/Entities/FileMetadata.cs) |
| SQL Server `CategoryImportBatches` | Owner, request ID, `Processing` -> `PendingReview` -> `Confirmed` (or `Failed`), attempts/lease, extraction JSON, confirmation JSON, timestamps and errors. [Batch entity](../../../src/Domain/Entities/CategoryImportBatch.cs), [EF configuration](../../../src/Infrastructure/Data/Configurations/CategoryImportBatchConfiguration.cs) |
| SQL Server `CategoryImportBatchFiles`, `CategoryImportBatchHistory` | Ordered links to metadata; created/processing/completed/failed/confirmed timeline entries. [File configuration](../../../src/Infrastructure/Data/Configurations/CategoryImportBatchFileConfiguration.cs), [history mapping](../../../src/Infrastructure/Data/Configurations/CategoryImportBatchConfiguration.cs) |
| SQL Server `OutboxMessages` / `ProcessedMessages`, Azure Queue | Durable publication and at-least-once worker dispatch/deduplication; not category data. [Outbox publisher](../../../src/Web/BackgroundJobs/OutboxPublisherService.cs), [queue processor](../../../src/Worker/Queues/QueueMessageProcessor.cs) |

## Evidence

- `src/Client/src/app/shared/categories/services/category-import.store.ts`
- `src/Client/src/app/shared/categories/services/category-import-review.store.ts`
- `src/Web/Endpoints/CategoryImportBatches.cs`
- `src/Application/Features/Categories/Commands/CreateCategoryImportBatch/CreateCategoryImportBatchCommandHandler.cs`
- `src/Application/Features/Categories/Commands/ProcessCategoryImportBatch/ProcessCategoryImportBatchCommandHandler.cs`
- `src/Application/Features/Categories/Commands/ConfirmCategoryImportBatch/ConfirmCategoryImportBatchCommandHandler.cs`
- `src/Infrastructure/Data/Configurations/CategoryImportBatchConfiguration.cs`
