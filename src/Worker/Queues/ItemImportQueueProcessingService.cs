using Azure.Storage.Queues;

namespace Worker.Queues;

public class ItemImportQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<ItemImportQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory)
    : QueueProcessingService<ItemImportQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        skestock.Shared.Services.ItemImportQueue,
        TimeSpan.FromSeconds(1200));
