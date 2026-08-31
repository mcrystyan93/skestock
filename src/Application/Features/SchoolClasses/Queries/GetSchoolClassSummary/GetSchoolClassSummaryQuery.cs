using skestock.Application.Features.SchoolClasses.Models;

namespace skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassSummary;

public class GetSchoolClassSummaryQuery : IRequest<Result<SchoolClassSummary>>
{
    public int Id { get; init; }
}
