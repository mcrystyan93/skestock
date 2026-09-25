using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ScheduledJobs.Commands.RecordScheduledJobRun;
using skestock.Application.Features.ScheduledJobs.Queries.GetScheduledJobLastRun;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.ScheduledJobs;

[TestFixture]
public sealed class ScheduledJobHandlersTests
{
    [Test]
    public async Task GetLastRun_WhenJobHasNotRun_ReturnsNull()
    {
        await using var context = CreateContext();
        var handler = new GetScheduledJobLastRunHandler(context);

        var result = await handler.Handle(
            new GetScheduledJobLastRunQuery { JobName = "daily-statistics" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Test]
    public async Task RecordRun_CreatesAndThenUpdatesTheJobRow()
    {
        await using var context = CreateContext();
        var handler = new RecordScheduledJobRunCommandHandler(context);
        var firstAttempt = new DateTimeOffset(2026, 9, 25, 18, 0, 0, TimeSpan.Zero);
        var success = firstAttempt.AddMinutes(2);

        var firstResult = await handler.Handle(
            new RecordScheduledJobRunCommand
            {
                JobName = "daily-statistics",
                Status = ScheduledJobRunStatus.Failed,
                AttemptCount = 1,
                AttemptedAtUtc = firstAttempt,
                NextRetryAtUtc = firstAttempt.AddMinutes(1),
                Error = "temporary failure"
            },
            CancellationToken.None);

        var secondResult = await handler.Handle(
            new RecordScheduledJobRunCommand
            {
                JobName = "daily-statistics",
                Status = ScheduledJobRunStatus.Succeeded,
                AttemptCount = 2,
                AttemptedAtUtc = firstAttempt.AddMinutes(1),
                SucceededAtUtc = success
            },
            CancellationToken.None);

        firstResult.IsSuccess.ShouldBeTrue();
        secondResult.IsSuccess.ShouldBeTrue();

        var run = await context.ScheduledJobRuns.SingleAsync(CancellationToken.None);
        run.JobName.ShouldBe("daily-statistics");
        run.LastAttemptAt.ShouldBe(firstAttempt.AddMinutes(1));
        run.LastSucceededAt.ShouldBe(success);
        run.LastError.ShouldBeNull();
        run.Status.ShouldBe(ScheduledJobRunStatus.Succeeded);
        run.AttemptCount.ShouldBe(2);
        run.NextRetryAt.ShouldBeNull();
    }

    [Test]
    public async Task Validators_RejectInvalidJobRunValues()
    {
        var queryValidation = await new GetScheduledJobLastRunQueryValidator().ValidateAsync(
            new GetScheduledJobLastRunQuery { JobName = string.Empty });
        var commandValidation = await new RecordScheduledJobRunCommandValidator().ValidateAsync(
            new RecordScheduledJobRunCommand
            {
                JobName = string.Empty,
                AttemptedAtUtc = new DateTimeOffset(
                    2026,
                    9,
                    25,
                    18,
                    0,
                    0,
                    TimeSpan.FromHours(3)),
                Error = new string('x', 4001)
            });

        queryValidation.IsValid.ShouldBeFalse();
        commandValidation.IsValid.ShouldBeFalse();
    }

    private static ScheduledJobTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ScheduledJobTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ScheduledJobTestDbContext(options);
    }
}

internal sealed class ScheduledJobTestDbContext(
    DbContextOptions<ScheduledJobTestDbContext> options)
    : DbContext(options), IScheduledJobRunDbContext
{
    public DbSet<ScheduledJobRun> ScheduledJobRuns => Set<ScheduledJobRun>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ScheduledJobRun>().HasKey(run => run.JobName);
    }
}
