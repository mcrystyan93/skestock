using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Items.Commands.CreateItem;

public class CreateItemCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateItemCommand, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Unit = request.Unit.Trim(),
            MinThreshold = request.MinThreshold,
            IsPerishable = request.IsPerishable,
            CategoryId = request.CategoryId,
            IsActive = true
        };

        dbContext.Items.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Category/CreatedBy/LastModifiedBy navigations aren't loaded on a freshly-inserted
        // entity (only the *Id FKs are set), so the category name is resolved with a follow-up
        // lookup here - mirrors CreateLocationCommandHandler's ParentLocation lookup.
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
