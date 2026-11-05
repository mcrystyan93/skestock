using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Infrastructure.FruitDessert;

public class MessageTemplateRendererTests
{
    private readonly MessageTemplateRenderer _renderer = new();

    // 2026-11-09 is a Monday.
    private static readonly MessageTemplateValues Values =
        new("Maria", new DateOnly(2026, 11, 9), "Congregația Nord", "Toamna 2026");

    [Test]
    public void Render_ReplacesAllPlaceholders()
    {
        var result = _renderer.Render("{ContactPerson} | {Date} | {Congregation} | {ClassName}", Values);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Maria | luni, 9 noiembrie 2026 | Congregația Nord | Toamna 2026");
    }

    [Test]
    public void Render_RepeatedPlaceholder_ReplacesEveryOccurrence()
    {
        _renderer.Render("{ContactPerson} și {ContactPerson}", Values).Value.ShouldBe("Maria și Maria");
    }

    [Test]
    public void Render_NoPlaceholders_ReturnsTextUnchanged()
    {
        _renderer.Render("Salut!", Values).Value.ShouldBe("Salut!");
    }

    [Test]
    public void Render_UnknownPlaceholder_FailsNamingIt()
    {
        var result = _renderer.Render("Salut {Foo} și {ContactPerson}", Values);

        result.IsFailed.ShouldBeTrue();
        result.Errors.OfType<FruitDessertErrors.UnknownPlaceholder>().Single().Message.ShouldContain("{Foo}");
    }
}
