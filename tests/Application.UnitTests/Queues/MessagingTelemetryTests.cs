using System.Diagnostics;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Queues;

namespace skestock.Application.UnitTests.Queues;

[NonParallelizable]
public class MessagingTelemetryTests
{
    private ActivityListener _listener = null!;

    [SetUp]
    public void SetUp()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MessagingTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [TearDown]
    public void TearDown() => _listener.Dispose();

    [Test]
    public void CaptureCurrentShouldReturnNullsWithoutActivity()
    {
        Activity.Current = null;

        MessagingTelemetry.CaptureCurrent().ShouldBe((null, null));
    }

    [Test]
    public void StartActivityShouldContinueCapturedContext()
    {
        using var origin = new Activity("origin").SetIdFormat(ActivityIdFormat.W3C).Start();
        origin.TraceStateString = "vendor=value";
        var (traceParent, traceState) = MessagingTelemetry.CaptureCurrent();
        origin.Stop();
        Activity.Current = null;

        using var continued = MessagingTelemetry.StartActivity("process", ActivityKind.Consumer, traceParent, traceState);

        continued.ShouldNotBeNull();
        continued.TraceId.ShouldBe(origin.TraceId);
        continued.ParentSpanId.ShouldBe(origin.SpanId);
        continued.TraceStateString.ShouldBe("vendor=value");
        continued.Kind.ShouldBe(ActivityKind.Consumer);
    }

    [TestCase(null)]
    [TestCase("not-a-traceparent")]
    public void StartActivityShouldStartRootForMissingOrInvalidContext(string? traceParent)
    {
        Activity.Current = null;

        using var activity = MessagingTelemetry.StartActivity("process", ActivityKind.Consumer, traceParent, null);

        activity.ShouldNotBeNull();
        activity.ParentSpanId.ShouldBe(default);
    }
}
