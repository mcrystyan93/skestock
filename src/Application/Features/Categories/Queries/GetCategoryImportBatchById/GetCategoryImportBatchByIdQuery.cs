using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Queries.GetCategoryImportBatchById;

public class GetCategoryImportBatchByIdQuery : IRequest<Result<CategoryImportBatchReviewDto>>
{
    public Guid Id { get; init; }
}
