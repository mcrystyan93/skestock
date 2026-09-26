using NUnit.Framework;
using Shouldly;
using Worker.Queues;

namespace Worker.UnitTests.Queues;

[TestFixture]
public sealed class QueueMessageDispositionPolicyTests
{
    [TestCase(QueueMessageProcessingStatus.Succeeded)]
    [TestCase(QueueMessageProcessingStatus.Duplicate)]
    public void Handled_message_is_deleted(QueueMessageProcessingStatus status)
    {
        QueueMessageDispositionPolicy.Decide(status, dequeueCount: 1).ShouldBe(QueueMessageDisposition.Delete);
    }

    [Test]
    public void Permanent_failure_is_poisoned_on_first_delivery()
    {
        QueueMessageDispositionPolicy.Decide(QueueMessageProcessingStatus.PermanentFailure, dequeueCount: 1)
            .ShouldBe(QueueMessageDisposition.MoveToPoison);
    }

    [Test]
    public void Retryable_failure_below_the_limit_is_retried()
    {
        QueueMessageDispositionPolicy.Decide(
                QueueMessageProcessingStatus.RetryableFailure,
                QueueMessageDispositionPolicy.MaxDequeueCount - 1)
            .ShouldBe(QueueMessageDisposition.Retry);
    }

    [Test]
    public void Retryable_failure_at_the_limit_is_poisoned()
    {
        QueueMessageDispositionPolicy.Decide(
                QueueMessageProcessingStatus.RetryableFailure,
                QueueMessageDispositionPolicy.MaxDequeueCount)
            .ShouldBe(QueueMessageDisposition.MoveToPoison);
    }
}
