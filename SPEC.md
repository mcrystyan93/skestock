# Spec: Stock batch source (Order / Donation) in the Add Stock wizard

## Objective
When adding stock, the user first picks where the item comes from (**Comandă** / Order or **Donație** / Donation), then fills the existing form. The choice is persisted on the batch.

Success: a batch created via the modal stores its source; every pre-existing batch reads as `Order`; Donation batches get a `Donation` stock transaction.

## Assumptions
1. "Order modal" = the order-list wizard (`shared/order-lists/ui/modals/wizard`), not the stock-count wizard. Reuse `CHECKABLE_CARD_CLASSES` (`steps/card-classes.ts`) and the radio-card pattern from `steps/source-step.ts`.
2. Wizard has 2 steps: `source` → `details`. Footer: Înapoi (details only), Închide, Următorul (source) / Salvează și închide + Salvează (details).
3. Step state kept in the modal component with a signal (a store like `OrderListWizardStore` is overkill for 2 steps).
4. Enum persisted as **string** (`HasConversion<string>()`), matching most enums here.
5. Other batch creators (goods receipt create/import, class rollover, move, adjust surplus, demo seeding) default to `Order` via the column default; no behaviour change.
6. Unit price stays required for Donation (user decision). No table column/filter for source yet (user decision).
7. `StockTransactionType.Donation` is **appended** at the end (that enum is stored as int).
8. Existing backfill: migration column `defaultValue: "Order"` is the backfill ("on first run" = on migration; no startup code).

## Tech Stack
.NET 10, EF Core (SQL Server), Mediator + FluentValidation; Angular signal forms + NG-ZORRO + Tailwind (`src/Client`).

## Commands
```
dotnet build
dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~StockBatch"
./run-functional-tests.sh --filter "FullyQualifiedName~StockBatch"
cd src/Client && npm run build && npm test
dotnet ef migrations add AddStockBatchSource --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations
```

## Project Structure / files touched
- Domain: new `Domain/Enums/StockBatchSource.cs` (`Order`, `Donation`); `StockBatch.Source` (default `Order`); `StockTransactionType.Donation` appended.
- Infrastructure: `StockBatchConfiguration` (string conversion, max length, default `Order`); new migration (never hand-edit designer/snapshot).
- Application: `CreateStockBatchRequest` + `CreateStockBatchCommand` get `Source`; validator `IsInEnum`; handler sets `Source` and transaction type (Donation → `Donation`, else `Order`); `StockBatchListItemDto` exposes `Source`.
- Client: `core/models/stock-batch.ts` (`StockBatchSource` type, request + list DTO field); new `stock-batches/ui/modals/steps/source-step.ts`; `add-stock-batch-modal.{ts,html}` wizard (nz-steps); `mapSaveRequest` sends `source`.
- Tests: validator + handler unit tests, functional create test, client spec for the source step/modal.

## Code Style
Match `source-step.ts`: standalone component, `CHECKABLE_CARD_CLASSES`, `sr-only` radio inputs, Romanian UI copy. Backend: file-scoped namespaces, `Result` for failures.

## Testing Strategy
- Unit: validator rejects undefined enum value; handler persists `Source` and writes `Donation` / `Order` transaction.
- Functional: POST with each source round-trips; omitted source (old clients) → `Order`.
- Client: modal starts on source step, Următorul advances, selected source ends up in the save request.
- Migration check: existing rows read `Order` after applying.

## Boundaries
- Always: central names/constants per `skestock.Shared`; run build + the tests above.
- Ask first: adding a source column/filter to the table; touching other creators' behaviour.
- Never: hand-edit migration designer/snapshot; reorder existing `StockTransactionType` values.

## Success Criteria
1. Opening Add Stock shows steps "Sursă" → "Detalii"; the first step has two selectable cards (Comandă, Donație), Comandă preselected.
2. Next is disabled until a source is chosen; Înapoi returns without losing form values.
3. Saved batch has the chosen `Source`; `StockBatchListItemDto.Source` returned.
4. Migration adds non-null `Source` with default `Order`; all existing batches = `Order`.
5. Donation creates a `Donation` StockTransaction; Order unchanged.
6. `dotnet build` (warnings-as-errors), unit, functional and client tests pass.

## Open Questions
1. Card labels/descriptions copy (proposed: "Comandă — articole achiziționate printr-o comandă", "Donație — articole primite gratuit")? OK?
2. Should "Salvează" (save without closing) reset to the source step for the next item, or stay on the form with the same source? Proposed: stay on the form.

## Execution plan (per your instructions)
Haiku explored (done). After you approve this spec: **Opus** writes the plan/tasks (`tasks/plan.md`, `tasks/todo.md`), then **Sonnet** implements.
