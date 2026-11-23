using skestock.Application.Common.Security;
using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetClassSummary;

// Not cached: expenses carry no cache tags, and these are four cheap aggregates.
[Authorize]
public class GetClassSummaryQuery : IRequest<Result<ClassSummaryDto>>
{
    public Guid ClassId { get; init; }
}
