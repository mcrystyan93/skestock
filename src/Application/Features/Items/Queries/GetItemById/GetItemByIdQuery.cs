using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemById;

public class GetItemByIdQuery : IRequest<Result<ItemDto>>
{
    public int Id { get; init; }
}
