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
- Mediator behavior order in `src/Application/DependencyInjection.cs` is significant: `LoggingBehaviour` → `UnhandledExceptionBehaviour` → `AuthorizationBehaviour` → `ValidationBehaviour` → `PerformanceBehaviour` → `CachingBehavior` → `CacheInvalidationBehavior`.
- Queries implement `ICacheableQuery`; commands implement `ICacheInvalidation`. Use HybridCache's tag invalidation with sensible expirations and both collection-level and entity-level tags.
- For new CQRS use cases, run the `ca-usecase` template from `src/Application`; install `Clean.Architecture.Solution.Template::10.8.0` if the template is unavailable. Complete slice-specific pagination/filter/sort/cache files manually when the template does not create them.
- Use `Guard.Against.*` for argument/configuration guards and preserve each project's existing `GlobalUsings.cs` and file-scoped namespace style.

## Data, auth, and client gotchas

- Persisted instants use UTC `DateTimeOffset` (see `docs/adr/0001-utc-datetimeoffset-for-persisted-instants.md`). Keep EF configuration in `src/Infrastructure/Data`; application handlers use `IApplicationDbContext` and `SaveChangesAsync`.
- Cookie auth requires explicit CORS origins with credentials; do not replace the configured allowlist with `AllowAnyOrigin()`. Antiforgery middleware is currently scaffolded but commented out, so do not assume it is enforced.
- Client dependencies are locked by `src/Client/package-lock.json`; use `npm ci` for a clean install. The real frontend is `src/Client` (not the old `src/Web/ClientApp` path). Angular-specific conventions are in `.github/copilot-instructions.md` and `.github/instructions/`.
- Do not hand-edit generated EF migration designer/snapshot files or other generated artifacts; update their source/configuration and regenerate using the repository's existing tooling.
