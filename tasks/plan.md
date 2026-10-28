# Plan: Item images (thumbnail generated in the Worker)

Source: `SPEC.md` (approved) + opus planner review of the outbox → queue → Worker path.

## Existing path (facts)
- Handlers enqueue by adding an `OutboxMessage { Type = typeof(Cmd).AssemblyQualifiedName, Payload = JSON, QueueName = Services.X, UserId }` in the same `SaveChanges` (reference: `CreateItemImportBatchCommandHandler`). `OutboxPublisherService` and `AzureQueueSender` are generic (queues auto-created).
- Worker: `QueueMessageProcessor` dedupes via `ProcessedMessage`, runs the Mediator request in a DB transaction; a failed `Result` = permanent → poison queue. Each queue is a ~20-line `QueueProcessingService<T>` subclass registered in `src/Worker/Program.cs`. Worker already has Infrastructure services (DbContext, blob storage, SkiaSharp).
- AppHost registers `storage.AddQueues(...)` only; no per-queue change.
- Stock cache: `ItemCreated/UpdatedEventHandler` already clear the coarse stock tag, so a new `ImageFileId` in `StockItemDto` shows after create/edit. Matches your "stock unaffected" decision at no cost.

## Design decisions
1. **New queue `item-image-thumbnail`** (2 min visibility). `item-import` has a 20 min timeout and slow AI batches; thumbnails would wait behind them.
2. **Message** `GenerateItemThumbnailCommand(ItemId, ImageFileId)`; no validator (stale = no-op, not poison).
3. **Fixed thumbnail path** `thumbnails/{ImageFileId}.webp` in the original's container (`ItemImagePaths.Thumbnail`). Worker never writes to the DB → idempotent; a stale job can only leave an orphan blob. "Ready" = blob exists. *(D1)*
4. **Worker steps:** load item (no-tracking); if missing or `ImageFileId` ≠ message → `Ok` no-op; load `FileMetadata` (missing → `Ok`); `TryDownloadAsync` original (null → `Ok`); decode failure → `Result.Fail(ItemErrors.InvalidImage)` (poison); upload WebP; `Ok`.
5. **`thumb` endpoint falls back to the original** until the thumbnail exists. Real thumb/full: `Cache-Control: private, max-age=31536000, immutable`; fallback: `no-cache` (else browser pins the full image under `?v=`).
6. **Old blobs deleted in the Web Edit handler**, after the commit, best effort (try/catch + log); old `FileMetadata.Status = Deleted` in the same save.
7. **Server validation** in Create/Edit validators via static `ItemImageRules` (`CustomAsync`): file exists + `Completed`, owned by current user, content-type allowlist, ≤ 5 MB, not used by another item, then (only if the above pass) decode check via `IImageThumbnailService.CanDecode`. → 400 through `ValidationException`. *(D4)*
8. **Unique filtered index** on `Items.ImageFileId`. *(D2)*
9. No new `app-files` literal (container comes from `FileMetadata.BlobContainer`).

## Deviations from SPEC.md — need your OK
- **D1** Only `Item.ImageFileId` column (no `ImageBlobPath`/`ThumbnailBlobPath`); original path from `FileMetadata`, thumbnail at the fixed path. Costs one extra blob lookup on `thumb` requests (immutable caching keeps it low).
- **D2** Unique filtered index on `ImageFileId` + "not used by another item" rule (spec said no index). Prevents one item's replace from deleting another item's shared image.
- **D3** New queue instead of reusing `item-import`.
- **D4** Image errors are `ValidationErrorCodes.InvalidImage` (400); `ItemErrors.InvalidImage` only for the Worker failure.

## Phases
0. **Setup:** branch `feature/item-images`.
1. **Foundations:** schema (T1), blob `UploadAsync`/`TryDownloadAsync` (T2), thumbnail service (T3). → Checkpoint A
2. **Worker job:** command + handler (T4), queue wiring (T5). → Checkpoint B
3. **Write path:** validation (T6), DTO + Create (T7), Edit set/replace/remove (T8), stock DTO (T9a). → Checkpoint C
4. **Read endpoint:** `GET /api/Items/{id}/image` (T9), functional tests (T10). → Checkpoint D
5. **Frontend:** models + `ske-item-thumbnail` (T11), show in 4 places (T12), upload component (T13), form + modal (T14).
6. **Verify & PR:** full build/tests, manual smoke via AppHost, `graphify update .`, haiku review, PR.

Backend tasks are test-first (red → green). Implementation by sonnet, exploration by haiku, per your instruction. Task details and acceptance criteria: `tasks/todo.md`.

## Risks
- Wrong `Cache-Control` on fallback → thumbnails never appear (explicit tests in T9/T10).
- Blob can be swapped within the 10 min SAS window after confirm; size/decode checks see it once. Non-image swapped in → Worker poison, endpoint serves the original. Low risk (authenticated staff); `X-Content-Type-Options: nosniff` on the image response closes it (user approved adding nosniff on the image response).
- A user could point an item at one of their own image import-source files; replacing would delete that blob. Minor.
- Orphan blobs (cancelled uploads, stale jobs). Accepted.
- Functional tests run the real Worker against Azurite while Web uses the fake blob store; the Worker finds no blob and returns `Ok`. Expected.
- Migration reaches production on deploy; review the SQL before merge.
- `?size=thumb` enum binding case-insensitivity unverified (covered by T10).
