using FluentResults;
using Mediator;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;
using skestock.Application.Features.Statistics.Commands.RecalculatePurchaseStatistics;
using skestock.Shared;

namespace skestock.Application.UnitTests.Features.Statistics.Commands.RecalculatePurchaseStatistics;

[TestFixture]
public sealed class RecalculatePurchaseStatisticsCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenLockIsHeld_FailsWithRunInProgressWithoutMaterializing()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var distributedLock = LockReturning(null);
        var handler = new RecalculatePurchaseStatisticsCommandHandler(sender.Object, distributedLock.Object);

        var result = await handler.Handle(new RecalculatePurchaseStatisticsCommand(), CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<StatisticsErrors.RunInProgress>();
        sender.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_WhenLockIsFree_MaterializesAndReleasesTheLock()
    {
        var lease = new Mock<IAsyncDisposable>();
        var distributedLock = LockReturning(lease.Object);
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(
                It.Is<MaterializePurchaseStatisticsCommand>(c => c.TimeZoneId == Services.BusinessTimeZoneId),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Result<int>>(Result.Ok(42)));
        var handler = new RecalculatePurchaseStatisticsCommandHandler(sender.Object, distributedLock.Object);

        var result = await handler.Handle(new RecalculatePurchaseStatisticsCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        lease.Verify(l => l.DisposeAsync(), Times.Once);
    }

    private static Mock<IDistributedLock> LockReturning(IAsyncDisposable? lease)
    {
        var distributedLock = new Mock<IDistributedLock>();
        distributedLock
            .Setup(l => l.TryAcquireAsync(Services.DailyStatisticsLockKey, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lease);
        return distributedLock;
    }
}
