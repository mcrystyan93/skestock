using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
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
        
        category.AddDomainEvent(new CategoryUpdatedEvent(category));

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            CreatedByName = category.CreatedBy?.FullName,
            LastModifiedByName = category.LastModifiedBy?.FullName,
            CreatedDate = category.CreatedDate,
            LastModifiedDate = category.LastModifiedDate
        });
    }
}
