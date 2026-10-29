# Spec: Item images

## Objective
Staff can attach one image to an item. The image shows as a thumbnail in the items table (desktop) and list (mobile), and in the stock page rows/cards. Create/Edit item form gets an `nz-upload` area (picture-card) to add, replace, remove the image.

Success: upload an image in the item modal → thumbnail appears in items list and stock page; replace/remove works; invalid files are rejected on client and server.

## Decisions (confirmed by user)
1. **Upload path:** reuse existing SAS flow (`request-upload` → PUT to blob → `confirm-upload`). Create/Edit item request carries `imageFileId` (a confirmed `FileMetadata.FileId`). No multipart.
2. **Thumbnail:** generated server-side (SkiaSharp, already a dependency) and stored as a second blob.
3. **Serving:** `GET /api/Items/{id}/image?size=thumb|full` streams the blob with cache headers. DTOs carry only `imageFileId` (doubles as cache-bust version).
4. **Rules:** 1 image per item, ≤ 5 MB, `image/jpeg|png|webp|gif`.

## Assumptions (correct me or I proceed)
- **Thumbnail generated in the Worker** (user decision): Create/Edit handler sets `ImageFileId` and raises an outbox event → queue → Worker downloads original, resizes, uploads thumbnail, sets `ThumbnailBlobPath`. Consumer must be idempotent (at-least-once). Until the thumbnail exists, the `thumb` endpoint falls back to the original image. Output: WebP, max edge 256 px; GIF → first frame.
- **Stock cache not invalidated by image changes** (user decision): stock query keeps its normal expiry; thumbnails appear when it expires. No stock tag changes.
- Server validates by decoding with SkiaSharp (not trusting client content type); undecodable → validation error.
- Replacing/removing an image deletes the old original + thumbnail blobs (best effort; leftover blobs are not a correctness issue).
- Item import flows (CSV/AI) are untouched; imported items have no image.
- Item autocomplete/dropdown and goods-receipt/batch tables are out of scope (no thumbnail there).
- Disabled items keep their image.
- Endpoint auth = same cookie auth as other endpoints; `<img src="/api/Items/{id}/image…">` works same-origin via the `/api` proxy.

## Tech stack
.NET 10 minimal APIs, Mediator, FluentValidation, FluentResults, EF Core 10/SQL Server, Azure Blob (Azurite), SkiaSharp; Angular 22 signals + Signal Forms, NG-ZORRO 22 `nz-upload`, `@ngrx/signals`, Vitest.

## Commands
```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Items"
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Storage"
./run-functional-tests.sh --filter "FullyQualifiedName~Items"
dotnet ef migrations add AddItemImage --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext
cd src/Client && npm run build && npm test -- --watch=false
cd src/Client && npx prettier --write <files>
```

## Project structure (touch points)
Backend
- `src/Domain/Entities/Items/Item.cs` — add `Guid? ImageFileId`, `string? ImageBlobPath`, `string? ThumbnailBlobPath` (container is the existing `app-files`).
- `src/Infrastructure/Data/Configurations/ItemConfiguration.cs` + new migration (nullable columns, backward safe; optional FK/index on `ImageFileId` not needed).
- `src/Application/Common/Interfaces/IImageThumbnailService.cs` (new) + `src/Infrastructure/...` SkiaSharp impl, registered in `Infrastructure/DependencyInjection.cs`.
- `IBlobStorageService`: add `UploadAsync(container, path, stream, contentType, ct)` (needed to write the thumbnail); impl in `AzureBlobStorageService`; update `FakeBlobStorageService` in functional tests.
- `Features/Items`: `CreateItemCommand`/`EditItemCommand`(+requests, validators, handlers) gain `ImageFileId`; `ItemDto` + `ItemDtoMapper` gain `ImageFileId`; new query `GetItemImage` (returns stream + content type; cacheable? no — streamed, so not an `ICacheableQuery`).
- `src/Web/Endpoints/Items.cs` — `GET {id}/image`.
- Stock: `StockItemDto` gains `ImageFileId`; `GetClassLocationStockHandler` already loads items — add the field.
- Shared constant for container name `app-files` is currently hard-coded in 4 places; reuse, do not add a 5th literal (move to `skestock.Shared.Services` only if trivially in scope — otherwise ask).

Frontend (`src/Client/src/app`)
- `core/models/item.ts`, `core/models/stock.ts` — add `imageFileId?`; `CreateItemRequest/EditItemRequest` add `imageFileId?: string | null`.
- `shared/items/ui/modals/detail/form.ts|html` — new `imageFileId` form field + picture-card `nz-upload` (new small component, e.g. `shared/items/ui/image-upload`), reusing `FileStorageState.uploadFiles` / `StorageHttp`.
- `shared/items/ui/item-thumbnail` (new, `ske-item-thumbnail`: `itemId`, `imageFileId`, `size`; shows `<img loading="lazy">` or placeholder icon) used in:
  - `features/items/list/tables/regular/large/table.html` (new narrow first column or inside Articol cell)
  - `features/items/list/tables/regular/small/item-list-small.html`
  - `shared/stock/ui/table/stock-category-item-row.html` and `stock-category-items-small.html`
- `shared/items/ui/modals/detail/item-detail-modal.ts` `mapSaveRequest` passes `imageFileId`.
- Export new pieces via `@ske/...` barrels.

## Code style
Follow CLAUDE.md §6: file-scoped namespaces, `Guard.Against`, `Result`/typed errors in `Application/Common/Errors` (add `ItemErrors.InvalidImage`), UTC `DateTimeOffset`; TS strict, `inject()`, `input()`, signals, native control flow, Romanian UI copy ("Imagine articol", "Încarcă imagine", "Format neacceptat", "Imaginea depășește 5 MB").

## Testing strategy
Backend is TDD (red → green per slice), NUnit + Shouldly + Moq, mirroring `tests/Application.UnitTests/Features/Items/**`:
- Validators: `ImageFileId` must reference a completed `FileMetadata`, size ≤ 5 MB, content type allowlist; null allowed.
- Handlers: create/edit with image → thumbnail generated + paths set; replace → old blobs deleted; null on edit → image removed; undecodable image → failure result; duplicate/idempotent edit with same fileId → no regeneration.
- `GetItemImage` handler: thumb/full, not-found item, item without image → typed not-found error.
- Thumbnail service: resizes ≥256 px down, leaves smaller as-is, GIF/PNG/JPEG/WebP decode, corrupt bytes throw/fail.
- `AzureBlobStorageServiceTests` for `UploadAsync`.
- Functional test: edit item with image → `GET /image` returns bytes (via `FakeBlobStorageService`); stock query returns `imageFileId`.
- Frontend (Vitest, colocated): image-upload component (rejects wrong type / >5 MB, emits fileId, remove clears), item-thumbnail (placeholder vs img URL with `?v=`), form model mapping.

## Boundaries
- Always: validate on server (not only client); keep Mediator pipeline order; commands keep `ICacheInvalidation` (item tag + list tag); run build + tests; `graphify update .` after code changes; feature branch + PR.
- Ask first: moving the `app-files` literal to `Services`; adding any NuGet/npm dependency; changing the stock cache tags; request size limits.
- Never: edit applied migrations or the snapshot by hand; serve blobs with long-lived SAS in DTOs; commit secrets; reorder persisted enums.

## Success criteria
1. Create and Edit item accept `imageFileId`; invalid (missing/unconfirmed/too big/wrong type/undecodable) → 400 with typed error.
2. `GET /api/Items/{id}/image?size=thumb` returns WebP ≤256 px; `size=full` returns original; 404 when no image; responses carry `Cache-Control` and work with `?v=<imageFileId>` busting.
3. Replace/remove updates item and deletes old blobs.
4. Item list (desktop + mobile) and stock page (desktop + mobile) show the thumbnail or a placeholder.
5. Item modal shows picture-card upload with preview, client-side type/size checks, remove button.
6. `dotnet build`, backend unit + functional tests, `npm run build`, `npm test` all pass; migration `has-pending-model-changes` clean.

## Open questions / risks
- **Stock cache:** the stock query is cached; editing an item's image must invalidate it, or thumbnails lag until expiry. Need to check current stock tags vs `ItemTag` (Ask-first item above).
- **Antiforgery** is currently off; the image GET is safe either way, but the SAS `confirm-upload` POST depends on it later (already true today).
- **Orphaned uploads:** user uploads an image in the modal then cancels → `FileMetadata` + blob remain unreferenced. Accept for now (same as existing imports) unless you want cleanup.
- GIF animation is dropped in thumbnails; `size=full` keeps the original.
