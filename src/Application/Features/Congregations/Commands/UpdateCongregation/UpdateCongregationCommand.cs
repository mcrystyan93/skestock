using skestock.Application.Common.Caching;
using skestock.Application.Features.Congregations.Models;

namespace skestock.Application.Features.Congregations.Commands.UpdateCongregation;

public record UpdateCongregationCommand : IRequest<Result<CongregationMutationDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Coordinator { get; init; }
    public string? CoordinatorPhone { get; init; }
    public string? DessertContactPerson { get; init; }
    public string? DessertContactPhone { get; init; }
    public bool CanReceiveStudents { get; init; }
    public bool IsSmallCongregation { get; init; }
    public string? Message { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.CongregationListTag];
}
