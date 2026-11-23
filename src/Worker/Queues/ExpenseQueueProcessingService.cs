using Azure.Storage.Queues;
using Worker.Services;
using SharedServices = skestock.Shared.Services;

namespace Worker.Queues;

public class ExpenseQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<ExpenseQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory,
    WorkerHeartbeat heartbeat)
    : QueueProcessingService<ExpenseQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        heartbeat,
        SharedServices.ExpenseQueue,
        VisibilityTimeout)
{
    // Must exceed the processing time of one message, or another instance may receive it concurrently.
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromSeconds(30);
}
