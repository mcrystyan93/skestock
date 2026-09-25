using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ScheduledJobs.Commands.RecordScheduledJobRun;

public sealed class RecordScheduledJobRunCommandHandler(IScheduledJobRunDbContext dbContext)
    : IRequestHandler<RecordScheduledJobRunCommand, Result>
{
    public async ValueTask<Result> Handle(
        RecordScheduledJobRunCommand request,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.ScheduledJobRuns
            .SingleOrDefaultAsync(job => job.JobName == request.JobName, cancellationToken);

        if (run is null)
        {
            run = new ScheduledJobRun { JobName = request.JobName };
            dbContext.ScheduledJobRuns.Add(run);
        }

        run.Status = request.Status;
        run.AttemptCount = request.AttemptCount;
        run.NextRetryAt = request.NextRetryAtUtc;
        run.LastAttemptAt = request.AttemptedAtUtc;
        run.LastError = request.Error;

        if (request.SucceededAtUtc.HasValue)
        {
            run.LastSucceededAt = request.SucceededAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
