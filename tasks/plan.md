# Implementation Plan: Stock batch source (Order / Donation)

Implements the approved [SPEC.md](../SPEC.md). Tasks with acceptance criteria live in
[todo.md](todo.md). (The previous plan in this file, the stock count wizard, is in git history:
`git show d1baf3a:tasks/plan.md`.)

## Components

| Layer | What changes | Files (verified) |
|-------|--------------|------------------|
| Domain | New enum `StockBatchSource { Order, Donation }`; `StockBatch.Source` (init `Order`); `StockTransactionType.Donation` appended last | `src/Domain/Enums/StockBatchSource.cs` (new), `src/Domain/Entities/Stock/StockBatch.cs`, `src/Domain/Enums/StockTransactionType.cs` |
| Infrastructure | `Source` as string, max 20, required, default `Order`; migration `AddStockBatchSource` | `src/Infrastructure/Data/Configurations/StockBatchConfiguration.cs`, `src/Infrastructure/Data/Migrations/<ts>_AddStockBatchSource.cs` (+ generated Designer/snapshot) |
| Application | `Source` on request, command, list DTO, both DTO projections; validator `IsInEnum`; handler sets batch source + transaction type | `Models/StockBatchRequests.cs`, `Models/StockBatchDto.cs`, `Commands/CreateStockBatch/{CreateStockBatchCommand,CreateStockBatchCommandValidator,CreateStockBatchCommandHandler}.cs`, `Queries/GetAllStockBatches/GetAllStockBatchesHandler.cs` (all under `src/Application/Features/StockBatches/`) |
| Web | Endpoint maps `request.Source` into the command (it copies fields by hand, so it must be edited) | `src/Web/Endpoints/StockBatches.cs` |
| Client | `StockBatchSource` type + fields; new source-step component; modal becomes 2-step wizard | `src/Client/src/app/core/models/stock-batch.ts`, `src/Client/src/app/shared/stock-batches/ui/modals/steps/source-step.ts` (new), `src/Client/src/app/shared/stock-batches/ui/modals/add-stock-batch-modal.{ts,html}` |
| Tests | Validator + handler unit tests, functional command test, client spec | `tests/Application.UnitTests/Features/StockBatches/Commands/CreateStockBatch/*Tests.cs`, `tests/Application.FunctionalTests/Features/StockBatches/Commands/CreateStockBatch/CreateStockBatchCommandTests.cs`, `src/Client/src/app/shared/stock-batches/ui/modals/add-stock-batch-modal.spec.ts` (new) |

## Verified facts the tasks rely on

- **Endpoint maps request -> command field by field** (`StockBatches.CreateStockBatch` builds `new CreateStockBatchCommand { ... }`), so `Source = request.Source` must be added there.
- **`StockBatchListItemDto` is projected in two places**: `CreateStockBatchCommandHandler` (follow-up projection) and `GetAllStockBatchesHandler` (`Data = new StockBatchListItemDto { ... }`). Both need `Source = b.Source`; otherwise the list returns `Order` for every row.
- **API enums are camelCase strings** (`JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` in `src/Web/DependencyInjection.cs`). The client type is `'order' | 'donation'`, not `'Order'`.
- `StockTransactionType` has no explicit `HasConversion`, so it is stored as int. Appending `Donation` (= 6) is safe; inserting it elsewhere would corrupt data.
- The string-enum config pattern is `OrderListConfiguration`: `.HasConversion<string>().HasMaxLength(20).IsRequired()`.
- The unit-test `CreateStockBatchTestDbContext` is a minimal model with no `StockBatchConfiguration`, so string conversion is not exercised there; the functional test covers the real mapping.
- `CHECKABLE_CARD_CLASSES` is already imported across features by relative path (`shared/stock/ui/modals/count/locations-step/locations-step.ts`). Do the same and don't move it.
- Icons `icons:cart-shopping` and `icons:gift` exist in `src/Client/src/assets/icons/`.
- Client tests run on Vitest through `ng test`; the step spec pattern is `order-lists/ui/modals/wizard/steps/steps.spec.ts`.

## Dependency order

```
T1 Domain + EF config + migration
 └─ T2 Application: request/command/validator/handler/DTO/query projection + endpoint + unit tests
     └─ T3 Functional test (needs migration + T2)
T4 Client model + source-step component (needs only the API contract, not T1-T3)
 └─ T5 Modal wizard + client spec
T6 Final verification
```

## Parallel vs sequential

- Sequential: T1 -> T2 -> T3 (each one compiles against the previous one).
- Parallel: T4/T5 can run alongside T1-T3 once the contract is fixed (`source: 'order' | 'donation'` on the create request and list DTO). Do T5 after T4.
- T6 last.

## Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Migration fails to backfill existing rows | The column must be non-null with `defaultValue: "Order"`. Inspect the generated `Up()`; if the scaffold emitted `defaultValue: ""`, fix it in the config (`HasDefaultValue(StockBatchSource.Order)`) and regenerate. Never edit the Designer/snapshot. |
| `Source` not flowing because one copy point was missed (endpoint, command, two projections) | T2 acceptance lists every copy point. The functional test asserts the round trip through the list query. |
| Old clients omitting `source` | Request and command properties default to `StockBatchSource.Order` (0). The functional test sends a command without `Source`. |
| Losing form values when going back from Detalii to Sursă | Keep `<ske-add-stock-batch-form>` always rendered and hide it on the source step (`[class.hidden]`). Do **not** wrap it in `@if`, which would destroy its signal form. |
| Donation transactions drop out of purchase statistics (`MaterializePurchaseStatisticsCommandHandler` filters `Type == Order`) | Intended: donations aren't purchases. No change, but call it out in the PR. |
| Other batch creators (goods receipt, rollover, move, seeding) | They keep the CLR default `Order`; no code change (spec assumption 5). |

## Verification checkpoints

1. After T1: `dotnet build` passes; the migration `Up()` adds `Source nvarchar(20) NOT NULL DEFAULT 'Order'` to `StockBatches`.
2. After T2: `dotnet build` and `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~StockBatch"` pass.
3. After T3: `./run-functional-tests.sh --filter "FullyQualifiedName~StockBatch"` passes.
4. After T5: `cd src/Client && npm run build && npm test` pass.
5. T6: full `dotnet build`, all of the above, and a manual or `/run` check of the success criteria in SPEC.md (Sursă -> Detalii, Comandă preselected, Înapoi keeps values).
