using skestock.Domain.Entities;
using skestock.Domain.Entities.ScheduledJobs;

namespace skestock.Application.Common.Interfaces;

public interface IScheduledJobRunDbContext
{
    DbSet<ScheduledJobRun> ScheduledJobRuns { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
