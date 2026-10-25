# Todo: Stock batch source (Order / Donation)

See [plan.md](plan.md) for order, risks and checkpoints. Spec: [SPEC.md](../SPEC.md).
Open questions in the spec use the proposed defaults: card copy as proposed, and "Salvează" stays on the form with the same source.

## Backend

- [ ] Task T1: Domain enum, entity property, EF config, migration
  - Do:
    - New `StockBatchSource` enum (`Order`, `Donation`, in that order) in namespace `skestock.Domain.Enums`, file-scoped.
    - `StockBatch`: `public StockBatchSource Source { get; set; } = StockBatchSource.Order;`.
    - Append `Donation` as the **last** member of `StockTransactionType` (int-stored; never reorder).
    - `StockBatchConfiguration`: `builder.Property(b => b.Source).HasConversion<string>().HasMaxLength(20).IsRequired().HasDefaultValue(StockBatchSource.Order);` (style of `OrderListConfiguration.Status`).
    - Run `dotnet ef migrations add AddStockBatchSource --project src/Infrastructure --startup-project src/Web --output-dir Data/Migrations`.
  - Acceptance: the generated `Up()` has `AddColumn<string>(name: "Source", table: "StockBatches", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Order")`; `Down()` drops it; Designer and snapshot are generated, not hand-edited.
  - Verify: `dotnet build`
  - Files: `src/Domain/Enums/StockBatchSource.cs` (new), `src/Domain/Entities/Stock/StockBatch.cs`, `src/Domain/Enums/StockTransactionType.cs`, `src/Infrastructure/Data/Configurations/StockBatchConfiguration.cs`, `src/Infrastructure/Data/Migrations/*_AddStockBatchSource*.cs` (generated)

- [ ] Task T2a: Request/command/DTO contract and endpoint mapping
  - Do:
    - `StockBatchRequests.CreateStockBatchRequest`: `public StockBatchSource Source { get; init; } = StockBatchSource.Order;`
    - `CreateStockBatchCommand`: the same property (with default `Order`, so old callers and other tests stay valid).
    - `StockBatchListItemDto`: `public StockBatchSource Source { get; init; }`.
    - `Web/Endpoints/StockBatches.cs` `CreateStockBatch`: add `Source = request.Source` to the command initializer.
    - `GetAllStockBatchesHandler`: add `Source = b.Source` to the `StockBatchListItemDto` projection.
    - Add `using skestock.Domain.Enums;` where needed, unless a GlobalUsings already covers it (check `src/Application/GlobalUsings.cs`).
  - Acceptance: compiles; the list query returns each batch's real `Source`.
  - Verify: `dotnet build`
  - Files: `src/Application/Features/StockBatches/Models/StockBatchRequests.cs`, `src/Application/Features/StockBatches/Models/StockBatchDto.cs`, `src/Application/Features/StockBatches/Commands/CreateStockBatch/CreateStockBatchCommand.cs`, `src/Application/Features/StockBatches/Queries/GetAllStockBatches/GetAllStockBatchesHandler.cs`, `src/Web/Endpoints/StockBatches.cs`

- [ ] Task T2b: Validator + handler behaviour with unit tests
  - Do:
    - Validator: `RuleFor(x => x.Source).IsInEnum().WithErrorCode(...)`. Use an existing code in `skestock.Application.Common.Errors.ValidationErrorCodes`; if there is no enum or invalid-value code, add one constant there following that file's naming.
    - Handler: set `Source = request.Source` on the new `StockBatch`; transaction `Type = request.Source == StockBatchSource.Donation ? StockTransactionType.Donation : StockTransactionType.Order`; add `Source = b.Source` to the follow-up DTO projection.
    - Unit tests (follow existing style in the same files):
      - Validator: `(StockBatchSource)99` produces an error on `Source` with the chosen code; the existing valid-command test stays green.
      - Handler: `[Theory]`-style or two tests showing `Source = Donation` persists the batch with `Donation` and a `StockTransactionType.Donation` transaction, and `Source = Order` persists `Order` + `StockTransactionType.Order`; the returned DTO has the matching `Source`.
  - Acceptance: tests above pass; the existing `Handle_WithValidCommand_CreatesBatchAndOrderTransaction` still passes (default `Order`).
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~StockBatch"`
  - Files: `src/Application/Features/StockBatches/Commands/CreateStockBatch/CreateStockBatchCommandValidator.cs`, `src/Application/Features/StockBatches/Commands/CreateStockBatch/CreateStockBatchCommandHandler.cs`, `tests/Application.UnitTests/Features/StockBatches/Commands/CreateStockBatch/CreateStockBatchCommandValidatorTests.cs`, `tests/Application.UnitTests/Features/StockBatches/Commands/CreateStockBatch/CreateStockBatchCommandHandlerTests.cs`, (maybe) `src/Application/Common/Errors/ValidationErrorCodes.cs`

- [ ] Task T3: Functional test (real SQL Server mapping + migration)
  - Do: in `CreateStockBatchCommandTests` (NUnit `[Test]`, `TestApp.SendAsync`, Shouldly), add:
    - Donation round trip: send with `Source = StockBatchSource.Donation`; `result.Value.Source == Donation`; `TestApp.FindAsync<StockBatch>` has `Source == Donation`; the transaction for `BatchId` has `Type == StockTransactionType.Donation`.
    - Omitted source: send without `Source`; the persisted batch and DTO are `Order`, and the transaction is `Order`. (The existing `Handle_WithValidData_...` test can take the batch/DTO `Source` asserts instead of a new test.)
    - List: after a Donation create, `GetAllStockBatchesQuery` returns that row with `Source == Donation` (proves the second projection).
  - Acceptance: tests pass against the migrated DB, which confirms the string column and default.
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~StockBatch"`
  - Files: `tests/Application.FunctionalTests/Features/StockBatches/Commands/CreateStockBatch/CreateStockBatchCommandTests.cs`

## Client

- [ ] Task T4: Client model + source-step component
  - Do:
    - `core/models/stock-batch.ts`: `export type StockBatchSource = 'order' | 'donation';` (camelCase, matching the API's `JsonStringEnumConverter(CamelCase)`); add `source: StockBatchSource` to `CreateStockBatchRequest` and `StockBatchListItemDto`. Check that `@ske/models` re-exports this file (`core/models/index.ts`).
    - New `stock-batches/ui/modals/steps/source-step.ts`: copy the structure of `order-lists/ui/modals/wizard/steps/source-step.ts`. Selector `ske-stock-batch-source-step`, class `StockBatchSourceStep`, `source = input<StockBatchSource | null>(null)`, `sourceChange = output<StockBatchSource>()`, radio `name="stock-batch-source"`, legend "De unde provin articolele?". Options:
      - `order`, `icons:cart-shopping`, "Comandă", "Articole achiziționate printr-o comandă."
      - `donation`, `icons:gift`, "Donație", "Articole primite gratuit."
    - Import `CHECKABLE_CARD_CLASSES` by relative path `../../../../order-lists/ui/modals/wizard/steps/card-classes` (same as `shared/stock/ui/modals/count/locations-step/locations-step.ts`; adjust the depth to the new file's location).
  - Acceptance: compiles; component is standalone, uses `sr-only` radios and Romanian copy.
  - Verify: `cd src/Client && npm run build` (the modal not sending `source` yet is a type error; if T5 isn't done in the same pass, set `source: 'order'` temporarily in `mapSaveRequest`).
  - Files: `src/Client/src/app/core/models/stock-batch.ts`, `src/Client/src/app/shared/stock-batches/ui/modals/steps/source-step.ts` (new)

- [ ] Task T5: Add-stock modal becomes a 2-step wizard + spec
  - Do (`add-stock-batch-modal.ts`):
    - `protected readonly step = signal<'source' | 'details'>('source');` and `protected readonly source = signal<StockBatchSource | null>('order');`.
    - `next()` sets `'details'` when `source()` is set; `back()` sets `'source'`.
    - `mapSaveRequest` adds `source: this.source()!`.
    - Import `NzStepsComponent`, `NzStepComponent` (`ng-zorro-antd/steps`) and `StockBatchSourceStep`.
  - Do (`add-stock-batch-modal.html`):
    - Above the content: `<nz-steps nzSize="small" class="mb-4" [nzCurrent]="step() === 'source' ? 0 : 1">` with `<nz-step nzTitle="Sursă" />` and `<nz-step nzTitle="Detalii" />`.
    - `@if (step() === 'source') { <ske-stock-batch-source-step [source]="source()" (sourceChange)="source.set($event)" /> }`.
    - Keep `<ske-add-stock-batch-form>` **always rendered**, wrapped in `<div [class.hidden]="step() === 'source'">`, so going back keeps its values (and `viewChild(Form)` stays resolvable).
    - Footer: on `source`: Închide + primary "Următorul" (`[disabled]="!source()"`, `(click)="next()"`). On `details`: "Înapoi" (`(click)="back()"`), Închide, and the existing Salvează și închide / Salvează split button. Fix the copy diacritics: "Închide", "Salvează și închide", "Salvează". "Salvează" (`save(false)`) stays on the details step with the same source.
  - Do (new `add-stock-batch-modal.spec.ts`, Vitest + TestBed):
    - Provide `NZ_MODAL_DATA` (`{ schoolClassId: 'c1', category: null, location: null }`), a `NzModalRef` stub `{ close: vi.fn() }`, `provideHttpClient()`, `provideHttpClientTesting()`, `provideNzIconsTesting()`, plus whatever the form's dropdowns need (add providers until it renders; `NzMessageService` is root-provided).
    - Replace the component-level `StockBatchStore` with `TestBed.overrideComponent(AddStockBatchModal, { set: { providers: [{ provide: StockBatchStore, useValue: { save: vi.fn(), stockBatchLoading: signal(false) } }] } })`.
    - Tests: (1) starts on the source step (radios rendered, first one checked, form wrapper hidden); (2) clicking the Donație radio, then "Următorul", shows the form; (3) with `vi.spyOn(formInstance, 'submit')` resolving a valid `StockBatchFormModel`, `save()` calls `store.save` with `source: 'donation'` (get `formInstance` via `fixture.debugElement.query(By.directive(Form)).componentInstance`).
  - Acceptance: SPEC success criteria 1-3 hold in the UI; the spec passes.
  - Verify: `cd src/Client && npm run build && npm test`
  - Files: `src/Client/src/app/shared/stock-batches/ui/modals/add-stock-batch-modal.ts`, `src/Client/src/app/shared/stock-batches/ui/modals/add-stock-batch-modal.html`, `src/Client/src/app/shared/stock-batches/ui/modals/add-stock-batch-modal.spec.ts` (new)

## Wrap-up

- [ ] Task T6: Full verification
  - Do: `dotnet build`; the unit and functional StockBatch filters; `cd src/Client && npm run build && npm test`; `graphify update .`. Optionally run the app (`dotnet run --project src/AppHost`) and add a Donation batch.
  - Acceptance: all six SPEC success criteria are met; no edited Designer/snapshot files beyond the generated migration; `StockTransactionType` order is unchanged except the appended `Donation`.
  - Verify: commands above all green.
  - Files: none (verification only)
