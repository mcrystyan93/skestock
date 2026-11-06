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

    [Test]
    public void Render_HtmlTemplate_ProducesWhatsAppMarkupWithValues()
    {
        var result = _renderer.Render("<p>Bună, <strong>{ContactPerson}</strong>!</p><p>Pe {Date}.</p>", Values);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldContain("Bună, *Maria*!");
        result.Value.ShouldContain("Pe luni, 9 noiembrie 2026.");
        result.Value.ShouldNotContain("<");
    }

    [Test]
    public void Render_ValuesWithMarkupCharacters_AreInsertedVerbatim()
    {
        var values = new MessageTemplateValues("<b>*Ana*</b>", new DateOnly(2026, 11, 9), "A<B", "C*D");

        var result = _renderer.Render("<p>{ContactPerson} {Congregation} {ClassName}</p>", values);

        result.Value.ShouldBe("<b>*Ana*</b> A<B C*D");
    }

    [Test]
    public void Render_HtmlTemplateWithUnknownPlaceholder_Fails()
    {
        var result = _renderer.Render("<p>{Foo}</p>", Values);

        result.Errors.OfType<FruitDessertErrors.UnknownPlaceholder>().Single().Message.ShouldContain("{Foo}");
    }

    [Test]
    public void Render_SeededLegacyBodies_RenderAsBefore()
    {
        _renderer.Render(
            "Bună ziua, {ContactPerson}! Vă reamintim că {Date} congregația {Congregation} aduce fructe și desert pentru {ClassName}. Vă mulțumim!",
            Values).Value.ShouldBe(
            "Bună ziua, Maria! Vă reamintim că luni, 9 noiembrie 2026 congregația Congregația Nord aduce fructe și desert pentru Toamna 2026. Vă mulțumim!");
        _renderer.Render(
            "Bună ziua, {ContactPerson}! Vă mulțumim că {Date} congregația {Congregation} a adus fructe și desert pentru {ClassName}. Dumnezeu să vă binecuvânteze!",
            Values).Value.ShouldBe(
            "Bună ziua, Maria! Vă mulțumim că luni, 9 noiembrie 2026 congregația Congregația Nord a adus fructe și desert pentru Toamna 2026. Dumnezeu să vă binecuvânteze!");
    }
}
