using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemImportBatchById;

public class GetItemImportBatchByIdQuery : IRequest<Result<ItemImportBatchReviewDto>>
{
    public Guid Id { get; init; }
}
