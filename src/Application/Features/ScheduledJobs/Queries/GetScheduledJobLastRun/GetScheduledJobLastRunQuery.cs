using skestock.Application.Features.ScheduledJobs.Models;

namespace skestock.Application.Features.ScheduledJobs.Queries.GetScheduledJobLastRun;

public sealed class GetScheduledJobLastRunQuery : IRequest<Result<ScheduledJobRunDto?>>
{
    public string JobName { get; init; } = string.Empty;
}
