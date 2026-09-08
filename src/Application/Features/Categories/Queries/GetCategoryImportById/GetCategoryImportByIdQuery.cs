using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Queries.GetCategoryImportById;

public class GetCategoryImportByIdQuery : IRequest<Result<CategoryImportReviewDto>>
{
    public Guid Id { get; init; }
}
