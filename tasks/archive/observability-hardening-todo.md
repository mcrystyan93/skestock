# Tasks: Observability hardening

Spec: `docs/specs/observability-hardening.md` · Plan: `tasks/plan.md`

## Task 1: Safe, cheap, correct request logging (spec A, #1–3)

**Acceptance:**
- [x] `LoggingBehaviour`, `PerformanceBehaviour`, `UnhandledExceptionBehaviour` never log the
      request object; `grep -rn '{@' src --include=*.cs` is empty.
- [x] Logging and Performance behaviours do not depend on `IIdentityService`.
- [x] "Handling {RequestName} for user {UserId}" logged at Debug; long-running (>500 ms) at
      Warning; unhandled at Error with exception — via `[LoggerMessage]`.
- [x] Elapsed time measured per invocation with `Stopwatch.GetTimestamp()`.

**Verify:** `dotnet test tests/Application.UnitTests --filter "FullyQualifiedName~Common.Behaviours"`
— rewritten `RequestLoggerTests`, new `PerformanceBehaviourTests` (two fast sends on one
instance don't warn; slow send warns once), new `UnhandledExceptionBehaviourTests`.

**Files:** `src/Application/Common/Behaviours/{LoggingBehaviour,PerformanceBehaviour,UnhandledExceptionBehaviour,RequestLogMessages}.cs`,
`tests/Application.UnitTests/Common/Behaviours/*`.

**Dependencies:** None.

## Task 2: Worker log levels (spec D, #6)

**Acceptance:**
- [x] `src/Worker/appsettings.json` has `"Microsoft": "Warning"` alongside existing entries.

**Verify:** `dotnet build src/Worker`; review JSON.

**Files:** `src/Worker/appsettings.json`.

**Dependencies:** None.

## Task 3: Trace-context contract and schema (spec B)

**Acceptance:**
- [x] `OutboxMessage` and `MessageEnvelope` have nullable `TraceParent` (max 55) and
      `TraceState` (max 512); EF configuration sets max lengths.
- [x] Migration `AddOutboxTraceContext` generated with `dotnet ef`; no hand edits.
- [x] `MessagingTelemetry` (`Application/Queues`) exposes `ActivitySource("skestock.Messaging")`
      plus capture/parse helpers.
- [x] ServiceDefaults tracing adds `.AddSource("skestock.*")`.

**Verify:** `dotnet build`; `dotnet ef migrations has-pending-model-changes ...` clean;
serializer tests: round-trip with trace fields, legacy JSON → nulls.

**Files:** `src/Domain/Queues/{OutboxMessage,MessageEnvelope}.cs`,
`src/Infrastructure/Data/Configurations/OutboxMessageConfiguration.cs`,
`src/Infrastructure/Data/Migrations/*` (generated), `src/Application/Queues/MessagingTelemetry.cs`,
`src/ServiceDefaults/Extensions.cs`, serializer tests.

**Dependencies:** None.

## Task 4: Capture trace context when outbox rows are added (spec B)

**Acceptance:**
- [x] Infrastructure `SaveChangesInterceptor` stamps `Added` `OutboxMessage` entries with the
      current activity context when `TraceParent` is null; explicit values preserved.
- [x] Registered in Infrastructure DI alongside existing interceptors.

**Verify:** unit test with `ActivityListener` + SQLite/InMemory context; `dotnet build`.

**Files:** `src/Infrastructure/Data/Interceptors/OutboxTraceContextInterceptor.cs`,
`src/Infrastructure/DependencyInjection.cs`, test file.

**Dependencies:** Task 3.

## Task 5: Publisher producer span (spec B)

**Acceptance:**
- [x] `OutboxPublisherService` copies trace fields into `MessageEnvelope`.
- [x] Each send runs inside a `Producer` activity `publish {queueName}` parented to the stored
      context (root if absent); error status on failure.

**Verify:** `dotnet build`; existing publisher tests (if any) green; unit test asserting
envelope carries trace fields.

**Files:** `src/Web/BackgroundJobs/OutboxPublisherService.cs`, related test.

**Dependencies:** Task 3.

## Task 6: Worker consumer span (spec B)

**Acceptance:**
- [x] `QueueMessageProcessor` starts a `Consumer` activity `process {queueName}` parented to the
      envelope context; tags `messaging.message.id`, `messaging.destination.name`; error status
      on failed/permanent results.
- [x] Missing/invalid context → root activity; processing outcome unchanged.

**Verify:** `dotnet test tests/Worker.UnitTests`; new tests with `ActivityListener` for parented
and legacy cases.

**Files:** `src/Worker/Queues/QueueMessageProcessor.cs` (and queue name plumbing if needed),
`tests/Worker.UnitTests/Queues/*`.

**Dependencies:** Tasks 3, 5.

## Task 7: Production Aspire Dashboard (spec C, #5)

**Acceptance:**
- [x] `deploy/quadlet/skestock-dashboard.container`: pinned 13.x image, `skestock.network`,
      UI `127.0.0.1:18888`, OTLP unpublished, OTLP auth Unsecured, frontend BrowserToken.
- [x] Web/Worker units `Wants=`/`After=` dashboard and set `OTEL_SERVICE_NAME`.
- [x] Workflow env render and `production.env.example` add
      `OTEL_EXPORTER_OTLP_ENDPOINT=http://skestock-dashboard:18889`.
- [x] `deploy/deploy.sh` enables/starts/checks the dashboard; `deploy/README.md` documents SSH
      tunnel + token lookup.

**Verify:** `bash -n deploy/deploy.sh`; review units; workflow YAML parses.

**Files:** `deploy/quadlet/{skestock-dashboard,skestock-web,skestock-worker}.container`,
`deploy/deploy.sh`, `deploy/README.md`, `deploy/production.env.example`,
`.github/workflows/deploy-production.yml`.

**Dependencies:** None (end-to-end value after Task 6).

## Task 8: End-to-end verification

**Acceptance:**
- [x] `dotnet build` clean; Application + Worker unit tests green.
- [x] AppHost run: one goods-receipt import → single trace with `webapi` and `worker` spans.
- [x] Worker logs contain no EF `Executed DbCommand` at Information.
- [x] `graphify update .` run.

**Verify:** commands above + manual dashboard check.

**Dependencies:** Tasks 1–7.
