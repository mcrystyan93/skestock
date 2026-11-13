# Plan: Item default location + description to supply-line notes

Spec: `tasks/item-default-location-spec.md` (approved). Decisions: merge keeps target's location (MergeItems untouched); Description copied to Notes as-is (both 500 chars, no truncation); location optional; copy is UI-only; backfill = location with highest total remaining batch quantity.

Engineering level: production grade for the migration (runs automatically on deploy, temporal tables), lean for the UI prefills.

## Components (dependency order)

1. **Domain + EF + migration** (blocks everything)
   - `src/Domain/Entities/Items/Item.cs`: `Guid? LocationId`, `Location? Location`.
   - `src/Infrastructure/Data/Configurations/ItemConfiguration.cs`: `HasOne(i => i.Location).WithMany().HasForeignKey(i => i.LocationId).IsRequired(false).OnDelete(DeleteBehavior.Restrict)`. No `Location.Items` collection (YAGNI). EF creates `IX_Items_LocationId` for the FK.
   - Migration `AddItemDefaultLocation` (generated) + appended `migrationBuilder.Sql` backfill (verified names: table `[Items]`, `[StockBatches]` (DbSet name, no `ToTable`), `StockBatches.Quantity` = remaining int quantity, `StockBatches.LocationId` non-null):
     ```sql
     UPDATE i SET i.[LocationId] = x.[LocationId], i.[LastModifiedById] = NULL
     FROM [Items] i
     CROSS APPLY (
         SELECT TOP 1 b.[LocationId] FROM [StockBatches] b
         WHERE b.[ItemId] = i.[Id]
         GROUP BY b.[LocationId]
         HAVING SUM(b.[Quantity]) > 0
         ORDER BY SUM(b.[Quantity]) DESC, b.[LocationId]
     ) x;
     ```
     `HAVING > 0` keeps items whose batches are all consumed at NULL ("items without stock stay NULL"). Ties break on `LocationId` so the result is deterministic.
2. **Item API** (after 1): `ItemDto.LocationId`; mapping in `ItemDtoMapper`, `GetAllItemsHandler`, `GetItemByIdHandler`; `ItemRequests.Create/EditItemRequest.LocationId`; `CreateItemCommand`/`EditItemCommand` + handlers + validators (location must exist when provided, `ValidationErrorCodes.InvalidReference`, same shape as `CategoryExistsAsync`); `src/Web/Endpoints/Items.cs` maps the request field into both commands (lines ~91 and ~114).
3. **Goods-receipt review match DTO** (after 1, parallel with 2): `GoodsReceiptImportReviewMatchDto.LocationId` + select in `GetGoodsReceiptImportByIdHandler`. Query-only; Process/Confirm untouched (as spec requires).
4. **Client model** (after 2/3 contracts are fixed): `core/models/item.ts` (`ItemDto.locationId`, `CreateItemRequest`/`EditItemRequest.locationId?`), `core/models/goods-receipt.ts` (`GoodsReceiptImportReviewMatchDto.locationId?`).
5. **Client features** (after 4, mutually parallel):
   - Item form: `shared/items/ui/modals/detail/form.{ts,html}` new `location: LocationDropdownValue` field (`item.locationId ? { id } : null`), `<ske-location-dropdown [formField]="itemForm.location" [allowClear]="true" />` labelled "Locație implicită"; `item-detail-modal.ts` `mapSaveRequest` sends `locationId: formData.location?.id ?? null`. The dropdown resolves a `{ id }` partial itself (`store.resolveSelectedLocation`).
   - Add stock: `shared/stock-batches/ui/modals/form.ts` `_itemChangeEffectRef`: when the item has `locationId` and the caller's `location()` input has no id, set `stockBatchForm.location` to `{ id: item.locationId }`. Caller (`shared/stock/ui/table/stock-list-container.ts`) passes the list's location filter; that still wins.
   - Review lines: `shared/goods-receipt-imports/services/review.store.ts` `buildEditableLine` copies `locationId` into the line's `item`; `review-line/review-line.ts` and `small/review-line-small.ts` `_itemChangeEffectRef` set `location` to `{ id: item.locationId }` when present. Dropdown keeps `prePopulateWithDefault` (only fills when value is null), so items without a location behave as today. Split rows are new components, so their effect re-applies the item location.
   - Supply list: the line is built in `shared/supply-lists/ui/modals/detail/form/supply-list-detail-form.ts` `addItem` (not `supply-list-line.ts`): `notes: item.description ?? ''`. A new line always starts empty and duplicates are ignored, so existing notes are never overwritten.

## Risks

- **Temporal tables**: `Items` is system-versioned. `AddColumn`/FK/index on a temporal table is plain DDL in EF (precedent: `20261110123836_AddInvitationSplit` added columns to temporal `SharedClassConfigurations` with no retention re-append). Inspect the generated migration: if EF emitted `SYSTEM_VERSIONING = OFF/ON`, re-append the `HISTORY_RETENTION_PERIOD = 2 YEARS` line for `[Items]` (pattern `20261108154911_SetTemporalRetention`); `tests/Infrastructure.IntegrationTests/TemporalSchemaTests.cs` catches a miss.
- **Raw UPDATE bypasses `AuditableEntityInterceptor`**: CLAUDE.md requires setting `LastModifiedById` on raw updates of covered tables; the backfill sets it to NULL (= system actor, interceptor convention). `LastModifiedDate` is left untouched so the "lastModifiedDate" keyset sort does not reshuffle every item (see open question). Every backfilled item gets one history row (expected).
- **Migration runs on deploy** (`DatabaseMigrationRunner`): column is nullable, no default needed, backward-safe for the old code. Single set-based UPDATE inside the migration transaction; item count is small.
- **FK delete behaviour**: there is no Location delete command (`Features/Locations/Commands` = Create/Update only) and every other Location FK is `Restrict`; use `Restrict` for consistency. If location deletion is added later, it must clear `Items.LocationId` first.
- **Caching**: `LocationId` lives on the item, so existing tags suffice: `CreateItemCommand` invalidates `ItemListTag`, `EditItemCommand` invalidates `ItemListTag` + `ItemTag(Id)`. DTO carries only the id (no location name), so Location updates need no Items invalidation. `GetGoodsReceiptImportByIdQuery` is not cacheable.
- **MergeItems**: untouched; target keeps its own `LocationId`, disabled sources keep theirs.
- **Item import** (`ConfirmItemImportBatch`) creates items with NULL location; acceptable, no change.
- **Client DTO mirroring**: `item.ts` and `goods-receipt.ts` mirror backend records by hand; request field optional so `item-detail.store.ts`/`items.http.ts` compile unchanged.
- **Effect overwrite (review lines / add stock)**: same behaviour as the existing expiry effect, which also overwrites on item change. A manually chosen location is replaced when the user picks another item that has a location; it is kept when the new item has none. Accepted as prefill semantics.

## Sequential vs parallel

- Sequential: branch -> T1 (domain/EF/migration) -> T2 (DTO/query mapping) -> T3/T4 (create/edit).
- Parallel after T1: T5 (review match DTO). Functional test T6 after T3/T4.
- Client: T7 (models) first, then T8-T11 in parallel.
- Then T12 build/test, T13 graphify, T14 review, T15 PR.

## Verification checkpoints

1. After T1: `dotnet build`; `has-pending-model-changes` clean; read generated migration (no unexpected versioning toggles); start dev stack (`aspire start`), confirm migration applied and spot-check the backfill with the SELECT in the todo.
2. After T2-T5: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Items|FullyQualifiedName~GoodsReceiptImportById"`.
3. After T6: `./run-functional-tests.sh --filter "FullyQualifiedName~Items"` (AppHost must be running per memory note).
4. After client tasks: `cd src/Client && npm run build && npm test -- --watch=false`.
5. Final: full `dotnet build`, Application/Worker unit tests, `Infrastructure.IntegrationTests` (TemporalSchemaTests), client build+tests.

## Open questions

- Backfill leaves `LastModifiedDate` unchanged but nulls `LastModifiedById` (UI "modified by" shows empty for backfilled items). Alternative: also set `LastModifiedDate = SYSUTCDATETIME()` (reorders lastModified sort). Plan assumes the former.
