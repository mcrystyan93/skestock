using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.OrderLists.Models;

namespace skestock.Application.Features.OrderLists.Commands.UpdateOrderList;

[Authorize]
public class UpdateOrderListCommand : IRequest<Result<OrderListDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Note { get; init; }
    public List<OrderListLineInput> Lines { get; init; } = [];

    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.OrderListListTag, CacheConstants.OrderListTag(Id)];
}
