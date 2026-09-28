# Tasks: Observability gaps

- [x] **T1 — redis-telemetry: one shared, traced multiplexer**
  - Acceptance:
    - `AddRedisClientBuilder(Services.Cache, AbortOnConnectFail=false).WithDistributedCache()` replaces both the manual singleton and `AddRedisDistributedCache`.
    - The FusionCache backplane and SignalR use the DI multiplexer.
    - The Redis health check is registered.
  - Verify:
    - Unit test: backplane factory, SignalR factory and lock resolve the same instance.
    - Integration test: lock acquire emits a StackExchangeRedis activity.
    - Build clean.
  - Files: `Directory.Packages.props`, `src/Infrastructure/Infrastructure.csproj`, `src/Infrastructure/DependencyInjection.cs`, new unit test, new integration test.

- [x] **T2 — cache-telemetry: FusionCache OpenTelemetry**
  - Acceptance:
    - `ZiggyCreatures.FusionCache.OpenTelemetry` 2.7.2 added.
    - Traces and metrics instrumentation registered in `ConfigureOpenTelemetry`.
    - Cache keys never appear as metric tags.
  - Verify: unit test shows `GetOrSet` emits a FusionCache activity and metric, with no key in the tags.
  - Files: `Directory.Packages.props`, `src/ServiceDefaults/ServiceDefaults.csproj`, `src/ServiceDefaults/Extensions.cs`, new unit test.

- [x] **T3 — cache-l2: real L1 + Redis L2**
  - Acceptance:
    - The serializer package (2.7.2) is added.
    - FusionCache is configured per `cache-l2.md` (prefix, timeouts, circuit breaker, background L2, registered distributed cache).
    - `CachingBehavior` treats an undeserializable payload as a miss.
  - Verify:
    - Integration tests: a value is shared between two providers, and tag removal crosses providers.
    - Unit test for the corrupt-payload fallback.
    - Existing caching tests pass.
    - **Checkpoint A.**
  - Files: `Directory.Packages.props`, `src/Infrastructure/Infrastructure.csproj`, `src/Infrastructure/DependencyInjection.cs`, `src/Application/Common/Behaviours/CachingBehavior.cs`, tests.

- [x] **T4 — health-endpoints (Web)**
  - Acceptance:
    - `/health` and `/alive` are mapped anonymously in all environments, with status text only.
    - `/health` and `/alive` requests are excluded from ASP.NET Core traces.
    - The SPA fallback doesn't shadow them.
    - README `DEPLOY_HEALTH_URL` is set to `/health`.
  - Verify:
    - Functional test: `/health` returns 200 `Healthy`; `/alive` returns 200.
    - Unit tests: an unhealthy response has no details; the trace filter works.
  - Files: `src/ServiceDefaults/Extensions.cs`, `deploy/README.md`, functional test, unit test.

- [x] **T5 — health-endpoints (Worker heartbeat)**
  - Acceptance:
    - `WorkerHeartbeat` singleton exists.
    - Queue loops beat every iteration.
    - `HeartbeatFileService` writes only when all queues are fresh.
    - Quadlet `HealthCmd` and `HealthOnFailure=kill` are set.
    - `Worker:HeartbeatFile` option defaults to `/tmp/skestock-worker.heartbeat`.
  - Verify:
    - Worker unit tests (`FakeTimeProvider`): fresh, stale, and beat on empty poll or exception.
    - Quadlet dry-run passes.
    - **Checkpoint B.**
  - Files: `src/Worker/Services/WorkerHeartbeat.cs`, `src/Worker/Services/HeartbeatFileService.cs`, `src/Worker/Queues/QueueProcessingService.cs`, `src/Worker/Program.cs`, `deploy/quadlet/skestock-worker.container`, tests.

- [x] **T6 — messaging-metrics (Worker side)**
  - Acceptance:
    - `MessagingTelemetry.Meter` exists.
    - `messaging.process.duration` is recorded with status and `error.type`.
    - `skestock.queue.poisoned` is recorded with its reason.
    - ServiceDefaults has `AddMeter("skestock.*")`.
  - Verify: Worker unit tests with `MeterListener`, covering every status, the poison reasons, and that tags contain no message IDs.
  - Files: `src/Application/Queues/MessagingTelemetry.cs`, `src/Worker/Queues/QueueMessageProcessor.cs`, `src/Worker/Queues/QueueProcessingService.cs`, `src/ServiceDefaults/Extensions.cs`, tests.

- [x] **T7 — messaging-metrics (publisher side)**
  - Acceptance:
    - `messaging.client.sent.messages` is recorded, with `error.type` on failure.
    - `skestock.outbox.lag` is recorded.
    - The `skestock.outbox.pending` gauge is backed by a throttled (30 s) count query in the publisher.
  - Verify: outbox integration tests cover the sent counter, lag, `error.type` on failure, and the pending value.
  - Files: `src/Application/Queues/MessagingTelemetry.cs`, `src/Web/BackgroundJobs/OutboxPublisherService.cs`, `tests/Infrastructure.IntegrationTests/OutboxPublisherConcurrencyTests.cs` (or a new metrics test file).

- [x] **T8 — trace-coverage (SignalR + OpenAI GenAI)**
  - Acceptance:
    - The SignalR server source is registered.
    - `GenAiTelemetry` exists.
    - `SendAsync` creates a `chat {model}` client span with GenAI attributes, token usage and `error.type`.
    - Token-usage and operation-duration histograms are recorded.
    - No prompt or file data appears in telemetry.
  - Verify:
    - Unit tests with a fake handler for success and 500 responses.
    - A test asserts no tag contains the prompt or base64 data.
    - **Checkpoint C.**
  - Files: `src/ServiceDefaults/Extensions.cs`, `src/Infrastructure/AI/GenAiTelemetry.cs`, `src/Infrastructure/AI/OpenAiDocumentExtractionClient.cs`, tests.

- [x] **T9 — final verification**
  - Acceptance: full build; Application, Worker and integration tests pass; health functional test passes; `graphify update .` run; summary delivered (no commit).
