using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemImportById;

public class GetItemImportByIdQuery : IRequest<Result<ItemImportReviewDto>>
{
    public Guid Id { get; init; }
}
