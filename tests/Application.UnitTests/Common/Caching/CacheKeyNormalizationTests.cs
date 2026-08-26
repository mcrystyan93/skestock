using System.Globalization;
using skestock.Application.Common.Caching;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Caching;

public class CacheKeyNormalizationTests
{
    [TestCase(null, "_")]
    [TestCase("", "_")]
    [TestCase("   ", "_")]
    [TestCase("Hello World", "hello world")]
    [TestCase("  MixedCase  ", "mixedcase")]
    public void Text_NormalizesValue(string? input, string expected)
    {
        CacheKeyNormalization.Text(input).ShouldBe(expected);
    }

    [Test]
    public void Cursor_WithNullOrWhitespace_ReturnsUnderscore()
    {
        CacheKeyNormalization.Cursor(null).ShouldBe("_");
        CacheKeyNormalization.Cursor("").ShouldBe("_");
        CacheKeyNormalization.Cursor("   ").ShouldBe("_");
    }

    [Test]
    public void Cursor_WithSameInput_ProducesSameHash()
    {
        CacheKeyNormalization.Cursor("abc").ShouldBe(CacheKeyNormalization.Cursor("abc"));
    }

    [Test]
    public void Cursor_WithDifferentInputs_ProducesDifferentHashes()
    {
        CacheKeyNormalization.Cursor("abc").ShouldNotBe(CacheKeyNormalization.Cursor("xyz"));
    }

    [Test]
    public void Cursor_ProducesLowercaseSha256Hex()
    {
        var hash = CacheKeyNormalization.Cursor("some-cursor-token");

        hash.Length.ShouldBe(64); // SHA-256 -> 32 bytes -> 64 hex chars
        hash.ShouldBe(hash.ToLowerInvariant());
        hash.ShouldAllBe(c => Uri.IsHexDigit(c));
    }

    [TestCase(null, "_")]
    [TestCase(true, "true")]
    [TestCase(false, "false")]
    public void Bool_NormalizesValue(bool? input, string expected)
    {
        CacheKeyNormalization.Bool(input).ShouldBe(expected);
    }

    [Test]
    public void Int_WithNull_ReturnsUnderscore()
    {
        CacheKeyNormalization.Int(null).ShouldBe("_");
    }

    [Test]
    public void Int_WithValue_ReturnsInvariantString()
    {
        CacheKeyNormalization.Int(42).ShouldBe("42");
    }

    [Test]
    public void Date_WithNull_ReturnsUnderscore()
    {
        CacheKeyNormalization.Date(null).ShouldBe("_");
    }

    [Test]
    public void Date_WithValue_ReturnsUtcRoundTripFormat()
    {
        var date = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.FromHours(2));

        var expected = date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        CacheKeyNormalization.Date(date).ShouldBe(expected);
    }

    [Test]
    public void Date_WithDifferentOffsetsForSameInstant_ProducesSameKey()
    {
        var utc = new DateTimeOffset(2024, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var plusTwo = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.FromHours(2));

        CacheKeyNormalization.Date(utc).ShouldBe(CacheKeyNormalization.Date(plusTwo));
    }

    [Test]
    public void Sort_WithNullOrEmpty_ReturnsUnderscore()
    {
        CacheKeyNormalization.Sort(null).ShouldBe("_");
        CacheKeyNormalization.Sort([]).ShouldBe("_");
    }

    [Test]
    public void Sort_NormalizesDirectionsAndJoinsMultipleEntries()
    {
        var sort = new List<PaginationSort>
        {
            new() { Key = "Name", Value = "ascend" },
            new() { Key = "createdDate", Value = "DESC" }
        };

        CacheKeyNormalization.Sort(sort).ShouldBe("name:asc,createddate:desc");
    }

    [Test]
    public void Sort_WithAlreadyShortDirections_KeepsThemNormalized()
    {
        var sort = new List<PaginationSort> { new() { Key = "id", Value = "ASC" } };

        CacheKeyNormalization.Sort(sort).ShouldBe("id:asc");
    }

    [Test]
    public void Filters_WithNullOrEmpty_ReturnsUnderscore()
    {
        CacheKeyNormalization.Filters(null).ShouldBe("_");
        CacheKeyNormalization.Filters([]).ShouldBe("_");
    }

    [Test]
    public void Filters_NormalizesFieldOperatorAndStringValue()
    {
        var filters = new List<ColumnFilter> { new("Name", FilterOperator.Equals, "Alpha") };

        CacheKeyNormalization.Filters(filters).ShouldBe("name:equals:alpha");
    }

    [Test]
    public void Filters_WithBooleanValue_NormalizesToTrueOrFalse()
    {
        var filters = new List<ColumnFilter> { new("isActive", FilterOperator.Equals, true) };

        CacheKeyNormalization.Filters(filters).ShouldBe("isactive:equals:true");
    }

    [Test]
    public void Filters_WithNullValue_UsesUnderscorePlaceholder()
    {
        var filters = new List<ColumnFilter> { new("rank", FilterOperator.Equals, null) };

        CacheKeyNormalization.Filters(filters).ShouldBe("rank:equals:_");
    }

    [Test]
    public void Filters_WithMultipleFilters_JoinsWithComma()
    {
        var filters = new List<ColumnFilter>
        {
            new("id", FilterOperator.Equals, 1),
            new("name", FilterOperator.Contains, "abc")
        };

        CacheKeyNormalization.Filters(filters).ShouldBe("id:equals:1,name:contains:abc");
    }

    [Test]
    public void Filters_WithFormattableValue_UsesInvariantCultureFormatting()
    {
        var filters = new List<ColumnFilter> { new("price", FilterOperator.GreaterThan, 15.5m) };

        CacheKeyNormalization.Filters(filters).ShouldBe("price:greaterthan:15.5");
    }
}
