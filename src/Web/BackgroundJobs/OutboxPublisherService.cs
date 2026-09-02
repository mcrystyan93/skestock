using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using skestock.Application.Common.Interfaces;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;

namespace skestock.Web.BackgroundJobs;

public class OutboxPublisherService(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherService> logger)
    : BackgroundService
{
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;
    private const int MaxRetries = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await PublishBatchAsync(stoppingToken);
                
                if (published == 0)
                    await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "An error occurred while publishing outbox messages.");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    private async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var queueSender = scope.ServiceProvider.GetRequiredService<IQueueSender>();

        // Pull a batch, oldest first. Row-lock so multiple Worker replicas don't grab the same rows.
        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAtUtc == null && x.RetryCount < MaxRetries)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return 0;

        foreach (OutboxMessage outboxMessage in messages)
        {
            try
            {
                var envelope = new MessageEnvelope
                {
                    MessageId = outboxMessage.Id, Type = outboxMessage.Type, Payload = outboxMessage.Payload
                };
                
                await queueSender.SendAsync(envelope, outboxMessage.QueueName, cancellationToken);
                
                outboxMessage.ProcessedAtUtc = DateTime.UtcNow;
                outboxMessage.Error = null;
            }
            catch (Exception ex)
            {
                outboxMessage.RetryCount++;
                outboxMessage.Error = ex.Message;
                logger.LogError(ex, "Failed to publish message {Id} to queue, attempt {RetryCount}.", outboxMessage.Id,
                    outboxMessage.RetryCount);
            }
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
