using NUnit.Framework;
using Shouldly;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Infrastructure.FruitDessert;

public class PhoneNumberNormalizerTests
{
    private readonly PhoneNumberNormalizer _normalizer = new();

    [TestCase("0722 123 456")]
    [TestCase("0722-123-456")]
    [TestCase("+40722123456")]
    [TestCase("0040722123456")]
    [TestCase("40722123456")]
    [TestCase("  (0722) 123.456 ")]
    public void Normalize_RomanianFormats_ReturnInternationalNumber(string input)
    {
        var result = _normalizer.Normalize(input);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("40722123456");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("abc")]
    [TestCase("0722 12a 456")]
    [TestCase("07221234")]
    [TestCase("072212345678")]
    [TestCase("+4072212345")]
    [TestCase("+49 151 2345 6789")]
    public void Normalize_InvalidInput_Fails(string? input)
    {
        _normalizer.Normalize(input).IsFailed.ShouldBeTrue();
    }
}
