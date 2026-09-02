using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQuery : IRequest<Result<CategoryDto>>
{
    public Guid Id { get; init; }
}
