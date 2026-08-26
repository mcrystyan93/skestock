using System.Text.Json;
using skestock.Application.Common.Filtering;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Filtering;

public class FilterValueParserTests
{
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void TryParseBoolean_WithBooleanValue_ReturnsItDirectly(bool input, bool expected)
    {
        FilterValueParser.TryParseBoolean(input, out var result).ShouldBeTrue();
        result.ShouldBe(expected);
    }

    [TestCase("true", true)]
    [TestCase("True", true)]
    [TestCase("false", false)]
    [TestCase("FALSE", false)]
    [TestCase("'true'", true)]
    [TestCase("\"false\"", false)]
    [TestCase(" true ", true)]
    [TestCase("1", true)]
    [TestCase("0", false)]
    public void TryParseBoolean_WithStringValue_ParsesCorrectly(string input, bool expected)
    {
        FilterValueParser.TryParseBoolean(input, out var result).ShouldBeTrue();
        result.ShouldBe(expected);
    }

    [TestCase("2")]
    [TestCase("yes")]
    [TestCase("")]
    [TestCase("not-a-bool")]
    public void TryParseBoolean_WithUnparsableString_ReturnsFalse(string input)
    {
        FilterValueParser.TryParseBoolean(input, out _).ShouldBeFalse();
    }

    [TestCase(1, true)]
    [TestCase(0, false)]
    public void TryParseBoolean_WithNumericValue_ParsesZeroOrOne(int input, bool expected)
    {
        FilterValueParser.TryParseBoolean(input, out var result).ShouldBeTrue();
        result.ShouldBe(expected);
    }

    [Test]
    public void TryParseBoolean_WithOutOfRangeNumericValue_ReturnsFalse()
    {
        FilterValueParser.TryParseBoolean(5, out _).ShouldBeFalse();
    }

    [Test]
    public void TryParseBoolean_WithJsonTrue_ReturnsTrue()
    {
        using var doc = JsonDocument.Parse("true");
        FilterValueParser.TryParseBoolean(doc.RootElement, out var result).ShouldBeTrue();
        result.ShouldBeTrue();
    }

    [Test]
    public void TryParseBoolean_WithJsonFalse_ReturnsFalse()
    {
        using var doc = JsonDocument.Parse("false");
        FilterValueParser.TryParseBoolean(doc.RootElement, out var result).ShouldBeTrue();
        result.ShouldBeFalse();
    }

    [Test]
    public void TryParseBoolean_WithJsonStringValue_ParsesCorrectly()
    {
        using var doc = JsonDocument.Parse("\"true\"");
        FilterValueParser.TryParseBoolean(doc.RootElement, out var result).ShouldBeTrue();
        result.ShouldBeTrue();
    }

    [Test]
    public void TryParseBoolean_WithJsonNumberValue_ParsesCorrectly()
    {
        using var doc = JsonDocument.Parse("1");
        FilterValueParser.TryParseBoolean(doc.RootElement, out var result).ShouldBeTrue();
        result.ShouldBeTrue();
    }

    [Test]
    public void TryParseBoolean_WithJsonArray_ReturnsFalse()
    {
        using var doc = JsonDocument.Parse("[1,2]");
        FilterValueParser.TryParseBoolean(doc.RootElement, out _).ShouldBeFalse();
    }

    [Test]
    public void TryParseBoolean_WithNullValue_ReturnsFalse()
    {
        FilterValueParser.TryParseBoolean(null, out _).ShouldBeFalse();
    }

    [Test]
    public void TryParseBoolean_WithUnsupportedType_ReturnsFalse()
    {
        FilterValueParser.TryParseBoolean(DateTimeOffset.UtcNow, out _).ShouldBeFalse();
    }
}
