using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using skestock.Shared;

namespace skestock.Application.Features.Categories.Commands.CreateCategoryImportBatch;

public class CreateCategoryImportBatchCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateCategoryImportBatchCommand, Result<CategoryImportBatchMutationDto>>
{
    public async ValueTask<Result<CategoryImportBatchMutationDto>> Handle(
        CreateCategoryImportBatchCommand request, CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(user.Id,
            message: "Creating a category import batch requires an authenticated user.");

        if (request.ClientRequestId is { } clientRequestId)
        {
            var existing = await dbContext.CategoryImportBatches
                .AsNoTracking()
                .Include(batch => batch.Files)
                .Where(batch => batch.UploadedByUserId == identityId && batch.ClientRequestId == clientRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                if (!existing.Files.OrderBy(file => file.SortOrder)
                    .Select(file => file.FileMetadataId)
                    .SequenceEqual(request.FileMetadataIds))
                    return Result.Fail(new CategoryImportBatchErrors.IdempotencyConflict(clientRequestId));

                return Result.Ok(ToMutationDto(existing));
            }
        }

        var batch = CategoryImportBatch.Create(identityId, request.ClientRequestId, request.FileMetadataIds);
        dbContext.CategoryImportBatches.Add(batch);
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = typeof(ProcessCategoryImportBatchCommand).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(new ProcessCategoryImportBatchCommand(batch.Id)),
            QueueName = Services.CategoryImportQueue,
            UserId = identityId
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (request.ClientRequestId is not null)
        {
            var existing = await dbContext.CategoryImportBatches
                .AsNoTracking()
                .Include(importBatch => importBatch.Files)
                .Where(importBatch => importBatch.UploadedByUserId == identityId &&
                                      importBatch.ClientRequestId == request.ClientRequestId)
                .SingleOrDefaultAsync(cancellationToken);

            if (existing is null)
                throw;

            if (!existing.Files.OrderBy(file => file.SortOrder)
                .Select(file => file.FileMetadataId)
                .SequenceEqual(request.FileMetadataIds))
                return Result.Fail(new CategoryImportBatchErrors.IdempotencyConflict(request.ClientRequestId.Value));

            return Result.Ok(ToMutationDto(existing));
        }

        return Result.Ok(ToMutationDto(batch));
    }

    private static CategoryImportBatchMutationDto ToMutationDto(CategoryImportBatch batch)
    {
        return new CategoryImportBatchMutationDto
        {
            Id = batch.Id,
            Status = batch.Status
        };
    }
}
