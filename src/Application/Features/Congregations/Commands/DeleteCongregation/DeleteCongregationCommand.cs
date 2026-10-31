using skestock.Application.Common.Caching;

namespace skestock.Application.Features.Congregations.Commands.DeleteCongregation;

public record DeleteCongregationCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid Id { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.CongregationListTag];
}
