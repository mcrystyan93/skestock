using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.DependencyInjection;
using skestock.Domain.Queues;

namespace Worker.Queues;

/// <summary>
/// Hosts the transport lifecycle for one Azure Queue: poll, delegate one message to the scoped
/// processor, then acknowledge or poison it according to the returned decision. Business dispatch
/// and database transactions live in <see cref="IQueueMessageProcessor"/>.
/// </summary>
public abstract class QueueProcessingService<TProcessor>(
    QueueServiceClient queueServiceClient,
    ILogger<TProcessor> logger,
    IServiceScopeFactory scopeFactory,
    string queueName,
    TimeSpan visibilityTimeout)
    : BackgroundService
    where TProcessor : class
{
    private readonly QueueClient _client = queueServiceClient.GetQueueClient(queueName);
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    private const int BatchSize = 10;
    private const int MaxDequeueCount = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _client.ReceiveMessagesAsync(
                    maxMessages: BatchSize,
                    visibilityTimeout: visibilityTimeout,
                    cancellationToken: stoppingToken);

                if (response.Value.Length == 0)
                {
                    await Task.Delay(_pollInterval, stoppingToken);
                    continue;
                }

                foreach (var message in response.Value)
                    await ProcessMessageAsync(message, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Queue polling loop failed for {QueueName}", queueName);
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(
        QueueMessage message,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IQueueMessageProcessor>();
        var result = await processor.ProcessAsync(message, cancellationToken);

        switch (result.Status)
        {
            case QueueMessageProcessingStatus.Succeeded:
            case QueueMessageProcessingStatus.Duplicate:
                await DeleteMessageAsync(message, cancellationToken);
                break;

            case QueueMessageProcessingStatus.PermanentFailure:
                logger.LogError(
                    "Message {Id} is permanent and will be moved to the poison queue {PoisonQueueName}.",
                    message.MessageId,
                    $"{queueName}-poison");
                await MoveToPoisonQueueAsync(message, result.Envelope, cancellationToken);
                await DeleteMessageAsync(message, cancellationToken);
                break;

            case QueueMessageProcessingStatus.RetryableFailure when
                message.DequeueCount >= MaxDequeueCount:
                logger.LogWarning(
                    "Message {Id} reached the retry limit for queue {QueueName}.",
                    message.MessageId,
                    queueName);
                await MoveToPoisonQueueAsync(message, result.Envelope, cancellationToken);
                await DeleteMessageAsync(message, cancellationToken);
                break;

            case QueueMessageProcessingStatus.RetryableFailure:
                // Leaving the message undeleted lets Azure Queue make it visible again after the
                // visibility timeout. No retry counter is stored in the database because Azure
                // owns the delivery count for transport failures.
                logger.LogWarning(
                    "Message {Id} will be retried from queue {QueueName}; dequeue count is {DequeueCount}.",
                    message.MessageId,
                    queueName,
                    message.DequeueCount);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private Task DeleteMessageAsync(
        QueueMessage message,
        CancellationToken cancellationToken) =>
        _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

    private async Task MoveToPoisonQueueAsync(
        QueueMessage message,
        MessageEnvelope? envelope,
        CancellationToken cancellationToken)
    {
        var poisonQueue = queueServiceClient.GetQueueClient($"{queueName}-poison");
        await poisonQueue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await poisonQueue.SendMessageAsync(message.MessageText, cancellationToken: cancellationToken);

        logger.LogWarning(
            "Message {Id} moved to poison queue {PoisonQueueName}. Envelope decoded: {HasEnvelope}.",
            envelope?.MessageId ?? Guid.Empty,
            $"{queueName}-poison",
            envelope is not null);
    }
}
