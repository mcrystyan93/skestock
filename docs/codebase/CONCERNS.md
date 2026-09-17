# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|-------------------|
| High | Antiforgery enforcement is still commented out while cookie authentication is enabled | `src/Web/Program.cs` (`// app.UseAntiforgeryValidation();`), `src/Web/DependencyInjection.cs` (`AddAntiforgery` commented), Angular already configures matching `XSRF-TOKEN`/`X-XSRF-TOKEN` names | State-changing cookie-authenticated requests are not protected by server-side antiforgery validation | Enable and test the configured XSRF cookie/header middleware before production |
| Medium | No CI gate for `dotnet test`/`npm test` on push or PR — the only GitHub Actions workflow (`deploy-production.yml`) is `workflow_dispatch`-only and deploy-focused | `.github/workflows/deploy-production.yml` | Regressions can merge undetected until manually run locally, despite substantial recent feature growth (28+ commits, 478 files changed since docs were last generated) | Add a CI workflow triggered on push/PR running `dotnet build` + `dotnet test` + `npm test`, given Docker/Podman is needed for full .NET test coverage |
| Medium | Worker queue-processing (`CategoryImportBatchQueueProcessingService`, `GoodsReceiptImportQueueProcessingService`, `ItemImportQueueProcessingService`) has no dedicated automated test coverage, and `TestAppHost` does not include Azurite/Worker, so functional tests never exercise the outbox→queue→worker path end-to-end | `tests/TestAppHost/Program.cs` (SQL Server + Redis only), no Worker-specific test project | The most operationally complex subsystem (async imports with retry/poison-queue logic) is the least tested | Add a Worker-focused test project or extend `TestAppHost` to include Azurite so functional tests can cover at least one import flow end-to-end |
| Medium | Outbox delivery is at-least-once and publisher rows are not claimed atomically | `src/Web/BackgroundJobs/OutboxPublisherService.cs` | A successful send can be resent if state persistence fails partway; correctness depends entirely on Worker-side `ProcessedMessages` idempotency checks being correct for every consumer | Add a focused test (or code review checklist item) verifying every new queue consumer performs the idempotency check before side effects |
| Low/Medium | `src/Web/appsettings.json` still contains a LocalDB connection-string fallback (`ConnectionStrings:skestockDb`) with no equivalent Azurite/Redis fallback | `src/Web/appsettings.json` | Running `dotnet run` on `Web` directly (bypassing `AppHost`) partially works (SQL via LocalDB) but silently fails on any Redis/Blob/Queue-dependent code path | Remove the stray connection string to force the Aspire-only path consistently, or document/complete the fallback — `[ASK USER]` (Q1) |
| Low | `tests/Domain.UnitTests` remains an empty project shell despite Domain growing significantly (import batches, stock visibility, etc.) | `tests/Domain.UnitTests/` (only `.csproj`) | Any Domain-layer invariants/behavior added to entities ship untested | Add tests as Domain gains behavior beyond plain data holders |
| Low | Command-verb naming remains inconsistent across feature slices (`Items`: Create/Edit/Disable/Enable; `Locations`/`SchoolClasses`/`Categories`: Create/Update; import flows: Create/Confirm/Process) | Direct listing of `src/Application/Features/*/Commands` | New slices copied from a "wrong" example perpetuate divergent naming | Document one canonical verb set for CRUD vs. one for async-import flows |

### 2) Technical Debt

| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|-----------|----------------|-------|-------------------|----------------|
| Dual error-handling mechanisms (`Result<T>`/`Error` vs. thrown exceptions caught by `ProblemDetailsExceptionHandler`) | Deliberate design choice, inherited partly from the Clean Architecture template's exception-based validation flow | `src/Application/Common/{Errors,Exceptions}/*`, `src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs` | Feature slices may inconsistently pick one mechanism, fragmenting the API contract | Document explicit decision criteria; add a code-review checklist item |
| No domain-layer test coverage | `tests/Domain.UnitTests` was scaffolded but never populated, even as Domain complexity grew (import batches, stock visibility, queue contracts) | `tests/Domain.UnitTests/` | Domain invariants ship untested | Prioritize once Domain gains behavior beyond data holders — has grown in urgency since the last pass given new entities |
| No Worker-specific automated tests | Worker/queue subsystem was added after the initial Application/Web-only test setup, and `TestAppHost` was never extended to include Azurite | `src/Worker/Queues/*.cs`, `tests/TestAppHost/Program.cs` | The transactional-outbox → queue → worker pipeline (the most operationally risky part of the system) has the least safety net | Extend `TestAppHost` with Azurite emulator resources, or add a dedicated Worker test project |
| Previously-flagged empty `src/Application/Models/` folder | Was flagged as stale scaffold debris in the prior documentation pass | — | **Resolved** — folder no longer exists (confirmed this pass) | None — closed |
| Previously-flagged `StockTransaction.UserId`/`User` commented out | Was flagged as an incomplete entity in the prior documentation pass | `src/Domain/Entities/StockTransaction.cs` | **Resolved** — `UserId`/`User` are now active, uncommented fields (confirmed this pass) | None — closed |
| Previously-flagged missing feature slices for `ClassBalance`/`StockBatch`/`StockTransaction` | Was flagged as domain-ahead-of-application-layer in the prior pass | — | **Resolved** — `Stock` and `StockBatches` feature slices now exist in `src/Application/Features/` with corresponding Web endpoints and tests | None — closed |

### 3) Security Concerns

| Risk | OWASP category | Evidence | Current mitigation | Gap |
|------|------------------|----------|----------------------|-----|
| Antiforgery enforcement disabled | A01:2021 Broken Access Control | `src/Web/Program.cs`, `src/Web/DependencyInjection.cs` | Angular configures matching `XSRF-TOKEN`/`X-XSRF-TOKEN` names | Server registration and middleware are commented out |
| Secrets handling relies on Aspire parameter resolution, not independently verified | A02:2021 Cryptographic Failures / secrets management | `src/AppHost/Program.cs` (`AddParameter(..., secret: true)`) | Aspire's parameter model keeps secrets out of source | `[TODO]` verify exact resolution source (user-secrets vs. env var) |
| No rate limiting / anti-automation controls found on any endpoint | A04:2021 Insecure Design | `src/Web/Program.cs`, `src/Web/Endpoints/*.cs` | None currently found | Consider ASP.NET Core's built-in rate limiting middleware, especially on `Users` (auth) endpoints |
| No security scanning config detected (Dependabot, Snyk, SECURITY.md) | N/A | No such files under repository root or `.github/` | None | Add Dependabot config at minimum for NuGet/npm dependency alerts |
| Document extraction sends uploaded content to a third-party OpenAI-backed API | A08:2021 Data Integrity Failures / data handling | `src/Infrastructure/AI/OpenAiDocumentExtractionClient.cs` | `[TODO]` — not verified whether uploaded documents may contain sensitive student/school data before being sent externally | Confirm data-handling policy for documents sent to the extraction API, especially if they may contain PII |

### 4) Performance and Scaling Concerns

| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---------|----------|--------------------|-----------------|--------------------------|
| Tag-based HybridCache invalidation is coarse per feature | `src/Application/Common/Behaviours/CacheInvalidationBehavior.cs`, per-feature `CacheConstants.cs` | Every write to a feature invalidates all cached "get-all" pages/filters/sorts for that feature | Under high write-throughput, cache hit rate for `GetAll` queries could drop | Accepted, documented tradeoff for correctness simplicity — revisit only if profiling shows a bottleneck |
| No EF Core-specific OpenTelemetry instrumentation package found | `Directory.Packages.props` | DB query-level latency may not appear as distinct spans | Harder to diagnose slow-query performance in production | Add `OpenTelemetry.Instrumentation.EntityFrameworkCore` if DB visibility becomes a need |
| SignalR Redis backplane adds a hop for every realtime message once scaled beyond one instance | `src/Infrastructure/DependencyInjection.cs` | Not yet observed as a bottleneck (single-instance dev/deploy today) | Message fan-out cost grows with connected clients + instance count | Monitor once multi-instance production deployment is in place |

### 5) Fragile/High-Churn Areas

Git history is now available (28+ commits since the prior documentation pass; 478 files, +26,692/-2,040 lines changed in `src/`). Highest-churn application/source files in the last 90 days (excluding generated `graphify-out/`/`.idea/` artifacts):

- `src/Client/src/app/app.config.ts` (13 changes) — central Angular app configuration; frequent churn suggests it's still absorbing new cross-cutting concerns (SignalR, locale, interceptors)
- `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs` (12) — seed data keeps growing alongside new reference data (e.g. new default locations)
- `src/Client/tsconfig.json` (12) — path-alias/config churn alongside feature growth
- `src/Client/src/app/features/categories/list/categories.page.ts` (11) — actively evolving UI
- `tests/Application.UnitTests/Features/SchoolClasses/Queries/GetSchoolClassSummaryHandlerTests.cs` (11) — this handler/its tests have been repeatedly revised, suggesting the summary calculation logic has been unstable
- `src/Client/src/app/features/school-classes/overview/header/header.html`, `src/Client/src/app/core/models/index.ts` (10 each)

Structurally (not just churn), the most likely fragile areas remain:
- `src/Application/Common/{Keyset,Filtering,Caching}/*` — most abstract, most-reused cross-cutting code
- `src/Application/Common/Behaviours/*` — pipeline order is load-bearing
- `src/Domain/Queues/*` + `src/Web/BackgroundJobs/OutboxPublisherService.cs` + `src/Worker/Queues/*` — the outbox/queue subsystem now spans three projects with an at-least-once delivery contract that every new consumer must respect

### 6) `[ASK USER]` Questions

1. **[ASK USER]** Is the LocalDB connection-string fallback in `src/Web/appsettings.json` intentional (e.g. for a non-Aspire dev/debug path), or is it stale leftover that should be removed to force the Aspire-only path consistently?
2. **[ASK USER]** Should a CI workflow (build + test on push/PR) be added now, given the significant recent feature growth and the current absence of any automated gate beyond the manual `workflow_dispatch` production deploy?
3. **[ASK USER]** Should Worker/queue-processing test coverage (e.g. extending `TestAppHost` with Azurite, or a dedicated Worker test project) be prioritized, given it's currently the least-tested subsystem carrying the most operational risk (async imports)?

### 7) Evidence

- `.github/workflows/deploy-production.yml` (only CI/CD config; deploy-only, `workflow_dispatch`)
- `src/Web/Program.cs`, `src/Web/appsettings.json`, `src/Web/DependencyInjection.cs`
- `src/Domain/Entities/StockTransaction.cs` (confirmed `UserId`/`User` resolved), absence of `src/Application/Models/` (confirmed resolved)
- `src/Application/Features/{Stock,StockBatches}/*` (confirmed feature slices now exist)
- `git log --since="90 days ago" --name-only` churn analysis
- `src/Worker/Queues/*.cs`, `tests/TestAppHost/Program.cs`

## Extended Sections (Optional)

Not added — no full bug inventory, cost/effort estimates, or issue-tracker data available in-repo.
