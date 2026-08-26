using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.EditItem;

public class EditItemCommandHandler(IApplicationDbContext dbContext)
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
        item.MinThreshold = request.MinThreshold;
        item.IsPerishable = request.IsPerishable;
        item.CategoryId = request.CategoryId;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Category navigation isn't guaranteed to be loaded on the tracked entity above (only
        // the FK is set), so the category name is resolved with a follow-up lookup here -
        // mirrors CreateItemCommandHandler.
        var categoryName = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == request.CategoryId)
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
