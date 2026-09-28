using Azure.Storage.Queues;
using Worker.Services;
using SharedServices = skestock.Shared.Services;

namespace Worker.Queues;

public class ItemImportQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<ItemImportQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory,
    WorkerHeartbeat heartbeat)
    : QueueProcessingService<ItemImportQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        heartbeat,
        SharedServices.ItemImportQueue,
        VisibilityTimeout)
{
    // Batch imports extract every uploaded file, so one message can take several minutes.
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(20);
}
