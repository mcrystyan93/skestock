using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Behaviours;

public record TestCommand : IRequest;

public record SensitiveCommand(string Payload) : IRequest;

public class RequestLoggerTests
{
    private const string SensitivePayload = "base64-file-content-and-personal-data";

    [Test]
    public async Task ShouldLogRequestNameAndUserIdAtDebug()
    {
        var userId = Guid.NewGuid();
        var logger = new CapturingLogger<SensitiveCommand>();
        var requestLogger = new LoggingBehaviour<SensitiveCommand, Unit>(logger, CreateUser(userId));

        await requestLogger.Handle(
            new SensitiveCommand(SensitivePayload),
            static (_, _) => new ValueTask<Unit>(Unit.Value),
            CancellationToken.None);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Debug);
        entry.State.ShouldContain(new KeyValuePair<string, object?>("RequestName", nameof(SensitiveCommand)));
        entry.State.ShouldContain(new KeyValuePair<string, object?>("UserId", userId));
    }

    [Test]
    public async Task ShouldNeverLogRequestPayload()
    {
        var logger = new CapturingLogger<SensitiveCommand>();
        var request = new SensitiveCommand(SensitivePayload);
        var requestLogger = new LoggingBehaviour<SensitiveCommand, Unit>(logger, CreateUser(Guid.NewGuid()));

        await requestLogger.Handle(request, static (_, _) => new ValueTask<Unit>(Unit.Value), CancellationToken.None);

        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Message.ShouldNotContain(SensitivePayload);
        entry.State.ShouldNotContain(pair => ReferenceEquals(pair.Value, request));
    }

    [Test]
    public async Task ShouldNotLogWhenDebugIsDisabled()
    {
        var logger = new CapturingLogger<TestCommand>(LogLevel.Information);
        var requestLogger = new LoggingBehaviour<TestCommand, Unit>(logger, CreateUser(null));

        await requestLogger.Handle(new TestCommand(), static (_, _) => new ValueTask<Unit>(Unit.Value), CancellationToken.None);

        logger.Entries.ShouldBeEmpty();
    }

    private static IUser CreateUser(Guid? id)
    {
        var user = new Mock<IUser>();
        user.Setup(x => x.Id).Returns(id);
        return user.Object;
    }
}
