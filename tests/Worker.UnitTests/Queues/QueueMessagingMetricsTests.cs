using System.Diagnostics.Metrics;
using Azure.Storage.Queues;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Queues;
using Worker.Queues;
using Worker.Services;

namespace Worker.UnitTests.Queues;

[TestFixture]
[NonParallelizable]
public sealed class QueueMessagingMetricsTests
{
    private const string ProcessDuration = "messaging.process.duration";
    private const string Poisoned = "skestock.queue.poisoned";

    private readonly List<Measurement> _measurements = [];
    private MeterListener _listener = null!;

    [SetUp]
    public void SetUp()
    {
        _measurements.Clear();
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == MessagingTelemetry.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            Record(instrument, value, tags));
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            Record(instrument, value, tags));
        _listener.Start();
    }

    [TearDown]
    public void TearDown() => _listener.Dispose();

    [Test]
    public async Task Succeeded_message_records_duration_without_error_type()
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(messageId);
        harness.ConfigureSender((_, _) => ValueTask.FromResult<object?>(Result.Ok()));

        await RunAsync(harness);

        var duration = Single(ProcessDuration);
        duration.Value.ShouldBeGreaterThanOrEqualTo(0);
        duration.Tags[MessagingTelemetry.ProcessingStatusTag].ShouldBe("succeeded");
        duration.Tags[MessagingTelemetry.SystemTag].ShouldBe(MessagingTelemetry.SystemName);
        duration.Tags[MessagingTelemetry.DestinationNameTag].ShouldBe(harness.QueueName);
        duration.Tags.ShouldNotContainKey(MessagingTelemetry.ErrorTypeTag);
        Measurements(Poisoned).ShouldBeEmpty();
        AssertNoMessageIdentifiers(messageId);
    }

    [Test]
    public async Task Duplicate_message_records_duplicate_status()
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(messageId);
        harness.AddProcessedMessage(messageId);

        await RunAsync(harness);

        Single(ProcessDuration).Tags[MessagingTelemetry.ProcessingStatusTag].ShouldBe("duplicate");
        AssertNoMessageIdentifiers(messageId);
    }

    [Test]
    public async Task Transient_exception_records_retryable_failure_with_exception_type()
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(messageId, dequeueCount: 1);
        harness.ConfigureSender((_, _) =>
        {
            harness.Stop();
            return ValueTask.FromException<object?>(new InvalidOperationException("Temporary failure."));
        });

        await RunAsync(harness);

        var duration = Single(ProcessDuration);
        duration.Tags[MessagingTelemetry.ProcessingStatusTag].ShouldBe("retryable_failure");
        duration.Tags[MessagingTelemetry.ErrorTypeTag].ShouldBe(typeof(InvalidOperationException).FullName);
        Measurements(Poisoned).ShouldBeEmpty();
        AssertNoMessageIdentifiers(messageId);
    }

    [Test]
    public async Task Exhausted_retries_increment_poisoned_with_retry_exhausted_reason()
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(messageId, dequeueCount: QueueMessageDispositionPolicy.MaxDequeueCount);
        harness.ConfigureSender((_, _) =>
            ValueTask.FromException<object?>(new InvalidOperationException("Temporary failure.")));

        await RunAsync(harness);

        var poisoned = Single(Poisoned);
        poisoned.Value.ShouldBe(1);
        poisoned.Tags[MessagingTelemetry.PoisonReasonTag].ShouldBe("retry_exhausted");
        poisoned.Tags[MessagingTelemetry.DestinationNameTag].ShouldBe(harness.QueueName);
        AssertNoMessageIdentifiers(messageId);
    }

    [Test]
    public async Task Permanent_exception_records_permanent_failure_and_poison_reason()
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(messageId);
        harness.ConfigureSender((_, _) =>
            ValueTask.FromException<object?>(new ArgumentException("Invalid request.")));

        await RunAsync(harness);

        var duration = Single(ProcessDuration);
        duration.Tags[MessagingTelemetry.ProcessingStatusTag].ShouldBe("permanent_failure");
        duration.Tags[MessagingTelemetry.ErrorTypeTag].ShouldBe(typeof(ArgumentException).FullName);
        Single(Poisoned).Tags[MessagingTelemetry.PoisonReasonTag].ShouldBe("permanent");
        AssertNoMessageIdentifiers(messageId);
    }

    [Test]
    public async Task Failed_result_records_failed_result_error_type()
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(messageId);
        harness.ConfigureSender((_, _) => ValueTask.FromResult<object?>(Result.Fail("The import no longer exists.")));

        await RunAsync(harness);

        var duration = Single(ProcessDuration);
        duration.Tags[MessagingTelemetry.ProcessingStatusTag].ShouldBe("permanent_failure");
        duration.Tags[MessagingTelemetry.ErrorTypeTag].ShouldBe("failed_result");
        Single(Poisoned).Tags[MessagingTelemetry.PoisonReasonTag].ShouldBe("permanent");
        AssertNoMessageIdentifiers(messageId);
    }

    [Test]
    public async Task Undecodable_message_records_invalid_envelope()
    {
        using var harness = new QueueProcessingTestHarness(
            QueueProcessorKind.GoodsReceipt,
            QueueMessageFactory.CreateRawText("not an envelope"));

        await RunAsync(harness);

        var duration = Single(ProcessDuration);
        duration.Tags[MessagingTelemetry.ProcessingStatusTag].ShouldBe("permanent_failure");
        duration.Tags[MessagingTelemetry.ErrorTypeTag].ShouldBe("invalid_envelope");
        Single(Poisoned).Tags[MessagingTelemetry.PoisonReasonTag].ShouldBe("permanent");
    }

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            copy[tag.Key] = tag.Value;
        }

        lock (_measurements)
        {
            _measurements.Add(new Measurement(instrument.Name, value, copy));
        }
    }

    private List<Measurement> Measurements(string instrument) =>
        _measurements.Where(measurement => measurement.Instrument == instrument).ToList();

    private Measurement Single(string instrument) => Measurements(instrument).ShouldHaveSingleItem();

    private void AssertNoMessageIdentifiers(Guid messageId)
    {
        foreach (var measurement in _measurements)
        {
            measurement.Tags.ShouldNotContainKey(MessagingTelemetry.MessageIdTag);
            measurement.Tags.Values.ShouldNotContain(messageId.ToString());
        }
    }

    private static QueueProcessingTestHarness CreateHarness(Guid messageId, int dequeueCount = 1) =>
        new(
            QueueProcessorKind.GoodsReceipt,
            QueueMessageFactory.Create(QueueProcessorKind.GoodsReceipt, messageId, dequeueCount: dequeueCount));

    private static Task RunAsync(QueueProcessingTestHarness harness) =>
        new TestableProcessor(
            harness.QueueServiceClient.Object,
            NullLogger<GoodsReceiptImportQueueProcessingService>.Instance,
            harness.ScopeFactory,
            new WorkerHeartbeat(TimeProvider.System)).RunAsync(harness.StopToken);

    private sealed record Measurement(string Instrument, double Value, Dictionary<string, object?> Tags);

    private sealed class TestableProcessor(
        QueueServiceClient queueServiceClient,
        ILogger<GoodsReceiptImportQueueProcessingService> logger,
        IServiceScopeFactory scopeFactory,
        WorkerHeartbeat heartbeat)
        : GoodsReceiptImportQueueProcessingService(queueServiceClient, logger, scopeFactory, heartbeat)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }
}
