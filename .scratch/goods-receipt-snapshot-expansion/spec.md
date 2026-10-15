# Spec: Goods receipt expansion from saved import lines

Status: Approved and implemented on 2026-10-14; see verification.md for results and baseline limitations.

## Objective

When an inventory user expands a goods receipt, show its persisted
`GoodsReceiptImportLine` snapshots rather than mutable `StockBatch` records.
This is one capability spanning an Application query, HTTP endpoint and client.
It builds on `.scratch/goods-receipt-import-snapshots/spec.md`.

## Assumptions and scope

- Apply the change to both desktop and mobile receipt expansion.
- Use the existing snapshot schema and import-to-receipt link; no migration or
  historical backfill. Manually created receipts and older imports may have no snapshots.
- Display `RawItemText` as the item label. Location names may come from the current
  location catalog; only location IDs are snapshotted today.
- Return all saved rows, including separate rows for split locations, without merging.
  Source-row order is not stored. Use deterministic ordering by `CreatedDate`, then `Id`;
  this does not promise original document order.
- Preserve receipt header summaries, downloads and expansion controls. Changing
  list summary calculations is outside this request.

## Tech stack

Existing .NET 10 solution (SDK 10.0.110), Mediator, FluentValidation, EF Core,
HybridCache and NUnit/Shouldly. Client uses Angular ^22.2.0, NG-ZORRO ^22.1.1,
NgRx Signals ^22.0.1 and Vitest ^5.0.2, resolved through the existing lockfile.
No new dependencies.

## API and query contract

- Add authenticated `GET /api/GoodsReceipts/{id:guid}/import-lines`, where `id`
  is a goods receipt ID, not an import ID.
- Dispatch `GetGoodsReceiptImportLinesQuery` through Mediator. Validate a nonempty
  receipt ID and use `IApplicationDbContext`, no tracking, and cancellation tokens.
- First establish receipt existence. Return the existing goods-receipt not-found
  problem (404) when absent; an existing receipt with no snapshots returns `200 []`.
- Resolve lines through `GoodsReceiptImport.ResultingGoodsReceiptId == id`, for
  confirmed imports only. Read persisted lines, never extraction JSON, review-time
  catalog matching or stock batches.
- Return a JSON array of dedicated `GoodsReceiptImportLineDto` objects:

| Field           | Meaning                                                   |
|-----------------|-----------------------------------------------------------|
| `id`            | Saved import-line ID; row identity                        |
| `importId`      | Owning confirmed import ID                                |
| `rawItemText`   | Saved source item label                                   |
| `matchedItemId` | Nullable saved catalog item ID                            |
| `locationId`    | Nullable saved destination ID                             |
| `locationName`  | Nullable current catalog label; not a historical snapshot |
| `quantity`      | Saved confirmed quantity                                  |
| `unitPrice`     | Saved confirmed unit price                                |
| `expiryDate`    | Nullable saved effective expiry, ISO date                 |
| `lineTotal`     | Saved quantity multiplied by saved unit price             |

- No `StockBatchId`, pagination, search or arbitrary sort/filter contract.
- Implement `ICacheableQuery` with a receipt-specific key, bounded expiration,
  collection and receipt-level tags. Confirmation must invalidate the relevant
  tags; catalog location-name edits must not leave cached labels indefinitely
  stale. Specify exact existing invalidation integration in the implementation plan.
- Keep existing receipt detail, creation and import-review API contracts intact.

## Client behavior

- A typed HTTP method loads import lines by receipt ID. Receipt expansion uses
  dedicated snapshot state and presentation rather than the stock-batch container.
- Load lazily on expansion. Keep rows/loading/errors scoped to the receipt;
  simultaneous expansions must not overwrite each other's data.
- Show item text, destination, quantity, unit price and line total in the current
  desktop table/mobile card presentation, with Romanian number/currency formatting.
  Keep expiry in the DTO; adding a new expiry column is outside this scope.
- For null location names display an accessible neutral placeholder.
- Loading, failure and empty states are distinct. Errors use the existing problem
  details UI and allow retry; never replace failure with an empty success state.
- For no saved lines, show `Nu există linii salvate pentru această recepție`.
  Never fall back to batches or reconstruct historical values.
- Preserve keyboard expansion, accessible labels, responsive layout and download actions.
  Operational stock-batch views continue to use their existing API.

## Commands

From the repository root:

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~GetGoodsReceiptImportLines
dotnet test tests/Application.UnitTests
./run-functional-tests.sh --filter FullyQualifiedName~GoodsReceipts
dotnet run --project src/AppHost
graphify update .
```

From `src/Client`:

```bash
npm ci
npm run build
npm test -- --watch=false
```

Use `npm ci` when a clean dependency install is needed. Functional tests and
AppHost require the repository's container runtime setup.

## Project structure

- `src/Application/Features/GoodsReceipts/Queries/GetGoodsReceiptImportLines/`:
  query, handler and validator, scaffolded with the existing `ca-usecase` template.
- `src/Application/Features/GoodsReceipts/Models/`: dedicated snapshot DTO.
- `src/Application/Features/GoodsReceipts/CacheConstants.cs` and relevant command
  invalidation: cache integration.
- `src/Web/Endpoints/GoodsReceipts.cs`: route and thin endpoint method.
- `src/Client/src/app/core/models/goods-receipt.ts`: matching TypeScript contract.
- `src/Client/src/app/shared/goods-receipts/`: HTTP, receipt-scoped snapshot state,
  snapshot presentation and both expansion integrations.
- `tests/Application.UnitTests/Features/GoodsReceipts/Queries/`: focused query tests.
- `tests/Application.FunctionalTests/`: real HTTP/authentication/SQL verification.
- Colocated client specs: HTTP/state and desktop/mobile rendering verification.
- `.scratch/goods-receipt-snapshot-expansion/spec.md`: this specification.

## Code style

Follow file-scoped namespaces, nullable types, existing global usings and inward
dependencies. Project into DTOs rather than serializing EF entities. Example style:

```csharp
public record GoodsReceiptImportLineDto
{
    public Guid Id { get; init; }
    public string RawItemText { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}
```

Client components use implicit standalone behavior, signal inputs/outputs,
`inject()`, native control flow and existing aliases. Presentation receives
typed data; services/stores own HTTP and side effects. Use `patchState`,
`rxMethod` and existing loading/problem-details features where applicable.

## Testing strategy

- NUnit/Shouldly query tests: receipt existence, invalid ID, receipt isolation,
  persisted scalar projection, deterministic order, split rows, nullable references
  and empty snapshots for manual/legacy receipts.
- Mutate batch quantity/price/expiry and item name after confirmation; snapshot
  response values and item text remain unchanged. Ensure no extraction reconstruction.
- Cache tests: keys isolate receipts and confirmation invalidation covers snapshot reads.
- Functional tests: authenticated route, unauthenticated rejection, 404, `200 []`,
  snapshot payload and real EF projection through SQL. Use repository reset helpers.
- Vitest: endpoint URL, lazy loading, independent receipt state, failed requests
  and retry, empty state, and both expansion views using snapshot values and IDs.
- Real browser check on desktop/mobile: expand, collapse, multiple receipts,
  loading/error/empty states, keyboard operation, layout and accessibility.
- Run relevant backend/client tests and builds. No new coverage percentage target;
  report environment-blocked checks explicitly.

## Boundaries

- Always: read saved snapshot scalars, retain split rows, validate inputs, preserve
  authentication, use existing result/problem mapping and caching conventions,
  verify relevant tests/builds, preserve unrelated working-tree changes.
- Ask first: schema changes, historical backfill, dependency changes, CI changes,
  broader snapshot fields or changes to receipt summary semantics.
- Never: read mutable batch values for expansion, reconstruct missing snapshots,
  repurpose the import review response, edit generated migration files manually,
  weaken checks or remove failing tests.

## Success criteria

1. Both receipt expansion layouts request the new endpoint by receipt ID and make
   no stock-batch request for their expanded rows.
2. Returned rows belong only to confirmed imports linked to the requested receipt.
3. Saved quantity, price, expiry and raw item text survive operational stock and
   catalog item-name changes; split rows retain distinct identities.
4. Missing receipts return 404; existing receipts without snapshots show the
   explicit empty state without stock fallback.
5. Receipt-specific loading/error/retry state works for concurrent expansions.
6. Header/download behavior and operational stock views remain functional.
7. Relevant tests, builds and browser verification establish the behavior, with
   any environmental limitations recorded.

## Review decisions

The user approved the spec, plan and tasks in sequence. The endpoint/array contract,
manual/legacy empty state, raw item label, current location names and deterministic
ordering without an original-row-order guarantee are accepted. Implementation and
verification are recorded in [verification.md](verification.md).
