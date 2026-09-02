using skestock.Domain.Queues;

namespace skestock.Application.Queues.Interfaces;

public interface IQueueSender
{
    Task SendAsync(MessageEnvelope message, string? queueName = null, CancellationToken ct = default);
}
