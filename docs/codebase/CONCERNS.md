# Codebase Concerns

## Core Sections

### 1) Top Risks

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|------------------|
| High | Cookie-authenticated API has no active antiforgery enforcement | `src/Web/DependencyInjection.cs`, `src/Web/Program.cs`, fully commented `src/Web/Endpoints/Antiforgery.cs` | State-changing browser requests lack server-side CSRF validation | Re-enable/configure antiforgery and add functional coverage before public exposure |
| High | Identity exposes public Identity API registration and no feature request currently uses a role-bearing `[Authorize]` attribute | `src/Web/Endpoints/Users.cs`, `src/Application/Common/Behaviours/AuthorizationBehaviour.cs` | Authentication exists, but privilege separation is not currently evident | Decide whether registration is internal-only; apply roles/policies to privileged commands |
| Medium | No push/PR CI gate; deployment workflow is `workflow_dispatch` only | `.github/workflows/deploy-production.yml` | Regressions can merge without automated validation | Add a normal CI workflow for build, .NET tests, and client tests |
| Medium | Outbox/queue delivery remains at-least-once | `src/Web/BackgroundJobs/OutboxPublisherService.cs`, `src/Worker/Queues`, `src/Domain/Queues` | Duplicate external effects are possible | Keep consumers idempotent and test every new queue handler with duplicate delivery |
| Medium | Daily purchase-statistics errors do not fail the scheduled daily-consumption run | `src/Worker/Statistics/DailyStatisticsJob.cs` | A successful scheduled run does not prove purchase statistics were refreshed | Monitor error logs and decide whether the secondary work needs its own retry/run state |
| Low | Domain test project is empty | `tests/Domain.UnitTests` | Domain invariants can regress without direct tests | Add domain tests as behavior moves into entities/value objects |

### 2) Technical Debt

| Debt | Where | Risk | Suggested fix |
|------|-------|------|---------------|
| Two HTTP error mechanisms (`Result<T>` and thrown exceptions) | `src/Application/Common/Errors`, `src/Application/Common/Exceptions`, `src/Web/Infrastructure` | New features may expose inconsistent API contracts | Prefer typed `Result` errors for expected business failures; reserve exceptions for cross-cutting failures |
| Feature command naming varies (`Edit`, `Update`, lifecycle verbs) | `src/Application/Features/*/Commands` | New slices can copy the wrong convention | Inspect the nearest slice and document the selected verb in the feature |
| Generated graph and build/client artifacts can dominate repository searches | `graphify-out`, `src/Client/graphify-out`, `src/Client/dist`, `.angular`, `bin`, `obj` | Agents may infer conventions from generated files | Exclude generated output from searches and edits |
| Stale instruction text exists under `src/Client/.github` and path globs mention `src/Web/ClientApp` | `.github/instructions/*`, `src/Client/.github/copilot-instructions.md` | Agents may incorrectly conclude the frontend is absent | Apply Angular rules to the actual `src/Client` project and treat source/config as truth |

### 3) Security Concerns

| Risk | Evidence | Current mitigation | Gap |
|------|----------|--------------------|-----|
| No active antiforgery middleware | `src/Web/Program.cs`, `src/Web/DependencyInjection.cs` | Angular names XSRF cookie/header | Server registration/endpoint/middleware are commented |
| Registration/role policy intent is unclear | `src/Web/Endpoints/Users.cs`, `src/Application/Common/Security` | Endpoint groups default to authorization; role plumbing exists | No role-bearing feature requests found; confirm deployment exposure |
| OpenAI receives uploaded document content | `src/Infrastructure/AI/OpenAiDocumentExtractionClient.cs` | API key configuration and HTTP resilience | Confirm PII/data-retention policy before using school documents |
| No dependency scanning policy was found | `.github/` and workflow inventory | Central versions and lockfiles | Add Dependabot or equivalent if required |

### 4) Performance and Scaling Concerns

| Concern | Evidence | Scaling risk | Suggested improvement |
|---------|----------|--------------|-----------------------|
| Feature-wide cache tags invalidate many list variants | `src/Application/Features/*/CacheConstants.cs`, `src/Application/Common/Behaviours/CacheInvalidationBehavior.cs` | [TODO] Cache-hit impact under write load is not measured | Profile first; add narrower tags only when justified |
| Redis is both cache/backplane/lock | `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/Distributed` | One Redis outage affects several concerns | Monitor separately and define degradation behavior |
| SignalR Redis backplane fan-out grows with instances/clients | `src/Infrastructure/Realtime`, `src/Infrastructure/DependencyInjection.cs` | Message volume scales with connected clients | Add metrics and load-test before multi-instance scale-out |

### 5) Fragile/High-Churn Areas

| Area | Evidence | Safe strategy |
|------|----------|---------------|
| `src/Client/src/app/features/school-classes/overview/header/header.html` (18 changes), `src/Client/tsconfig.json` (16) | `git log --since='90 days' --pretty=format: --name-only -- src tests .github/workflows` | Keep markup and alias changes focused and run client build/tests |
| `src/Infrastructure/Data/ApplicationDbContextInitialiser.cs` (17) | `git log --since='90 days' --pretty=format: --name-only -- src tests .github/workflows`; 808 handwritten lines and seed responsibility | Test migrations/seed changes against a disposable database |
| `src/Client/src/app/shared/items/ui/autocomplete/item-autocomplete.ts` (563 lines) | Source file length; client input, menu, and selection logic share one component | Refactor only with focused component behavior tests |
| `src/Application/Common/{Behaviours,Filtering,Keyset,Caching}` | Shared by many features | Add focused unit tests before refactoring |
| Outbox/Worker queue path | `src/Web/BackgroundJobs`, `src/Domain/Queues`, `src/Worker/Queues` | Preserve idempotency, cancellation, poison behavior; test duplicate/failure paths |

### 6) `[ASK USER]` Questions

1. [ASK USER] Should public Identity registration remain enabled for the current deployment model?
2. [ASK USER] Should a push/PR CI workflow be added separately from the manual production deployment workflow?
3. [ASK USER] Should purchase-statistics materialization failures cause a scheduled-job retry or remain log-only secondary work?

### 7) Evidence

- `src/Web/Program.cs`, `src/Web/DependencyInjection.cs`, `src/Web/Endpoints/Users.cs`, `src/Web/Endpoints/Antiforgery.cs`
- `src/Application/Common/Behaviours/AuthorizationBehaviour.cs`
- `src/AppHost/Program.cs`, `src/Web/appsettings.json`
- `src/Web/BackgroundJobs/OutboxPublisherService.cs`, `src/Worker/Queues`, `src/Domain/Queues`
- `src/Worker/Statistics/DailyStatisticsJob.cs`, `src/Client/src/app/shared/items/ui/autocomplete/item-autocomplete.ts`
- `tests/Domain.UnitTests`, `tests/TestAppHost/Program.cs`
- `.github/workflows/deploy-production.yml`
- `git log --since='90 days' --pretty=format: --name-only -- src tests .github/workflows` (as of 2026-09-27; excludes generated migrations)
