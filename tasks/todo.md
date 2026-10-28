# Tasks: Item images

Backend tasks: write the failing tests first. Paths relative to repo root.

## Phase 0
- [ ] T0 Branch `feature/item-images` from up-to-date `master` (commit SPEC.md/tasks on it).

## Phase 1: Foundations
- [ ] T1 Schema
  - Acceptance: `Item.ImageFileId` (Guid?); unique filtered index (`IS NOT NULL`); migration `AddItemImage` adds one nullable column + index only; no hand edits.
  - Verify: `dotnet build && dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext`
  - Files: `src/Domain/Entities/Items/Item.cs`, `src/Infrastructure/Data/Configurations/ItemConfiguration.cs`, generated migration + snapshot
- [ ] T2 Blob `UploadAsync` + `TryDownloadAsync`
  - Tests first: upload→TryDownload same bytes + content type; missing → null; re-upload overwrites.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Storage"`
  - Files: `IBlobStorageService.cs`, `Storage/DTOs/UploadDto.cs`, `AzureBlobStorageService.cs`, `tests/.../FakeBlobStorageService.cs` (in-memory store), `AzureBlobStorageServiceTests.cs`
- [ ] T3 `IImageThumbnailService` (SkiaSharp)
  - Tests first: 600×300 PNG → 256×128 WebP; small image not enlarged; JPEG/WebP/GIF decode, GIF first frame; garbage → `CanDecode` false, `Create` throws.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Thumbnail"`
  - Files: `Application/Common/Interfaces/IImageThumbnailService.cs`, `Infrastructure/Images/SkiaImageThumbnailService.cs`, `Infrastructure/DependencyInjection.cs`, tests
- [ ] **Checkpoint A:** `dotnet build` + T2/T3 green.

## Phase 2: Worker job
- [ ] T4 `GenerateItemThumbnailCommand` + handler + `ItemImagePaths` + `ItemErrors.InvalidImage/ImageNotFound`
  - Tests first: happy path uploads `thumbnails/{fileId}.webp` image/webp; stale ImageFileId → Ok, no I/O; item missing → Ok; original missing → Ok, no upload; corrupt → `InvalidImage`; duplicate delivery → Ok twice, same path.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~GenerateItemThumbnail"`
- [ ] T5 Queue wiring
  - Acceptance: `Services.ItemImageThumbnailQueue = "item-image-thumbnail"`; processing service (2 min); registered in `Worker/Program.cs`; harness entry.
  - Tests first: reads from queue; Duplicate → Delete; permanent failure → `-poison`.
  - Verify: `dotnet test tests/Worker.UnitTests`
- [ ] **Checkpoint B:** Worker builds; Worker + Application unit tests green.

## Phase 3: Write path
- [ ] T6 Validation (`ItemImageRules`, `ValidationErrorCodes.InvalidImage`, command `ImageFileId` property)
  - Tests first: null ok; missing/pending/other owner/>5 MB/pdf/used by other item each fail with their code; Edit with own current file ok; undecodable → InvalidImage; no download if DB check already failed.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Items"`
- [ ] T7 `ItemDto.ImageFileId` + Create (request, endpoint, handler)
  - Tests first: image → item set + 1 outbox row (queue, payload ItemId+FileId); no image → no outbox; DTO has field.
- [ ] T8 Edit set/replace/remove
  - Tests first: same id → no outbox/deletes; null→file → outbox; A→B → outbox B, A metadata Deleted, delete A original + thumb after save; A→null cleared+deleted; `DeleteAsync` throws → still Ok.
- [ ] T9a `StockItemDto.ImageFileId` (+ handler select, test)
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Stock"`
- [ ] **Checkpoint C:** all unit tests + build green.

## Phase 4: Read endpoint
- [ ] T9 `GET /api/Items/{id}/image?size=thumb|full`
  - Tests first: item missing → ItemNotFound; no image/metadata missing or Deleted → ImageNotFound; full → original + content type; thumb exists → image/webp, not fallback; thumb missing → original, `IsFallback`.
  - Acceptance: Cache-Control immutable for real, `no-cache` for fallback; `X-Content-Type-Options: nosniff` on the image response (user-approved).
- [ ] T10 Functional tests (`ItemImageEndpointTests`)
  - Cases: edit with imageFileId → DTO has it; full bytes + immutable; thumb fallback + no-cache; garbage → 400 InvalidImage; no image → 404; stock query returns imageFileId.
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~Items"`
- [ ] **Checkpoint D:** backend complete.

## Phase 5: Frontend (Vitest specs alongside)
- [ ] T11 `imageFileId` in `core/models/item.ts` + `stock.ts`; `ske-item-thumbnail` (img with `?v=` or placeholder icon) + spec + barrel.
- [ ] T12 Thumbnail in `items/.../large/table.html`, `small/item-list-small.html`, `shared/stock/ui/table/stock-category-item-row.html`, `stock-category-items-small.html`. Verify `npm run build`; existing stock specs green.
- [ ] T13 `ske-item-image-upload`: `nz-upload` picture-card, rejects wrong type ("Format neacceptat") and >5 MB ("Imaginea depășește 5 MB"), own `FileStorageState` provider, does not use the global `uploadSuccess` event, remove sets null, preview. Spec.
- [ ] T14 Form field `imageFileId` ("Imagine articol") in `form.ts/html`; `mapSaveRequest` passes it; mapping spec.
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false`; prettier on touched files.

## Phase 6: Verify & PR
- [ ] Full: `dotnet build`, Application + Worker unit tests, functional tests, `has-pending-model-changes`, `npm run build`, `npm test`.
- [ ] Manual smoke via AppHost (upload → fallback → WebP thumb; queue drains, no poison; replace/remove deletes blobs).
- [ ] `graphify update .`
- [ ] Haiku review of diff (goal + diff only); fix real issues.
- [ ] Commit, push, `gh pr create` (flag migration for review).
