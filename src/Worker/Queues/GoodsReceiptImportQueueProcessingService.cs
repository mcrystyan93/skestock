using Azure.Storage.Queues;
using Worker.Services;
using SharedServices = skestock.Shared.Services;

namespace Worker.Queues;

public class GoodsReceiptImportQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<GoodsReceiptImportQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory,
    WorkerHeartbeat heartbeat)
    : QueueProcessingService<GoodsReceiptImportQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        heartbeat,
        SharedServices.GoodsReceiptImportQueue,
        VisibilityTimeout)
{
    // Must exceed the processing time of one message, or another instance may receive it concurrently.
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromSeconds(30);
}
