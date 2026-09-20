using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Queues;

public class AzureQueueSender(
    QueueServiceClient queueServiceClient,
    IMessageEnvelopeSerializer envelopeSerializer,
    ILogger<AzureQueueSender> logger) : IQueueSender
{
    /// <summary>
    /// Creates the destination queue on demand and writes the shared envelope format. The sender
    /// intentionally knows nothing about outbox claims or Worker processing.
    /// </summary>
    public async Task SendAsync(MessageEnvelope message, string? queueName = null, CancellationToken ct = default)
    {
        Guard.Against.Null(message);
        Guard.Against.NullOrWhiteSpace(queueName);

        try
        {
            var client = queueServiceClient.GetQueueClient(queueName);
            await client.CreateIfNotExistsAsync(cancellationToken: ct);

            await client.SendMessageAsync(envelopeSerializer.Serialize(message), cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to send message {MessageId} to queue {QueueName}.",
                message.MessageId, queueName);
            throw;
        }
    }
}
