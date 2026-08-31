# Copilot Instructions — skestock

> See also `/AGENTS.md` for the canonical quick-reference (layer rules, DI convention, Mediator
> pipeline order, endpoint pattern). This file adds deeper file-by-file structure, the
> filtering/keyset-pagination/error conventions, Aspire resource details, and the **Angular
> frontend** (now implemented, not just planned). Don't contradict AGENTS.md — if anything here
> seems to diverge, AGENTS.md wins for the topics it covers.

## Overview

`skestock` (school stock/inventory management) is a .NET 10 (`global.json` pins SDK
`10.0.110`, `rollForward: latestFeature`) solution generated from the **Jason Taylor Clean
Architecture** template, orchestrated end-to-end with **.NET Aspire 13.5.x**. A real frontend
now exists at **`src/Client`**: an **Angular 22** app using **ng-zorro-antd**, **NgRx
SignalStore**, and **Tailwind CSS 4**, wired into the Aspire app model via `AddViteApp` and
built/served with `npm`/Vite (not the classic Angular CLI dev-server-via-webpack path). The
solution file (`skestock.slnx`) includes `src/Client/Client.esproj`, so `dotnet build`/
`dotnet test` at the solution level will also invoke the JS project build (requires Node/npm).

The domain is a school inventory/stock system: `Category`, `Item`, `Location`, `SchoolClass`,
`ClassBalance`, `GoodsReceipt`, `StockBatch`, `StockTransaction`, `UserProfile`. **Seven**
backend feature slices exist under `Application/Features/`: `Categories`, `Items`, `Locations`,
`SchoolClasses`, `GoodsReceipts`, `Stock`, `StockBatches` (each with a matching
`Web/Endpoints/<Feature>.cs`). `ClassBalance` still has no dedicated feature slice.
`StockBatch`/`StockTransaction` rows are still created *as a side effect* of
`GoodsReceipts.CreateGoodsReceipt` (one `StockBatch` + one `StockTransaction` per line item) —
`StockBatches` only exposes a **read** slice (`GetAllStockBatches`) on top of those rows, and
`Stock` exposes a **derived reporting + adjustment** slice (`GetClassLocationStock` query +
`AdjustStock` command) that reconciles physical counts against batch totals. Neither `Stock` nor
`StockBatches` creates `GoodsReceipt`/`Category`/etc.-style aggregate roots of their own.

Slice-naming still diverges — check the existing slice before copying a pattern blindly:
- `Items`: richest command set — `Create`/`Edit`/`Disable`/`Enable` + `GetAllItems` +
  `GetItemById`. Reference for a full mutable-lifecycle CRUD-ish slice.
- `Locations`/`SchoolClasses`: use `Create`/`Update` (not `Edit`), each with a
  `Get<Feature>ById` query alongside `GetAll<Feature>`.
- `GoodsReceipts`: create/read-only (`CreateGoodsReceipt`, `GetAllGoodsReceipts`,
  `GetGoodsReceiptById`) — a goods receipt is an immutable ledger entry once created. Its
  handler is the reference for a command that fans out into multiple child entities
  (`StockBatch` + `StockTransaction` per line) inside one `SaveChangesAsync`. **Still has no
  `Application.FunctionalTests/Features/GoodsReceipts` folder** — unit tests only.
- `Stock`: **no** `FilterConfiguration`/`SortConfiguration`/keyset pagination — `GetClassLocationStock`
  returns a plain `List<StockItemDto>` for one class (optionally one location), not a
  `PaginatedResponse<T>`. `AdjustStock` is the one command in this slice: it diffs a physically
  counted quantity against the sum of remaining batch quantities, draws down existing batches
  (oldest expiry first) on a shortfall, or creates a new unattributed `StockBatch` on a surplus.
  Has full unit + functional test coverage.
- `StockBatches`: read-only `GetAllStockBatches` query, full `CacheConstants`/
  `StockBatchSortConfiguration`/`StockBatchFilterConfiguration` boilerplate (filter by
  `goodsReceiptId`, `itemId`, `locationId`, etc.), full unit + functional test coverage.
- All slices with a `GetAll<Feature>` query have `CacheConstants.cs`, `<Feature>SortConfiguration.cs`,
  and `<Feature>FilterConfiguration.cs` — required boilerplate, not optional extras (see
  `Stock` above for the one slice that legitimately skips them because it isn't paginated).
- `Web/Endpoints/Antiforgery.cs` is still **entirely commented out** (an SPA
  `GET /api/Antiforgery/token` endpoint) and `app.UseAntiforgeryValidation()` in `Web/Program.cs`
  is also commented out — but the frontend **already** configures XSRF cookie/header handling
  (`provideHttpClient(withXsrfConfiguration(...))`, see Frontend section) in anticipation of this
  being turned on. Don't assume server-side antiforgery enforcement is live yet.

## Solution layout (file-level)

```
skestock.slnx                      # solution file (NOT a classic .sln) — includes src/Client/Client.esproj
Directory.Build.props              # shared MSBuild props (net10.0, Nullable, TreatWarningsAsErrors)
Directory.Packages.props           # central package management — ALL .NET package versions live here
global.json                        # pins .NET SDK 10.0.110
aspire.config.json                 # Aspire CLI/tooling config (AppHost project path)
AGENTS.md                          # canonical quick-reference for agents

src/
  Domain/                          # no project references — pure C#
  Application/                     # → references Domain only (see AGENTS.md for pipeline/DI rules)
    Features/<FeatureName>/        # Categories, Items, Locations, SchoolClasses, GoodsReceipts,
                                    #   Stock, StockBatches — see Overview for per-slice divergence
  Infrastructure/                  # → references Application (implements its interfaces) + Domain
  Web/                             # → references Application + Infrastructure + ServiceDefaults
    Endpoints/                     # Categories, GoodsReceipts, Items, Locations, SchoolClasses,
                                    #   Stock, StockBatches, Users (all active); Antiforgery.cs
                                    #   (commented out)
    Program.cs                     # composition root — see startup order below (CORS now explicit-origin)
  AppHost/                         # .NET Aspire orchestrator — see resource graph below
  ServiceDefaults/                 # shared OpenTelemetry/health-check/service-discovery extensions
  Shared/                          # skestock.Shared.Services — service/resource name constants
  Client/                          # ← Angular 22 SPA (see Frontend section) — Client.esproj, NOT Client.csproj
    Client.esproj                  # MSBuild "JS project" wrapper so the SPA is part of the .slnx
    angular.json, package.json, tsconfig*.json
    src/app/
      core/{auth,layouts,models,theme}/   # cross-cutting singletons (see Frontend section)
      features/<name>/             # routed page composition per backend feature
      shared/<name>/                # Http services + SignalStores + reusable UI per backend feature
      shared/{tables,loader,errors,theme}/  # generic reusable primitives (not tied to one feature)

tests/
  Domain.UnitTests/               # project shell exists but has NO tests yet
  Application.UnitTests/          # NUnit, mirrors Application/ folder layout 1:1 — Features/ now has
                                   #   Categories, Items, Locations, SchoolClasses, GoodsReceipts,
                                   #   Stock, StockBatches (7 folders)
  Application.FunctionalTests/    # full Aspire-hosted stack via TestAppHost — Features/ has
                                   #   Categories, Items, Locations, SchoolClasses, Stock, StockBatches
                                   #   (6 folders — GoodsReceipts still missing, see Overview)
  Infrastructure.IntegrationTests/
  TestAppHost/                    # slimmed Aspire host: SQL Server + Redis ONLY (no Storage, no Client)
```
`Web/ClientApp/` also exists on disk but is now an **empty leftover directory** (no files under
it) — the real, actively-developed frontend is `src/Client`. Don't add new frontend code under
`Web/ClientApp`.

## Architecture & dependency direction

`Domain` ← `Application` ← `Infrastructure` & `Web`. `Web` is the composition root. `Shared` is
referenced by `AppHost` and app projects for resource-name constants only. `Application` never
references an EF Core provider directly — only `IApplicationDbContext`. `src/Client` is a
standalone npm/Angular project with **no** reference to any .NET project; it only talks to `Web`
over HTTP (see Frontend section) and is wired into the solution purely for build orchestration
(`Client.esproj`) and Aspire process orchestration (`AddViteApp` in `AppHost/Program.cs`).

### AppHost resource graph (`src/AppHost/Program.cs`)

`AppHost.csproj` targets `Aspire.AppHost.Sdk/13.5.2` and references
`Aspire.Hosting.{AppHost, Azure.AppContainers, Docker, JavaScript, Azure.Sql, Redis}`.
Resources, in dependency order:
- `compose` — `builder.AddDockerComposeEnvironment("env")` with a dashboard forwarded on port
  `8080`. All infra resources below are attached to it via `.WithComputeEnvironment(compose)` and
  also `.PublishAsDockerComposeService(...)` so `aspire publish`/docker-compose generation works,
  not just `dotnet run` orchestration.
- `databaseServer` (`Services.DatabaseServer`) — SQL Server container, secret `sql-password`
  parameter, `WithEndpoint(targetPort: 1433, port: 1433, name: "tcp")`, data volume
  `Services.DatabaseVolumes`, database `Services.Database` (`skestockDb`).
- `cache` (`Services.Cache`) — Redis container, secret `redis-password` parameter, data volume
  `Services.CacheVolumes`, `WithEndpoint(targetPort: 6379, port: 6379, name: "tcp")`.
- `storage` (`Services.Storage`) — **`AddAzureStorage(...).RunAsEmulator(azurite => ...)`**
  (Azurite), persistent lifetime, data volume `Services.StorageVolumes`, explicit blob/queue/table
  ports (`10000`/`10001`/`10002`). Two blob containers are declared: `blobs` and `app-files`
  (`blobContainerName: "app-files"`). **Not yet consumed anywhere in `Application`/`Infrastructure`/
  `Web` code** (no `BlobServiceClient`/`BlobContainerClient` usage found) — treat this as
  reserved/scaffolded for a future file-attachment feature, not an active integration. `Web`
  depends on it (`WithReference(blobs).WaitFor(filesContainer)`), so it still starts even though
  unused.
- `web` (`Services.WebApi`) — the `Web` project; references + waits on `databaseServer`, `cache`,
  and the blob containers; external HTTP endpoints; `WithAspNetCoreEnvironment()`; dashboard
  shortcut to `/scalar`.
- `webfrontend` (`Services.WebFrontend`) — **`builder.AddViteApp(Services.WebFrontend, "../Client", "dev")`**:
  runs the Angular app's `npm run dev` (i.e. `ng serve`) script under Aspire. References + waits
  on `web`, injects `ASPNETCORE_URLS` = the Web API's resolved HTTP endpoint, `.WithNpm()` (Aspire
  runs `npm install` automatically), `.WithHttpEndpoint(port: 7001, env: "PORT")`, external HTTP
  endpoints.
- CORS wiring: `web.WithEnvironment("Cors__AllowedOrigins__0", webfrontend.GetEndpoint("http"))`
  — the frontend's Aspire-assigned origin is fed into `Web`'s `Cors:AllowedOrigins` config at
  orchestration time (see Program.cs below); this is required because `AllowCredentials()`
  (needed for cookie auth) is incompatible with `AllowAnyOrigin()`.
- No local fallback connection strings exist in `appsettings.json` for SQL/Redis/Storage —
  everything is wired through Aspire resource references. Running `Web` standalone (`dotnet run`
  without AppHost) has no working DB/cache/storage connection, but `Web/Program.cs` does supply a
  `https://localhost:4200`/`http://localhost:4200` CORS fallback in Development so you can still
  point a manually-started `ng serve` at a standalone `Web` for quick iteration.
- Running via AppHost requires **both** Docker (SQL Server, Redis, Azurite containers) and
  Node/npm (Vite dev server for the frontend) to be available locally.

### Web startup order (`src/Web/Program.cs`)

```
AddServiceDefaults() → AddKeyVaultIfConfigured() → AddApplicationServices()
  → AddInfrastructureServices() → AddWebServices()
→ (Development only) InitialiseDatabaseAsync()  else  UseHsts()
→ UseHttpsRedirection()
→ UseCors(policy => WithOrigins(<Cors:AllowedOrigins config, or localhost:4200 fallback in Dev>)
    .AllowAnyMethod().AllowAnyHeader().AllowCredentials())
→ UseAuthentication() → UseAuthorization()
  // app.UseAntiforgeryValidation();  -- commented out, see Overview
→ UseFileServer() → MapOpenApi() → MapScalarApiReference()
→ UseExceptionHandler(options => { }) → Map("/", redirect to /scalar)
→ MapDefaultEndpoints() → MapEndpoints(assembly)
```
This is a real change from a naive Clean Architecture template: CORS is **no longer**
`AllowAnyOrigin()` — it's an explicit origin allowlist (`Cors:AllowedOrigins` config section,
empty array by default in `appsettings.json`, populated by Aspire at run time) combined with
`AllowCredentials()`, because auth now supports cookie sign-in for the SPA (see Auth below).
Follow the existing ordering/DI-per-layer convention described in AGENTS.md when adding new
middleware — don't insert ad hoc `builder.Services.AddX()` calls directly in `Program.cs`.

## Auth

Two schemes are registered side by side in `Infrastructure/DependencyInjection.cs`:
```csharp
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddBearerToken(IdentityConstants.BearerScheme)
    .AddCookie(IdentityConstants.ApplicationScheme);
```
`ApplicationScheme` (cookie) is the **default** — this is what the Angular SPA uses (credentialed
CORS + XSRF, see Frontend section); `BearerScheme` remains available for non-browser/API clients.
`Web/Endpoints/Users.cs` maps `MapIdentityApi<ApplicationUser>()` plus a custom `logout` POST
(`SignInManager.SignOutAsync()`, `[RequireAuthorization]`). Roles are int-keyed
(`IdentityRole<int>`); `Domain.Constants.Roles.Administrator` is the only role defined so far.

## Query/filter/pagination/caching/error conventions

These conventions (column filtering, keyset/cursor pagination, `HybridCache` tag-based caching,
the FluentResults `Result<T>` + typed `Error` pattern vs. thrown exceptions, `ValidationErrorCodes`)
are unchanged from before and are documented in full detail in the previous revision of this file
— see `Application/Common/{Filtering,Keyset,Models,Caching,Errors}` and use `Categories`
(`GetAllCategoriesQuery`/`Handler`/`Validator`) as the reference implementation for a full
paginated/filterable/cached query, and `Stock.GetClassLocationStockQuery` as the reference for a
deliberately **non**-paginated query that skips that boilerplate. The `ApiErrorContract`
(`Code`/`Errors[]`/`Diagnostics.CorrelationId`) produced by `ResultProblemDetailsMapper` is now
actively consumed by the frontend — see `core/models/errors.ts` below, which mirrors this
contract's shape exactly (`ProblemDetails`, `BackendErrorPayload`, `BackendErrorItem`,
`isValidationProblem()` type guard keyed on `status === 400` + at least one error with a `field`).

## Scaffolding new CQRS features (backend)

```bash
cd src/Application
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```
After scaffolding, still add `FilterConfiguration`/`SortConfiguration`/`CacheConstants` by hand
for a paginated `GetAll<Feature>` query (the template doesn't generate those) — follow
`Categories` or `StockBatches` as the model, or skip them entirely if the query isn't paginated
(follow `Stock` as the model for that case).

## Testing strategy

- **Domain.UnitTests**: still empty — no domain logic test coverage.
- **Application.UnitTests**: NUnit + Shouldly + Moq. `Features/` now has 7 folders (see Overview);
  `Features/GoodsReceipts/Commands/CreateGoodsReceipt/GoodsReceiptTestDbContext.cs` is a
  dedicated in-memory `IApplicationDbContext` fixture — check it before adding a new in-memory DB
  helper for another multi-entity command handler (e.g. `Stock.AdjustStock`, which also touches
  multiple `StockBatch`/`StockTransaction` rows).
- **Application.FunctionalTests**: boots the real stack via `TestAppHost`
  (`DistributedApplicationTestingBuilder`), waits for `Services.Database` health, resets the DB
  per test/fixture with `DatabaseResetter` (Respawn-based). `TestAppHost` only stands up SQL
  Server + Redis (**no** Storage/Azurite, **no** Client/frontend) — functional tests never
  exercise the frontend or blob storage. Currently covers `Categories`, `Items`, `Locations`,
  `SchoolClasses`, `Stock`, `StockBatches` (not `GoodsReceipts` — add that folder if you extend it).
- **Infrastructure.IntegrationTests**: exercises `ApplicationDbContext`/EF Core directly.
- Run backend tests: `dotnet test` (needs Docker running for functional/integration tests).
- Frontend has its own test runner (`vitest`, wired via `@angular/build`'s unit-test builder) —
  run from `src/Client`: `npm test` (invokes `ng test`). Not currently exercised by `dotnet test`
  at the solution level beyond the `Client.esproj` build step.

## Frontend (`src/Client`) — Angular 22 + ng-zorro-antd + NgRx SignalStore

This is a fully implemented SPA, not just scaffolding. Stack: Angular `^22.1.0` (standalone
components, no `NgModule`s), `ng-zorro-antd ^22.0.1`, `@ngrx/signals` + `@ngrx/operators ^22.0.0`
(SignalStore, not classic NgRx store/effects/reducers), Tailwind CSS `^4.1.12` + LESS for
component/theme styling, `lodash-es`, `vitest` for unit tests. Package manager is npm
(`packageManager: "npm@11.16.0"` in `package.json`); `src/Client/package.json` scripts:
`dev` (`ng serve`, used by Aspire's `AddViteApp`), `build`, `watch`, `test` (`ng test`).

### Project structure & conventions

```
src/app/
  app.config.ts        # ApplicationConfig: provideRouter, provideHttpClient(withXsrfConfiguration
                        #   + withInterceptors([authInterceptor])), provideAppInitializer,
                        #   provideNzI18n(ro_RO) + provideNzDateFnsAdapter, locale 'ro' / currency 'RON'
  app.routes.ts         # top-level routes just re-export layout route groups (see below)
  app.init.ts           # app initializer (bootstrap-time auth/session check)
  core/
    auth/               # AuthStore (SignalStore), AuthHttp, AntiforgeryHttp, authInterceptor,
                         #   auth.guard / guest.guard (functional route guards)
    layouts/            # simple/ (public shell, e.g. login) and full/ (authenticated shell with
                         #   header) — each exports its own lazy route group (simpleRoutes/fullRoutes)
    models/             # shared DTOs/types mirroring backend contracts 1:1 (see below)
    theme/               # theme service/tokens (dark/default LESS bundles, see styles below)
  features/<name>/      # ROUTED PAGE composition per backend feature, e.g. features/items/:
                         #   list/{header,filter,table}/ sub-components + list/items.page.ts +
                         #   list/items.routes.ts (lazy loadComponent) + services/<feature>-list.store.ts
                         #   (a thin SignalStore that composes the shared collection feature, see below)
  shared/<name>/         # DATA LAYER + reusable UI per backend feature, e.g. shared/items/:
                         #   services/items.http.ts (HttpClient wrapper), services/item-detail.store.ts,
                         #   store-features/item-collection.feature.ts (signalStoreFeature — reusable
                         #   composable, NOT a full store), ui/modals/* (create/edit modal + form)
  shared/{tables,loader,errors,theme}/  # generic, feature-agnostic reusable primitives:
                         #   tables/base-table.ts — generic <T, K extends BasePaginationFilter>
                         #     Component base class (virtual scroll via NzTable + CdkScrollable,
                         #     infinite "load more" via hasNextPage/onLoadMore, sort-map helpers)
                         #   loader/services/loading.feature.ts — withLoadingFeature(prefix)
                         #     signalStoreFeature generating `{prefix}Loading()`/`set{Prefix}Loading()`/
                         #     `set{Prefix}Loaded()` etc. by naming convention
                         #   errors/services/problem-detail.feature.ts — withProblemDetailsFeature(prefix)
                         #     signalStoreFeature generating `{prefix}ProblemDetail`/`{prefix}ValidationErrors`
                         #     state + `handle{Prefix}Error(error)`/`clear{Prefix}Errors()` methods that
                         #     parse an HttpErrorResponse body into the ProblemDetails/ValidationProblemDetails
                         #     shape from core/models/errors.ts
```

**`features/<name>` vs `shared/<name>`** is the key convention to preserve when adding a new
feature: `shared/<name>` owns the HTTP client, the reusable SignalStore/`signalStoreFeature`
building blocks, and any reusable UI (e.g. create/edit modals) that could be reused from more
than one route; `features/<name>` owns the routed page itself (page component + its
header/filter/table sub-components + its own lazy `*.routes.ts`) and composes the `shared`
building blocks into a page-specific store (e.g. `ItemListState` in
`features/items/services/item-list.store.ts` calls `withItemCollection()` from
`shared/items/store-features/item-collection.feature.ts` and adds page-only state like
`togglingItemId`).

**SignalStore pattern**: stores are built with `signalStore(...)` (root stores, e.g. `AuthStore`,
`ItemListState`) or `signalStoreFeature(...)` (reusable composables meant to be plugged into
multiple stores, e.g. `withItemCollection()`, `withLoadingFeature()`, `withProblemDetailsFeature()`),
composed from `withState`/`withComputed`/`withMethods`/`withProps`/`withHooks`. Async work uses
`rxMethod<T>(pipe(tap(...), switchMap(...)))` from `@ngrx/signals/rxjs-interop`, with
`mapResponse({ next, error })` from `@ngrx/operators` to route HTTP success/failure into
`patchState(...)` calls — this is the standard shape for every HTTP-backed store method in this
codebase; don't reach for manual `.subscribe()` or classic NgRx effects. Client-side keyset
pagination mirrors the backend contract: `PaginatedResponseData` (`hasNextPage`/`nextCursor`)
and `BasePaginationFilter`-shaped request filters (`sort`/`filters`/`cursor`/`pageSize`/`searchTerm`)
in `core/models/pagination.ts` line up field-for-field with `Application/Common/Models`.

**Error handling**: `core/models/errors.ts` defines `ProblemDetails`/`ValidationProblemDetails`/
`BackendErrorPayload`/`BackendErrorItem` matching `ApiErrorContract` exactly, plus the
`isValidationProblem()` type guard. `withProblemDetailsFeature(prefix)` is the standard way any
store surfaces a failed request to its template — always call the generated
`handle<Prefix>Error(error)` in a `mapResponse({ error })` branch and `clear<Prefix>Errors()`
before issuing a new request of that kind.

**Path aliases** (`tsconfig.json` → `compilerOptions.paths`): every `core/*` and `shared/*`
folder (and each `features/<name>`) is exposed as an `@ske/...` import alias (e.g. `@ske/models`,
`@ske/auth`, `@ske/layouts`, `@ske/shared/items`, `@ske/features/items`) — **always import via
the alias**, never a relative `../../` path across feature/shared/core boundaries, and add a new
alias entry here whenever you add a new `features/<name>` or `shared/<name>` folder.

**Routing**: `app.routes.ts` only loads the two layout route groups (`simpleRoutes`/`fullRoutes`
from `@ske/layouts`); each backend-feature route (e.g. `items`) is lazy-loaded
(`loadComponent`/`loadChildren`) from inside `full.routes.ts`, guarded by `authGuard`. Login lives
under the `simple` layout guarded by `guestGuard`.

**Styling/i18n**: LESS (`src/styles/styles.less`, plus non-injected `default.less`/`dark.less`
bundles for runtime theme switching, see `shared/theme/theme-switcher*.ts`) + Tailwind v4
(`src/styles/tailwind.css`, configured via `@tailwindcss/postcss`). Locale is Romanian by default
(`provideNzI18n(ro_RO)`, `registerLocaleData(ro)`, `LOCALE_ID: 'ro'`, `DEFAULT_CURRENCY_CODE: 'RON'`).

**XSRF**: `provideHttpClient(withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName:
'X-XSRF-TOKEN' }))` uses Angular's own default cookie/header names, named explicitly in
`app.config.ts` to stay in lockstep with the (currently commented-out, see Overview)
`AddAntiforgery(...)`/`Antiforgery.cs` server-side wiring — when that endpoint is turned back on,
the frontend is already set up to consume it via `core/auth/services/antiforgery.http.ts`.

**Reference docs**: `.github/instructions/llms-full.txt` is a large aggregated dump of the
ng-zorro-antd component documentation (77 components) — consult it for component API details
instead of guessing prop names; it is not a set of project conventions.

## Custom agents & skills available

- `.github/agents/CSharpExpert.agent.md` — general .NET/C# development assistance.
- `.github/agents/csharp-dotnet-janitor.agent.md` — cleanup/modernization/tech-debt on C#/.NET code.
- `.github/skills/angular-developer`, `.github/skills/ngrx-signalstore` — Angular/SignalStore
  authoring guidance, now directly applicable since `src/Client` is a real, active project.
- `.github/skills/aspire` — Aspire CLI/AppHost/dashboard guidance, applicable to `src/AppHost`.
- `.github/instructions/angular-guidelines.instructions.md` and
  `ng-zorro-guidelines.instructions.md` apply to `src/Web/ClientApp/**` per their front-matter
  glob, but in practice should be treated as applying to `src/Client/**` too, since that's where
  Angular code actually lives now.
