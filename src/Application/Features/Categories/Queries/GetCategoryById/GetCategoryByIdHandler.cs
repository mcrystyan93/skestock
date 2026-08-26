using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    public async ValueTask<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                CreatedByName = c.CreatedBy != null ? c.CreatedBy.FullName : null,
                LastModifiedByName = c.LastModifiedBy != null ? c.LastModifiedBy.FullName : null,
                CreatedDate = c.CreatedDate,
                LastModifiedDate = c.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (category is null)
            return Result.Fail(new CategoryErrors.CategoryNotFound(request.Id));

        return Result.Ok(category);
    }
}
