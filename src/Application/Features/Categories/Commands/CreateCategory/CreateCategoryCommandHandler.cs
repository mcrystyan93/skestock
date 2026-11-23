using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;

namespace skestock.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryMutationDto>>
{
    public async ValueTask<Result<CategoryMutationDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            Icon = request.Icon is null
                ? null
                : new CategoryIcon(request.Icon.Name, request.Icon.FileName, request.Icon.Path)
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Parent navigation isn't loaded on the tracked entity (only the FK is set), so its name is resolved here.
        var parentName = request.ParentId.HasValue
            ? await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == request.ParentId)
                .Select(c => c.Name)
                .SingleOrDefaultAsync(cancellationToken)
            : null;

        return Result.Ok(new CategoryMutationDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            ParentName = parentName,
            Icon = category.Icon is null
                ? null
                : new CategoryIconDto
                {
                    Name = category.Icon.Name,
                    FileName = category.Icon.FileName,
                    Path = category.Icon.Path
                }
        });
    }
}
