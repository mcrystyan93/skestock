using skestock.Domain.Entities;

namespace skestock.Application.Common.Interfaces;

public interface IScheduledJobRunDbContext
{
    DbSet<ScheduledJobRun> ScheduledJobRuns { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
