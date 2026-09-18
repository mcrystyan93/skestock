using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.CreateOrderList;

[Authorize]
public class CreateOrderListCommand : IRequest<Result<OrderListDto>>, ICacheInvalidation
{
    public Guid ClassId { get; init; }
    public string? Name { get; init; }
    public string? Note { get; init; }
    public List<OrderListLineInput> Lines { get; init; } = [];

    public IReadOnlyCollection<string> Tags => [CacheConstants.OrderListListTag];
}
