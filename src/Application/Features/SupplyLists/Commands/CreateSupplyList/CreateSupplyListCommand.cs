using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;

[Authorize]
public class CreateSupplyListCommand : IRequest<Result<SupplyListDto>>, ICacheInvalidation, ISupplyListPayload
{
    public string Name { get; init; } = string.Empty;
    public string? Note { get; init; }
    public SupplyListFrequency Frequency { get; init; }
    public int? IntervalWeeks { get; init; }
    public List<SupplyListLineInput> Lines { get; init; } = [];

    public IReadOnlyCollection<string> Tags => [CacheConstants.SupplyListListTag];
}
