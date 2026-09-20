using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using FluentResults;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Queues;
using skestock.Domain.Queues;
using Worker.Services;

namespace Worker.Queues;

public abstract class QueueProcessingService<TProcessor>(
    QueueServiceClient queueServiceClient,
    ILogger<TProcessor> logger,
    IServiceScopeFactory scopeFactory,
    string queueName,
    TimeSpan visibilityTimeout)
    : BackgroundService
    where TProcessor : class
{
    private readonly QueueClient _client = queueServiceClient.GetQueueClient(queueName);
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    private const int BatchSize = 10;
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
                    visibilityTimeout: visibilityTimeout,
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
                logger.LogError(ex, "Queue polling loop failed for {QueueName}", queueName);
                await Task.Delay(_pollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        MessageEnvelope envelope;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
            envelope = JsonSerializer.Deserialize<MessageEnvelope>(json)
                       ?? throw new InvalidOperationException("Empty envelope");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to deserialize envelope for {QueueName} message {Id}",
                queueName,
                message.MessageId);
            await MoveToPoisonQueueAsync(message, null, cancellationToken);
            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return;
        }

        scope.ServiceProvider.GetRequiredService<AmbientUser>().Id = envelope.UserId;

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

            var processed = await DispatchWithinTransactionAsync(
                request,
                envelope,
                dbContext,
                mediator,
                cancellationToken);

            if (!processed)
            {
                await MoveToPoisonQueueAsync(message, envelope, cancellationToken);
                await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
                return;
            }

            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogInformation(
                "Message {Id} processed concurrently by another instance",
                envelope.MessageId);
            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (ImportBatchProcessingInProgressException)
        {
            logger.LogInformation(
                "Import batch for message {Id} is already being processed",
                envelope.MessageId);
            await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed processing {QueueName} message {Id}, dequeue count {Count}",
                queueName,
                envelope.MessageId,
                message.DequeueCount);

            if (MessageFailureClassifier.IsPermanent(ex) || message.DequeueCount >= MaxDequeueCount)
            {
                await MoveToPoisonQueueAsync(message, envelope, cancellationToken);
                await _client.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            }
        }
    }

    private async Task<bool> DispatchWithinTransactionAsync(
        object request,
        MessageEnvelope envelope,
        IApplicationDbContext dbContext,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(
            strategyCancellationToken => DispatchWithinTransactionCoreAsync(
                request,
                envelope,
                dbContext,
                mediator,
                strategyCancellationToken),
            cancellationToken);
    }

    private async Task<bool> DispatchWithinTransactionCoreAsync(
        object request,
        MessageEnvelope envelope,
        IApplicationDbContext dbContext,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var response = await mediator.Send(request, cancellationToken);

        if (response is IResultBase { IsSuccess: false } failedResult)
        {
            logger.LogError(
                "{QueueName} message {Id} failed: {Errors}",
                queueName,
                envelope.MessageId,
                string.Join("; ", failedResult.Errors.Select(e => e.Message)));

            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = envelope.MessageId,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private async Task MoveToPoisonQueueAsync(
        QueueMessage message,
        MessageEnvelope? envelope,
        CancellationToken cancellationToken)
    {
        var poisonQueue = queueServiceClient.GetQueueClient($"{queueName}-poison");
        await poisonQueue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await poisonQueue.SendMessageAsync(message.MessageText, cancellationToken: cancellationToken);

        if (envelope is not null)
        {
            logger.LogWarning(
                "Message {Id} moved to poison queue after {Count} attempts",
                envelope.MessageId,
                message.DequeueCount);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx && sqlEx.Number is 2627 or 2601;
}
