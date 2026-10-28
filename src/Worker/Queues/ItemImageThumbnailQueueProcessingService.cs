using Azure.Storage.Queues;
using Worker.Services;
using SharedServices = skestock.Shared.Services;

namespace Worker.Queues;

public class ItemImageThumbnailQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<ItemImageThumbnailQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory,
    WorkerHeartbeat heartbeat)
    : QueueProcessingService<ItemImageThumbnailQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        heartbeat,
        SharedServices.ItemImageThumbnailQueue,
        VisibilityTimeout)
{
    // Kept separate from item-import so a thumbnail never waits behind a slow AI batch.
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(2);
}
