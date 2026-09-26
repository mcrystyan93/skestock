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
/// <remarks>
/// Idempotency: a <see cref="ProcessedMessage"/> row keyed by the envelope's message id is saved
/// in the same transaction as the handler's changes. Either both are committed or neither is, so
/// a re-delivered message is either skipped (already committed) or safely processed again.
/// </remarks>
public sealed class QueueMessageProcessor(
    IApplicationDbContext dbContext,
    ISender mediator,
    AmbientUser ambientUser,
    IMessageEnvelopeSerializer envelopeSerializer,
    ILogger<QueueMessageProcessor> logger,
    TimeProvider timeProvider) : IQueueMessageProcessor
{
    // SQL Server error numbers for duplicate keys in a unique constraint / unique index.
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlUniqueIndexViolation = 2601;

    public async Task<QueueMessageProcessingResult> ProcessAsync(
        QueueMessage message,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeEnvelope(message, out var envelope))
        {
            return QueueMessageProcessingResult.Permanent();
        }

        // Handlers authorize and audit as the user who originally triggered the work.
        ambientUser.Id = envelope.UserId;

        if (await IsAlreadyProcessedAsync(envelope, cancellationToken))
        {
            logger.LogInformation("Message {Id} already processed, skipping", envelope.MessageId);
            return QueueMessageProcessingResult.Duplicate(envelope);
        }

        try
        {
            var request = DeserializeRequest(envelope);
            var committed = await DispatchWithinTransactionAsync(request, envelope, cancellationToken);

            return committed
                ? QueueMessageProcessingResult.Succeeded(envelope)
                : QueueMessageProcessingResult.Permanent(envelope);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Another instance committed the same ProcessedMessage id first (the pre-check above
            // is only a fast path; the primary key is the real guard against concurrent delivery).
            logger.LogInformation("Message {Id} was processed concurrently by another instance.", envelope.MessageId);
            return QueueMessageProcessingResult.Duplicate(envelope);
        }
        catch (ImportBatchProcessingInProgressException)
        {
            logger.LogInformation("Import batch for message {Id} is already being processed.", envelope.MessageId);
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

    // An undecodable message can never succeed, so it is reported as permanent (without envelope).
    private bool TryDecodeEnvelope(QueueMessage message, out MessageEnvelope envelope)
    {
        try
        {
            envelope = envelopeSerializer.Deserialize(message.MessageText);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize queue message {Id}.", message.MessageId);
            envelope = null!;
            return false;
        }
    }

    private Task<bool> IsAlreadyProcessedAsync(MessageEnvelope envelope, CancellationToken cancellationToken) =>
        dbContext.ProcessedMessages.AnyAsync(x => x.Id == envelope.MessageId, cancellationToken);

    /// <summary>
    /// Restores the original Mediator request from the envelope's type name and JSON payload.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The type cannot be resolved or the payload is empty. Kept retryable so that a Worker that
    /// knows the type (e.g. after a rolling deployment) can still pick the message up.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The type is not a Mediator message; classified as permanent.
    /// </exception>
    private static object DeserializeRequest(MessageEnvelope envelope)
    {
        var requestType = Type.GetType(envelope.Type)
                          ?? throw new InvalidOperationException($"Unknown message type: {envelope.Type}");

        // Guards against the envelope naming an arbitrary type to instantiate from JSON.
        if (!typeof(IMessage).IsAssignableFrom(requestType))
        {
            throw new ArgumentException($"Message type is not a Mediator request: {envelope.Type}");
        }

        return JsonSerializer.Deserialize(envelope.Payload, requestType)
               ?? throw new InvalidOperationException("Empty message payload");
    }

    // The retrying execution strategy does not allow user-initiated transactions on their own;
    // the whole transaction must run inside the strategy so it can be replayed as a unit.
    private Task<bool> DispatchWithinTransactionAsync(
        object request,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();

        return executionStrategy.ExecuteAsync(
            strategyCancellationToken => DispatchAndMarkProcessedAsync(request, envelope, strategyCancellationToken),
            cancellationToken);
    }

    /// <returns>
    /// <see langword="true"/> when committed; <see langword="false"/> when the handler returned a
    /// failed result, in which case every change it saved is rolled back.
    /// </returns>
    private async Task<bool> DispatchAndMarkProcessedAsync(
        object request,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var response = await mediator.Send(request, cancellationToken);

        if (response is IResultBase { IsFailed: true } failedResult)
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
        ex.InnerException is SqlException { Number: SqlUniqueConstraintViolation or SqlUniqueIndexViolation };
}
