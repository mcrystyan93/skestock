using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassEvolution;

// Not cached, same reason as GetClassSummaryQuery.
[Authorize]
public class GetClassEvolutionQuery : IRequest<Result<ClassEvolutionDto>>
{
    public Guid ClassId { get; init; }
}
