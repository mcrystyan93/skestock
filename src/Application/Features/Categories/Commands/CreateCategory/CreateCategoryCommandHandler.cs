using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    public async ValueTask<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name.Trim(),
            Icon = request.Icon is null
                ? null
                : new CategoryIcon(request.Icon.Name, request.Icon.FileName, request.Icon.Path)
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        // CreatedBy/LastModifiedBy navigations aren't loaded on a freshly-inserted entity
        // (only the *Id FKs are set by AuditableEntityInterceptor), so the *Name fields are
        // null here by design - callers needing the name can re-fetch via GetAllCategories.
        return Result.Ok(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Icon = category.Icon is null
                ? null
                : new CategoryIconDto
                {
                    Name = category.Icon.Name,
                    FileName = category.Icon.FileName,
                    Path = category.Icon.Path
                },
            CreatedByName = category.CreatedBy?.FullName,
            LastModifiedByName = category.LastModifiedBy?.FullName,
            CreatedDate = category.CreatedDate,
            LastModifiedDate = category.LastModifiedDate
        });
    }
}
