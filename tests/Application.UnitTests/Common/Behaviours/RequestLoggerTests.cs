using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Interfaces;
using Mediator;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace skestock.Application.UnitTests.Common.Behaviours;

public record TestCommand : IRequest;

public class RequestLoggerTests
{
    private Mock<ILogger<TestCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TestCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid());

        var requestLogger = new LoggingBehaviour<TestCommand, Unit>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Handle(new TestCommand(), static (_, _) => new ValueTask<Unit>(Unit.Value), new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<Guid>()), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<TestCommand, Unit>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Handle(new TestCommand(), static (_, _) => new ValueTask<Unit>(Unit.Value), new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<Guid>()), Times.Never);
    }
}
