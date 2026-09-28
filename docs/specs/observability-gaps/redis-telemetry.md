# Spec: redis-telemetry

## Objective
Every Redis command the app issues is traced and the app holds **one** Redis connection per
process instead of up to four.

Today (`src/Infrastructure/DependencyInjection.cs`):
- a hand-built `ConnectionMultiplexer.Connect(...)` singleton (used by `RedisDistributedLock`) — untraced;
- `AddRedisDistributedCache(Services.Cache)` — Aspire, traced, but its `IDistributedCache` is not
  what the rest of the code uses;
- FusionCache `RedisBackplane` built from the raw connection string — own, untraced connection;
- SignalR `AddStackExchangeRedis(connectionString)` — own, untraced connection.

## Design
- Replace the manual multiplexer + `AddRedisDistributedCache` with
  `builder.AddRedisClientBuilder(Services.Cache, configureOptions: o => o.AbortOnConnectFail = false).WithDistributedCache()`.
  This registers a single traced `IConnectionMultiplexer`, keeps `IDistributedCache` registered
  (unchanged behaviour), and adds Aspire's Redis health check (consumed by `health-endpoints`).
- FusionCache backplane: `RedisBackplaneOptions.ConnectionMultiplexerFactory` returns the DI
  multiplexer (no `Configuration` string).
- SignalR: `options.ConnectionFactory` returns the DI multiplexer; `ChannelPrefix` unchanged.
- `RedisDistributedLock` keeps consuming `IConnectionMultiplexer` — no code change.
- `Aspire.StackExchange.Redis` gets an explicit `PackageVersion` (13.5.2, matching the
  DistributedCaching package) and a `PackageReference` in Infrastructure.

## Success criteria
1. Exactly one `IConnectionMultiplexer` registration exists and is resolved by the lock, the
   backplane factory and the SignalR factory (unit test on the built `ServiceProvider` with a fake
   connection string / `AbortOnConnectFail=false`, asserting same instance).
2. Integration test (real Redis via TestAppHost): acquiring the distributed lock produces an
   activity from the `OpenTelemetry.Instrumentation.StackExchangeRedis` source.
3. A Redis health check is registered.
4. Existing cache, lock, SignalR-backplane and scheduled-job tests still pass; `dotnet build` clean.

## Risks
- SignalR's `RedisHubLifetimeManager` disposes the connection it was given on shutdown. Acceptable
  (shutdown only); verified by the host stopping cleanly in functional tests.
- Aspire settings binding: `Aspire:StackExchange:Redis` config section now applies; defaults
  (tracing + health check on) are what we want.
