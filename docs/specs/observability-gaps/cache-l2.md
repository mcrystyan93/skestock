# Spec: cache-l2

## Objective
Make HybridCache actually two-level. Today FusionCache is registered with a Redis **backplane**
but no **distributed cache**, so every process keeps its own memory-only copy; a restart or a
second instance starts cold, and the `IDistributedCache` Aspire registers is never used.

## Design
- `src/Infrastructure/DependencyInjection.cs`:
  ```csharp
  builder.Services.AddFusionCache()
      .WithOptions(o =>
      {
          o.CacheKeyPrefix = "skestock:";
          o.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(30);
      })
      .WithDefaultEntryOptions(e =>
      {
          e.DistributedCacheHardTimeout = TimeSpan.FromSeconds(2);
          e.AllowBackgroundDistributedCacheOperations = true;
      })
      .WithSerializer(new FusionCacheSystemTextJsonSerializer())
      .WithRegisteredDistributedCache()
      .WithBackplane(/* shared multiplexer, see redis-telemetry */)
      .AsHybridCache();
  ```
- New NuGet: `ZiggyCreatures.FusionCache.Serialization.SystemTextJson` 2.7.2 (required: FusionCache
  refuses an L2 without a serializer). Cached payloads are already `string`, so serialization is trivial.
- `CachingBehavior`: if `ResultCacheTransformer.Deserialize` throws `JsonException` (payload
  written by an older build with a different DTO shape), remove the key, log a warning (key only,
  no payload) and execute the handler — a stale L2 entry must never turn into a 500.
- Tag invalidation (`CacheInvalidationBehavior` → `RemoveByTagAsync`) is unchanged; FusionCache
  stores tag expirations in L2 and propagates via the backplane, so Web and Worker stay consistent.
- Redis outage: circuit breaker + hard timeout keep requests working from L1/database; the
  `/health` Redis check (health-endpoints) still reports it.

## Success criteria
1. Integration test (real Redis via TestAppHost): value cached through `HybridCache` in one
   `ServiceProvider` is returned by a **second, independent** provider without invoking the factory.
2. Integration test: `RemoveByTagAsync` in provider A makes provider B re-invoke the factory.
3. Unit test: `CachingBehavior` with a cache returning an undeserializable payload calls the
   handler, returns its result and removes the key.
4. Existing caching/invalidation unit tests pass; build clean.

## Out of scope (recorded)
`ResultCacheTransformer.ToResult` rebuilds cached failures as plain `Error`s, losing the typed
error (`NotFoundError` etc.), so a cached failure can map to a different HTTP status on the second
request. Pre-existing; unaffected by L1 vs L2. Recommended follow-up: stop caching failed results.
