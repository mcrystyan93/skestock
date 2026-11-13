# Todo: Item default location + description to supply-line notes

Plan: `tasks/item-default-location-plan.md`. Spec: `tasks/item-default-location-spec.md`. Model for every implementation task: **Sonnet**.

- [ ] Task T0: Create branch `feature/item-default-location` from up-to-date `master` (Sonnet)
  - Acceptance: on the branch; untracked spec/plan/todo carried over.
  - Verify: `git status` shows the branch.
  - Files: none

- [x] Task T1: Domain + EF config + migration with backfill (Sonnet)
  - Acceptance: `Item.LocationId` (`Guid?`) + `Location?` nav; FK `Restrict`, optional, `WithMany()`; migration `AddItemDefaultLocation` generated with `dotnet ef migrations add` (never hand-edit Designer/snapshot), backfill SQL from the plan appended at the end of `Up` via `migrationBuilder.Sql` (sets `LastModifiedById = NULL`, `HAVING SUM > 0`). If the generated migration toggles `SYSTEM_VERSIONING` on `Items`, re-append the 2-year retention line.
  - Verify: `dotnet build`; `has-pending-model-changes` clean; `aspire start --apphost src/AppHost/AppHost.csproj`, `aspire wait webapi`, then on the dev DB this returns 0 rows: `SELECT i.Id FROM Items i OUTER APPLY (SELECT TOP 1 b.LocationId FROM StockBatches b WHERE b.ItemId = i.Id GROUP BY b.LocationId HAVING SUM(b.Quantity) > 0 ORDER BY SUM(b.Quantity) DESC, b.LocationId) x WHERE EXISTS (SELECT i.LocationId EXCEPT SELECT x.LocationId)`; `aspire stop`.
  - Files: `src/Domain/Entities/Items/Item.cs`, `src/Infrastructure/Data/Configurations/ItemConfiguration.cs`, `src/Infrastructure/Data/Migrations/<ts>_AddItemDefaultLocation.cs` (+ generated Designer, snapshot)

- [x] Task T2: Return LocationId from item reads (Sonnet)
  - Acceptance: `ItemDto.LocationId` (`Guid?`) filled by `ItemDtoMapper`, `GetAllItemsHandler`, `GetItemByIdHandler`.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Items.Queries"` with an assertion added in `GetItemByIdHandlerTests`.
  - Files: `src/Application/Features/Items/Models/ItemDto.cs`, `.../Models/ItemDtoMapper.cs`, `.../Queries/GetAllItems/GetAllItemsHandler.cs`, `.../Queries/GetItemById/GetItemByIdHandler.cs`, `tests/Application.UnitTests/Features/Items/Queries/GetItemById/GetItemByIdHandlerTests.cs`

- [x] Task T3: CreateItem accepts LocationId (Sonnet)
  - Acceptance: `CreateItemRequest.LocationId` -> `CreateItemCommand.LocationId` (mapped in `Items.cs`) -> persisted by handler; validator: when set, location must exist (`InvalidReference`), null allowed.
  - Verify: unit tests for valid/null/unknown location (validator) and persisted value (handler) pass: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~CreateItem"`.
  - Files: `src/Application/Features/Items/Models/ItemRequests.cs`, `.../Commands/CreateItem/{CreateItemCommand,CreateItemCommandHandler,CreateItemCommandValidator}.cs`, `src/Web/Endpoints/Items.cs`; tests `tests/Application.UnitTests/Features/Items/Commands/CreateItem/{CreateItemCommandValidatorTests,CreateItemCommandHandlerTests}.cs`

- [x] Task T4: EditItem accepts LocationId (Sonnet)
  - Acceptance: same as T3 for edit; setting null clears the location. Cache tags unchanged.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~EditItem"`.
  - Files: `src/Application/Features/Items/Models/ItemRequests.cs`, `.../Commands/EditItem/{EditItemCommand,EditItemCommandHandler,EditItemCommandValidator}.cs`, `src/Web/Endpoints/Items.cs`; tests `.../Commands/EditItem/{EditItemCommandValidatorTests,EditItemCommandHandlerTests}.cs`

- [x] Task T5: Goods-receipt review match carries LocationId (Sonnet)
  - Acceptance: `GoodsReceiptImportReviewMatchDto.LocationId` set in `GetGoodsReceiptImportByIdHandler` select. Process/Confirm untouched.
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~GetGoodsReceiptImportById"` with a LocationId assertion.
  - Files: `src/Application/Features/GoodsReceipts/Models/GoodsReceiptImportReviewDto.cs`, `src/Application/Features/GoodsReceipts/Queries/GetGoodsReceiptImportById/GetGoodsReceiptImportByIdHandler.cs`, `tests/Application.UnitTests/Features/GoodsReceipts/Queries/GetGoodsReceiptImportById/GetGoodsReceiptImportByIdHandlerTests.cs`

- [x] Task T6: Functional round-trip (Sonnet)
  - Acceptance: create with location -> get by id returns it; edit to another location and to null round-trips; unknown location -> 400.
  - Verify: AppHost running, `./run-functional-tests.sh --filter "FullyQualifiedName~Items"`.
  - Files: `tests/Application.FunctionalTests/Features/Items/Commands/CreateItem/CreateItemCommandTests.cs`, `.../Commands/EditItem/EditItemCommandTests.cs`

- [x] Task T7: Client models (Sonnet)
  - Acceptance: `ItemDto.locationId?: string | null`, `CreateItemRequest`/`EditItemRequest.locationId?: string | null`, `GoodsReceiptImportReviewMatchDto.locationId?: string | null`.
  - Verify: `cd src/Client && npm run build`.
  - Files: `src/Client/src/app/core/models/item.ts`, `src/Client/src/app/core/models/goods-receipt.ts`

- [x] Task T8: Item form location field (Sonnet)
  - Acceptance: "Locație implicită" `ske-location-dropdown` (`allowClear`, no `prePopulateWithDefault`) bound to `itemForm.location`; loads `{ id: item.locationId }`; save sends `locationId` (null when cleared). Check `nz-form-*` usage against `.github/instructions/llms-full.txt`.
  - Verify: `npm test -- --watch=false` with new cases in `form.spec.ts` (renders existing location, cleared -> null).
  - Files: `src/Client/src/app/shared/items/ui/modals/detail/{form.ts,form.html,item-detail-modal.ts,form.spec.ts}`

- [x] Task T9: Add-stock prefill (Sonnet)
  - Acceptance: picking an item with `locationId` sets the form location when the caller passed no location; caller location wins; item without location leaves the field as is.
  - Verify: new colocated `form.spec.ts` (3 cases) passes.
  - Files: `src/Client/src/app/shared/stock-batches/ui/modals/form.ts`, `src/Client/src/app/shared/stock-batches/ui/modals/form.spec.ts` (new)

- [x] Task T10: Goods-receipt review-line prefill (Sonnet)
  - Acceptance: `buildEditableLine` copies `matchedItem.locationId` into `item`; both line components set `location` to `{ id: item.locationId }` on item change when present; otherwise the dropdown default applies as today.
  - Verify: cases in `review-line-small.spec.ts` and new `review-line.spec.ts` (item with location -> prefilled; without -> unchanged).
  - Files: `src/Client/src/app/shared/goods-receipt-imports/services/review.store.ts`, `.../ui/review-lines-table/review-line/review-line.ts`, `.../ui/review-lines-table/small/review-line-small.ts`, `.../small/review-line-small.spec.ts`, `.../review-line/review-line.spec.ts` (new)

- [x] Task T11: Supply-list description -> notes (Sonnet)
  - Acceptance: `addItem` builds the line with `notes: item.description ?? ''` (as-is, no trim/truncate); duplicate add leaves existing line/notes untouched.
  - Verify: extend "adds an item..." case in `supply-list-detail-form.spec.ts` (description copied; item without description -> ''; duplicate keeps edited notes).
  - Files: `src/Client/src/app/shared/supply-lists/ui/modals/detail/form/{supply-list-detail-form.ts,supply-list-detail-form.spec.ts}`

- [ ] Task T12: Final build and tests (Sonnet)
  - Acceptance: all green; failures reported.
  - Verify: `dotnet build`; `dotnet test tests/Application.UnitTests`; `dotnet test tests/Worker.UnitTests`; `dotnet test tests/Infrastructure.IntegrationTests` (TemporalSchemaTests); `./run-functional-tests.sh --filter "FullyQualifiedName~Items"`; `cd src/Client && npm run build && npm test -- --watch=false`.
  - Files: none

- [ ] Task T13: `graphify update .` from repo root (Sonnet)
  - Acceptance: graph refreshed. Verify: command exits 0. Files: `graphify-out/`

- [ ] Task T14: Independent review (haiku sub-agent) (Sonnet orchestrates)
  - Acceptance: haiku gets `git diff master...` + the spec objective only; real findings fixed, rest reported.
  - Verify: rerun T12 checks touched by fixes.
  - Files: depends on findings

- [ ] Task T15: Commit and open PR (Sonnet)
  - Acceptance: commits on `feature/item-default-location`, PR to `master` describing the migration/backfill (reviewer must check it: runs on deploy) and test results.
  - Verify: `gh pr view` shows the PR.
  - Files: none
