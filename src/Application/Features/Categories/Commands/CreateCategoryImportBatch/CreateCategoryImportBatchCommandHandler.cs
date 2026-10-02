using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.ProcessCategoryImportBatch;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
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
            var existing = await FindExistingBatchAsync(identityId, clientRequestId, cancellationToken);

            if (existing is not null)
                return MatchExistingBatch(existing, clientRequestId, request.FileMetadataIds);
        }

        var batch = CategoryImportBatch.Create(identityId, request.ClientRequestId, request.FileMetadataIds);
        dbContext.CategoryImportBatches.Add(batch);
        // Persist the queue message with the batch so a successful creation always schedules processing.
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
            // Another request may have won the unique client-request-id race.
            var existing = await FindExistingBatchAsync(identityId, request.ClientRequestId.Value, cancellationToken);

            if (existing is null)
                throw;

            return MatchExistingBatch(existing, request.ClientRequestId.Value, request.FileMetadataIds);
        }

        return Result.Ok(ToMutationDto(batch));
    }

    private Task<CategoryImportBatch?> FindExistingBatchAsync(
        Guid identityId, Guid clientRequestId, CancellationToken cancellationToken)
    {
        return dbContext.CategoryImportBatches
            .AsNoTracking()
            .Include(batch => batch.Files)
            .SingleOrDefaultAsync(batch =>
                batch.UploadedByUserId == identityId && batch.ClientRequestId == clientRequestId,
                cancellationToken);
    }

    private static Result<CategoryImportBatchMutationDto> MatchExistingBatch(
        CategoryImportBatch batch, Guid clientRequestId, IReadOnlyCollection<Guid> fileMetadataIds)
    {
        var originalFileIds = batch.Files.OrderBy(file => file.SortOrder)
            .Select(file => file.FileMetadataId);

        if (!originalFileIds.SequenceEqual(fileMetadataIds))
            return Result.Fail(new CategoryImportBatchErrors.IdempotencyConflict(clientRequestId));

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
