using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    public async ValueTask<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var categoryDto = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == request.Id)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                ItemCount = category.Items.Count,
                Icon = category.Icon == null
                    ? null
                    : new CategoryIconDto
                    {
                        Name = category.Icon.Name,
                        FileName = category.Icon.FileName,
                        Path = category.Icon.Path
                    },
                CreatedByName = category.CreatedBy != null ? category.CreatedBy.FullName : null,
                LastModifiedByName = category.LastModifiedBy != null ? category.LastModifiedBy.FullName : null,
                CreatedDate = category.CreatedDate,
                LastModifiedDate = category.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (categoryDto is null)
            return Result.Fail(new CategoryErrors.CategoryNotFound(request.Id));

        return Result.Ok(categoryDto);
    }
}
