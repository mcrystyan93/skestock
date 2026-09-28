using System.Diagnostics;
using skestock.Application.Queues;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;

namespace skestock.Web.BackgroundJobs;

/// <summary>
/// Polls the outbox and sends claimed messages to Azure Queue. Database claim ownership and state
/// transitions live in <see cref="IOutboxClaimStore"/>; this module only coordinates the external
/// send and the publisher's retry loop.
/// </summary>
public class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherService> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly OutboxClaimOptions _claimOptions = new(
        BatchSize: 50,
        MaxRetries: 5,
        Lease: TimeSpan.FromMinutes(5));

    // COUNT over the pending rows is cheap but not free; the gauge only needs coarse freshness.
    private static readonly TimeSpan PendingRefreshInterval = TimeSpan.FromSeconds(30);
    private DateTimeOffset? _pendingRefreshedAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await PublishBatchAsync(stoppingToken);

                if (published == 0)
                    await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "An error occurred while publishing outbox messages.");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Claims one batch, sends each envelope, and records the result only while the claim is still
    /// owned by this invocation. Keeping this operation callable through the protected seam makes
    /// the delivery policy testable without waiting for the hosted five-second poll.
    /// </summary>
    protected async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var claimStore = scope.ServiceProvider.GetRequiredService<IOutboxClaimStore>();
        var queueSender = scope.ServiceProvider.GetRequiredService<IQueueSender>();
        var claim = await claimStore.ClaimBatchAsync(
            _claimOptions,
            timeProvider.GetUtcNow(),
            cancellationToken);

        foreach (var outboxMessage in claim.Messages)
        {
            var envelope = new MessageEnvelope
            {
                MessageId = outboxMessage.Id,
                Type = outboxMessage.Type,
                Payload = outboxMessage.Payload,
                UserId = outboxMessage.UserId,
                TraceParent = outboxMessage.TraceParent,
                TraceState = outboxMessage.TraceState
            };

            // Continues the trace of the request that wrote the outbox row; the queue send span
            // nests under this producer span.
            using var activity = MessagingTelemetry.StartActivity(
                $"publish {outboxMessage.QueueName}",
                ActivityKind.Producer,
                outboxMessage.TraceParent,
                outboxMessage.TraceState);
            activity?.SetTag(MessagingTelemetry.SystemTag, MessagingTelemetry.SystemName);
            activity?.SetTag(MessagingTelemetry.DestinationNameTag, outboxMessage.QueueName);
            activity?.SetTag(MessagingTelemetry.MessageIdTag, outboxMessage.Id);
            var destination = outboxMessage.QueueName ?? "unknown";

            try
            {
                await queueSender.SendAsync(envelope, outboxMessage.QueueName, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutdown does not consume a retry. The lease remains as the crash-safe recovery
                // mechanism if the process ends before it can release the claim.
                throw;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                var errorType = ex.GetType().FullName ?? ex.GetType().Name;
                activity?.SetTag(MessagingTelemetry.ErrorTypeTag, errorType);
                RecordSent(destination, errorType);

                var retryCount = await claimStore.RecordFailureAsync(
                    claim,
                    outboxMessage.Id,
                    ex.Message,
                    cancellationToken);

                if (retryCount is null)
                {
                    logger.LogWarning(
                        "Failed to publish outbox message {Id}, but the publisher no longer owns its claim.",
                        outboxMessage.Id);
                    continue;
                }

                logger.LogError(ex, "Failed to publish message {Id} to queue, attempt {RetryCount}.",
                    outboxMessage.Id, retryCount.Value);

                if (retryCount >= _claimOptions.MaxRetries)
                    logger.LogWarning(
                        "Outbox message {Id} dead-lettered after {MaxRetries} attempts and will no longer be published. Last error: {Error}",
                        outboxMessage.Id, _claimOptions.MaxRetries, ex.Message);

                continue;
            }

            RecordSent(destination, errorType: null);
            MessagingTelemetry.OutboxLag.Record(
                (timeProvider.GetUtcNow() - outboxMessage.CreatedAtUtc).TotalSeconds,
                DestinationTags(destination));

            var completed = await claimStore.MarkPublishedAsync(
                claim,
                outboxMessage.Id,
                timeProvider.GetUtcNow(),
                cancellationToken);

            if (!completed)
            {
                logger.LogWarning(
                    "Outbox message {Id} was sent but its claim was no longer owned by this publisher.",
                    outboxMessage.Id);
            }
        }

        await RefreshPendingCountAsync(claimStore, cancellationToken);

        return claim.Messages.Count;
    }

    private async Task RefreshPendingCountAsync(IOutboxClaimStore claimStore, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (_pendingRefreshedAt is { } refreshedAt && now - refreshedAt < PendingRefreshInterval)
        {
            return;
        }

        _pendingRefreshedAt = now;
        try
        {
            MessagingTelemetry.ReportOutboxPending(
                await claimStore.CountPendingAsync(_claimOptions.MaxRetries, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Metrics must never stop publishing; the gauge keeps its previous value.
            logger.LogWarning(ex, "Could not count pending outbox messages.");
        }
    }

    private static void RecordSent(string destination, string? errorType)
    {
        var tags = DestinationTags(destination);
        if (errorType is not null)
        {
            tags.Add(MessagingTelemetry.ErrorTypeTag, errorType);
        }

        MessagingTelemetry.SentMessages.Add(1, tags);
    }

    private static TagList DestinationTags(string destination) => new()
    {
        { MessagingTelemetry.SystemTag, MessagingTelemetry.SystemName },
        { MessagingTelemetry.DestinationNameTag, destination }
    };
}
