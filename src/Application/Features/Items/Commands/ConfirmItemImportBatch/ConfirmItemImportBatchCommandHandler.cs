using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Commands.ConfirmItemImportBatch;

public class ConfirmItemImportBatchCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<ConfirmItemImportBatchCommand, Result<ItemImportBatchConfirmationResultDto>>
{
    public async ValueTask<Result<ItemImportBatchConfirmationResultDto>> Handle(
        ConfirmItemImportBatchCommand request,
        CancellationToken cancellationToken)
    {
        if (user.Id is not { } identityId)
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(request.BatchId));

        var batch = await dbContext.ItemImportBatches
            .Include(b => b.History)
            .FirstOrDefaultAsync(
                b => b.Id == request.BatchId && b.UploadedByUserId == identityId,
                cancellationToken);

        if (batch is null)
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotFound(request.BatchId));

        if (batch.Status == ItemImportBatchStatus.Confirmed && !string.IsNullOrWhiteSpace(batch.ConfirmationResultJson))
        {
            var stored = JsonSerializer.Deserialize<ItemImportBatchConfirmationResultDto>(batch.ConfirmationResultJson)
                         ?? new ItemImportBatchConfirmationResultDto { BatchId = batch.Id, Status = batch.Status };
            return Result.Ok(stored);
        }

        if (batch.Status != ItemImportBatchStatus.PendingReview)
            return Result.Fail(new ItemImportBatchErrors.ItemImportBatchNotInReview(request.BatchId, batch.Status));

        var duplicateItemIds = request.Items
            .GroupBy(item => item.ItemId)
            .Where(group => group.Key != Guid.Empty && group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateItemIds.Length > 0)
            return Result.Fail(new ItemImportBatchErrors.DuplicateItemsInBatch(request.BatchId, duplicateItemIds));

        var itemIds = request.Items.Select(item => item.ItemId).Distinct().ToArray();
        var selectedItems = await dbContext.Items
            .Include(item => item.Category)
            .Where(item => itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var missingItemIds = itemIds.Where(itemId => !selectedItems.ContainsKey(itemId)).ToArray();
        if (missingItemIds.Length > 0)
            return Result.Fail(new ItemImportBatchErrors.ItemsNotFoundInBatch(request.BatchId, missingItemIds));

        var result = new ItemImportBatchConfirmationResultDto
        {
            BatchId = batch.Id,
            Status = ItemImportBatchStatus.Confirmed,
            Items = request.Items.Select(item =>
            {
                var selectedItem = selectedItems[item.ItemId];
                return new ItemImportBatchResultItemDto
                {
                    Id = selectedItem.Id,
                    Sku = selectedItem.Sku,
                    Name = selectedItem.Name,
                    CategoryName = selectedItem.Category.Name,
                    Created = false,
                    CategoryCreated = false
                };
            }).ToList()
        };

        batch.MarkAsConfirmed(JsonSerializer.Serialize(result));
        batch.History.Add(ImportBatchHistory.Confirmed());

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request may have confirmed the batch first. Return its result when available;
            // otherwise preserve the original concurrency error for the caller to handle.
            var currentBatch = await dbContext.ItemImportBatches
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    b => b.Id == batch.Id && b.UploadedByUserId == identityId,
                    cancellationToken);

            if (currentBatch?.Status == ItemImportBatchStatus.Confirmed &&
                !string.IsNullOrWhiteSpace(currentBatch.ConfirmationResultJson))
            {
                var stored = JsonSerializer.Deserialize<ItemImportBatchConfirmationResultDto>(
                    currentBatch.ConfirmationResultJson);
                if (stored is not null)
                    return Result.Ok(stored);
            }

            throw;
        }

        return Result.Ok(result);
    }
}
