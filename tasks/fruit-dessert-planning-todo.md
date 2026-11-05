# Fruit & dessert planning: task list

Spec: `tasks/fruit-dessert-planning-spec.md`. Plan: `tasks/fruit-dessert-planning-plan.md` (read "Resolved open points" first).
Branch: `feature/fruit-dessert-planning`. TDD: write the failing test, implement, refactor. Tick a box only after its Verify passes.
Model per task in `[brackets]`. Paths below are relative to the repo root; `App/` = `src/Application/Features/FruitDessertPlans/`,
`UT/` = `tests/Application.UnitTests/Features/FruitDessertPlans/`, `FT/` = `tests/Application.FunctionalTests/Features/FruitDessertPlans/`,
`Tab/` = `src/Client/src/app/features/school-classes/overview/tabs/fruit-dessert/`, `Svc/` = `src/Client/src/app/features/school-classes/services/`.
Client rule: no Prettier, match surrounding formatting by hand, Romanian UI, NG-ZORRO APIs checked in `.github/instructions/llms-full.txt`.

## Phase 0 — Setup

- [x] T0 Create branch `feature/fruit-dessert-planning` from `master` `[Sonnet]`
  - Acceptance: on new branch; spec/plan/todo files carried over (untracked)
  - Verify: `git branch --show-current`
  - Files: none

## Phase 1 — Domain + migration

- [x] T1 Domain entities and enums `[Sonnet]`
  - Acceptance: `FruitDessertPlanEntry` (`SchoolClassId`, `DateOnly Date`, `Guid? CongregationId`, `EmailSent`, `Messages` collection), `FruitDessertMessage` (`EntryId`, `Type`, `Status`, `SentAt` UTC `DateTimeOffset`, `RecipientPhone`, `Body`, `TemplateId`), `MessageTemplate` (`Type`, `Name`, `Body`, `IsActive`), all `BaseAuditableEntity`; enums `FruitDessertMessageType { Reminder = 1, Thanks = 2 }`, `FruitDessertMessageStatus { Opened = 1, Sent = 2, Delivered = 3, Failed = 4 }` (append-only, persisted as int)
  - Verify: `dotnet build`
  - Files: `src/Domain/Entities/FruitDessert/{FruitDessertPlanEntry,FruitDessertMessage,MessageTemplate}.cs`, `src/Domain/Enums/{FruitDessertMessageType,FruitDessertMessageStatus}.cs`
- [x] T2 EF configurations + DbSets `[Sonnet]`
  - Acceptance: unique `(SchoolClassId, Date)`; index `CongregationId`; FK class cascade, congregation restrict; message index `(EntryId, Type)`, FK entry cascade, template restrict; `Name` max 100, `Body` max 1000 (template), message `Body`/`RecipientPhone` bounded; filtered unique index `MessageTemplates(Type)` `.HasFilter("[IsActive] = 1")`; `HasData` for one Reminder + one Thanks template with fixed Guids and fixed `Created` literal (placeholder Romanian text using `{ContactPerson}`, `{Date}`, `{Congregation}`, `{ClassName}`); 3 DbSets on `ApplicationDbContext` and as default-throwing members on `IApplicationDbContext` (pattern: `Congregations`)
  - Verify: `dotnet build`
  - Files: `src/Infrastructure/Data/Configurations/{FruitDessertPlanEntryConfiguration,FruitDessertMessageConfiguration,MessageTemplateConfiguration}.cs`, `src/Infrastructure/Data/ApplicationDbContext.cs`, `src/Application/Common/Interfaces/IApplicationDbContext.cs`
- [x] T3 Migration `AddFruitDessertPlanning` `[Sonnet]`
  - Acceptance: generated only by the command; contains only `CreateTable`/`CreateIndex`/`InsertData`; no pending model changes after
  - Verify: `dotnet ef migrations add AddFruitDessertPlanning --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations && dotnet ef migrations has-pending-model-changes --project src/Infrastructure --startup-project src/Web --context ApplicationDbContext && dotnet build`
  - Files: `src/Infrastructure/Data/Migrations/*_AddFruitDessertPlanning.cs` (+ generated Designer/snapshot, never hand-edited)

**Checkpoint 1:** build green, no pending model changes, migration reviewed by eye (additive only). Commit.

## Phase 2 — Abstractions (tests first)

- [x] T4 Tests: `WorkingDayProvider` and `PhoneNumberNormalizer` `[Sonnet]`
  - Acceptance: failing tests for: Mon–Fri across two weeks, start/end on weekend, start == end, single weekday, inverted interval → empty; phone `0722 123 456`, `0722-123-456`, `+40722123456`, `0040722123456`, `40722123456` → `40722123456`; null/empty/letters/too short/landline-length mismatch → failure
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"` (red, compiles once interfaces exist)
  - Files: `tests/Application.UnitTests/Infrastructure/FruitDessert/{WorkingDayProviderTests,PhoneNumberNormalizerTests}.cs`
- [x] T5 Implement `IWorkingDayProvider` + `IPhoneNumberNormalizer` `[Sonnet]`
  - Acceptance: T4 green; interfaces in `App/Abstractions/`; implementations in `src/Infrastructure/FruitDessert/`; registered in `src/Infrastructure/DependencyInjection.cs`
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"`
  - Files: `App/Abstractions/{IWorkingDayProvider,IPhoneNumberNormalizer}.cs`, `src/Infrastructure/FruitDessert/{WorkingDayProvider,PhoneNumberNormalizer}.cs`, `src/Infrastructure/DependencyInjection.cs`
- [x] T6 Tests: `MessageTemplateRenderer` and `WhatsAppLinkSender` `[Sonnet]`
  - Acceptance: failing tests: all 4 placeholders replaced; `{Date}` formatted ro-RO (e.g. `luni, 10 noiembrie 2026` or the capitalised form chosen, assert one exact string); repeated placeholder; unknown placeholder `{Foo}` → failure naming it; no placeholders → unchanged; sender builds `https://wa.me/40722123456?text=<url-encoded body>` with status `Opened` (diacritics and newlines encoded)
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"` (red)
  - Files: `tests/Application.UnitTests/Infrastructure/FruitDessert/{MessageTemplateRendererTests,WhatsAppLinkSenderTests}.cs`
- [x] T7 Implement renderer + sender + `FruitDessertErrors` `[Sonnet]`
  - Acceptance: T6 green; `IMessageTemplateRenderer`, `IMessageSender` returning `Result<MessageDispatch>` (`Url`, `Status`); `FruitDessertErrors` with `ErrorMetadataKeys` (pattern: `CongregationErrors.cs`): `EntryNotFound` 404, `ContactMissing` 409, `InvalidPhone` 409, `ActiveTemplateMissing` 409, `UnknownPlaceholder` 409; registered in Infrastructure DI
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert" && dotnet build`
  - Files: `App/Abstractions/{IMessageTemplateRenderer,IMessageSender,MessageDispatch}.cs`, `src/Infrastructure/FruitDessert/{MessageTemplateRenderer,WhatsAppLinkSender}.cs`, `src/Application/Common/Errors/FruitDessertErrors.cs`, `src/Infrastructure/DependencyInjection.cs`

**Checkpoint 2:** all abstraction tests green, build green. Commit.

## Phase 3 — Use cases (tests first; command + validator + handler together because the Mediator generator rejects a request without a handler)

- [x] T8 Tests: `GetFruitDessertPlanQuery` + `FruitDessertPlanSync` `[Sonnet]`
  - Acceptance: failing tests with a minimal test DbContext (pattern: `tests/Application.UnitTests/Features/Congregations/CongregationTestDbContext.cs`, in-memory): first call creates exactly the working days; second call creates none; extended `EndDate` adds only new days; rows outside a shrunk interval are kept but not listed; closed class → no rows created, existing returned; unknown class → `SchoolClassErrors` not-found; deleted congregation still returns its name + `congregationDeleted = true`; contact/phone are live from the congregation; `normalizedPhone` null when invalid; `reminderSent`/`thanksSent` true when a message row exists; ordered by date
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~GetFruitDessertPlan"` (red)
  - Files: `UT/FruitDessertTestDbContext.cs`, `UT/Queries/GetFruitDessertPlanHandlerTests.cs`
- [x] T9 Implement query + sync + DTOs + `CacheConstants` `[Sonnet]`
  - Acceptance: T8 green; query not `ICacheableQuery`; sync catches `DbUpdateException`, `ChangeTracker.Clear()`, then reads (plan §2); projection uses `Congregations.IgnoreQueryFilters()`; `CacheConstants.PlanTag(Guid classId)`
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~GetFruitDessertPlan"`
  - Files: `App/Queries/GetFruitDessertPlan/{GetFruitDessertPlanQuery,GetFruitDessertPlanQueryHandler}.cs`, `App/FruitDessertPlanSync.cs`, `App/Models/FruitDessertPlanEntryDto.cs`, `App/CacheConstants.cs`
- [x] T10 `SetFruitDessertCongregationCommand`: tests, then validator + handler `[Sonnet]`
  - Acceptance: tests first (validator: empty ids; handler: set, clear with null, unknown entry → `EntryNotFound`, entry of another class → `EntryNotFound`, deleted/unknown congregation → `CongregationNotFound`); command implements `IRequiresOpenClass` (`ClassId`) and `ICacheInvalidation` (`PlanTag`); returns updated DTO or `Result`
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~SetFruitDessertCongregation"`
  - Files: `App/Commands/SetFruitDessertCongregation/SetFruitDessertCongregationCommand{,Validator,Handler}.cs`, `UT/Commands/SetFruitDessertCongregationTests.cs`
- [x] T11 `SetFruitDessertEmailSentCommand`: tests, then validator + handler `[Sonnet]`
  - Acceptance: tests first (true/false persisted, unknown entry, entry of another class); `IRequiresOpenClass` + `ICacheInvalidation`
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~SetFruitDessertEmailSent"`
  - Files: `App/Commands/SetFruitDessertEmailSent/SetFruitDessertEmailSentCommand{,Validator,Handler}.cs`, `UT/Commands/SetFruitDessertEmailSentTests.cs`
- [x] T12 `SendFruitDessertMessageCommand`: tests, then validator + handler `[Sonnet]`
  - Acceptance: tests first with Moq for `IMessageSender` and real renderer/normalizer: success appends a `FruitDessertMessage` (type, `Opened`, `SentAt` from `TimeProvider`, normalized phone, rendered body, template id) and returns the URL; resend appends a second row; no congregation / missing contact person or phone → `ContactMissing`; invalid phone → `InvalidPhone`; no active template → `ActiveTemplateMissing`; template with unknown placeholder → `UnknownPlaceholder`, nothing saved; validator rejects undefined `Type`; uses the newest active template (new template row works without code change); `IRequiresOpenClass` + `ICacheInvalidation`
  - Verify: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~SendFruitDessertMessage"`
  - Files: `App/Commands/SendFruitDessertMessage/SendFruitDessertMessageCommand{,Validator,Handler}.cs`, `App/Models/FruitDessertMessageDispatchDto.cs`, `UT/Commands/SendFruitDessertMessageTests.cs`

**Checkpoint 3:** `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"` and `dotnet build` green. Commit.

## Phase 4 — Endpoints + functional tests

- [x] T13 Functional tests `[Sonnet]`
  - Acceptance: failing tests (Respawn via `TestBase`, `TestApp.SendAsync`): list creates rows, second list no duplicates; extending interval adds only new days; two concurrent `GetFruitDessertPlanQuery` sends → no exception, one row per day; direct insert of duplicate `(SchoolClassId, Date)` throws `DbUpdateException`; message → history row with snapshot; resend → second row; closed class rejects all 3 commands with `SchoolClassClosed`; inserting a second active template of the same type throws `DbUpdateException`; seeded templates exist (one active per type)
  - Verify: `./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"` (red until T14 only where endpoint-dependent; handler-level tests may already pass)
  - Files: `FT/FruitDessertPlanQueryTests.cs`, `FT/FruitDessertPlanCommandsTests.cs`
- [x] T14 Endpoint group `FruitDessertPlans` `[Sonnet]`
  - Acceptance: `IEndpointGroup` with default prefix `/api/FruitDessertPlans`; `GET {classId:guid}`, `PUT {classId:guid}/entries/{entryId:guid}/congregation`, `PUT …/email-sent`, `POST …/messages`; summaries/descriptions like `src/Web/Endpoints/Congregations.cs`; failures via `ToProblemHttpResult()`; T13 green
  - Verify: `dotnet build && ./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"`
  - Files: `src/Web/Endpoints/FruitDessertPlans.cs`, `App/Models/FruitDessertPlanRequests.cs`

**Checkpoint 4:** functional + unit tests green, API contract frozen. Commit.

## Phase 5 — Frontend

- [x] T15 Models + http + store spec `[Sonnet]`
  - Acceptance: TS models mirror DTOs (no `any`); failing store spec: load populates entries + loading flags; load error sets problem detail; congregation change PUTs and patches the entry (name, contact, phone); email toggle PUTs and patches; `sendMessage(entryId, type, win)` POSTs, then `win.location.replace(url)` and marks sent; on error `win.close()` + problem detail; `win === null` keeps `lastDispatchUrl` for the link fallback
  - Verify: `cd src/Client && npm test -- --watch=false` (red)
  - Files: `src/Client/src/app/core/models/fruit-dessert.ts`, `src/Client/src/app/core/models/index.ts`, `Svc/fruit-dessert-plans.http.ts`, `Svc/fruit-dessert.store.spec.ts`
- [x] T16 Implement `FruitDessertStore` `[Sonnet]`
  - Acceptance: T15 green; `@ngrx/signals` with `withLoadingFeature`/`withProblemDetailsFeature` like `school-class-overview.store.ts`; http class uses `@Service()` like `shared/congregations/services/congregations.http.ts`
  - Verify: `cd src/Client && npm test -- --watch=false && npm run build`
  - Files: `Svc/fruit-dessert.store.ts`
- [x] T17 Date formatter + row spec `[Sonnet]`
  - Acceptance: failing specs: `formatPlanDate('2026-11-10')` → `Luni, 10 Noiembrie 2026` (timezone-safe, parses `DateOnly` string without UTC shift); row renders date, congregation (with `(ștearsă)` when deleted), contact, phone; shows icon + „Trimis" per type when sent; WhatsApp buttons disabled with tooltip when `normalizedPhone` is null; all controls disabled when read-only; emits `congregationChange`, `emailSentChange`, `sendMessage`
  - Verify: `cd src/Client && npm test -- --watch=false` (red)
  - Files: `Tab/format-plan-date.ts`, `Tab/format-plan-date.spec.ts`, `Tab/row/fruit-dessert-row.spec.ts`
- [x] T18 Implement row `tr[ske-fruit-dessert-row]` `[Sonnet]`
  - Acceptance: T17 green; `input()`/`output()`; reuses `ske-congregation-dropdown` (`allowCreate=false`, `allowEdit=false`, `allowClear=true`) via Signal Forms; `nz-checkbox` for „Email trimis"; buttons „Aducere aminte" / „Mulțumire" with `nz-tooltip`; no `ngClass`/`ngStyle`/`@HostBinding`, no explicit `OnPush`/`standalone`
  - Verify: `cd src/Client && npm test -- --watch=false && npm run build`
  - Files: `Tab/row/fruit-dessert-row.ts`, `Tab/row/fruit-dessert-row.html`
- [x] T19 Container tab + wiring `[Sonnet]`
  - Acceptance: `ske-school-class-overview-fruit-dessert-tab` with `[classId]`; provides `FruitDessertStore`; loads on classId; native `<table>` inside an `overflow-x-auto` wrapper; loading / empty („Clasa nu are zile în interval") / error (`ske-error-display`) states; read-only from `injectClassReadOnly()`; send click opens blank tab synchronously then calls store (plan §1); link fallback rendered when popup blocked; new header tab `{label: 'Fructe și desert', compactLabel: 'Fructe'}` appended last in `header.ts` and a matching `<nz-tab>` appended last in `school-class-overview.page.html` (indexes stay aligned; statistics `active` index 4 unchanged)
  - Verify: `cd src/Client && npm run build && npm test -- --watch=false`
  - Files: `Tab/fruit-dessert-tab.ts`, `Tab/fruit-dessert-tab.html`, `src/Client/src/app/features/school-classes/overview/header/header.ts`, `src/Client/src/app/features/school-classes/overview/school-class-overview.page.html`, `src/Client/src/app/features/school-classes/overview/school-class-overview.page.ts` (imports)

**Checkpoint 5:** client build + tests green. Manual smoke: `aspire start --apphost src/AppHost/AppHost.csproj`, `aspire wait webfrontend`, check success criteria 1–7 in the browser, `aspire stop`.

## Phase 6 — Review and ship

- [x] T20 Full verification run `[Sonnet]`
  - Acceptance: everything green; results (incl. failures) recorded for the PR
  - Verify: `dotnet build && dotnet test tests/Application.UnitTests && ./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert" && cd src/Client && npm run build && npm test -- --watch=false`
  - Files: none
- [ ] T21 Independent review `[Haiku sub-agent]`
  - Acceptance: sub-agent receives only `git diff master...HEAD` + spec goal; real findings fixed (re-run T20), the rest listed in the PR
  - Verify: `git diff master...HEAD --stat`
  - Files: as findings require
- [ ] T22 Graph update + PR `[Sonnet]`
  - Acceptance: `graphify update .` run; branch pushed; PR opened against `master` with summary, test results, out-of-scope notes (template editing via SQL, Cloud API, holidays), and the required attribution line
  - Verify: `graphify update . && gh pr view`
  - Files: none
