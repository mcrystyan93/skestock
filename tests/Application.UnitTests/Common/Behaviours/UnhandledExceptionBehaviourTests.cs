using skestock.Application.Common.Behaviours;
using Mediator;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Behaviours;

public class UnhandledExceptionBehaviourTests
{
    private const string SensitivePayload = "base64-file-content-and-personal-data";

    [Test]
    public async Task ShouldLogErrorWithExceptionAndRethrowWithoutPayload()
    {
        var logger = new CapturingLogger<SensitiveCommand>();
        var request = new SensitiveCommand(SensitivePayload);
        var failure = new InvalidOperationException("boom");
        var behaviour = new UnhandledExceptionBehaviour<SensitiveCommand, Unit>(logger);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behaviour.Handle(request, (_, _) => throw failure, CancellationToken.None));

        thrown.ShouldBeSameAs(failure);
        var entry = logger.Entries.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Error);
        entry.Exception.ShouldBeSameAs(failure);
        entry.State.ShouldContain(new KeyValuePair<string, object?>("RequestName", nameof(SensitiveCommand)));
        entry.Message.ShouldNotContain(SensitivePayload);
        entry.State.ShouldNotContain(pair => ReferenceEquals(pair.Value, request));
    }
}
