using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Events.Categories;

namespace skestock.Application.Features.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    public async ValueTask<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
            return Result.Fail(new CategoryErrors.CategoryNotFound(request.Id));

        category.Name = request.Name.Trim();
        category.Icon = request.Icon is null
            ? null
            : new CategoryIcon(request.Icon.Name, request.Icon.FileName, request.Icon.Path);
        
        category.AddDomainEvent(new CategoryUpdatedEvent(category));

        await dbContext.SaveChangesAsync(cancellationToken);

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
