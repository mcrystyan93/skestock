# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|-------------------|
| Medium | `NotFoundException` (from `Ardalis.GuardClauses`) is referenced by `ProblemDetailsExceptionHandler` but never thrown by any current handler — dead branch, and a second "not found" mechanism exists in parallel to the `Result<T>`/`Error` pattern that every implemented feature actually uses | `grep -rn "NotFoundException" src/` shows only the handler reference; `grep` for `Guard.Against.NotFound(` / `throw new NotFoundException` returns zero handler hits | A future contributor could throw `NotFoundException` instead of returning a typed `Error`, silently losing the `ApiErrorContract`/`Code` metadata the (planned) frontend depends on for localized error copy | Either remove the dead `NotFoundException` branch and standardize fully on `Result<T>`+`Error` for "not found", or explicitly document when to use each (currently undocumented which to prefer) |
| Medium | Permissive CORS policy (`AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin()`) applied unconditionally in `src/Web/Program.cs`, not gated to Development only | `src/Web/Program.cs` lines calling `app.UseCors(...)` | Any origin can call the API cross-origin in any environment; combined with bearer-token auth (no cookies) the credential-theft risk is lower, but this is still broader than typical production hardening | Scope CORS to environment-specific allowed origins before any non-local deployment; gate the wildcard policy to `Development` only |
| Low/Medium | `src/Web/appsettings.json` contains a LocalDB connection-string fallback (`ConnectionStrings:skestockDb`) that contradicts the documented assumption ("no local fallback connection strings exist") in `AGENTS.md`/`.github/copilot-instructions.md`, and has no matching Redis fallback | `src/Web/appsettings.json` | Running `dotnet run` on `Web` directly (bypassing `AppHost`) will partially work (SQL via LocalDB) but silently fail on any Redis-dependent code path (HybridCache falls back to L1-only, or throws depending on configuration) — a confusing half-working state for a new contributor | Either remove the stray connection string to force the Aspire-only path consistently, or add an equivalent documented Redis fallback and update `AGENTS.md`/`copilot-instructions.md` to reflect the corrected reality — `[ASK USER]` (see Q1 below) |
| Low | Command-verb naming is inconsistent across the four implemented feature slices (`Items`: Create/Edit/Disable/Enable; `Locations`/`SchoolClasses`: Create/Update; `Categories`: Create only) | Direct file listing of `src/Application/Features/{Categories,Items,Locations,SchoolClasses}` | New slices copied from the "wrong" example will perpetuate divergent naming, making the API less predictable for future frontend/client-generation work | Pick one canonical verb (`Update` is more common industry convention) and document it as the required pattern for new slices; consider a follow-up rename pass |
| Low | `src/Application/Models/` (top-level, distinct from `Common/Models/`) is an empty directory | `ls -la src/Application/Models/` — zero files | Likely a stale scaffold leftover; risks confusing a contributor about where feature-agnostic Application models belong | Delete the empty folder, or clarify its intended purpose if one exists — `[ASK USER]` (see Q2 below) |
| Low | No CI/CD pipeline exists in the repo (confirmed by scan: "No CI/CD pipelines detected") | `docs/codebase/.codebase-scan.txt` "CI/CD PIPELINES" section | No automated build/test/lint gate on changes — regressions can merge undetected until manually run locally | Add a GitHub Actions workflow (or equivalent) running `dotnet build` + `dotnet test` at minimum, given Docker/Podman is needed for full test coverage |

### 2) Technical Debt

| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|-----------|----------------|-------|-------------------|----------------|
| Dual error-handling mechanisms (`Result<T>`/`Error` vs. thrown exceptions caught by `ProblemDetailsExceptionHandler`) | Deliberate design choice per `.github/copilot-instructions.md` ("intentionally separate"), inherited partly from the Clean Architecture template's exception-based validation flow (`ValidationBehaviour` still throws `ValidationException` rather than returning a `Result`) | `src/Application/Common/Errors/*`, `src/Application/Common/Exceptions/*`, `src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs` | Feature slices may inconsistently pick one mechanism over the other (as already seen — `Categories` has no update/delete path to demonstrate either), fragmenting the API contract the frontend needs to consume uniformly | Document explicit decision criteria (already partially done) and add a code-review checklist item; consider migrating `ValidationBehaviour` to a `Result`-returning form long-term for full consistency |
| No domain-layer test coverage | `tests/Domain.UnitTests` project was scaffolded but never populated | `tests/Domain.UnitTests/` (only `.csproj` present) | Any future Domain logic (e.g. validation invariants added to entities) ships untested | Add tests as Domain gains behavior beyond plain data holders; currently low urgency since Domain entities are mostly anemic POCOs |
| `ClassBalance`, `StockBatch`, `StockTransaction` entities exist with no corresponding Application feature slice, Web endpoints, or tests | Domain modeling appears to be ahead of Application/Web implementation — these three entities represent the core "stock movement" business logic (the actual inventory tracking) but aren't exposed via any API yet | `src/Domain/Entities/{ClassBalance,StockBatch,StockTransaction}.cs` — no matching `src/Application/Features/*` folder | The most business-critical part of the domain (stock transactions/balances) is currently unreachable via the API — `Categories`/`Items`/`Locations`/`SchoolClasses` are largely supporting/reference data | Prioritize scaffolding these three feature slices next, following the `Items` slice as the closest template (richest command set) |
| `StockTransaction.Type`/commented-out `UserId`/`User` fields | Incomplete entity — `// public Guid UserId { get; set; }` / `// public User User { get; set; } = null!;` are commented out | `src/Domain/Entities/StockTransaction.cs` lines 20-21 | Stock transactions currently cannot be attributed to a specific acting user beyond the generic `BaseAuditableEntity.CreatedBy`/`LastModifiedBy` audit fields | Decide whether `CreatedBy` (already present via `BaseAuditableEntity`) is sufficient, or whether a dedicated actor field is still needed — `[ASK USER]` (see Q3 below) |

### 3) Security Concerns

| Risk | OWASP category | Evidence | Current mitigation | Gap |
|------|------------------|----------|----------------------|-----|
| Wildcard CORS in all environments | A05:2021 Security Misconfiguration | `src/Web/Program.cs` (`AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin()`) | Bearer-token auth (no cookies) reduces CSRF-style exploitation, and no HttpOnly cookie is at risk of exfiltration via CORS | No environment-specific origin allowlist exists |
| Secrets handling relies on Aspire parameter resolution, not independently verified | A02:2021 Cryptographic Failures / secrets management | `src/AppHost/Program.cs` (`AddParameter(..., secret: true)`) | Aspire's parameter model is designed for exactly this (keeps secrets out of source), but this repo's exact resolution source (user-secrets vs. env var) wasn't independently confirmed | `[TODO]` — verify via `dotnet user-secrets list --project src/AppHost` or documented onboarding steps |
| No rate limiting / anti-automation controls found on any endpoint | A04:2021 Insecure Design | `src/Web/Program.cs`, `src/Web/Endpoints/*.cs` — no `AddRateLimiter`/rate-limit middleware found | None currently | Consider ASP.NET Core's built-in rate limiting middleware, especially on `Users` (auth) endpoints |
| No security scanning config detected (Dependabot, Snyk, SECURITY.md) | N/A | `docs/codebase/.codebase-scan.txt` "SECURITY & COMPLIANCE" section: "No security configs detected" | None | Add Dependabot config at minimum for NuGet dependency alerts, given no CI/CD exists yet either |

### 4) Performance and Scaling Concerns

| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---------|----------|--------------------|-----------------|--------------------------|
| Tag-based HybridCache invalidation is coarse per feature (e.g. `CreateCategoryCommand` invalidates the *entire* category-list tag) | `src/Application/Common/Behaviours/CacheInvalidationBehavior.cs`, per-feature `CacheConstants.cs` | Every write to a feature invalidates all cached "get-all" pages/filters/sorts for that feature, not just the affected page | Under high write-throughput on a given feature, cache hit rate for `GetAll` queries could drop significantly, increasing DB load | This is an accepted, documented tradeoff (per `.github/copilot-instructions.md`) for correctness simplicity — only revisit if profiling shows it's a bottleneck |
| No EF Core-specific OpenTelemetry instrumentation package found | `Directory.Packages.props` has ASP.NET Core/HTTP/Runtime OTEL instrumentation but not an EF Core-specific one | DB query-level latency may not appear as distinct spans in traces | Harder to diagnose slow-query performance issues in production without per-query tracing | Add `OpenTelemetry.Instrumentation.EntityFrameworkCore` (or equivalent) if DB performance visibility becomes a need |
| Keyset pagination fetches `pageSize + 1` rows per query (standard technique) | `src/Application/Features/Categories/Queries/GetAllCategories/GetAllCategoriesHandler.cs` | Expected/acceptable overhead of one extra row per page — not a concern by itself | None significant | No action needed — flagged here only for completeness since it's a reused pattern across all `GetAll*` queries |

### 5) Fragile/High-Churn Areas

- No git history is available in this environment (`git log` / churn analysis returned "No commits found" / "None found" per the scan — this repository is not currently tracked as a git repo, or history is unavailable), so churn-based fragility signals **cannot be computed**. `[TODO]` re-run this analysis once git history is available.
- Based on structural complexity alone (not churn), the most likely fragile areas are:
  - `src/Application/Common/{Keyset,Filtering,Caching}/*` — the most abstract, most-reused cross-cutting code; a bug here silently affects every `GetAll<Feature>` query across all 4 slices.
  - `src/Application/Common/Behaviours/*` — pipeline order is load-bearing and easy to break silently (see `AGENTS.md`/`copilot-instructions.md` warnings about behaviour registration order).

### 6) `[ASK USER]` Questions

1. **[ASK USER]** Is the LocalDB connection-string fallback in `src/Web/appsettings.json` (`ConnectionStrings:skestockDb`) intentional (e.g. for a non-Aspire dev/debug path), or is it stale leftover from the Clean Architecture template generation that should be removed to force the Aspire-only path consistently?
2. **[ASK USER]** Is `src/Application/Models/` (the empty top-level folder, distinct from `Common/Models/`) intended for some future cross-feature model type, or is it safe to delete as scaffold debris?
3. **[ASK USER]** For `StockTransaction`, is the commented-out `UserId`/`User` field intentionally deferred (relying on `BaseAuditableEntity.CreatedBy` for attribution), or is a dedicated "acting user" field still planned as part of implementing the stock-transaction feature slice?
4. **[ASK USER]** Should `ClassBalance`, `StockBatch`, and `StockTransaction` (the core inventory-movement entities with no feature slice yet) be prioritized next, and if so, which existing slice (`Items` vs. `Locations`/`SchoolClasses`) should be the naming/structure template for their commands?
5. **[ASK USER]** Is the permissive CORS policy (`AllowAnyOrigin`) intentional for the current development/demo phase only, with a plan to restrict it before any production deployment, or does it need addressing now?

### 7) Evidence

- `docs/codebase/.codebase-scan.txt` (CI/CD, security, containers, git-history sections all reporting "none found")
- `src/Web/Program.cs`, `src/Web/appsettings.json`
- `src/Application/Common/Exceptions/*`, `src/Application/Common/Errors/*`, `src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs`
- `src/Domain/Entities/{ClassBalance,StockBatch,StockTransaction}.cs`
- `src/Application/Models/` (empty directory)
- Direct feature-slice file listings under `src/Application/Features/*`

## Extended Sections (Optional)

Not added — no full bug inventory, cost/effort estimates, or dependency-ownership mapping were requested or derivable without issue-tracker/ticketing data (none found in-repo).
