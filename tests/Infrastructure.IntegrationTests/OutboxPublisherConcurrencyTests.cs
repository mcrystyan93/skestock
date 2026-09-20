using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;
using skestock.Infrastructure.Data;
using skestock.Infrastructure.Queues;
using skestock.Web.BackgroundJobs;

namespace skestock.Infrastructure.IntegrationTests;

[SetUpFixture]
public sealed class IntegrationTestSetup
{
    internal static string DatabaseConnectionString { get; private set; } = null!;

    private static DistributedApplication? _app;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var cancellationToken = cts.Token;

        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.TestAppHost>(
                args: [],
                configureBuilder: (options, _) =>
                {
                    options.DisableDashboard = true;
                });

        builder.Configuration["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";

        _app = await builder
            .BuildAsync(cancellationToken)
            .WaitAsync(cancellationToken);

        await _app
            .StartAsync(cancellationToken)
            .WaitAsync(cancellationToken);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync(
            Services.Database, cancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(
            Services.Cache, cancellationToken);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(
            Services.Queues, cancellationToken);

        DatabaseConnectionString = (await _app.GetConnectionStringAsync(Services.Database))!;

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    internal static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DatabaseConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}

public sealed class OutboxPublisherConcurrencyTests
{
    [Test]
    public async Task ConcurrentPublishers_SendEachPendingMessageOnlyOnce()
    {
        var messageId = Guid.NewGuid();
        try
        {
            await SeedMessageAsync(new OutboxMessage
            {
                Id = messageId,
                Type = "test.message",
                Payload = "{}",
                QueueName = $"outbox-test-{Guid.NewGuid():N}",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var sender = new RecordingQueueSender(
                async cancellationToken => await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken));
            using var serviceProvider = CreatePublisherServiceProvider(sender);
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var publisher1 = new TestableOutboxPublisherService(scopeFactory);
            var publisher2 = new TestableOutboxPublisherService(scopeFactory);

            await Task.WhenAll(
                publisher1.PublishOnceAsync(CancellationToken.None),
                publisher2.PublishOnceAsync(CancellationToken.None));

            sender.SendCount.ShouldBe(1);

            await using var dbContext = IntegrationTestSetup.CreateDbContext();
            var message = await dbContext.OutboxMessages.SingleAsync(x => x.Id == messageId);
            message.ProcessedAtUtc.ShouldNotBeNull();
            message.ClaimId.ShouldBeNull();
            message.ClaimedUntilUtc.ShouldBeNull();
            message.RetryCount.ShouldBe(0);
        }
        finally
        {
            await DeleteMessageAsync(messageId);
        }
    }

    [Test]
    public async Task ExpiredClaim_IsReclaimedAndPublished()
    {
        var messageId = Guid.NewGuid();
        try
        {
            await SeedMessageAsync(new OutboxMessage
            {
                Id = messageId,
                Type = "test.message",
                Payload = "{}",
                QueueName = $"outbox-test-{Guid.NewGuid():N}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ClaimId = Guid.NewGuid(),
                ClaimedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
            });

            var sender = new RecordingQueueSender();
            using var serviceProvider = CreatePublisherServiceProvider(sender);
            var publisher = new TestableOutboxPublisherService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>());

            await publisher.PublishOnceAsync(CancellationToken.None);

            sender.SendCount.ShouldBe(1);

            await using var dbContext = IntegrationTestSetup.CreateDbContext();
            var message = await dbContext.OutboxMessages.SingleAsync(x => x.Id == messageId);
            message.ProcessedAtUtc.ShouldNotBeNull();
            message.ClaimId.ShouldBeNull();
            message.ClaimedUntilUtc.ShouldBeNull();
        }
        finally
        {
            await DeleteMessageAsync(messageId);
        }
    }

    [Test]
    public async Task SendFailure_ReleasesClaimAndIncrementsRetryCount()
    {
        var messageId = Guid.NewGuid();
        try
        {
            await SeedMessageAsync(new OutboxMessage
            {
                Id = messageId,
                Type = "test.message",
                Payload = "{}",
                QueueName = $"outbox-test-{Guid.NewGuid():N}",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var sender = new RecordingQueueSender(
                _ => Task.FromException(new InvalidOperationException("queue unavailable")));
            using var serviceProvider = CreatePublisherServiceProvider(sender);
            var publisher = new TestableOutboxPublisherService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>());

            await publisher.PublishOnceAsync(CancellationToken.None);

            await using var dbContext = IntegrationTestSetup.CreateDbContext();
            var message = await dbContext.OutboxMessages.SingleAsync(x => x.Id == messageId);
            message.ProcessedAtUtc.ShouldBeNull();
            message.ClaimId.ShouldBeNull();
            message.ClaimedUntilUtc.ShouldBeNull();
            message.RetryCount.ShouldBe(1);
            message.Error.ShouldBe("queue unavailable");
        }
        finally
        {
            await DeleteMessageAsync(messageId);
        }
    }

    [Test]
    public async Task Cancellation_DoesNotBurnRetryAndLeavesClaimRecoverable()
    {
        var messageId = Guid.NewGuid();
        try
        {
            await SeedMessageAsync(new OutboxMessage
            {
                Id = messageId,
                Type = "test.message",
                Payload = "{}",
                QueueName = $"outbox-test-{Guid.NewGuid():N}",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            var sendStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sender = new RecordingQueueSender(async cancellationToken =>
            {
                sendStarted.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            });
            using var serviceProvider = CreatePublisherServiceProvider(sender);
            var publisher = new TestableOutboxPublisherService(
                serviceProvider.GetRequiredService<IServiceScopeFactory>());
            using var cancellation = new CancellationTokenSource();

            var publishTask = publisher.PublishOnceAsync(cancellation.Token);
            await sendStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();

            Assert.CatchAsync<OperationCanceledException>(async () => await publishTask);

            await using var dbContext = IntegrationTestSetup.CreateDbContext();
            var message = await dbContext.OutboxMessages.SingleAsync(x => x.Id == messageId);
            message.ProcessedAtUtc.ShouldBeNull();
            message.RetryCount.ShouldBe(0);
            message.ClaimId.ShouldNotBeNull();
            message.ClaimedUntilUtc.ShouldNotBeNull();
            message.ClaimedUntilUtc.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        }
        finally
        {
            await DeleteMessageAsync(messageId);
        }
    }

    private static ServiceProvider CreatePublisherServiceProvider(IQueueSender sender)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(IntegrationTestSetup.DatabaseConnectionString));
        services.AddScoped<IApplicationDbContext>(
            provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IOutboxClaimStore, OutboxClaimStore>();
        services.AddSingleton(sender);

        return services.BuildServiceProvider();
    }

    private static async Task SeedMessageAsync(OutboxMessage message)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();
    }

    private static async Task DeleteMessageAsync(Guid messageId)
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        await dbContext.OutboxMessages
            .Where(x => x.Id == messageId)
            .ExecuteDeleteAsync();
    }

    private sealed class TestableOutboxPublisherService(
        IServiceScopeFactory scopeFactory)
        : OutboxPublisherService(
            scopeFactory,
            NullLogger<OutboxPublisherService>.Instance,
            TimeProvider.System)
    {
        public Task<int> PublishOnceAsync(CancellationToken cancellationToken) =>
            PublishBatchAsync(cancellationToken);
    }

    private sealed class RecordingQueueSender(
        Func<CancellationToken, Task>? send = null) : IQueueSender
    {
        private int _sendCount;

        public int SendCount => Volatile.Read(ref _sendCount);

        public Task SendAsync(
            MessageEnvelope message,
            string? queueName = null,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _sendCount);
            return send?.Invoke(ct) ?? Task.CompletedTask;
        }
    }
}
