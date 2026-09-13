using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.Items.Commands.ProcessItemImportBatch;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using skestock.Shared;
using System.Text.Json;

namespace skestock.Application.Features.Items.Commands.CreateItemImportBatch;

public class CreateItemImportBatchCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateItemImportBatchCommand, Result<ItemImportBatchDto>>
{
    public async ValueTask<Result<ItemImportBatchDto>> Handle(CreateItemImportBatchCommand request,
        CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(user.Id,
            message: "Creating an item import batch requires an authenticated user.");

        if (request.ClientRequestId is { } clientRequestId)
        {
            var existing = await dbContext.ItemImportBatches
                .AsNoTracking()
                .Include(b => b.Files)
                .Where(b => b.UploadedByUserId == identityId && b.ClientRequestId == clientRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                if (!existing.Files.OrderBy(file => file.SortOrder)
                    .Select(file => file.FileMetadataId)
                    .SequenceEqual(request.FileMetadataIds))
                {
                    return Result.Fail(new ItemImportBatchErrors.IdempotencyConflict(clientRequestId));
                }

                return Result.Ok(await LoadDtoAsync(dbContext, existing.Id, cancellationToken));
            }
        }

        var batch = ItemImportBatch.Create(identityId, request.ClientRequestId, request.FileMetadataIds);

        dbContext.ItemImportBatches.Add(batch);

        // The existing item-import queue dispatches by the message's embedded type name, so batch
        // processing uses the same queue and worker plumbing.
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
            var existing = await dbContext.ItemImportBatches
                .AsNoTracking()
                .Include(b => b.Files)
                .Where(b => b.UploadedByUserId == identityId && b.ClientRequestId == request.ClientRequestId)
                .SingleOrDefaultAsync(cancellationToken);

            if (existing is null)
                throw;

            if (!existing.Files.OrderBy(file => file.SortOrder)
                .Select(file => file.FileMetadataId)
                .SequenceEqual(request.FileMetadataIds))
            {
                return Result.Fail(new ItemImportBatchErrors.IdempotencyConflict(request.ClientRequestId.Value));
            }

            return Result.Ok(await LoadDtoAsync(dbContext, existing.Id, cancellationToken));
        }

        return Result.Ok(await LoadDtoAsync(dbContext, batch.Id, cancellationToken));
    }

    internal static async Task<ItemImportBatchDto> LoadDtoAsync(
        IApplicationDbContext dbContext, Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await dbContext.ItemImportBatches
            .AsNoTracking()
            .Include(b => b.Files).ThenInclude(f => f.FileMetadata)
            .Include(b => b.History)
            .SingleAsync(b => b.Id == batchId, cancellationToken);

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
                .OrderBy(f => f.SortOrder)
                .Select(f => new ItemImportBatchFileDto
                {
                    FileMetadataId = f.FileMetadataId,
                    OriginalName = f.FileMetadata.OriginalName,
                    BlobPath = f.FileMetadata.BlobPath,
                    ContentType = f.FileMetadata.ContentType,
                    SizeBytes = f.FileMetadata.SizeBytes,
                    Status = f.FileMetadata.Status,
                    SortOrder = f.SortOrder
                })
                .ToList(),
            History = batch.History
                .OrderBy(h => h.CreatedAtUtc)
                .Select(h => new ImportBatchHistoryDto
                {
                    Status = h.Status,
                    Attempt = h.Attempt,
                    Message = h.Message,
                    CreatedAtUtc = h.CreatedAtUtc
                })
                .ToList()
        };
    }
}
