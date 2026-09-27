# Spec: Observability hardening

Status: **Draft — awaiting approval**

## Objective

Fix six confirmed logging/telemetry defects so that logs are safe and cheap, request timings are
correct, one user action produces one connected trace across Web → Azure Queue → Worker, and
production telemetry is actually collected.

Consumers: developers/operators diagnosing issues locally (Aspire dashboard) and on the Fedora
production host.

| # | Area | Defect today | Required outcome |
|---|------|--------------|------------------|
| 1 | Request logging | `LoggingBehaviour`, `PerformanceBehaviour`, `UnhandledExceptionBehaviour` log `{@Request}`. MEL ignores `@` and stringifies the whole record, including base64 import payloads (up to ~146 MB) and personal data. | Request objects are never logged. Only request type name, `UserId`, elapsed ms (perf) and the exception (unhandled). |
| 2 | Request logging | Logging and Performance behaviours call `IIdentityService.GetUserNameAsync` → an extra DB query per Mediator request. | Logging and Performance behaviours do not depend on `IIdentityService`; no DB access for logging. |
| 3 | Request logging | `PerformanceBehaviour` keeps a `Stopwatch` field, calls `Start()` without reset; with scoped Mediator the same instance accumulates time across sends in a scope → false "long running" warnings. | Elapsed time is measured per invocation (local `Stopwatch.GetTimestamp()`/`GetElapsedTime`). Threshold stays 500 ms. |
| 4 | Trace propagation | `OutboxMessage`/`MessageEnvelope` carry no W3C trace context, so Worker processing starts a disconnected root trace. | Trace context is captured when the outbox row is added, carried in the envelope, and restored in the Worker as the parent of a Consumer span. |
| 5 | Production export | No `OTEL_EXPORTER_OTLP_ENDPOINT` in production → telemetry only goes to journald console. | A standalone Aspire Dashboard container receives OTLP from Web and Worker; UI reachable only on `127.0.0.1` (SSH tunnel). |
| 6 | Log noise | Worker `appsettings.json` lacks a `Microsoft` override → every EF SQL command is logged at Information, duplicating SQL spans. | Worker log levels match Web (`Microsoft: Warning`, `Microsoft.Hosting.Lifetime: Information`). |

## Assumptions (confirmed)

- No new NuGet packages; use `System.Diagnostics.ActivitySource`/`ActivityContext` and
  `Microsoft.Extensions.Logging` source-generated `[LoggerMessage]`.
- DB schema change approved: two nullable columns on `OutboxMessages`.
- Production backend: `mcr.microsoft.com/dotnet/aspire-dashboard` (in-memory, no persistence).
- Dashboard UI bound to `127.0.0.1` only; OTLP endpoint not published to the host, reachable only
  on the `skestock` Podman network.
- Messages already in flight without trace context remain valid and are processed as new root
  traces (backward compatible JSON: missing properties deserialize to `null`).

## Design

### A. Request logging (1–3)

- `LoggingBehaviour<TRequest,TResponse>(ILogger<TRequest>, IUser)` — logs at **Debug** (was Information):
  `"Handling {RequestName} for user {UserId}"`.
- `PerformanceBehaviour<TRequest,TResponse>(ILogger<TRequest>, IUser)` — local timestamp,
  Warning when elapsed > 500 ms: `"Long running request {RequestName} ({ElapsedMilliseconds} ms) for user {UserId}"`.
- `UnhandledExceptionBehaviour` — Error with exception: `"Unhandled exception for request {RequestName}"`.
- Log statements are `[LoggerMessage]` source-generated methods in one `internal static partial`
  class in `Application/Common/Behaviours` (stable event IDs, no boxing/allocation when disabled).
- Pipeline order in `Application/DependencyInjection.cs` is unchanged.

### B. Trace propagation (4)

- `OutboxMessage` gains `string? TraceParent` (max 55) and `string? TraceState` (max 512);
  configured in `OutboxMessageConfiguration`; new EF migration `AddOutboxTraceContext`.
- `MessageEnvelope` gains the same two nullable properties.
- `Application/Queues/MessagingTelemetry` (static): `ActivitySource` named `skestock.Messaging`
  plus helpers to capture `Activity.Current` (`Id`, `TraceStateString`) and to parse it back
  with `ActivityContext.TryParse`.
- Capture point: an Infrastructure `SaveChangesInterceptor` stamps every `Added` `OutboxMessage`
  whose `TraceParent` is null. The three creating handlers stay untouched.
- `OutboxPublisherService` copies both fields into the envelope and wraps each send in a
  `Producer` activity `"publish {queueName}"` parented to the stored context, so the Azure SDK
  send span nests under the original request trace.
- `QueueMessageProcessor`, after decoding, starts a `Consumer` activity `"process {queueName}"`
  parented to the envelope context (root when absent/invalid), with tags
  `messaging.message.id`, `messaging.destination.name`, and sets `ActivityStatusCode.Error` on
  failure/permanent results. Mediator, EF and SQL spans become its children.
- `ServiceDefaults` tracing adds `.AddSource("skestock.*")`.

### C. Production export (5)

- New `deploy/quadlet/skestock-dashboard.container`:
  - image `mcr.microsoft.com/dotnet/aspire-dashboard` pinned to the Aspire 13.x tag,
  - `Network=skestock.network`, `ContainerName=skestock-dashboard`,
  - `PublishPort=127.0.0.1:18888:18888` (UI); OTLP `18889` not published,
  - `DASHBOARD__OTLP__AUTHMODE=Unsecured` (network-internal only),
    `DASHBOARD__FRONTEND__AUTHMODE=BrowserToken` (login token visible in `journalctl`).
- Web and Worker units: `Wants=`/`After=skestock-dashboard.service` (telemetry outage never
  blocks the app) and `Environment=OTEL_SERVICE_NAME=webapi|worker`.
- Rendered production env (workflow + `production.env.example`) adds
  `OTEL_EXPORTER_OTLP_ENDPOINT=http://skestock-dashboard:18889`.
- `deploy/deploy.sh` enables/starts/checks `skestock-dashboard.service`.
- `deploy/README.md` documents `ssh -L 18888:127.0.0.1:18888 <host>` and the token lookup.

### D. Log noise (6)

- `src/Worker/appsettings.json` → add `"Microsoft": "Warning"`.

## Commands

```bash
dotnet build
dotnet test tests/Application.UnitTests
dotnet test tests/Worker.UnitTests
dotnet ef migrations add AddOutboxTraceContext --project src/Infrastructure \
  --startup-project src/Web --context ApplicationDbContext --output-dir Data/Migrations
dotnet ef migrations has-pending-model-changes --project src/Infrastructure \
  --startup-project src/Web --context ApplicationDbContext
./run-functional-tests.sh        # optional, container-backed regression check
dotnet run --project src/AppHost # manual trace verification in dashboard
```

## Project Structure (touched)

```
src/Application/Common/Behaviours/       Logging/Performance/UnhandledException + log messages
src/Application/Queues/                  MessagingTelemetry
src/Domain/Queues/                       OutboxMessage, MessageEnvelope
src/Infrastructure/Data/                 configuration, interceptor, generated migration, DI
src/Web/BackgroundJobs/                  OutboxPublisherService
src/Worker/Queues/, src/Worker/appsettings.json
src/ServiceDefaults/Extensions.cs
deploy/quadlet/, deploy/deploy.sh, deploy/README.md, deploy/production.env.example
.github/workflows/deploy-production.yml
tests/Application.UnitTests/Common/Behaviours/, tests/Worker.UnitTests/Queues/
```

## Code Style

```csharp
internal static partial class RequestLogMessages
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Debug,
        Message = "Handling {RequestName} for user {UserId}")]
    public static partial void HandlingRequest(this ILogger logger, string requestName, Guid? userId);
}

var startedAt = Stopwatch.GetTimestamp();
var response = await next(request, cancellationToken);
var elapsed = Stopwatch.GetElapsedTime(startedAt);
```

File-scoped namespaces, primary constructors, `Guard.Against.*`, existing `GlobalUsings.cs`,
warnings-as-errors respected.

## Testing Strategy

NUnit + Shouldly + Moq, mirrored paths.

- `RequestLoggerTests` (rewrite): never logs request payload (capture log state; assert no
  property value is the request object and no payload text appears); no `IIdentityService`
  dependency exists.
- New `PerformanceBehaviourTests`: two sequential sends on one instance where each is fast do not
  warn (regression for #3); a slow handler (>500 ms via delay) warns once with elapsed ≥ 500.
- New `UnhandledExceptionBehaviourTests`: rethrows, logs Error with exception, no payload.
- `MessageEnvelopeSerializer` tests: round-trip with trace fields; legacy JSON without them
  deserializes with nulls.
- Worker `QueueMessageProcessor` tests with an `ActivityListener`: Consumer activity has the
  envelope's `TraceId` as parent; missing/invalid context → new root, processing still succeeds.
- Outbox interceptor test: added `OutboxMessage` under an active `Activity` gets `TraceParent`;
  explicit value is preserved.
- Manual: AppHost run, create a goods-receipt import, dashboard shows one trace spanning
  `webapi` HTTP → publish → `worker` process → SQL.

## Boundaries

- Always: keep Mediator behaviour order; regenerate (never hand-edit) migration designer/snapshot;
  keep delivery at-least-once and idempotency semantics unchanged; run targeted tests.
- Ask first: adding NuGet packages; exposing the dashboard beyond localhost; changing log levels
  other than the Worker `Microsoft` override; touching CI beyond the env render + deploy script.
- Never: log request bodies, file contents, usernames, or secrets; make Web/Worker depend on the
  dashboard being up; publish the OTLP port to the host.

## Success Criteria

1. `grep -rn '{@' src --include=*.cs` returns nothing; Logging/Performance behaviours do not take `IIdentityService`.
2. New/updated unit tests pass; the #3 regression test fails on the old implementation.
3. `has-pending-model-changes` reports none after the migration.
4. Locally, one import produces one trace containing both `webapi` and `worker` spans.
5. Legacy envelopes (no trace fields) are processed successfully.
6. Production: after deploy, `skestock-dashboard.service` is active, UI answers on
   `127.0.0.1:18888` on the host only, and shows logs/traces from `webapi` and `worker`.
7. Worker no longer emits EF `Executed DbCommand` Information logs.
8. `dotnet build` clean (warnings-as-errors).
9. Per-request "Handling" logs are emitted at Debug and are absent at the default Information level.

## Open Questions

None. (Resolved: "Handling {RequestName}" is demoted to Debug.)
