# Plan: Tab „Fructe și desert"

Spec: `tasks/fruit-dessert-planning-spec.md` (approved). Tasks: `tasks/fruit-dessert-planning-todo.md`.
Branch: `feature/fruit-dessert-planning` (from `master`). Implementation model: Sonnet. Final review: Haiku sub-agent.

Engineering level: production grade for data (additive migration, unique indexes, closed-class guard), lean everywhere else
(no template editor, no Cloud API, no holidays, no Playwright spec).

## Resolved open points

### 1. Popup blocker (spec risk 1)
`window.open` after an awaited POST is blocked by most browsers. Decision:
- On click, the container opens a blank tab **synchronously**: `const win = window.open('', '_blank');` then `if (win) win.opener = null;`.
  Do not pass the `noopener` feature string: with it `window.open` returns `null` and the tab cannot be navigated later.
  Setting `opener = null` gives the same protection.
- The store's `sendMessage(entryId, type, win)` POSTs; on success it calls `win?.location.replace(dispatch.url)`; on error it calls `win?.close()` and sets the problem detail.
- If `win` is `null` (popup blocked anyway), the row shows the returned URL as an `<a target="_blank" rel="noopener">Deschide WhatsApp</a>` fallback (store keeps `lastDispatchUrl` per entry). No extra library.
- Window handling stays at the container/store edge, so the row component stays pure and the store spec passes a fake `{ location: { replace }, close }`.

### 2. GET with side effect + concurrency on the unique index (spec risk 3)
- One request: `GetFruitDessertPlanQuery` (not `ICacheableQuery`, not `IRequiresOpenClass`). Its handler first calls a static helper
  `FruitDessertPlanSync.EnsureEntriesAsync(dbContext, workingDayProvider, schoolClass, ct)` (pattern: `Features/Congregations/CongregationCommandSupport.cs`), then projects the list.
  Reason: one round trip for the client, no second mediator call from the endpoint, and the sync helper is unit-testable on its own. A separate "sync command" would add a request type, validator and endpoint plumbing for no extra behavior.
- Sync inserts only dates from `IWorkingDayProvider.GetWorkingDays(StartDate, EndDate)` that have no row.
- Concurrency: unique index `(SchoolClassId, Date)` is the guard. On `DbUpdateException` the helper calls `dbContext.ChangeTracker.Clear()` and continues to the read: the concurrent request already inserted the rows. No retry loop (a second conflict is impossible: the missing rows now exist). Same catch-and-recheck style as `CongregationCommandSupport.SaveAsync`.
- Closed class (`ClassStatus.Closed`): sync is skipped, existing rows are returned read-only. Keeps success criterion 7 ("nicio modificare") literally true.
- Interval shrinks: rows outside the current interval are kept (they may own message history) but filtered out of the listing. No deletes.

### 3. Deleted congregation display (spec risk 5)
- `CongregationConfiguration` has `HasQueryFilter(c => c.DeletedDate == null)`, so a plain join/navigation returns `null` for a soft-deleted congregation.
- The list projection joins `dbContext.Congregations.IgnoreQueryFilters()` and returns `congregationName` + `congregationDeleted`. UI shows the name with a `(ștearsă)` suffix.
- `SetFruitDessertCongregationCommand` validates against the **filtered** set, so a deleted congregation cannot be newly assigned (`CongregationErrors.CongregationNotFound`, exists).
- Sending a message for an entry whose congregation was deleted afterwards is still allowed (live contact read with `IgnoreQueryFilters()`), because the planning commitment already exists.
- Dropdown: reuse `shared/congregations/ui/dropdown/congregation-dropdown.ts` (`ske-congregation-dropdown`, a Signal Forms `FormValueControl`) with `allowCreate=false`, `allowEdit=false`, `allowClear=true`. It already renders a hidden option for the current value, so a deleted congregation still shows its name. No raw `nz-select` needed.

### 4. Where the `*.http.ts` lives (spec risk 6)
Single consumer, so feature-local, following `features/class-configuration/services/configuration.http.ts`:
- `src/Client/src/app/features/school-classes/services/fruit-dessert-plans.http.ts`
- `src/Client/src/app/features/school-classes/services/fruit-dessert.store.ts` (next to `school-class-overview.store.ts`)
- DTOs in `src/Client/src/app/core/models/fruit-dessert.ts`, exported from `core/models/index.ts` (`@ske/models`).
Move to `shared/school-classes/services` only if a second feature needs it.

### 5. Other decisions taken here
- **Routes:** default group prefix (`src/Web/Infrastructure/WebApplicationExtensions.cs` maps `/api/{ClassName}`), so endpoints are
  `GET /api/FruitDessertPlans/{classId:guid}`, `PUT /api/FruitDessertPlans/{classId:guid}/entries/{entryId:guid}/congregation`,
  `PUT …/email-sent`, `POST …/messages`. Same convention as every other group; the spec's `/school-classes/...` path was illustrative.
- **Abstractions:** interfaces in `src/Application/Features/FruitDessertPlans/Abstractions/`, implementations in `src/Infrastructure/FruitDessert/`, registered in `src/Infrastructure/DependencyInjection.cs` (spec). `WorkingDayProvider`, `PhoneNumberNormalizer`, `MessageTemplateRenderer` as singletons, `WhatsAppLinkSender` singleton.
- **`IApplicationDbContext`:** add the 3 new `DbSet`s as default-throwing members (like `Congregations`, `SharedClassConfigurations`), so the ~40 hand-written unit test DbContexts in `tests/Application.UnitTests` keep compiling untouched.
- **Template seed:** `HasData` in `MessageTemplateConfiguration` with fixed Guids and a fixed `Created` instant; `dotnet ef migrations add` then emits `InsertData`. No hand edits to the migration or snapshot.
- **Phone validity in the list:** GET returns `normalizedPhone` (null when invalid/missing); the row disables WhatsApp buttons when null with a tooltip („Telefon lipsă sau invalid").
- **Cache:** commands implement `ICacheInvalidation` with tag `FruitDessertPlans.CacheConstants.PlanTag(classId)` (spec requirement). The GET is uncached, so the tag is currently inert; it is kept so a future cached read is correct by default.
- **Mediator gotcha** (from `tasks/congregation-todo.md` B2/B3): the source generator fails the build for a request without a handler, so command + validator + handler land in one task.
- **Playwright:** skipped. Vitest on store + row covers the behavior; the wa.me flow cannot be asserted end-to-end anyway.

## Components

| # | Component | Layer | Files (main) |
|---|---|---|---|
| C1 | Entities `FruitDessertPlanEntry`, `FruitDessertMessage`, `MessageTemplate`; enums `FruitDessertMessageType`, `FruitDessertMessageStatus` | Domain | `src/Domain/Entities/FruitDessert/*` |
| C2 | EF configurations + DbSets + migration `AddFruitDessertPlanning` (+ template seed) | Infrastructure | `src/Infrastructure/Data/Configurations/*`, `ApplicationDbContext.cs`, `IApplicationDbContext.cs` |
| C3 | Abstractions + implementations (working days, phone, renderer, sender) | Application/Infrastructure | `Features/FruitDessertPlans/Abstractions/*`, `src/Infrastructure/FruitDessert/*` |
| C4 | `FruitDessertErrors`, `CacheConstants`, DTOs | Application | `Common/Errors/FruitDessertErrors.cs`, `Features/FruitDessertPlans/{CacheConstants.cs,Models/*}` |
| C5 | `GetFruitDessertPlanQuery` + `FruitDessertPlanSync` | Application | `Features/FruitDessertPlans/Queries/GetFruitDessertPlan/*` |
| C6 | Commands: `SetFruitDessertCongregation`, `SetFruitDessertEmailSent`, `SendFruitDessertMessage` | Application | `Features/FruitDessertPlans/Commands/*` |
| C7 | Endpoint group `FruitDessertPlans` | Web | `src/Web/Endpoints/FruitDessertPlans.cs` |
| C8 | Client models + http + store | Client | `core/models/fruit-dessert.ts`, `features/school-classes/services/fruit-dessert*.ts` |
| C9 | Row `tr[ske-fruit-dessert-row]` + date formatter | Client | `overview/tabs/fruit-dessert/row/*`, `overview/tabs/fruit-dessert/format-plan-date.ts` |
| C10 | Container tab + header tab + page wiring | Client | `overview/tabs/fruit-dessert/fruit-dessert-tab.*`, `overview/header/header.ts`, `school-class-overview.page.html` |

## Dependency graph

```
C1 ──> C2 ──┬──> C5 ──┐
            │         ├──> C7 ──> (API contract frozen) ──> C8 ──> C10
C3 ─────────┤         │                               C9 ──┘
C4 ─────────┴──> C6 ──┘
```

- C3 and C4 depend only on C1 (enums) and can run in parallel with C2.
- C5 and C6 need C2 (DbSets) + C3 + C4. C5 and the three C6 commands are independent of each other.
- C8 needs the C7 contract (DTO shapes), not a running backend. C9 is pure presentation and can start once C8's models exist.
- C10 last.

Sequential spine: C1 → C2 → C5/C6 → C7 → C8 → C10. Parallel lanes: {C3, C4} beside C2; {C5, C6a, C6b, C6c}; {C9} beside C8.
With a single implementer, follow the todo order; it already respects the graph.

## Phases and checkpoints

**Phase 1 — Domain + persistence (C1, C2)**
Checkpoint 1: `dotnet build` green; `dotnet ef migrations has-pending-model-changes …` reports none; migration file reviewed by eye: only `CreateTable`/`CreateIndex`/`InsertData`, no `AlterColumn`/`DropTable` on existing tables; filtered unique index on `MessageTemplates(Type) WHERE [IsActive] = 1`.

**Phase 2 — Abstractions (C3, C4)**
Checkpoint 2: `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~FruitDessert"` green for provider/normalizer/renderer/sender tests.

**Phase 3 — Use cases (C5, C6)**
Checkpoint 3: all `FruitDessert` unit tests green (handlers + validators: success, closed class through `ClassOpenBehaviour` contract, unknown congregation, missing contact, invalid phone, no active template, unknown placeholder).

**Phase 4 — API (C7)**
Checkpoint 4: `./run-functional-tests.sh --filter "FullyQualifiedName~FruitDessert"` green; `dotnet build` green. API contract frozen; commit.

**Phase 5 — Client (C8, C9, C10)**
Checkpoint 5: `cd src/Client && npm run build && npm test -- --watch=false` green. Manual smoke via `aspire start --apphost src/AppHost/AppHost.csproj` (tab lists days, select persists, checkbox persists after reload, WhatsApp tab opens with normalized number, closed class disables controls).

**Phase 6 — Wrap-up**
Haiku review of the full diff, fix real findings, `graphify update .`, push, PR.

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Popup blocked after async POST | WhatsApp never opens | Synchronous blank tab + `location.replace`; link fallback when `window.open` returns null |
| Concurrent first GETs | Duplicate-key 500 | Unique index + `ChangeTracker.Clear()` on `DbUpdateException`, then read; functional test with two parallel sends |
| Soft-deleted congregation invisible through query filter | Row loses its name | `IgnoreQueryFilters()` in projection; unit test with deleted congregation |
| `HasData` on a `BaseAuditableEntity` requires all non-null audit columns | Migration generation fails or uses `DateTimeOffset.Now` (non-deterministic → pending changes on every build) | Fixed Guids and fixed `Created` literal in seed; checkpoint 1 runs `has-pending-model-changes` |
| New `DbSet`s break ~40 unit test DbContexts | Large unrelated diff | Default-throwing interface members |
| Migration auto-applied on deploy | Prod schema change | Purely additive tables + seed rows; reviewed at checkpoint 1 |
| Renderer leaves `{Unknown}` in text sent to parents | Corrupt message | Renderer returns a failure listing unknown placeholders; unit test |
| RO phone formats (`07…`, `+407…`, `00407…`, spaces/dashes) | Wrong wa.me link | Normalizer table-driven tests; anything else → invalid, buttons disabled |
| `ClassStatus.Closed` sync skip hides days for a class closed before the tab was first opened | Empty tab on old classes | Acceptable (read-only history); empty state text explains it |
| Template text only editable via SQL | Ops friction | Out of scope per spec (ask-first); noted in PR |

## Out of scope (ask first)
Template editor, WhatsApp Cloud API, legal holidays, real email sending, Playwright spec, deleting rows when the interval shrinks.
