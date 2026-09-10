using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Commands.ConfirmItemImport;

public class ConfirmItemImportCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ConfirmItemImportCommand, Result<ItemImportConfirmationResultDto>>
{
    public async ValueTask<Result<ItemImportConfirmationResultDto>> Handle(
        ConfirmItemImportCommand request,
        CancellationToken cancellationToken)
    {
        var import = await dbContext.ItemImports
            .FirstOrDefaultAsync(i => i.Id == request.ImportId, cancellationToken);

        if (import is null)
            return Result.Fail(new ItemImportErrors.ItemImportNotFound(request.ImportId));

        // Return the persisted confirmation for repeated submissions instead of validating and
        // processing the same reviewed import again.
        if (import.Status == ItemImportStatus.Confirmed && !string.IsNullOrWhiteSpace(import.ConfirmationResultJson))
        {
            var stored = JsonSerializer.Deserialize<ItemImportConfirmationResultDto>(import.ConfirmationResultJson)
                         ?? new ItemImportConfirmationResultDto { ImportId = import.Id, Status = import.Status };
            return Result.Ok(stored);
        }

        if (import.Status != ItemImportStatus.PendingReview)
            return Result.Fail(new ItemImportErrors.ItemImportNotInReview(request.ImportId, import.Status));

        var duplicateItemIds = request.Items
            .GroupBy(item => item.ItemId)
            .Where(group => group.Key != Guid.Empty && group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateItemIds.Length > 0)
            return Result.Fail(new ItemImportErrors.DuplicateItems(request.ImportId, duplicateItemIds));

        var itemIds = request.Items.Select(item => item.ItemId).Distinct().ToArray();
        var selectedItems = await dbContext.Items
            .Include(item => item.Category)
            .Where(item => itemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var missingItemIds = itemIds.Where(itemId => !selectedItems.ContainsKey(itemId)).ToArray();
        if (missingItemIds.Length > 0)
            return Result.Fail(new ItemImportErrors.ItemsNotFound(request.ImportId, missingItemIds));

        // Build the response from the current catalog records so edited suggestion text cannot
        // overwrite the already-selected item or category data.
        var result = new ItemImportConfirmationResultDto
        {
            ImportId = import.Id,
            Status = ItemImportStatus.Confirmed,
            Items = request.Items.Select(item =>
            {
                var selectedItem = selectedItems[item.ItemId];
                return new ItemImportResultItemDto
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

        import.MarkAsConfirmed(JsonSerializer.Serialize(result));

        // Persist the confirmation result together with the import status so future requests can
        // return the same result without repeating the confirmation.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request may have confirmed the import first. Return its result when available;
            // otherwise preserve the original concurrency error for the caller to handle.
            var currentImport = await dbContext.ItemImports
                .AsNoTracking()
                .SingleOrDefaultAsync(i => i.Id == import.Id, cancellationToken);

            if (currentImport?.Status == ItemImportStatus.Confirmed &&
                !string.IsNullOrWhiteSpace(currentImport.ConfirmationResultJson))
            {
                var stored = JsonSerializer.Deserialize<ItemImportConfirmationResultDto>(
                    currentImport.ConfirmationResultJson);
                if (stored is not null)
                    return Result.Ok(stored);
            }

            throw;
        }

        return Result.Ok(result);
    }
}
