using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace skestock.Application.Queues;

/// <summary>
/// Carries W3C trace context across the outbox → Azure Queue → Worker seam so that work started
/// by an HTTP request and finished by the Worker appears as one distributed trace, and owns the
/// messaging metrics of that pipeline.
/// </summary>
/// <remarks>
/// Metric tags must stay low-cardinality: never tag with message ids, payloads or user ids.
/// </remarks>
public static class MessagingTelemetry
{
    public const string ActivitySourceName = "skestock.Messaging";
    public const string MeterName = "skestock.Messaging";

    // "00-" + 32 hex trace id + "-" + 16 hex span id + "-" + 2 hex flags.
    public const int TraceParentMaxLength = 55;

    // W3C limits tracestate to 32 list members; 512 characters is the recommended propagation cap.
    public const int TraceStateMaxLength = 512;

    public const string MessageIdTag = "messaging.message.id";
    public const string DestinationNameTag = "messaging.destination.name";
    public const string SystemTag = "messaging.system";
    public const string SystemName = "azure_storage_queue";
    public const string ErrorTypeTag = "error.type";
    public const string ProcessingStatusTag = "skestock.processing.status";
    public const string PoisonReasonTag = "skestock.poison.reason";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    // The SDK default buckets assume milliseconds; batch imports can take up to 20 minutes.
    private static readonly InstrumentAdvice<double> SecondsBuckets = new()
    {
        HistogramBucketBoundaries = [0.01, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30, 60, 120, 300, 600, 1200]
    };

    /// <summary>Time the Worker spent processing one decoded (or undecodable) message.</summary>
    public static readonly Histogram<double> ProcessDuration = Meter.CreateHistogram<double>(
        "messaging.process.duration",
        unit: "s",
        description: "Duration of processing one queue message.",
        tags: null,
        advice: SecondsBuckets);

    public static readonly Counter<long> SentMessages = Meter.CreateCounter<long>(
        "messaging.client.sent.messages",
        unit: "{message}",
        description: "Outbox messages the publisher attempted to send to a queue.");

    /// <summary>Time from writing the outbox row to sending it to the queue.</summary>
    public static readonly Histogram<double> OutboxLag = Meter.CreateHistogram<double>(
        "skestock.outbox.lag",
        unit: "s",
        description: "Age of an outbox message when it was sent to the queue.",
        tags: null,
        advice: SecondsBuckets);

    // -1 means "not measured yet": the gauge reports nothing rather than a misleading zero, which
    // also keeps it silent in hosts that do not run the outbox publisher (the Worker).
    private static long _outboxPending = -1;

    public static readonly ObservableGauge<long> OutboxPending = Meter.CreateObservableGauge(
        "skestock.outbox.pending",
        ObserveOutboxPending,
        unit: "{message}",
        description: "Outbox messages waiting to be published (excluding dead-lettered ones).");

    public static readonly Counter<long> PoisonedMessages = Meter.CreateCounter<long>(
        "skestock.queue.poisoned",
        unit: "{message}",
        description: "Messages moved to a poison queue.");

    /// <summary>
    /// Stores the latest pending count for <see cref="OutboxPending"/>. The count is queried by
    /// the publisher in its own scope; the gauge callback never touches the database.
    /// </summary>
    public static void ReportOutboxPending(long count) => Interlocked.Exchange(ref _outboxPending, count);

    private static IEnumerable<Measurement<long>> ObserveOutboxPending()
    {
        var pending = Interlocked.Read(ref _outboxPending);
        return pending < 0 ? [] : [new Measurement<long>(pending)];
    }

    /// <summary>
    /// Returns the current activity's W3C context, or nulls when there is no W3C activity.
    /// Oversized trace state is dropped rather than truncated, which would corrupt it.
    /// </summary>
    public static (string? TraceParent, string? TraceState) CaptureCurrent()
    {
        var activity = Activity.Current;
        if (activity is not { IdFormat: ActivityIdFormat.W3C, Id: { } id } || id.Length > TraceParentMaxLength)
        {
            return (null, null);
        }

        var traceState = activity.TraceStateString;
        return (id, string.IsNullOrEmpty(traceState) || traceState.Length > TraceStateMaxLength ? null : traceState);
    }

    /// <summary>
    /// Starts an activity continuing the stored context. Missing or invalid context starts a root
    /// activity, so legacy messages without trace fields are still processed.
    /// </summary>
    public static Activity? StartActivity(string name, ActivityKind kind, string? traceParent, string? traceState)
    {
        var parentContext = ActivityContext.TryParse(traceParent, traceState, isRemote: true, out var context)
            ? context
            : default;

        return ActivitySource.StartActivity(name, kind, parentContext);
    }
}
