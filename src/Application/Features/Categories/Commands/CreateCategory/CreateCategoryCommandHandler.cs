using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCategoryCommand, Result<CategoryMutationDto>>
{
    public async ValueTask<Result<CategoryMutationDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
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

        return Result.Ok(new CategoryMutationDto
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
                }
        });
    }
}
