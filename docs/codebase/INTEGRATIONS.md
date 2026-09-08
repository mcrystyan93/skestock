# External Integrations

## Core Sections (Required)

### 1) Integration Inventory

| System | Type | Purpose | Auth model | Criticality | Evidence |
|--------|------|---------|------------|--------------|----------|
| SQL Server (containerized via Aspire, resource `Services.DatabaseServer`) | Relational DB | Primary datastore for all domain entities + ASP.NET Core Identity tables | Container-level password parameter (`sql-password`, Aspire secret parameter) | High | `src/AppHost/Program.cs`, `src/Infrastructure/Data/ApplicationDbContext.cs` |
| Redis (containerized via Aspire, resource `Services.Cache`) | Distributed cache | L2 backing store for `HybridCache` (read-through query caching + tag-based invalidation) | Container-level password parameter (`redis-password`) | Medium — caching is a performance optimization, not a correctness dependency (queries recompute on cache miss) | `src/AppHost/Program.cs`, `src/Infrastructure/DependencyInjection.cs` (`AddRedisDistributedCache`, `AddHybridCache`) |
| ASP.NET Core Identity (self-hosted, backed by the same SQL Server via EF Core) | Auth/identity provider | User accounts, Guid-keyed roles (`IdentityRole<Guid>`), cookie and bearer authentication | Application cookie is default; bearer tokens are also registered | High | `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/Identity/{ApplicationUser,IdentityService}.cs` |
| Azure Key Vault (optional) | Secrets/config source | Conditionally adds Key Vault as a configuration provider if configured | Azure Identity (`Azure.Identity` package) | Low/optional — `AddKeyVaultIfConfigured()` is conditional, exact trigger condition not independently re-verified in this pass | `src/Web/DependencyInjection.cs` (referenced in `Program.cs` as `AddKeyVaultIfConfigured()`) — `[TODO]` confirm exact activation condition by reading `Web/DependencyInjection.cs` in full |
| OpenTelemetry OTLP exporter | Observability/telemetry sink | Traces/metrics/logs export target (destination endpoint not hardcoded in repo — configured via standard OTEL env vars) | N/A | Medium | `src/ServiceDefaults/Extensions.cs`, `Directory.Packages.props` (`OpenTelemetry.Exporter.OpenTelemetryProtocol`) |
| Scalar (`/scalar`) | API documentation UI | Serves interactive OpenAPI reference in place of Swagger UI; also the Aspire dashboard's shortcut URL for the Web resource | N/A (dev/docs tooling) | Low | `src/Web/Program.cs` (`MapScalarApiReference()`), `src/AppHost/Program.cs` (`WithUrlForEndpoint`) |

### 2) Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|-------|------|---------------|----------|----------|
| SQL Server (`skestockDb`) | System of record for all Domain entities + Identity | `ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext`, accessed exclusively through the `IApplicationDbContext` interface from `Application` handlers | `src/Web/appsettings.json` contains a LocalDB fallback connection string that has no Redis equivalent — running `Web` standalone (outside Aspire) is a partially-broken configuration (see `STACK.md`) | `src/Infrastructure/Data/ApplicationDbContext.cs`, `src/Web/appsettings.json` |
| Redis | HybridCache L2 (distributed) + likely session/output-cache backing (not confirmed for the latter) | `HybridCache` injected into `CachingBehavior`/`CacheInvalidationBehavior`; feature-level tags declared per `Features/<Feature>/CacheConstants.cs` | Cache invalidation is tag-based and lazy (`RemoveByTagAsync` marks a watermark, doesn't physically evict) — a bug in tag assignment could serve stale data indefinitely until the sliding expiration elapses | `src/Application/Common/Behaviours/CachingBehavior.cs`, `src/Application/Common/Caching/*` |

### 3) Secrets and Credentials Handling

- Credential sources: Aspire secret **parameters** (`builder.AddParameter("sql-password", secret: true)`, `builder.AddParameter("redis-password", secret: true)` in `src/AppHost/Program.cs`) rather than plain environment variables or a committed secrets file — Aspire resolves these via its standard parameter-resolution mechanism (typically .NET user-secrets or environment variables at the AppHost level; exact resolution path not independently verified — `[TODO]`).
- Hardcoding check: no hardcoded passwords/API keys found in scanned source files; the one plaintext connection string in `src/Web/appsettings.json` (`skestockDb` LocalDB, `Trusted_Connection=True`) uses Windows Integrated Auth, not a stored password, and only applies to the LocalDB fallback path.
- Rotation/lifecycle notes: `[TODO]` — no rotation policy or Key Vault reference documented in-repo for the Aspire secret parameters; `AddKeyVaultIfConfigured()` suggests Key Vault is an optional/future secret source for non-local environments.

### 4) Reliability and Failure Behavior

- Retry/backoff behavior: document extraction HTTP clients configure standard resilience with two exponential-backoff retries, per-attempt and total timeouts, and a circuit breaker in `src/Infrastructure/DependencyInjection.cs`; ServiceDefaults also applies standard resilience defaults to HttpClient.
- Timeout policy: `FunctionalTestSetup.OneTimeSetUp` uses an explicit 90-second `CancellationTokenSource` when booting the test Aspire host and waiting for SQL/Redis health — no equivalent hard timeout was found for the production `AppHost` boot path.
- Circuit-breaker/fallback: Aspire's `WaitFor(databaseServer)`/`WaitFor(cache)` on the `webapi` resource (`src/AppHost/Program.cs`) delays the Web project's startup until SQL Server and Redis report healthy, rather than implementing a runtime circuit breaker — this is a startup-order guarantee, not a request-time fallback.

### 5) Observability for Integrations

- Logging around external calls: `ServiceDefaults.AddServiceDefaults()` wires OpenTelemetry instrumentation for ASP.NET Core, HTTP client, and .NET runtime metrics — this covers HTTP/DB/cache calls generically via OTEL auto-instrumentation rather than bespoke per-integration logging.
- Metrics/tracing coverage: OpenTelemetry `Instrumentation.AspNetCore`/`Instrumentation.Http`/`Instrumentation.Runtime` + OTLP exporter are all present as pinned dependencies, and `MapDefaultEndpoints()` (health/readiness endpoints) is called in `Web/Program.cs`.
- Missing visibility gaps: `[TODO]` — no EF Core-specific instrumentation package (e.g. `OpenTelemetry.Instrumentation.EntityFrameworkCore`) was found in `Directory.Packages.props`, so SQL query-level tracing may rely solely on ASP.NET Core span auto-instrumentation rather than per-query spans; confirm before relying on traces for DB performance debugging.

### 6) Evidence

- `src/AppHost/Program.cs` (resource graph, secret parameters, health-wait chain)
- `src/Infrastructure/DependencyInjection.cs` (SQL Server, Redis, HybridCache, Identity wiring)
- `src/ServiceDefaults/Extensions.cs` (OpenTelemetry/health checks/service discovery)
- `src/Web/appsettings.json` (LocalDB fallback connection string)
- `Directory.Packages.props` (pinned integration-related package versions)

## Extended Sections (Optional)

Not added — no per-endpoint external API catalog exists (this app has no outbound third-party API integrations beyond the datastore/cache/telemetry sinks documented above), and no auth-flow sequence diagram was requested.
