using System.Text.Json;
using Azure.Storage.Queues;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Queues;

public class AzureQueueSender(QueueServiceClient queueServiceClient) : IQueueSender
{
    public async Task SendAsync(MessageEnvelope message, string? queueName = null, CancellationToken ct = default)
    {
        var client = queueServiceClient.GetQueueClient(queueName ?? "default-queue");
        await client.CreateIfNotExistsAsync(cancellationToken: ct);
        
        var json = JsonSerializer.Serialize(message);
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        
        await client.SendMessageAsync(base64, cancellationToken: ct);
    }
}
