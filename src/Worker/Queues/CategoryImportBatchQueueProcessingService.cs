using Azure.Storage.Queues;
using Worker.Services;
using SharedServices = skestock.Shared.Services;

namespace Worker.Queues;

public class CategoryImportBatchQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<CategoryImportBatchQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory,
    WorkerHeartbeat heartbeat)
    : QueueProcessingService<CategoryImportBatchQueueProcessingService>(
        queueServiceClient,
        logger,
        scopeFactory,
        heartbeat,
        SharedServices.CategoryImportQueue,
        VisibilityTimeout)
{
    // Batch imports extract every uploaded file, so one message can take several minutes.
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(20);
}
