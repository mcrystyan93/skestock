using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.FruitDessertTemplates.Commands;

public class UpdateMessageTemplateCommandValidatorTests
{
    private readonly UpdateMessageTemplateCommandValidator _validator = new();

    private static UpdateMessageTemplateCommand Valid(string? body = null, string? name = null) =>
        new(FruitDessertMessageType.Reminder, name ?? "Nume", body ?? "<p>Salut {ContactPerson}</p>");

    [Test]
    public void Valid_Passes() => _validator.Validate(Valid()).IsValid.ShouldBeTrue();

    [Test]
    public void UndefinedType_Fails() =>
        _validator.Validate(Valid() with { Type = (FruitDessertMessageType)999 }).IsValid.ShouldBeFalse();

    [TestCase("")]
    [TestCase(" ")]
    public void EmptyName_Fails(string name) => _validator.Validate(Valid(name: name)).IsValid.ShouldBeFalse();

    [Test]
    public void Name_BoundaryIs100()
    {
        _validator.Validate(Valid(name: new string('a', 100))).IsValid.ShouldBeTrue();
        _validator.Validate(Valid(name: new string('a', 101))).IsValid.ShouldBeFalse();
    }

    [Test]
    public void EmptyBody_Fails() => _validator.Validate(Valid(body: "")).IsValid.ShouldBeFalse();

    [Test]
    public void Body_BoundaryIs4000()
    {
        // Asserts on the MaxLength error code only; both bodies also trip the separate 2000 rendered check.
        _validator.Validate(Valid(body: new string('a', 4001)))
            .Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
        _validator.Validate(Valid(body: new string('a', 4000)))
            .Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public void Body_LengthIsMeasuredAfterSanitizing()
    {
        // 2000 '&' becomes 10000 chars once encoded.
        _validator.Validate(Valid(body: "<p>" + new string('&', 2000) + "</p>"))
            .Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
        _validator.Validate(Valid(body: "<p>" + new string('&', 700) + "</p>"))
            .Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public void UnknownPlaceholder_FailsListingTokens()
    {
        var result = _validator.Validate(Valid(body: "<p>{Foo} {ClassName}</p>"));

        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorMessage.ShouldContain("{Foo}");
        result.Errors.Single().ErrorMessage.ShouldContain("{ContactPerson}");
    }

    [Test]
    public void RenderedLength_BoundaryIs2000()
    {
        // {ClassName} expands to 100 chars worst case: 11 chars -> 111.
        var filler = 2000 - 100;
        _validator.Validate(Valid(body: new string('a', filler) + "{ClassName}")).IsValid.ShouldBeTrue();
        var result = _validator.Validate(Valid(body: new string('a', filler + 1) + "{ClassName}"));
        result.IsValid.ShouldBeFalse();
        result.Errors.Single().ErrorMessage.ShouldContain("2000");
    }
}
