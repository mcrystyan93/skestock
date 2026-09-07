using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemById;

public class GetItemByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetItemByIdQuery, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Id == request.Id)
            .Select(i => new ItemDto
            {
                Id = i.Id,
                Sku = i.Sku,
                Name = i.Name,
                Description = i.Description,
                Unit = i.Unit,
                MinThreshold = i.MinThreshold,
                IsPerishable = i.IsPerishable,
                ShelfLifeDays = i.ShelfLifeDays,
                IsActive = i.IsActive,
                CategoryId = i.CategoryId,
                CategoryName = i.Category != null ? i.Category.Name : null,
                CreatedByName = i.CreatedBy != null ? i.CreatedBy.FullName : null,
                LastModifiedByName = i.LastModifiedBy != null ? i.LastModifiedBy.FullName : null,
                CreatedDate = i.CreatedDate,
                LastModifiedDate = i.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
            return Result.Fail(new ItemErrors.ItemNotFound(request.Id));

        return Result.Ok(item);
    }
}
