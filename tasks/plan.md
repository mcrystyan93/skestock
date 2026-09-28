# Plan: Observability gaps

Specs: `docs/specs/observability-gaps/` (map in `README.md`, one spec per module id).
Previous initiative archived in `tasks/archive/observability-hardening-*.md`.

## Dependency graph

```
redis-telemetry ──► cache-telemetry ──► cache-l2
        │
        └────────► health-endpoints (web) ──► health-endpoints (worker)

messaging-metrics (processor/poison) ──► messaging-metrics (publisher)     [independent]
trace-coverage                                                              [independent]
```

`redis-telemetry` goes first: it replaces the connection that the backplane, SignalR, the lock and
the Redis health check all depend on. `cache-l2` comes after `cache-telemetry`, so L1/L2 hit rates
can be seen as soon as L2 is on.

## Order and checkpoints

1. T1 redis-telemetry
2. T2 cache-telemetry
3. T3 cache-l2 → **Checkpoint A:** build, Application unit tests, and Infrastructure integration
   tests (Redis) pass
4. T4 health (Web)
5. T5 health (Worker) → **Checkpoint B:** build, Worker unit tests, functional health test, and
   quadlet dry-run pass
6. T6 messaging metrics (Worker side)
7. T7 messaging metrics (publisher side)
8. T8 trace coverage (SignalR + OpenAI) → **Checkpoint C:** build, all unit tests, and outbox
   integration tests pass
9. T9 final verification, `graphify update .`, and summary

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| Aspire `AddRedisClientBuilder` registration conflicts with the existing manual singleton | Remove the manual one in the same task; a unit test asserts a single shared instance |
| SignalR disposes the shared multiplexer at shutdown | Shutdown only; functional host stop is observed in tests |
| L2 holds payloads from an older DTO shape after a deploy | `CachingBehavior` treats a `JsonException` as a miss, removes the key, then runs the handler; staleness is bounded by the TTL |
| Redis outage slows requests once L2 is on | FusionCache hard timeout (2 s), background L2 writes, 30 s circuit breaker |
| `/health` becomes public | Default writer returns status text only; tests assert that check names and exception text are absent |
| Worker heartbeat reports "stale" during a legitimately long OpenAI extraction | Per-queue staleness is `visibilityTimeout + 2 × poll`, so a single message can't exceed it without also being redelivered |
| Metric cardinality | Tags come only from bounded enums; tests assert that IDs are absent |
| Instability from the new FusionCache OTel/serializer packages | Versions pinned to 2.7.2, matching the core package |

## Verification commands

- `dotnet build`
- `dotnet test tests/Application.UnitTests`
- `dotnet test tests/Worker.UnitTests`
- Podman: `DOTNET_ASPIRE_CONTAINER_RUNTIME=podman DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock dotnet test tests/Infrastructure.IntegrationTests`
- Functional tests: `./run-functional-tests.sh --filter Health`
- `QUADLET_UNIT_DIRS=$PWD/deploy/quadlet /usr/libexec/podman/quadlet -dryrun -user`
