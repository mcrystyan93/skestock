using Azure.Storage.Queues;

namespace Worker.Queues;

public class GoodsReceiptImportQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<GoodsReceiptImportQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory)
    : QueueProcessingService<GoodsReceiptImportQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        skestock.Shared.Services.GoodsReceiptImportQueue,
        TimeSpan.FromSeconds(30));
