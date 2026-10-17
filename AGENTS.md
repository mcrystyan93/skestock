# Agent instructions

## Source of truth

- This is a .NET 10 solution (`global.json` SDK `10.0.110`, roll-forward `latestFeature`) using the XML solution `skestock.slnx`; it is not a classic `.sln`.
- `Directory.Build.props` enables nullable/implicit usings and `TreatWarningsAsErrors`; central NuGet versions belong in `Directory.Packages.props`, never in project files.
- `CLAUDE.md` contains the repo's graphify workflow. For broader coding conventions, storage/queue details, and Angular rules, consult `.github/copilot-instructions.md` rather than duplicating them here.

## Commands

```bash
dotnet build
dotnet test
dotnet test tests/Application.UnitTests
dotnet test tests/Infrastructure.IntegrationTests
dotnet test tests/Worker.UnitTests
cd src/Client && npm ci && npm run build
cd src/Client && npm test
```

- The supported full-stack run is `dotnet run --project src/AppHost`; it needs Docker or a compatible container runtime and Node/npm. AppHost starts SQL Server, Redis, Azurite, Web, Worker, and the Angular/Vite client. The client is on port `7001`; the Aspire dashboard is on `18080`; Web's API reference is `/scalar`.
- Functional tests require containers and are normally run with `./run-functional-tests.sh [extra dotnet test args...]` when using Podman. The script starts/checks the user Podman socket and sets the required `DOCKER_HOST`/Aspire runtime variables. The direct command is `dotnet test tests/Application.FunctionalTests` when Docker is already configured.
- Functional-test setup starts `TestAppHost`, waits up to 90 seconds for its SQL/Redis/queue resources, and uses `DatabaseResetter`/Respawn. Do not assume a clean database outside that reset helper. `TestAppHost` does not exercise Azurite, the Worker, or the browser client.

## Architecture and boundaries

- Dependencies point inward: `Web`/`Infrastructure` → `Application` → `Domain`; `Shared` contains cross-project service/resource constants. `Client` is an independent Angular project. `AppHost` is only the Aspire resource graph, not the HTTP app.
- `Application` uses Mediator source generation (not MediatR), FluentValidation, and only the `IApplicationDbContext` abstraction. EF providers/configuration and external adapters belong in `Infrastructure`.
- `Web` and `Worker` are separate hosts. `Worker` consumes goods-receipt queue messages with idempotency/retry/poison-queue handling; do not add processing to the leftover sample loop in `src/Worker/Worker.cs`.
- Use `skestock.Shared.Services` for every Aspire resource, queue, database, cache, volume, and configuration-section name; do not duplicate string literals.

## Implementation patterns

- Layer service registration is exposed from each layer's own namespace (`AddApplicationServices`, `AddInfrastructureServices`, `AddWebAuthenticationServices`, `AddWebServices`, `AddServiceDefaults`). `src/Web/Program.cs` composes them in this order: service defaults → optional Key Vault → Application → Infrastructure → Web auth → Web services.
- Add HTTP features as endpoint-group classes under `src/Web/Endpoints` implementing `IEndpointGroup` with a static `Map(RouteGroupBuilder)`; reflection discovery via `MapEndpoints` means no manual endpoint registration. Dispatch business work through Application handlers and map failed `Result`s with the existing problem-result mapper.
- Mediator behavior order in `src/Application/DependencyInjection.cs` is significant: `LoggingBehaviour` → `UnhandledExceptionBehaviour` → `AuthorizationBehaviour` → `ClassOpenBehaviour` → `ValidationBehaviour` → `PerformanceBehaviour` → `CachingBehavior` → `CacheInvalidationBehavior` → `ClassLockBehaviour`. The two class behaviours apply to commands implementing `IRequiresOpenClass`: `ClassOpenBehaviour` rejects a closed class early; `ClassLockBehaviour` stays innermost so its transaction (shared class-row lock, taken through the execution strategy) commits before cache invalidation, and `CloseSchoolClassCommandHandler` takes the exclusive lock.
- Queries implement `ICacheableQuery`; commands implement `ICacheInvalidation`. Use HybridCache's tag invalidation with sensible expirations and both collection-level and entity-level tags.
- For new CQRS use cases, run the `ca-usecase` template from `src/Application`; install `Clean.Architecture.Solution.Template::10.8.0` if the template is unavailable. Complete slice-specific pagination/filter/sort/cache files manually when the template does not create them.
- Use `Guard.Against.*` for argument/configuration guards and preserve each project's existing `GlobalUsings.cs` and file-scoped namespace style.

## Data, auth, and client gotchas

- Persisted instants use UTC `DateTimeOffset` (see `docs/adr/0001-utc-datetimeoffset-for-persisted-instants.md`). Keep EF configuration in `src/Infrastructure/Data`; application handlers use `IApplicationDbContext` and `SaveChangesAsync`.
- Cookie auth requires explicit CORS origins with credentials; do not replace the configured allowlist with `AllowAnyOrigin()`. Antiforgery middleware is currently scaffolded but commented out, so do not assume it is enforced.
- Client dependencies are locked by `src/Client/package-lock.json`; use `npm ci` for a clean install. The real frontend is `src/Client` (not the old `src/Web/ClientApp` path). Angular-specific conventions are in `.github/copilot-instructions.md` and `.github/instructions/`.
- Do not hand-edit generated EF migration designer/snapshot files or other generated artifacts; update their source/configuration and regenerate using the repository's existing tooling.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

## Imported GitHub Copilot instructions

The following sections copy the contents of the Copilot instruction files. Apply
this guidance when working in `src/Client/**`; the original `applyTo` metadata
uses the former `src/Web/ClientApp/**` path and is retained for provenance.

For detailed NG-ZORRO component APIs and examples, consult
`.github/instructions/llms-full.txt` when implementing or checking a component.
That file is a documentation reference and is not embedded here because it
exceeds Codex's default instruction size limit.

### Source: `.github/instructions/angular-guidelines.instructions.md`

---
applyTo: 'src/Web/ClientApp/**'
---
You are an expert in TypeScript, Angular, and scalable web application development. You write functional, maintainable, performant, and accessible code following Angular and TypeScript best practices.

## TypeScript Best Practices

- Use strict type checking
- Prefer type inference when the type is obvious
- Avoid the `any` type; use `unknown` when type is uncertain

## Angular Best Practices

- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default in Angular v20+.
- Do NOT set `changeDetection: ChangeDetectionStrategy.OnPush` explicitly. `OnPush` is the default in Angular v22+.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

## Accessibility Requirements

- It MUST pass all AXE checks.
- It MUST follow all WCAG AA minimums, including focus management, color contrast, and ARIA attributes.

## Components

- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Prefer inline templates for small components
- Prefer Signal Forms (`@angular/forms/signals`) for new forms. They are stable in Angular v22+ and provide signal-based state, type-safe field access, and schema-based validation
- When not using Signal Forms, prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- Do NOT use `ngStyle`, use `style` bindings instead
- When using external templates/styles, use paths relative to the component TS file.

## State Management

- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

## Templates

- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables
- Do not assume globals like (`new Date()`) are available.

## Services

- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Prefer the `@Service` decorator over `@Injectable({providedIn: 'root'})` for new singleton services (Angular v22+)
- Use the `inject()` function instead of constructor injection


### Source: `.github/instructions/ng-zorro-guidelines.instructions.md`

---
applyTo: 'src/Web/ClientApp/**'
---
# ng-zorro-antd Component Documentation (English)

Use this guidance when generating Angular UI code with `ng-zorro-antd` in this repository.

## Version and Platform Expectations

- Target Angular `^22.0.0` and `ng-zorro-antd` matching Angular major versions.
- Prefer standalone components and application-level providers in `app.config.ts`.
- Use Angular native control flow (`@if`, `@for`, `@switch`) and modern signal-based patterns used by this codebase.

## Core ng-zorro Rules

- Import only required component APIs and icons; avoid broad imports that increase bundle size.
- Prefer immutable state updates for inputs passed to NG-ZORRO components.
- Use global config via `provideNzConfig(...)` for shared defaults instead of repeating per-component settings.
- For overlays inside custom scroll containers, ensure CDK scrolling support (`CdkScrollable` / `ScrollingModule`) so positioning updates correctly.
- For service-based UI (`NzModalService`, `NzDrawerService`, `NzMessageService`, `NzNotificationService`), pass strongly typed options and keep side effects in component/store methods.

## Animations

- For disabling animations globally or locally, use `provideNzNoAnimation()`.
- For component-level template control, use the `nzNoAnimation` directive.
- Use `provideNzWave({ disabled: true })` (or `provideNzNoAnimation`) to disable wave effects when required.

## Date Components (Critical in v22)

- Date components require an explicit `NzDateAdapter` provider at application root.
- Configure one of:
  - `provideNzDateFnsAdapter(...)`
  - `provideNzNativeDateAdapter(...)`
  - `provideNzDateAdapter(CustomAdapter, ...)`
- If locale changes at runtime, update adapter locale via `dateAdapter.setLocale(...)`.
- Ensure date format tokens match the selected adapter/date library.

## Internationalization and Direction

- Configure `provideNzI18n(...)` and Angular locale data registration together.
- Respect runtime RTL/LTR requirements via `dir` or component/service config (`nzDirection`).

## Theming

- Prefer official theming/global config hooks (`provideNzConfig`, `NzConfigService.set`) for dynamic theme updates.
- Keep theme strategy consistent (CSS variables vs Less-based) within a feature.

## Component Usage Standards

- Prefer documented component APIs over custom wrappers when a native NG-ZORRO option exists.
- For forms, pair NG-ZORRO controls with Angular Reactive/Signal Forms and explicit validation states (`nzStatus`, form errors).
- Use accessibility-friendly patterns: semantic labels, keyboard support, visible focus, and appropriate ARIA attributes.

## Recommended Documentation Source

When behavior is unclear, follow NG-ZORRO’s official docs first:

- https://ng.ant.design/llms.txt
- https://ng.ant.design/llms-full.txt
- Single component docs: `https://ng.ant.design/components/<component>.en.md`

Favor current, documented NG-ZORRO APIs and avoid deprecated patterns when equivalent modern APIs exist.
