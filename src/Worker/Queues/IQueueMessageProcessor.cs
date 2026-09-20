using Azure.Storage.Queues.Models;

namespace Worker.Queues;

/// <summary>
/// Processes one received message and returns a lifecycle decision to the queue host. It does not
/// acknowledge or poison messages; those transport actions belong to the polling module.
/// </summary>
public interface IQueueMessageProcessor
{
    Task<QueueMessageProcessingResult> ProcessAsync(
        QueueMessage message,
        CancellationToken cancellationToken);
}
