using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Queues;
using skestock.Shared;
using Worker.Services;

namespace Worker.Queues;

public class GoodsReceiptImportQueueProcessingService(
    QueueServiceClient queueServiceClient,
    ILogger<GoodsReceiptImportQueueProcessingService> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly QueueClient _client =
        queueServiceClient.GetQueueClient(skestock.Shared.Services.GoodsReceiptImportQueue);

    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 10;
    private const int MaxRetries = 5;
    private const int VisibilityTimeoutSeconds = 30;
    private const int MaxDequeueCount = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _client.ReceiveMessagesAsync(
                    maxMessages: BatchSize,
                    visibilityTimeout: TimeSpan.FromSeconds(VisibilityTimeoutSeconds),
                    cancellationToken: stoppingToken);

                if (response.Value.Length == 0)
                {
                    await Task.Delay(_pollInterval, stoppingToken);
                    continue;
                }

                foreach (var message in response.Value)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Queue polling loop failed");
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>(); // MediatR ISender, template convention

        MessageEnvelope envelope;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
            envelope = JsonSerializer.Deserialize<MessageEnvelope>(json)
                       ?? throw new InvalidOperationException("Empty envelope");
        }
        catch (Exception ex)
        {
            // malformed message — can't be retried into success, move it out immediately
            logger.LogError(ex, "Failed to deserialize envelope for message {Id}", message.MessageId);
            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return;
        }

        // Populate the ambient user for this message scope so command handlers / pipeline
        // behaviours resolve the originating user captured at enqueue time.
        scope.ServiceProvider.GetRequiredService<AmbientUser>().Id = envelope.UserId;

        // idempotency pre-check (fast path, avoids re-running business logic on known duplicates)
        var alreadyProcessed = await dbContext.ProcessedMessages
            .AnyAsync(x => x.Id == envelope.MessageId, cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation("Message {Id} already processed, skipping", envelope.MessageId);
            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return;
        }

        try
        {
            var type = Type.GetType(envelope.Type)
                       ?? throw new InvalidOperationException($"Unknown message type: {envelope.Type}");
            var request = JsonSerializer.Deserialize(envelope.Payload, type)
                          ?? throw new InvalidOperationException("Empty payload");

            // dispatch into Application layer — same handlers Web would call
            await mediator.Send(request, cancellationToken);

            dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                Id = envelope.MessageId, ProcessedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);

            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // race: another instance processed this message concurrently — safe to treat as done
            logger.LogInformation("Message {Id} processed concurrently by another instance", envelope.MessageId);
            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing message {Id}, dequeue count {Count}",
                envelope.MessageId, message.DequeueCount);

            if (message.DequeueCount >= MaxDequeueCount)
            {
                await MoveToPoisonQueueAsync(message, envelope, cancellationToken);
                await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            }
            // else: don't delete — visibility timeout expires, Storage Queue redelivers automatically
        }
    }

    private async Task MoveToPoisonQueueAsync(QueueMessage msg, MessageEnvelope envelope, CancellationToken ct)
    {
        var poisonQueue =
            queueServiceClient.GetQueueClient($"{skestock.Shared.Services.GoodsReceiptImportQueue}-poison");
        await poisonQueue.CreateIfNotExistsAsync(cancellationToken: ct);
        await poisonQueue.SendMessageAsync(msg.MessageText, cancellationToken: ct);

        logger.LogWarning("Message {Id} moved to poison queue after {Count} attempts",
            envelope.MessageId, msg.DequeueCount);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx && (sqlEx.Number is 2627 or 2601); // adjust for your provider
}
