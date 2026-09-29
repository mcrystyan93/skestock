using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;

namespace skestock.Application.Features.SupplyLists;

// Shared read-side projection so GetById and the command handlers return the same shape.
internal static class SupplyListProjection
{
    public static async Task<SupplyListDto?> LoadAsync(
        IApplicationDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.SupplyLists
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new SupplyListDto
            {
                Id = l.Id,
                Name = l.Name,
                Note = l.Note,
                Frequency = l.Frequency.ToString(),
                IntervalWeeks = l.IntervalWeeks,
                IsActive = l.IsActive,
                Lines = l.Lines
                    .OrderBy(x => x.Item.Name)
                    .ThenBy(x => x.Id)
                    .Select(x => new SupplyListLineDto
                    {
                        Id = x.Id,
                        ItemId = x.ItemId,
                        ItemName = x.Item.Name,
                        ItemSku = x.Item.Sku,
                        CategoryName = x.Item.Category.Name,
                        Quantity = x.Quantity,
                        Unit = x.Unit,
                        Notes = x.Notes
                    })
                    .ToList(),
                CreatedByName = l.CreatedBy != null ? l.CreatedBy.FullName : null,
                LastModifiedByName = l.LastModifiedBy != null ? l.LastModifiedBy.FullName : null,
                CreatedDate = l.CreatedDate,
                LastModifiedDate = l.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
