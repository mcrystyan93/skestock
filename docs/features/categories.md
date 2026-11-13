# Categories

Categories group items (`Item.CategoryId`, required). Staff create and edit them by hand, or bulk-import names from uploaded documents, which an AI extraction step turns into suggestions that a person reviews before anything is saved.

UI copy is Romanian. Everything below was read from source; where a behaviour is a gap or looks unintended it is listed under [Open points](#open-points), not fixed.

## At a glance

| Layer | Where |
|---|---|
| Domain | `src/Domain/Entities/Categories/` (`Category`, `CategoryIcon`, `CategoryImportBatch`, `CategoryImportBatchFile`), `Domain/Enums/CategoryImportBatchStatus.cs`, `Domain/Events/Categories/` |
| Application | `src/Application/Features/Categories/` (commands, queries, event handlers, models, filter/sort/cursor/cache config), errors in `Application/Common/Errors/CategoryErrors.cs` |
| Infrastructure | `Data/Configurations/Category*Configuration.cs`, `AI/OpenAiCategoryDocumentExtractionService.cs`, `AI/Schemas/CategoryExtractionSchemaFactory.cs` |
| Web | `src/Web/Endpoints/Categories.cs`, `CategoryImportBatches.cs` |
| Worker | `src/Worker/Queues/CategoryImportBatchQueueProcessingService.cs` |
| Client | `src/Client/src/app/features/categories` (page, tabs, tables), `shared/categories` (HTTP, stores, modals, dropdown), `core/models/category*.ts` |

## Backend

### Endpoints

All routes sit under `/api/<GroupName>` and are auto-discovered `IEndpointGroup`s. The group registration calls `RequireAuthorization()` (`src/Web/Infrastructure/WebApplicationExtensions.cs:28`).

| Method + route | Use case | Notes |
|---|---|---|
| `POST /api/Categories/get-all` | `GetAllCategoriesQuery` | Keyset-paginated, filter + sort + search. POST because filters are a body. |
| `GET /api/Categories/{id}` | `GetCategoryByIdQuery` | 404 `categories.not_found`. |
| `POST /api/Categories` | `CreateCategoryCommand` | 201 with `CategoryMutationDto`. |
| `PUT /api/Categories/{id}` | `UpdateCategoryCommand` | Replaces name and icon. |
| `POST /api/CategoryImportBatches` | `CreateCategoryImportBatchCommand` | Command carries `[Authorize]`. Idempotent on `ClientRequestId`. |
| `POST /api/CategoryImportBatches/get-all` | `GetAllCategoryImportBatchesQuery` | Paginated import list. |
| `GET /api/CategoryImportBatches/{id}` | `GetCategoryImportBatchByIdQuery` | Review payload (suggestions, files, history). |
| `POST /api/CategoryImportBatches/{id}/confirm` | `ConfirmCategoryImportBatchCommand` | Command carries `[Authorize]`. Body: `names`. |

There is no delete or disable. `Item.Category` is `DeleteBehavior.Restrict`, so categories in use could not be deleted anyway.

### Rules

- Name: required, trimmed on save, max 100 characters. The limit is duplicated in the validators (`NameMaxLength = 100`) and `DataSchemaConstants.DEFAULT_NAME_LENGTH` because Application cannot reference Infrastructure; keep them in sync.
- Name uniqueness is enforced **only in the validators** (`ValidationErrorCodes.DuplicateName`): trimmed, case-insensitive, and accent-insensitive on SQL Server via `TextSearchCollation.AccentInsensitive` (a plain `ToLower()` comparison is the fallback for non-SQL-Server providers, i.e. tests). Update excludes its own id.
- Icon is optional. It is a `CategoryIcon` value (`Name`, `FileName`, `Path`), stored as JSON in the `Icon` column. Create/Update replace it wholesale; sending `null` clears it.

### Listing, filtering and sorting

- Filterable fields (`CategoryFilterConfiguration`): `id`, `name`, `createdDate`, `lastModifiedDate`.
- Sortable keys (`CategorySortConfiguration`): `name`, `createdDate`, `lastModifiedDate`, `id`; each is tied to `Id` as the keyset tie-breaker. Default sort: `CreatedDate desc, Id desc`. The client overrides this with `name ascend`.
- `searchTerm` does an accent-insensitive `Contains` on `Name`.
- Page size must be between 1 and `PaginationConstants.DEFAULT_PAGE_SIZE`; the validator rejects anything else (the handler's clamp is only a backstop). The handler reads `pageSize + 1` rows to compute `hasNextPage` without a count query.
- The cursor encodes the sort it was issued for; a cursor reused with a different sort fails validation (`CursorSortMismatch`).
- `ItemCount` is `category.Items.Count`, projected in SQL. It is returned but **not sortable or filterable** on the server.

### Caching and invalidation

- `GetAllCategoriesQuery` is `ICacheableQuery`: tag `categories` (`CacheConstants.CategoryListTag`), sliding expiration of about 5 minutes with 30 s jitter, key built from normalized search/filters/page size/cursor/sort.
- `CreateCategoryCommand` and `UpdateCategoryCommand` invalidate `categories` through `ICacheInvalidation`.
- `CategoryUpdatedEventHandler` (raised by update) also removes the coarse Stock tag, because stock listings embed category data, and pushes `CategoryUpdated` to the `categories-list` SignalR group.
- `GetCategoryByIdQuery` is not cached.

### Import flow (AI-assisted)

```
Client uploads files ──► POST CategoryImportBatches {fileMetadataIds, clientRequestId}
                              │  one transaction: batch (Processing) + OutboxMessage
                              ▼
                  outbox publisher ──► CategoryImportQueue ──► Worker
                                                                 │ ProcessCategoryImportBatchCommand
                                                                 ▼
                       lease → download blobs → OpenAI extraction → PendingReview (or Failed)
                                                                 │ SignalR: CategoryImportBatchProcessed
                              ┌──────────────────────────────────┘
                              ▼
         GET batch (review: suggestions + which already exist) ──► person edits names
                              ▼
               POST …/{id}/confirm {names} ──► categories created if missing ──► Confirmed
```

Status (`CategoryImportBatchStatus`, persisted as int; append only): `Processing = 0`, `PendingReview = 1`, `Confirmed = 2`, `Failed = 3`.

- **Create** is idempotent per `(UploadedByUserId, ClientRequestId)`. A repeat with the same files returns the existing batch; with different files it returns 409 `category_import_batches.idempotency_conflict`. A race on the unique index is caught and resolved the same way. Limits (min 1 file, `ImportBatchOptions.MaxFiles`, no duplicate files) are in the validator.
- **Process** (Worker; delivery is at-least-once so it is built to be repeatable):
  - Batches already `PendingReview`, `Confirmed` or `Failed` return OK without work.
  - A lease (`ProcessingLeaseUntilUtc`, `ConcurrencyStamp` as concurrency token) is committed *before* the external I/O. A second worker hitting a live lease throws `ImportBatchProcessingInProgressException`, so the message is retried later.
  - `UnprocessableDocumentException` fails the batch immediately. Any other exception releases the lease and rethrows (queue retry) until `MaxProcessingAttempts`, then the batch is marked `Failed`.
  - The queue visibility timeout is 20 minutes because one message extracts every file.
- **Confirm** only accepts the uploader's own batch in `PendingReview`. The validator rejects blank names (400) and more than 200 names or any name over 100 characters; the handler then trims and de-duplicates case-insensitively. Existing categories (same accent-insensitive match) are reused; the rest are created **without an icon**. The outcome is stored in `ConfirmationResultJson`, and a repeated confirm returns the stored result. Losing a concurrency race returns the winner's stored result.
- Domain events on the batch (`Created`, `Completed`, `Failed`, `Confirmed`) each invalidate the relevant cache tags and notify `category-import-batches-list`. `Confirmed` also invalidates `categories` and the Stock coarse tag.

### Errors

`categories.not_found` (404), `category_import_batches.not_found` (404), `category_import_batches.idempotency_conflict` (409), `category_import_batches.not_in_review` (409). Validation failures use `ValidationErrorCodes`.

### Tests

Unit: `tests/Application.UnitTests/Features/Categories/` (create, create-import, process-import, confirm, import list and review queries), `Documents/OpenAiCategoryDocumentExtractionServiceTests.cs`, `Documents/Schemas/CategoryExtractionSchemaFactoryTests.cs`. The Worker queue base class is covered by `tests/Worker.UnitTests/Queues/QueueProcessingServiceTests.cs`.
Functional: `tests/Application.FunctionalTests/Features/Categories/` (create, update, get-all, get-by-id, create-import, confirm-import). Run them with `./run-functional-tests.sh`.
No functional test was found for the import list/review query endpoints or the process step, and none covers a concurrent duplicate-name create.

## SQL

All tables are SQL Server temporal tables with 2-year history retention (`SetTemporalRetention` migration; none of the category tables is in `TemporalExclusions`). The `Icon` JSON owned type has no table of its own.

### `Categories`

| Column | Type | Notes |
|---|---|---|
| `Id` | `uniqueidentifier` | PK |
| `Name` | `nvarchar(100)` | not null; **no unique index** |
| `Icon` | `nvarchar(max)` null | JSON: `{Name, FileName, Path}` (max 200/200/500, all required when present) |
| `CreatedDate`, `LastModifiedDate` | `datetimeoffset` | UTC |
| `CreatedById`, `LastModifiedById` | `uniqueidentifier` null | FK to `UserProfiles.IdentityId`, `Restrict` |

Indexes (named to match keyset sorts): `IX_Categories_CreatedDate_Id`, `IX_Categories_LastModifiedDate_Id`, `IX_Categories_Name_Id`.

### Import tables

- `CategoryImportBatches`: status (int), `ErrorMessage` (2000), `UploadedByUserId` (FK, `Restrict`), `ClientRequestId`, `UploadedAt`, `ProcessedAt`, `AttemptCount`, `ExtractedDataJson` and `ConfirmationResultJson` (`nvarchar(max)`), `ConcurrencyStamp` (concurrency token), `ProcessingLeaseUntilUtc`, audit columns. Indexes: unique `(UploadedByUserId, ClientRequestId) WHERE ClientRequestId IS NOT NULL`, `(CreatedDate, Id)`, `(UploadedAt, Id)`.
- `CategoryImportBatchFiles`: batch (cascade delete), `FileMetadataId` (`Restrict`), `SortOrder`. Unique `(CategoryImportBatchId, FileMetadataId)`.
- `CategoryImportBatchHistory`: owned collection of status events (`Status` stored as string, max 32; `Message` max 2000), index on `CreatedAtUtc`.

### Relationships

`Items.CategoryId → Categories.Id`, `Restrict`. Items have `IX_Items_CategoryId_CreatedDate_Id` for category-filtered listing. Statistics (`GetClassStockByCategory`) and stock listings also read categories.

### Migrations

`InitialCreate` (tables), `AddItemListingIndexesAndCollation` (Items-side index), `UseDateTimeOffsetForPersistedInstants`, `AddTemporalTables`, `SetTemporalRetention`. New schema changes: change the entity or configuration, then `dotnet ef migrations add` (see `CLAUDE.md` section 4); never edit applied migrations.

## Frontend

### Routing and page

The lazy route `categoriesRoutes` is registered in `core/layouts/full/full.routes.ts`. `CategoriesPage` provides `CategoryListState`, `CategoryImportState`, `NzModalService` and `FileStorageState`, and joins the SignalR groups `categories-list` and `category-import-batches-list` for its lifetime. Two tabs, **Categorii** and **Importuri categorii**; their labels are defined in the header component (the tab bar itself is hidden in the page template). The header has **Adaugă** and **Importă** actions.

Tables switch by breakpoint: a large table at `lg` and above, a card list below it. Lists load more by cursor (`loadMore`).

### State

| Store | File | Role |
|---|---|---|
| `withCategoryCollection()` | `shared/categories/services/category-collection.feature.ts` | Paged list, filter, load more. Reused by the page store and the dropdown store. |
| `CategoryListState` | `features/categories/services/category-list.store.ts` | Page store; reloads on `categoryImportBatchConfirmed`. |
| `CategoryDetailState` | `shared/categories/services/category-detail.store.ts` | Load/create/update one category; `saveCategory` picks create or update by presence of `id`. |
| `CategoryDropdownStore` | `shared/categories/services/category-dropdown.store.ts` | Backs the reusable `CategoryDropdown`. |
| `CategoryImportState` | `shared/categories/services/category-import.store.ts` | Create batch, list batches. |
| `CategoryImportReviewState` | `shared/categories/services/category-import-review.store.ts` | Polls the batch every 2 s while `processing`, holds editable suggestion lines, confirms. |

HTTP clients: `CategoriesHttp` (`/api/Categories`) and `CategoryImportsHttp` (`/api/CategoryImportBatches`). Types in `core/models/category.ts` and `category-import.ts` mirror the backend DTOs.

### UI pieces

- `CategoryDetailModal` + `Form`: signal form, name required with max 100 (messages in Romanian), icon via `IconPicker`.
- `CategoryImportModal` uploads files then creates the batch; `CategoryImportReviewModal` shows suggestions, which already exist, source files and history. Each line's final name is `line.category?.name?.trim() ?? line.name.trim()` (empty ones are filtered out client-side); confirm sends the names only.
- `CategoryDropdown` is reused by the item form and filter, stock filter, stock batch form, the class statistics cards, and each line of the import review table.

## Open points

Not fixed here; each needs a decision.

1. **Authorization on manual writes.** `CreateCategoryCommand` and `UpdateCategoryCommand` have no `[Authorize]` attribute, while the import commands do. `CLAUDE.md` section 10 says commands writing global/shared data should carry `[Authorize(Roles = Roles.Administrator)]`. Categories are global.
2. **Uniqueness is not a database constraint.** Validators check names before insert, so two concurrent creates (or a create racing an import confirm) can produce duplicates. Adding a unique index would need a migration and a duplicate check on existing data first.
3. **`CategoryCreated` is never raised.** The event name exists in `RealtimeEvents.cs` and the client's `realtime-event-names.ts`, but nothing publishes it. Create does not notify other open sessions; Create also does not use a domain event.
4. **Leftover debug in the client.** `category-collection.feature.ts` handles `categoryUpdated` with a no-op reducer that only calls `console.log`, so other sessions are not refreshed by that event.
5. **Table columns the server cannot sort.** The client column set includes `itemCount`, `createdByName` and `lastModifiedByName`; the server accepts only `name`, `createdDate`, `lastModifiedDate`, `id`. `table.html` has no sort attributes, so this may be moot; the sort wiring was not traced.
6. **Imported categories have no icon**, and the confirm step cannot set one.
7. **Test gaps** listed under Backend > Tests.
