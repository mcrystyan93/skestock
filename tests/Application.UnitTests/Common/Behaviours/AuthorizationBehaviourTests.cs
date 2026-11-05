using Mediator;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;
using skestock.Domain.Constants;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Common.Behaviours;

public class AuthorizationBehaviourTests
{
    private static readonly UpdateMessageTemplateCommand Command = new(FruitDessertMessageType.Reminder, "n", "<p>b</p>");

    private static async Task<bool> RunAsync(string[] roles)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(Guid.NewGuid());
        user.SetupGet(u => u.Roles).Returns([..roles]);
        var behaviour = new AuthorizationBehaviour<UpdateMessageTemplateCommand, FluentResults.Result>(
            user.Object, Mock.Of<IIdentityService>());
        var called = false;
        await behaviour.Handle(Command, (_, _) => { called = true; return ValueTask.FromResult(FluentResults.Result.Ok()); },
            CancellationToken.None);
        return called;
    }

    [Test]
    public async Task UpdateMessageTemplate_Administrator_Passes() =>
        (await RunAsync([Roles.Administrator])).ShouldBeTrue();

    [Test]
    public void UpdateMessageTemplate_NonAdministrator_IsForbidden() =>
        Should.Throw<ForbiddenAccessException>(() => RunAsync([]));
}
