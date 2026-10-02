using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Commands.ProcessItemImportBatch;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Items;
using skestock.Domain.Queues;
using skestock.Shared;

namespace skestock.Application.Features.Items.Commands.CreateItemImportBatch;

public class CreateItemImportBatchCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateItemImportBatchCommand, Result<ItemImportBatchDto>>
{
    public async ValueTask<Result<ItemImportBatchDto>> Handle(
        CreateItemImportBatchCommand request, CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(user.Id,
            message: "Creating an item import batch requires an authenticated user.");

        if (request.ClientRequestId is { } clientRequestId)
        {
            var existing = await FindExistingBatchAsync(identityId, clientRequestId, cancellationToken);

            if (existing is not null)
                return await MatchExistingBatchAsync(existing, clientRequestId, request.FileMetadataIds, cancellationToken);
        }

        var batch = ItemImportBatch.Create(identityId, request.ClientRequestId, request.FileMetadataIds);
        dbContext.ItemImportBatches.Add(batch);

        // Persist the typed queue message with the batch so a successful creation schedules processing.
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(ProcessItemImportBatchCommand).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new ProcessItemImportBatchCommand(batch.Id)),
            QueueName = Services.ItemImportQueue,
            UserId = identityId
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (request.ClientRequestId is not null)
        {
            // Another request may have won the unique client-request-id race.
            var existing = await FindExistingBatchAsync(identityId, request.ClientRequestId.Value, cancellationToken);

            if (existing is null)
                throw;

            return await MatchExistingBatchAsync(
                existing, request.ClientRequestId.Value, request.FileMetadataIds, cancellationToken);
        }

        return Result.Ok(await LoadDtoAsync(dbContext, batch.Id, cancellationToken));
    }

    private Task<ItemImportBatch?> FindExistingBatchAsync(
        Guid identityId, Guid clientRequestId, CancellationToken cancellationToken)
    {
        return dbContext.ItemImportBatches
            .AsNoTracking()
            .Include(batch => batch.Files)
            .SingleOrDefaultAsync(batch =>
                batch.UploadedByUserId == identityId && batch.ClientRequestId == clientRequestId,
                cancellationToken);
    }

    private async Task<Result<ItemImportBatchDto>> MatchExistingBatchAsync(
        ItemImportBatch batch, Guid clientRequestId, IReadOnlyCollection<Guid> fileMetadataIds,
        CancellationToken cancellationToken)
    {
        var originalFileIds = batch.Files.OrderBy(file => file.SortOrder)
            .Select(file => file.FileMetadataId);

        if (!originalFileIds.SequenceEqual(fileMetadataIds))
            return Result.Fail(new ItemImportBatchErrors.IdempotencyConflict(clientRequestId));

        return Result.Ok(await LoadDtoAsync(dbContext, batch.Id, cancellationToken));
    }

    private static async Task<ItemImportBatchDto> LoadDtoAsync(
        IApplicationDbContext dbContext, Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await dbContext.ItemImportBatches
            .AsNoTracking()
            .Include(importBatch => importBatch.Files).ThenInclude(file => file.FileMetadata)
            .Include(importBatch => importBatch.History)
            .SingleAsync(importBatch => importBatch.Id == batchId, cancellationToken);

        return new ItemImportBatchDto
        {
            Id = batch.Id,
            Status = batch.Status,
            ClientRequestId = batch.ClientRequestId,
            AttemptCount = batch.AttemptCount,
            UploadedAt = batch.UploadedAt,
            ProcessedAt = batch.ProcessedAt,
            ErrorMessage = batch.ErrorMessage,
            Files = batch.Files
                .OrderBy(file => file.SortOrder)
                .Select(file => new ItemImportBatchFileDto
                {
                    FileMetadataId = file.FileMetadataId,
                    OriginalName = file.FileMetadata.OriginalName,
                    BlobPath = file.FileMetadata.BlobPath,
                    ContentType = file.FileMetadata.ContentType,
                    SizeBytes = file.FileMetadata.SizeBytes,
                    Status = file.FileMetadata.Status,
                    SortOrder = file.SortOrder
                })
                .ToList(),
            History = batch.History
                .OrderBy(history => history.CreatedAtUtc)
                .Select(history => new ImportBatchHistoryDto
                {
                    Status = history.Status,
                    Attempt = history.Attempt,
                    Message = history.Message,
                    CreatedAtUtc = history.CreatedAtUtc
                })
                .ToList()
        };
    }
}
