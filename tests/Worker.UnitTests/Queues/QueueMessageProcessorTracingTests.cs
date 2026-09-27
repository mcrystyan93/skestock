using System.Diagnostics;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.GoodsReceipts.Commands.ProcessGoodsReceiptImport;
using skestock.Application.Queues;
using skestock.Domain.Queues;
using Worker.Queues;

namespace Worker.UnitTests.Queues;

[TestFixture]
[NonParallelizable]
public sealed class QueueMessageProcessorTracingTests
{
    private const string TraceId = "0af7651916cd43dd8448eb211c80319c";
    private const string ParentSpanId = "b7ad6b7169203331";
    private const string TraceParent = $"00-{TraceId}-{ParentSpanId}-01";

    private ActivityListener _listener = null!;
    private readonly List<Activity> _stopped = [];

    [SetUp]
    public void SetUp()
    {
        _stopped.Clear();
        Activity.Current = null;
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MessagingTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _stopped.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    [TearDown]
    public void TearDown() => _listener.Dispose();

    [Test]
    public async Task Consumer_activity_continues_envelope_trace_during_dispatch()
    {
        var messageId = Guid.NewGuid();
        using var harness = CreateHarness(messageId, TraceParent);
        Activity? dispatchActivity = null;
        harness.ConfigureSender((_, _) =>
        {
            dispatchActivity = Activity.Current;
            return ValueTask.FromResult<object?>(Result.Ok());
        });

        var result = await ProcessAsync(harness);

        result.Status.ShouldBe(QueueMessageProcessingStatus.Succeeded);
        dispatchActivity.ShouldNotBeNull();
        dispatchActivity.Kind.ShouldBe(ActivityKind.Consumer);
        dispatchActivity.TraceId.ToHexString().ShouldBe(TraceId);
        dispatchActivity.ParentSpanId.ToHexString().ShouldBe(ParentSpanId);
        dispatchActivity.GetTagItem(MessagingTelemetry.MessageIdTag).ShouldBe(messageId.ToString());
        dispatchActivity.GetTagItem(MessagingTelemetry.DestinationNameTag).ShouldBe(harness.QueueName);
        _stopped.ShouldContain(dispatchActivity);
    }

    [Test]
    public async Task Legacy_envelope_without_trace_context_starts_root_activity_and_succeeds()
    {
        using var harness = CreateHarness(Guid.NewGuid(), traceParent: null);
        Activity? dispatchActivity = null;
        harness.ConfigureSender((_, _) =>
        {
            dispatchActivity = Activity.Current;
            return ValueTask.FromResult<object?>(Result.Ok());
        });

        var result = await ProcessAsync(harness);

        result.Status.ShouldBe(QueueMessageProcessingStatus.Succeeded);
        dispatchActivity.ShouldNotBeNull();
        dispatchActivity.ParentSpanId.ShouldBe(default);
    }

    [Test]
    public async Task Failed_result_marks_consumer_activity_as_error()
    {
        using var harness = CreateHarness(Guid.NewGuid(), TraceParent);
        harness.ConfigureSender((_, _) => ValueTask.FromResult<object?>(Result.Fail("nope")));

        var result = await ProcessAsync(harness);

        result.Status.ShouldBe(QueueMessageProcessingStatus.PermanentFailure);
        _stopped.ShouldHaveSingleItem().Status.ShouldBe(ActivityStatusCode.Error);
    }

    private static QueueProcessingTestHarness CreateHarness(Guid messageId, string? traceParent)
    {
        var envelope = new MessageEnvelope
        {
            MessageId = messageId,
            Type = typeof(ProcessGoodsReceiptImportCommand).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new { GoodsReceiptImportId = Guid.NewGuid() }),
            TraceParent = traceParent
        };

        return new QueueProcessingTestHarness(
            QueueProcessorKind.GoodsReceipt,
            QueueMessageFactory.CreateRaw(envelope));
    }

    private static async Task<QueueMessageProcessingResult> ProcessAsync(QueueProcessingTestHarness harness)
    {
        using var scope = harness.ScopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IQueueMessageProcessor>();
        var message = QueueMessageFactory.CreateRawText(harness.SourceMessageText);

        return await processor.ProcessAsync(message, harness.QueueName, CancellationToken.None);
    }
}
