# Spec: cache-telemetry

## Objective
See whether `CachingBehavior` actually helps: cache hit/miss/set/remove counts and cache spans
under request traces.

## Design
- Add `ZiggyCreatures.FusionCache.OpenTelemetry` 2.7.2 (matches core 2.7.2) to
  `Directory.Packages.props` and **ServiceDefaults** (where OTel is configured).
- In `ConfigureOpenTelemetry`: `tracing.AddFusionCacheInstrumentation()` and
  `metrics.AddFusionCacheInstrumentation()` with default options (no key tagging —
  `IncludeMemoryLevel`/key options stay off so keys never become metric tags).
- `FusionCacheEntryOptions`/behaviour untouched.

## Success criteria
1. Unit test: a `ServiceProvider` built with `AddServiceDefaults`-equivalent OTel config plus
   `AddFusionCache()`; a `GetOrSet` call emits an activity from the FusionCache source and a metric
   from the FusionCache meter (listeners filtered on the library's published source/meter names).
2. No cache key string appears in any emitted metric tag (asserted in the same test).
3. Build clean; Application unit tests pass.

## Open question (out of scope, recorded only)
FusionCache is registered without `.WithDistributedCache(...)`/`TryWithRegisteredDistributedCache()`,
so HybridCache is **memory-only + Redis backplane**; the registered `IDistributedCache` is unused.
That is a caching-design decision, not telemetry — flagged for a separate change.
