using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Queues;

public class AzureQueueSender(QueueServiceClient queueServiceClient, ILogger<AzureQueueSender> logger) : IQueueSender
{
    public async Task SendAsync(MessageEnvelope message, string? queueName = null, CancellationToken ct = default)
    {
        Guard.Against.Null(message);
        Guard.Against.NullOrWhiteSpace(queueName);

        try
        {
            var client = queueServiceClient.GetQueueClient(queueName);
            await client.CreateIfNotExistsAsync(cancellationToken: ct);

            var json = JsonSerializer.Serialize(message);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            await client.SendMessageAsync(base64, cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to send message {MessageId} to queue {QueueName}.",
                message.MessageId, queueName);
            throw;
        }
    }
}
