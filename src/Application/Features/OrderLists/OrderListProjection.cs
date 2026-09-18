using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.OrderLists;

// Shared read-side projection so GetById and the command handlers return the same shape.
internal static class OrderListProjection
{
    public static async Task<OrderListDto?> LoadAsync(
        IApplicationDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.OrderLists
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrderListDto
            {
                Id = o.Id,
                ClassId = o.ClassId,
                ClassName = o.Class != null ? o.Class.Name : null,
                Name = o.Name,
                Note = o.Note,
                Status = o.Status.ToString(),
                SubmittedAt = o.SubmittedAt,
                Lines = o.Lines
                    .OrderBy(l => l.CreatedDate)
                    .Select(l => new OrderListLineDto
                    {
                        Id = l.Id,
                        ItemId = l.ItemId,
                        ProductName = l.ProductName,
                        Quantity = l.Quantity,
                        Unit = l.Unit,
                        Notes = l.Notes
                    })
                    .ToList(),
                CreatedByName = o.CreatedBy != null ? o.CreatedBy.FullName : null,
                LastModifiedByName = o.LastModifiedBy != null ? o.LastModifiedBy.FullName : null,
                CreatedDate = o.CreatedDate,
                LastModifiedDate = o.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
