using NUnit.Framework;
using Shouldly;
using Worker.Statistics;

namespace Worker.UnitTests.Statistics;

[TestFixture]
public sealed class DailyStatisticsRetryPolicyTests
{
    private static readonly DateTimeOffset FailedAt = new(2026, 9, 25, 18, 0, 0, TimeSpan.Zero);

    [Test]
    public void Allows_four_attempts()
    {
        DailyStatisticsRetryPolicy.MaxAttempts.ShouldBe(4);
    }

    [TestCase(0, 1)]
    [TestCase(1, 5)]
    [TestCase(2, 15)]
    public void Retry_delay_backs_off(int attemptIndex, int expectedMinutes)
    {
        DailyStatisticsRetryPolicy.GetRetryDelay(attemptIndex).ShouldBe(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Test]
    public void Next_retry_is_scheduled_after_a_non_final_attempt()
    {
        DailyStatisticsRetryPolicy.GetNextRetryAt(FailedAt, 1).ShouldBe(FailedAt.AddMinutes(5));
    }

    [Test]
    public void No_retry_is_scheduled_after_the_last_attempt()
    {
        DailyStatisticsRetryPolicy.GetNextRetryAt(FailedAt, DailyStatisticsRetryPolicy.LastAttemptIndex).ShouldBeNull();
    }
}
