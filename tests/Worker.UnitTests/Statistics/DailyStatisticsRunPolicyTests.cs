using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.ScheduledJobs.Models;
using skestock.Domain.Enums;
using Worker.Statistics;

namespace Worker.UnitTests.Statistics;

[TestFixture]
public sealed class DailyStatisticsRunPolicyTests
{
    private static readonly DateTimeOffset Occurrence = new(2026, 9, 25, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = Occurrence.AddMinutes(10);

    [Test]
    public void Never_run_job_should_run()
    {
        DailyStatisticsRunPolicy.ShouldRun(null, Occurrence, Now, allowRetry: false).ShouldBeTrue();
    }

    [Test]
    public void Job_succeeded_for_the_occurrence_should_not_run()
    {
        var lastRun = Run(ScheduledJobRunStatus.Succeeded, attemptCount: 1, succeededAt: Occurrence.AddMinutes(1));

        DailyStatisticsRunPolicy.ShouldRun(lastRun, Occurrence, Now, allowRetry: true).ShouldBeFalse();
    }

    [Test]
    public void Attempt_from_a_previous_occurrence_is_ignored()
    {
        var lastRun = Run(ScheduledJobRunStatus.Failed, attemptCount: 4, attemptedAt: Occurrence.AddDays(-1));

        DailyStatisticsRunPolicy.ShouldRun(lastRun, Occurrence, Now, allowRetry: false).ShouldBeTrue();
    }

    [Test]
    public void Exhausted_attempts_should_not_run()
    {
        var lastRun = Run(ScheduledJobRunStatus.Failed, attemptCount: DailyStatisticsRetryPolicy.MaxAttempts);

        DailyStatisticsRunPolicy.ShouldRun(lastRun, Occurrence, Now, allowRetry: true).ShouldBeFalse();
    }

    [Test]
    public void Interrupted_running_attempt_should_resume()
    {
        var lastRun = Run(ScheduledJobRunStatus.Running, attemptCount: 1);

        DailyStatisticsRunPolicy.ShouldRun(lastRun, Occurrence, Now, allowRetry: false).ShouldBeTrue();
    }

    [TestCase(-1, true)]
    [TestCase(1, false)]
    public void Failed_attempt_runs_only_when_its_retry_is_due(int retryOffsetMinutes, bool expected)
    {
        var lastRun = Run(ScheduledJobRunStatus.Failed, attemptCount: 1, nextRetryAt: Now.AddMinutes(retryOffsetMinutes));

        DailyStatisticsRunPolicy.ShouldRun(lastRun, Occurrence, Now, allowRetry: false).ShouldBe(expected);
    }

    [Test]
    public void Failed_attempt_runs_immediately_inside_the_retry_loop()
    {
        var lastRun = Run(ScheduledJobRunStatus.Failed, attemptCount: 1, nextRetryAt: Now.AddMinutes(5));

        DailyStatisticsRunPolicy.ShouldRun(lastRun, Occurrence, Now, allowRetry: true).ShouldBeTrue();
    }

    [Test]
    public void Next_attempt_index_resumes_after_the_persisted_attempt_count()
    {
        var lastRun = Run(ScheduledJobRunStatus.Failed, attemptCount: 2);

        DailyStatisticsRunPolicy.GetNextAttemptIndex(lastRun, Occurrence).ShouldBe(2);
    }

    [Test]
    public void Next_attempt_index_restarts_for_a_new_occurrence()
    {
        var lastRun = Run(ScheduledJobRunStatus.Failed, attemptCount: 3, attemptedAt: Occurrence.AddDays(-1));

        DailyStatisticsRunPolicy.GetNextAttemptIndex(lastRun, Occurrence).ShouldBe(0);
    }

    [Test]
    public void Pending_retry_is_returned_when_not_yet_due()
    {
        var nextRetryAt = Now.AddMinutes(5);
        var lastRun = Run(ScheduledJobRunStatus.Failed, attemptCount: 1, nextRetryAt: nextRetryAt);

        DailyStatisticsRunPolicy.GetPendingRetryAt(lastRun, Occurrence, Now).ShouldBe(nextRetryAt);
    }

    [Test]
    public void No_pending_retry_after_exhausted_attempts()
    {
        var lastRun = Run(
            ScheduledJobRunStatus.Failed,
            attemptCount: DailyStatisticsRetryPolicy.MaxAttempts,
            nextRetryAt: Now.AddMinutes(5));

        DailyStatisticsRunPolicy.GetPendingRetryAt(lastRun, Occurrence, Now).ShouldBeNull();
    }

    private static ScheduledJobRunDto Run(
        ScheduledJobRunStatus status,
        int attemptCount,
        DateTimeOffset? attemptedAt = null,
        DateTimeOffset? succeededAt = null,
        DateTimeOffset? nextRetryAt = null) =>
        new()
        {
            Status = status,
            AttemptCount = attemptCount,
            LastAttemptAt = attemptedAt ?? Occurrence.AddMinutes(1),
            LastSucceededAt = succeededAt,
            NextRetryAt = nextRetryAt
        };
}
