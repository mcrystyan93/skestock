using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateCategoryCommand, Result<CategoryMutationDto>>
{
    public async ValueTask<Result<CategoryMutationDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return Result.Fail(new CategoryErrors.CategoryNotFound(request.Id));

        category.Name = request.Name.Trim();
        category.ParentId = request.ParentId;
        category.Icon = request.Icon is null
            ? null
            : new CategoryIcon(request.Icon.Name, request.Icon.FileName, request.Icon.Path);

        category.AddDomainEvent(new CategoryUpdatedEvent(category));

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
