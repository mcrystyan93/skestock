using Microsoft.Extensions.Logging;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities.Items;
using skestock.Domain.Enums;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.Commands.EditItem;

public class EditItemCommandHandler(
    IApplicationDbContext dbContext,
    IUser user,
    IBlobStorageService blobStorage,
    ILogger<EditItemCommandHandler> logger)
    : IRequestHandler<EditItemCommand, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(EditItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .SingleOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (item is null)
            return Result.Fail(new ItemErrors.ItemNotFound(request.Id));

        item.Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        item.Name = request.Name.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        item.Unit = request.Unit.Trim();
        item.IsPerishable = request.IsPerishable;
        item.ShelfLifeDays = request.ShelfLifeDays;
        item.CategoryId = request.CategoryId;
        item.AddDomainEvent(new ItemUpdatedEvent(item));

        // Add/update only: locations not sent keep their value (list rows carry no thresholds).
        if (request.LocationThresholds.Count > 0)
        {
            var existing = await dbContext.ItemLocationThresholds
                .Where(t => t.ItemId == item.Id)
                .ToDictionaryAsync(t => t.LocationId, cancellationToken);
            foreach (var requested in request.LocationThresholds)
            {
                if (existing.TryGetValue(requested.LocationId, out var row))
                    row.MinThreshold = requested.MinThreshold;
                else
                    dbContext.ItemLocationThresholds.Add(new ItemLocationThreshold
                    {
                        ItemId = item.Id,
                        LocationId = requested.LocationId,
                        MinThreshold = requested.MinThreshold
                    });
            }
        }

        var previousFileId = item.ImageFileId;
        string? replacedContainer = null, replacedPath = null;
        if (previousFileId != request.ImageFileId)
        {
            item.ImageFileId = request.ImageFileId;

            if (request.ImageFileId is { } newFileId)
            {
                dbContext.OutboxMessages.Add(ItemImageOutbox.ThumbnailMessage(item.Id, newFileId, user.Id));
            }

            if (previousFileId is { } oldFileId)
            {
                var oldFile = await dbContext.FileMetadata
                    .SingleOrDefaultAsync(f => f.FileId == oldFileId, cancellationToken);
                if (oldFile is not null)
                {
                    oldFile.Status = FileStatus.Deleted;
                    (replacedContainer, replacedPath) = (oldFile.BlobContainer, oldFile.BlobPath);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousFileId is { } oldId && replacedContainer is not null && replacedPath is not null)
            await TryDeleteOldImageBlobsAsync(oldId, replacedContainer, replacedPath, cancellationToken);

        return Result.Ok(await ItemDtoMapper.FromTrackedItemAsync(dbContext, item, cancellationToken));
    }

    // Best effort: a leftover blob is harmless, a failed edit is not.
    private async Task TryDeleteOldImageBlobsAsync(
        Guid fileId, string container, string originalPath, CancellationToken cancellationToken)
    {
        try
        {
            await blobStorage.DeleteAsync(new DeleteDto(container, originalPath), cancellationToken);
            await blobStorage.DeleteAsync(new DeleteDto(container, ItemImagePaths.Thumbnail(fileId)), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete the old image blobs of file {FileId}.", fileId);
        }
    }
}
