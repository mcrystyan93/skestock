using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using NUnit.Framework;
using Shouldly;
using Worker.Queues;
using Worker.Services;

namespace Worker.UnitTests.Services;

[TestFixture]
public sealed class HeartbeatTests
{
    private const string QueueName = "test-queue";
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromSeconds(30);

    private string _heartbeatFile = null!;

    [SetUp]
    public void SetUp() =>
        _heartbeatFile = Path.Combine(Path.GetTempPath(), $"skestock-heartbeat-{Guid.NewGuid():N}");

    [TearDown]
    public void TearDown() => File.Delete(_heartbeatFile);

    [Test]
    public async Task File_is_written_when_every_queue_is_fresh()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var heartbeat = new WorkerHeartbeat(time);
        heartbeat.Register(QueueName, TimeSpan.FromMinutes(1));
        heartbeat.Register("other-queue", TimeSpan.FromMinutes(5));
        time.Advance(TimeSpan.FromSeconds(50));
        heartbeat.Beat(QueueName);
        time.Advance(TimeSpan.FromSeconds(50));

        var written = await CreateFileService(heartbeat, time).TryWriteAsync(CancellationToken.None);

        written.ShouldBeTrue();
        File.Exists(_heartbeatFile).ShouldBeTrue();
    }

    [Test]
    public async Task File_is_not_written_when_a_queue_is_stale()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var heartbeat = new WorkerHeartbeat(time);
        heartbeat.Register(QueueName, TimeSpan.FromMinutes(1));
        heartbeat.Register("other-queue", TimeSpan.FromMinutes(5));
        time.Advance(TimeSpan.FromSeconds(61));
        heartbeat.Beat("other-queue");

        var written = await CreateFileService(heartbeat, time).TryWriteAsync(CancellationToken.None);

        written.ShouldBeFalse();
        File.Exists(_heartbeatFile).ShouldBeFalse();
        heartbeat.GetStaleQueues().ShouldBe([QueueName]);
    }

    [Test]
    public void Queue_service_registers_itself_with_visibility_timeout_plus_two_polls()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var heartbeat = new WorkerHeartbeat(time);
        using var stop = new CancellationTokenSource();
        _ = CreateQueueService(heartbeat, stop, _ => throw new InvalidOperationException());

        time.Advance(VisibilityTimeout + TimeSpan.FromSeconds(10));
        heartbeat.GetStaleQueues().ShouldBeEmpty();

        time.Advance(TimeSpan.FromSeconds(1));
        heartbeat.GetStaleQueues().ShouldBe([QueueName]);
    }

    [Test]
    public async Task Queue_service_beats_on_an_empty_poll()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var heartbeat = new WorkerHeartbeat(time);
        using var stop = new CancellationTokenSource();
        var service = CreateQueueService(heartbeat, stop, _ =>
            Task.FromResult(Response.FromValue(Array.Empty<QueueMessage>(), Mock.Of<Response>())));
        time.Advance(TimeSpan.FromMinutes(10));
        heartbeat.GetStaleQueues().ShouldBe([QueueName]);

        await RunUntilStoppedAsync(service, stop);

        heartbeat.GetStaleQueues().ShouldBeEmpty();
    }

    [Test]
    public async Task Queue_service_beats_when_the_poll_fails()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var heartbeat = new WorkerHeartbeat(time);
        using var stop = new CancellationTokenSource();
        var service = CreateQueueService(heartbeat, stop, _ =>
            Task.FromException<Response<QueueMessage[]>>(new RequestFailedException("storage unavailable")));
        time.Advance(TimeSpan.FromMinutes(10));

        await RunUntilStoppedAsync(service, stop);

        heartbeat.GetStaleQueues().ShouldBeEmpty();
    }

    private HeartbeatFileService CreateFileService(WorkerHeartbeat heartbeat, TimeProvider time) =>
        new(
            heartbeat,
            Options.Create(new WorkerHeartbeatOptions { HeartbeatFile = _heartbeatFile }),
            time,
            NullLogger<HeartbeatFileService>.Instance);

    // Every receive cancels the stop token, so the service performs exactly one poll iteration.
    private static TestQueueService CreateQueueService(
        WorkerHeartbeat heartbeat,
        CancellationTokenSource stop,
        Func<CancellationToken, Task<Response<QueueMessage[]>>> receive)
    {
        var queueClient = new Mock<QueueClient>();
        queueClient
            .Setup(client => client.CreateIfNotExistsAsync(
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());
        queueClient
            .Setup(client => client.ReceiveMessagesAsync(
                It.IsAny<int?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((int? _, TimeSpan? _, CancellationToken cancellationToken) =>
            {
                stop.Cancel();
                return receive(cancellationToken);
            });

        var serviceClient = new Mock<QueueServiceClient>();
        serviceClient.Setup(client => client.GetQueueClient(It.IsAny<string>())).Returns(queueClient.Object);

        return new TestQueueService(serviceClient.Object, Mock.Of<IServiceScopeFactory>(), heartbeat);
    }

    private static async Task RunUntilStoppedAsync(TestQueueService service, CancellationTokenSource stop)
    {
        try
        {
            await service.RunAsync(stop.Token);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            // The poll delay observes the cancelled token after the iteration beat.
        }
    }

    private sealed class TestQueueService(
        QueueServiceClient queueServiceClient,
        IServiceScopeFactory scopeFactory,
        WorkerHeartbeat heartbeat)
        : QueueProcessingService<TestQueueService>(
            queueServiceClient,
            NullLogger<TestQueueService>.Instance,
            scopeFactory,
            heartbeat,
            QueueName,
            VisibilityTimeout)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }
}
