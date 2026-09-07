using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.DisableItem;

public class DisableItemCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DisableItemCommand, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(DisableItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .SingleOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (item is null)
            return Result.Fail(new ItemErrors.ItemNotFound(request.Id));

        // Idempotent: disabling an already-inactive item is a no-op success rather than an error.
        item.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        var categoryName = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == item.CategoryId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(cancellationToken);

        return Result.Ok(new ItemDto
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
        });
    }
}
