# Spec: Confirmed goods receipt import line snapshots

Status: Approved and implemented on 2026-10-14

## Objective

When a goods receipt import is confirmed, persist its reviewed lines as historical snapshots in
`GoodsReceiptImportLine`. Stock batches remain the operational records and may change independently. This is one
capability and requires no capability map.

## Assumptions

- Create one snapshot per submitted confirmation row, including separate rows when an extraction line is split across
  locations.
- Use the existing entity and database schema. Snapshot values cover its existing scalar fields; catalog item and
  location names are not independently snapshotted.
- Preserve snapshots through normal application behavior; database-level write protection and a general immutability
  framework are outside this change.
- Apply this to future confirmations. Do not backfill previously confirmed imports.

## Tech Stack

Existing .NET 10 solution, Mediator handlers, EF Core, NUnit and Shouldly. Use existing centrally managed dependencies.

## Snapshot Contract

| Field                       | Confirmed value                                                           |
|-----------------------------|---------------------------------------------------------------------------|
| ImportId / Import           | The import being confirmed                                                |
| MatchedItemId / MatchedItem | Resolved catalog item, including items created during confirmation        |
| LocationId                  | Reviewed destination location                                             |
| Quantity                    | Reviewed row quantity, preserving splits                                  |
| UnitPrice                   | Reviewed unit price                                                       |
| ExpiryDate                  | Effective expiry used by the stock batch, including shelf-life derivation |
| RawItemText                 | Extracted source line name selected by SourceLineIndex                    |

Proposed fallback for missing or blank extracted names: use the resolved catalog item name. Preserve nonblank extracted
names as supplied. If neither source supplies usable text, fail before persisting rather than store an invalid snapshot.
Do not silently truncate text to bypass the existing 500-character limit; implementation must respect existing
validation and persistence constraints.

## Commands

```bash
dotnet build
dotnet test tests/Application.UnitTests --filter FullyQualifiedName~ConfirmGoodsReceiptImport
dotnet test tests/Application.UnitTests
graphify update .
```

## Project Structure

- `src/Application/Features/GoodsReceipts/Commands/ConfirmGoodsReceiptImport/`: confirmation handler and existing
  validation.
- `src/Domain/Entities/GoodsReceipts/GoodsReceiptImportLine.cs`: snapshot entity; update stale review-stage comments as
  appropriate.
- `src/Infrastructure/Data/Configurations/GoodsReceiptImportLineConfiguration.cs`: existing persistence constraints.
- `tests/Application.UnitTests/Features/GoodsReceipts/Commands/ConfirmGoodsReceiptImport/`: handler tests and EF
  in-memory test context.
- `.scratch/goods-receipt-import-snapshots/spec.md`: this specification.

## Code Style

Keep file-scoped namespaces, existing global usings, nullable handling and inward dependencies. Use
`IApplicationDbContext` and the handler's existing single `SaveChangesAsync` call. Example of the existing
collection/object-initializer style:

```csharp
import.Lines.Add(new GoodsReceiptImportLine
{
    Import = import,
    LocationId = line.LocationId,
    Quantity = line.Quantity,
    UnitPrice = line.UnitPrice,
    ExpiryDate = expiryDate
});
```

This illustrates style only; production construction must also populate raw text and the resolved item relationship.

## Testing Strategy

Extend the existing NUnit/Shouldly handler suite using its EF in-memory context. Reload persisted records to verify
existing-item and new-item references, split rows, extracted text, fallback text, explicit and derived expiry dates.
Verify repeat confirmation preserves snapshot IDs and values without adding rows. Mutate and save stock batch quantity,
price and expiry, then reload snapshots and verify their values remain unchanged. Keep existing handler and validator
tests passing. The in-memory provider cannot prove SQL transaction rollback; retain one save operation for the entire
graph and introduce no intermediate saves. No new coverage percentage target.

## Boundaries

- Always: create snapshots only during successful first confirmation; save snapshots, receipt, batches, transactions,
  new catalog entities and confirmed status together; respect existing authorization and validation; run focused tests
  and build; preserve unrelated work.
- Ask first: expand snapshot fields/schema, introduce dependencies, backfill historical imports, change receipt API
  projections, or introduce database-level immutability enforcement.
- Never: synchronize saved snapshot values from mutable stock batches; rewrite snapshots during repeat confirmation;
  hand-edit generated migration files; weaken validation or remove failing tests.

## Success Criteria

1. Successful confirmation persists exactly one import line for every submitted reviewed row.
2. Snapshot quantities, prices, destinations and effective expiry dates match the batches at confirmation time.
3. Existing and newly created items have correct persisted references; repeated new-item rows retain separate snapshots
   while sharing the resolved item.
4. Split rows retain separate destinations and quantities and use their shared source's extracted text.
5. Snapshots and confirmation-related entities are persisted in the existing single save operation.
6. Later stock-batch edits leave snapshot scalar values unchanged.
7. Repeat confirmation adds no snapshots and changes none of the saved snapshot values.
8. Rejected confirmation creates no snapshots.

## Open Questions for Review

- Approve using the extracted source name for RawItemText, falling back to the resolved item name when extraction text
  is absent?
- Confirm that preserving the existing scalar fields meets the requested snapshot scope; independently preserving
  catalog names would require a separate schema decision.
