using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemById;

public class GetItemByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetItemByIdQuery, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var itemDto = await dbContext.Items
            .AsNoTracking()
            .Where(item => item.Id == request.Id)
            .Select(item => new ItemDto
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
                CategoryName = item.Category != null ? item.Category.Name : null,
                CreatedByName = item.CreatedBy != null ? item.CreatedBy.FullName : null,
                LastModifiedByName = item.LastModifiedBy != null ? item.LastModifiedBy.FullName : null,
                CreatedDate = item.CreatedDate,
                LastModifiedDate = item.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (itemDto is null)
            return Result.Fail(new ItemErrors.ItemNotFound(request.Id));

        return Result.Ok(itemDto);
    }
}
