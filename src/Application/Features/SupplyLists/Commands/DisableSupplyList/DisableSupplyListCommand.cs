using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.SupplyLists.Models;

namespace skestock.Application.Features.SupplyLists.Commands.DisableSupplyList;

[Authorize]
public class DisableSupplyListCommand : IRequest<Result<SupplyListDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }

    public IReadOnlyCollection<string> Tags =>
        [CacheConstants.SupplyListListTag, CacheConstants.SupplyListTag(Id)];
}
