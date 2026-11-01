using skestock.Application.Features.Congregations.Models;

namespace skestock.Application.Features.Congregations.Queries.GetCongregationById;

public class GetCongregationByIdQuery : IRequest<Result<CongregationDto>>
{
    public Guid Id { get; init; }
}
