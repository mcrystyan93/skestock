using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Behaviours;

public class PerformanceBehaviourTests
{
    private const string SensitivePayload = "base64-file-content-and-personal-data";

    [Test]
    public async Task ShouldNotAccumulateElapsedTimeAcrossRequestsOnTheSameInstance()
    {
        var logger = new CapturingLogger<SensitiveCommand>();
        var behaviour = new PerformanceBehaviour<SensitiveCommand, Unit>(logger, CreateUser());

        await behaviour.Handle(new SensitiveCommand(SensitivePayload), DelayedHandler(TimeSpan.FromMilliseconds(300)), CancellationToken.None);
        await behaviour.Handle(new SensitiveCommand(SensitivePayload), DelayedHandler(TimeSpan.FromMilliseconds(300)), CancellationToken.None);

        logger.Entries.ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldWarnOnceForSlowRequestWithoutPayload()
    {
        var logger = new CapturingLogger<SensitiveCommand>();
        var request = new SensitiveCommand(SensitivePayload);
        var behaviour = new PerformanceBehaviour<SensitiveCommand, Unit>(logger, CreateUser());

        await behaviour.Handle(request, DelayedHandler(TimeSpan.FromMilliseconds(600)), CancellationToken.None);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.State.ShouldContain(new KeyValuePair<string, object?>("RequestName", nameof(SensitiveCommand)));
        var elapsed = entry.State.Single(pair => pair.Key == "ElapsedMilliseconds").Value.ShouldBeOfType<long>();
        elapsed.ShouldBeGreaterThanOrEqualTo(500);
        entry.Message.ShouldNotContain(SensitivePayload);
        entry.State.ShouldNotContain(pair => ReferenceEquals(pair.Value, request));
    }

    private static MessageHandlerDelegate<SensitiveCommand, Unit> DelayedHandler(TimeSpan delay) =>
        async (_, cancellationToken) =>
        {
            await Task.Delay(delay, cancellationToken);
            return Unit.Value;
        };

    private static IUser CreateUser()
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(Guid.NewGuid());
        return user.Object;
    }
}
