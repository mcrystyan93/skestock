using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items.Models;

internal static class ItemDtoMapper
{
    internal static async Task<ItemDto> FromTrackedItemAsync(
        IApplicationDbContext dbContext, Item item, CancellationToken cancellationToken)
    {
        // Command handlers may only have the category FK, not its navigation, after saving.
        var categoryName = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == item.CategoryId)
            .Select(category => category.Name)
            .SingleOrDefaultAsync(cancellationToken);

        return new ItemDto
        {
            Id = item.Id,
            Sku = item.Sku,
            Name = item.Name,
            Description = item.Description,
            Unit = item.Unit,
            MinThreshold = item.MinThreshold,
            IsPerishable = item.IsPerishable,
            ShelfLifeDays = item.ShelfLifeDays,
            IsActive = item.IsActive,
            CategoryId = item.CategoryId,
            CategoryName = categoryName,
            CreatedByName = item.CreatedBy?.FullName,
            LastModifiedByName = item.LastModifiedBy?.FullName,
            CreatedDate = item.CreatedDate,
            LastModifiedDate = item.LastModifiedDate
        };
    }
}
