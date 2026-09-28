# Capability Map: Observability gaps

Follow-up to [`../observability-hardening.md`](../observability-hardening.md) (payload logging,
per-request timing, outbox → queue → Worker trace propagation, production OTLP dashboard — done).
This initiative closes the remaining telemetry *coverage* gaps. Backend only; browser telemetry,
new log sinks and log redaction are out of scope.

| Module id | Responsibility | Depends on | New NuGet |
|---|---|---|---|
| `redis-telemetry` | One shared, traced `IConnectionMultiplexer` (Aspire `AddRedisClientBuilder`) reused by the distributed lock, FusionCache backplane and SignalR; Redis health check | — | `Aspire.StackExchange.Redis` 13.5.2 (already transitive; made explicit) |
| `cache-telemetry` | FusionCache OpenTelemetry traces + hit/miss metrics | `redis-telemetry` | `ZiggyCreatures.FusionCache.OpenTelemetry` 2.7.2 |
| `cache-l2` | Turn HybridCache into real L1 + Redis L2 (FusionCache distributed cache + serializer), stale-payload safe | `redis-telemetry`, `cache-telemetry` | `ZiggyCreatures.FusionCache.Serialization.SystemTextJson` 2.7.2 |
| `health-endpoints` | Production `/health` + `/alive` (status text only), health excluded from traces, deploy check uses `/health`, Worker heartbeat + Podman `HealthCmd` | `redis-telemetry` | — |
| `messaging-metrics` | `skestock.Messaging` Meter: outbox sent/lag/pending, queue process duration/outcome, poison count | — | — |
| `trace-coverage` | SignalR server spans; OpenAI calls as `gen_ai` client spans with model/token usage + GenAI metrics | — | — |

Build order: `redis-telemetry` → `cache-telemetry` → `cache-l2`; `health-endpoints` (after
`redis-telemetry`); then `messaging-metrics`, `trace-coverage` (the last two are independent and may go in any order).

Module specs: [`redis-telemetry.md`](redis-telemetry.md), [`cache-telemetry.md`](cache-telemetry.md),
[`cache-l2.md`](cache-l2.md), [`health-endpoints.md`](health-endpoints.md), [`messaging-metrics.md`](messaging-metrics.md),
[`trace-coverage.md`](trace-coverage.md).

## Shared conventions (apply to every module)

- **Commands:** `dotnet build`; `dotnet test tests/Application.UnitTests`;
  `dotnet test tests/Worker.UnitTests`; integration tests via Podman
  (`systemctl --user start podman.socket`, `DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock`,
  `DOTNET_ASPIRE_CONTAINER_RUNTIME=podman dotnet test tests/Infrastructure.IntegrationTests`).
- **Naming:** our own ActivitySources/Meters are named `skestock.<Area>` and are picked up by the
  `skestock.*` wildcard in `ServiceDefaults` (`AddSource` already; `AddMeter` added by
  `messaging-metrics`). Attribute names follow OpenTelemetry semantic conventions where one exists
  (`messaging.*`, `gen_ai.*`, `error.type`); custom ones use the `skestock.` prefix.
- **Cardinality:** metric tags are bounded enums (queue name, status, model). Never IDs, user
  names, file names or payloads.
- **Testing:** NUnit + Shouldly + Moq; metrics asserted with `MeterListener` /
  `Microsoft.Extensions.Diagnostics.Testing` `MetricCollector<T>` if already available, otherwise
  `MeterListener`; spans with `ActivityListener`; such fixtures are `[NonParallelizable]`.
- **Boundaries — Always:** test-first, central package versions, `Services.*` constants for names,
  `graphify update .` after code changes. **Ask first:** any NuGet beyond the table above, schema
  changes, exposing anything new publicly beyond `/health` + `/alive`. **Never:** payloads, tokens,
  prompts or document content in telemetry; hand-edit generated migrations; commit.
