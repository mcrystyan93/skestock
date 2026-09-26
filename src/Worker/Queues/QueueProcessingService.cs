using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Worker.Queues;

/// <summary>
/// Hosts the transport lifecycle for one Azure Queue: poll, delegate one message to the scoped
/// processor, then acknowledge, retry or poison it according to
/// <see cref="QueueMessageDispositionPolicy"/>. Business dispatch and database transactions live
/// in <see cref="IQueueMessageProcessor"/>.
/// </summary>
/// <remarks>
/// Delivery is at-least-once: a message is only deleted after it was processed, so a crash
/// between processing and deletion re-delivers it, and the processor's idempotency check
/// turns the re-delivery into a <see cref="QueueMessageProcessingStatus.Duplicate"/>.
/// </remarks>
/// <typeparam name="TService">The concrete hosted service; used as the logger category.</typeparam>
public abstract class QueueProcessingService<TService>(
    QueueServiceClient queueServiceClient,
    ILogger<TService> logger,
    IServiceScopeFactory scopeFactory,
    string queueName,
    TimeSpan visibilityTimeout)
    : BackgroundService
    where TService : class
{
    private const int BatchSize = 10;
    private const string PoisonQueueSuffix = "-poison";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly QueueClient _client = queueServiceClient.GetQueueClient(queueName);
    private readonly string _poisonQueueName = queueName + PoisonQueueSuffix;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receivedCount = await ReceiveAndProcessBatchAsync(stoppingToken);
                if (receivedCount == 0)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Messages that were not deleted become visible again after the visibility
                // timeout, so a failed iteration never loses work.
                logger.LogError(ex, "Queue polling loop failed for {QueueName}", queueName);
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    /// <returns>The number of messages received.</returns>
    private async Task<int> ReceiveAndProcessBatchAsync(CancellationToken cancellationToken)
    {
        // The visibility timeout hides received messages from other Worker instances while this
        // one processes them; it must exceed the slowest expected processing time.
        var response = await _client.ReceiveMessagesAsync(
            maxMessages: BatchSize,
            visibilityTimeout: visibilityTimeout,
            cancellationToken: cancellationToken);

        foreach (var message in response.Value)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }

        return response.Value.Length;
    }

    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        // One scope per message: a fresh DbContext and AmbientUser for every message.
        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IQueueMessageProcessor>();
        var result = await processor.ProcessAsync(message, cancellationToken);

        var disposition = QueueMessageDispositionPolicy.Decide(result.Status, message.DequeueCount);
        await ApplyDispositionAsync(message, result, disposition, cancellationToken);
    }

    private async Task ApplyDispositionAsync(
        QueueMessage message,
        QueueMessageProcessingResult result,
        QueueMessageDisposition disposition,
        CancellationToken cancellationToken)
    {
        switch (disposition)
        {
            case QueueMessageDisposition.Delete:
                await DeleteMessageAsync(message, cancellationToken);
                break;

            case QueueMessageDisposition.MoveToPoison:
                LogPoisonReason(message, result.Status);
                // Send before deleting: if deleting fails, the message is re-delivered rather than lost.
                await SendToPoisonQueueAsync(message, result, cancellationToken);
                await DeleteMessageAsync(message, cancellationToken);
                break;

            case QueueMessageDisposition.Retry:
                // No retry counter is stored in the database: Azure owns the delivery count.
                logger.LogWarning(
                    "Message {Id} will be retried from queue {QueueName}; dequeue count is {DequeueCount}.",
                    message.MessageId,
                    queueName,
                    message.DequeueCount);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(disposition), disposition, "Unknown disposition.");
        }
    }

    private void LogPoisonReason(QueueMessage message, QueueMessageProcessingStatus status)
    {
        if (status == QueueMessageProcessingStatus.PermanentFailure)
        {
            logger.LogError(
                "Message {Id} is permanent and will be moved to the poison queue {PoisonQueueName}.",
                message.MessageId,
                _poisonQueueName);
            return;
        }

        logger.LogWarning(
            "Message {Id} reached the retry limit for queue {QueueName}.",
            message.MessageId,
            queueName);
    }

    private Task DeleteMessageAsync(QueueMessage message, CancellationToken cancellationToken) =>
        _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

    // The raw message text is forwarded unchanged, so even undecodable messages can be inspected.
    private async Task SendToPoisonQueueAsync(
        QueueMessage message,
        QueueMessageProcessingResult result,
        CancellationToken cancellationToken)
    {
        var poisonQueue = queueServiceClient.GetQueueClient(_poisonQueueName);
        await poisonQueue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await poisonQueue.SendMessageAsync(message.MessageText, cancellationToken: cancellationToken);

        logger.LogWarning(
            "Message {Id} moved to poison queue {PoisonQueueName}. Envelope decoded: {HasEnvelope}.",
            result.Envelope?.MessageId ?? Guid.Empty,
            _poisonQueueName,
            result.Envelope is not null);
    }
}
