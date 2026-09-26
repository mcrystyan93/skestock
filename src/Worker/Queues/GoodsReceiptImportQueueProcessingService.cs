using Azure.Storage.Queues;
using SharedServices = skestock.Shared.Services;

namespace Worker.Queues;

public class GoodsReceiptImportQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<GoodsReceiptImportQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory)
    : QueueProcessingService<GoodsReceiptImportQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        SharedServices.GoodsReceiptImportQueue,
        VisibilityTimeout)
{
    // Must exceed the processing time of one message, or another instance may receive it concurrently.
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromSeconds(30);
}
