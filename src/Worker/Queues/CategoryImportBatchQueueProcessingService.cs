using Azure.Storage.Queues;

namespace Worker.Queues;

public class CategoryImportBatchQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<CategoryImportBatchQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory)
    : QueueProcessingService<CategoryImportBatchQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        skestock.Shared.Services.CategoryImportQueue,
        TimeSpan.FromSeconds(1200));
