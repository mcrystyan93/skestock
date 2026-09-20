using System.Text.Json;
using Azure.Storage.Queues.Models;
using FluentResults;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Queues;
using skestock.Application.Queues.Interfaces;
using skestock.Domain.Queues;
using Worker.Services;

namespace Worker.Queues;

/// <summary>
/// Owns the business side of one queue message: decode, restore the acting user, deduplicate,
/// dispatch through Mediator, and atomically persist the processed marker with handler changes.
/// </summary>
public sealed class QueueMessageProcessor(
    IApplicationDbContext dbContext,
    ISender mediator,
    AmbientUser ambientUser,
    IMessageEnvelopeSerializer envelopeSerializer,
    ILogger<QueueMessageProcessor> logger,
    TimeProvider timeProvider) : IQueueMessageProcessor
{
    public async Task<QueueMessageProcessingResult> ProcessAsync(
        QueueMessage message,
        CancellationToken cancellationToken)
    {
        MessageEnvelope envelope;
        try
        {
            envelope = envelopeSerializer.Deserialize(message.MessageText);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize queue message {Id}.", message.MessageId);
            return QueueMessageProcessingResult.Permanent();
        }

        ambientUser.Id = envelope.UserId;

        var alreadyProcessed = await dbContext.ProcessedMessages
            .AnyAsync(x => x.Id == envelope.MessageId, cancellationToken);

        if (alreadyProcessed)
        {
            logger.LogInformation("Message {Id} already processed, skipping", envelope.MessageId);
            return QueueMessageProcessingResult.Duplicate(envelope);
        }

        try
        {
            var requestType = Type.GetType(envelope.Type)
                              ?? throw new InvalidOperationException(
                                  $"Unknown message type: {envelope.Type}");
            var request = JsonSerializer.Deserialize(envelope.Payload, requestType)
                          ?? throw new InvalidOperationException("Empty message payload");

            var processed = await DispatchWithinTransactionAsync(
                request,
                envelope,
                cancellationToken);

            return processed
                ? QueueMessageProcessingResult.Succeeded(envelope)
                : QueueMessageProcessingResult.Permanent(envelope);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            logger.LogInformation(
                "Message {Id} was processed concurrently by another instance.",
                envelope.MessageId);
            return QueueMessageProcessingResult.Duplicate(envelope);
        }
        catch (ImportBatchProcessingInProgressException)
        {
            logger.LogInformation(
                "Import batch for message {Id} is already being processed.",
                envelope.MessageId);
            return QueueMessageProcessingResult.Duplicate(envelope);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed dispatching message {Id}; it will be retried unless the failure is permanent.",
                envelope.MessageId);

            return MessageFailureClassifier.IsPermanent(ex)
                ? QueueMessageProcessingResult.Permanent(envelope)
                : QueueMessageProcessingResult.Retryable(envelope);
        }
    }

    private async Task<bool> DispatchWithinTransactionAsync(
        object request,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(
            strategyCancellationToken => DispatchWithinTransactionCoreAsync(
                request,
                envelope,
                strategyCancellationToken),
            cancellationToken);
    }

    private async Task<bool> DispatchWithinTransactionCoreAsync(
        object request,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var response = await mediator.Send(request, cancellationToken);

        if (response is IResultBase { IsSuccess: false } failedResult)
        {
            logger.LogError(
                "Message {Id} returned failed result: {Errors}",
                envelope.MessageId,
                string.Join("; ", failedResult.Errors.Select(e => e.Message)));

            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            Id = envelope.MessageId,
            ProcessedAtUtc = timeProvider.GetUtcNow()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx && sqlEx.Number is 2627 or 2601;
}
