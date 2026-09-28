# Spec: messaging-metrics

## Objective
Alertable numbers for the async pipeline: is the outbox draining, how stale are messages, how
long does processing take, how many go to poison. Today no `Meter` exists anywhere in `src`.

## Design
- `MessagingTelemetry` (`src/Application/Queues/`) gains `public static readonly Meter Meter =
  new("skestock.Messaging")` next to its ActivitySource, plus static instruments. ServiceDefaults
  adds `metrics.AddMeter("skestock.*")`.
- Common tags: `messaging.system=azure_storage_queue`, `messaging.destination.name=<queue>`.

| Instrument | Kind / unit | Recorded where | Extra tags |
|---|---|---|---|
| `messaging.client.sent.messages` | Counter `{message}` | `OutboxPublisherService`, per send | `error.type` on failure (exception type name) |
| `skestock.outbox.lag` | Histogram `s` | publisher, on successful send: `now − CreatedAtUtc` | — |
| `skestock.outbox.pending` | ObservableGauge `{message}` | value cached by publisher; refreshed at most every 30 s with `COUNT(*)` over `ProcessedAtUtc IS NULL AND RetryCount < MaxRetries` via `IOutboxClaimStore.CountPendingAsync` (uses existing filtered index; dead-lettered rows are excluded so they do not look like a stuck backlog); reports nothing until first measured | none (no queue tag) |
| `messaging.process.duration` | Histogram `s` | `QueueMessageProcessor.ProcessAsync` around envelope processing | `skestock.processing.status` (`succeeded`/`duplicate`/`retryable_failure`/`permanent_failure`), `error.type` for failures |
| `skestock.queue.poisoned` | Counter `{message}` | `QueueProcessingService` on `MoveToPoison` | `skestock.poison.reason` (`permanent`/`retry_exhausted`) |

- Time from `TimeProvider` where the component already has one; otherwise `Stopwatch.GetTimestamp`.
- The pending-count query runs in the publisher's own scope/DbContext, never inside a gauge callback.

## Success criteria
1. Unit tests with `MeterListener`: processor records duration with the right status tag for each
   `QueueMessageProcessingStatus`; poison counter increments with correct reason; tags contain no
   message IDs.
2. Integration test (existing outbox fixture): a published message records `sent.messages` and
   `outbox.lag`; a failed send records `error.type`; pending gauge reports the unpublished count.
3. Build clean; Application, Worker unit and outbox integration tests pass.
