# Spec: health-endpoints

## Objective
Production can answer "is the app healthy?" truthfully — for deploys and for Podman — without
leaking dependency details.

Today: `/health` and `/alive` are mapped only in Development; the deploy workflow verifies
`DEPLOY_HEALTH_URL` which the README says is `/scalar` (proves nothing about SQL/Redis/storage);
the Worker has no health signal.

## Design (decisions approved)
**Web**
- `MapDefaultEndpoints` maps `/health` (all checks) and `/alive` (`live` tag) in every environment,
  `.AllowAnonymous()`, default writer (plain `Healthy` / `Degraded` / `Unhealthy`, 503 when
  Unhealthy) — no per-check details, no exception text.
- Registered checks come from Aspire integrations: SQL Server/EF (`EnrichSqlServerDbContext`),
  Blob, Queue, Redis (`redis-telemetry`). No custom checks.
- ASP.NET Core tracing `Filter` drops requests whose path starts with `/health` or `/alive`.
- The SPA `MapFallback` must not shadow the health routes (verified by test).
- `deploy/README.md`: `DEPLOY_HEALTH_URL` = `http://<host>:7001/health`. Workflow unchanged
  (already curls `--fail` with retries); only the documented value changes.

**Worker** (no HTTP)
- New singleton `WorkerHeartbeat` in `src/Worker/Services/`: each `QueueProcessingService` loop
  calls `Beat(queueName)` once per poll iteration (success, empty or handled failure).
- New `HeartbeatFileService : BackgroundService` every 30 s writes the current UTC time to
  `Worker:HeartbeatFile` (default `/tmp/skestock-worker.heartbeat`) **only if** every registered
  queue beat within its allowed staleness (`visibilityTimeout + 2 × poll interval`); otherwise logs a
  warning and skips the write.
- `deploy/quadlet/skestock-worker.container`:
  `HealthCmd=/bin/sh -c 'test $(( $(date +%s) - $(stat -c %Y /tmp/skestock-worker.heartbeat) )) -lt 180'`,
  `HealthInterval=60s`, `HealthStartPeriod=120s`, `HealthRetries=3`, `HealthOnFailure=kill`
  (systemd `Restart=always` then restarts it). Base image `aspnet:10.0` (Debian) has `sh`, `date`, `stat`.

## Success criteria
1. Functional test: anonymous `GET /health` → 200 with body exactly `Healthy`; `GET /alive` → 200.
2. Unit test: with a failing check the body is `Unhealthy`, status 503, and contains no check name
   or exception message.
3. Unit test: the tracing filter rejects `/health`, `/alive`, accepts `/api/items`.
4. Worker unit tests (`FakeTimeProvider`): file written when all queues are fresh; not written when
   one queue is stale; `QueueProcessingService` beats on empty polls and on loop exceptions.
5. `podman`'s quadlet dry-run accepts the Worker unit; README updated.

## Boundaries
Ask first before exposing detailed health JSON or adding health UI packages.
