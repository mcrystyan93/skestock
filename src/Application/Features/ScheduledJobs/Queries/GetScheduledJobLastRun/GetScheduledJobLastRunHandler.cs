using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ScheduledJobs.Models;

namespace skestock.Application.Features.ScheduledJobs.Queries.GetScheduledJobLastRun;

public sealed class GetScheduledJobLastRunHandler(IScheduledJobRunDbContext dbContext)
    : IRequestHandler<GetScheduledJobLastRunQuery, Result<ScheduledJobRunDto?>>
{
    public async ValueTask<Result<ScheduledJobRunDto?>> Handle(
        GetScheduledJobLastRunQuery request,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.ScheduledJobRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(job => job.JobName == request.JobName, cancellationToken);

        return Result.Ok(run is null
            ? null
            : new ScheduledJobRunDto
            {
                JobName = run.JobName,
                Status = run.Status,
                AttemptCount = run.AttemptCount,
                NextRetryAt = run.NextRetryAt,
                LastSucceededAt = run.LastSucceededAt,
                LastAttemptAt = run.LastAttemptAt,
                LastError = run.LastError
            });
    }
}
