using System.Diagnostics;

namespace skestock.Application.Queues;

/// <summary>
/// Carries W3C trace context across the outbox → Azure Queue → Worker seam so that work started
/// by an HTTP request and finished by the Worker appears as one distributed trace.
/// </summary>
public static class MessagingTelemetry
{
    public const string ActivitySourceName = "skestock.Messaging";

    // "00-" + 32 hex trace id + "-" + 16 hex span id + "-" + 2 hex flags.
    public const int TraceParentMaxLength = 55;

    // W3C limits tracestate to 32 list members; 512 characters is the recommended propagation cap.
    public const int TraceStateMaxLength = 512;

    public const string MessageIdTag = "messaging.message.id";
    public const string DestinationNameTag = "messaging.destination.name";
    public const string SystemTag = "messaging.system";
    public const string SystemName = "azure_storage_queue";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

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
