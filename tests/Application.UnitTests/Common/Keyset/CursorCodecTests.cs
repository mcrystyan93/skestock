using skestock.Application.Common.Keyset;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Keyset;

public class CursorCodecTests
{
    private static readonly KeysetTestItemSortConfiguration Config = new();

    [Test]
    public void EncodeThenDecode_RoundTripsKeyValues()
    {
        var entity = new KeysetTestItem { Id = KeysetTestIds.Of(7), CreatedDate = DateTimeOffset.UtcNow, Priority = 42 };
        var sort = new List<(string Key, string Direction)> { ("Priority", "asc"), ("Id", "asc") };

        var token = CursorCodec<KeysetTestItem>.Encode(entity, sort, Config);
        var decoded = CursorCodec<KeysetTestItem>.Decode(token);

        decoded.ShouldNotBeNull();
        decoded!.KeyValues["Priority"]!.ToString().ShouldBe("42");
        decoded.KeyValues["Id"]!.ToString().ShouldBe(KeysetTestIds.Of(7).ToString());
    }

    [Test]
    public void EncodeThenDecode_RoundTripsExplicitNullValues()
    {
        // Regression test: null values used to be skipped entirely during Encode, which made
        // KeysetPredicateBuilder's cursorValues.TryGetValue lookup fail and abort the predicate
        // for that key (and any deeper tie-breaker keys) instead of treating it as "IS NULL".
        var entity = new KeysetTestItem { Id = KeysetTestIds.Of(7), CreatedDate = DateTimeOffset.UtcNow, Priority = null };
        var sort = new List<(string Key, string Direction)> { ("Priority", "asc"), ("Id", "asc") };

        var token = CursorCodec<KeysetTestItem>.Encode(entity, sort, Config);
        var decoded = CursorCodec<KeysetTestItem>.Decode(token);

        decoded.ShouldNotBeNull();
        decoded!.KeyValues.ShouldContainKey("Priority");
        decoded.KeyValues["Priority"].ShouldBeNull();
        decoded.KeyValues["Id"]!.ToString().ShouldBe(KeysetTestIds.Of(7).ToString());
    }

    [Test]
    public void Decode_WithNullOrWhitespaceToken_ReturnsNull()
    {
        CursorCodec<KeysetTestItem>.Decode(null).ShouldBeNull();
        CursorCodec<KeysetTestItem>.Decode("").ShouldBeNull();
        CursorCodec<KeysetTestItem>.Decode("   ").ShouldBeNull();
    }

    [Test]
    public void Decode_WithMalformedToken_ReturnsNull()
    {
        CursorCodec<KeysetTestItem>.Decode("not-a-valid-base64-token!!!").ShouldBeNull();
    }

    [Test]
    public void MatchesSort_ReturnsTrueForIdenticalSortSpec()
    {
        var entity = new KeysetTestItem { Id = KeysetTestIds.Of(1), CreatedDate = DateTimeOffset.UtcNow };
        var sort = new List<(string Key, string Direction)> { ("CreatedDate", "desc"), ("Id", "desc") };

        var decoded = CursorCodec<KeysetTestItem>.Decode(CursorCodec<KeysetTestItem>.Encode(entity, sort, Config))!;

        CursorCodec<KeysetTestItem>.MatchesSort(decoded, sort).ShouldBeTrue();
    }

    [Test]
    public void MatchesSort_ReturnsFalseWhenDirectionDiffers()
    {
        var entity = new KeysetTestItem { Id = KeysetTestIds.Of(1), CreatedDate = DateTimeOffset.UtcNow };
        var originalSort = new List<(string Key, string Direction)> { ("CreatedDate", "desc"), ("Id", "desc") };
        var requestedSort = new List<(string Key, string Direction)> { ("CreatedDate", "asc"), ("Id", "desc") };

        var decoded = CursorCodec<KeysetTestItem>.Decode(CursorCodec<KeysetTestItem>.Encode(entity, originalSort, Config))!;

        CursorCodec<KeysetTestItem>.MatchesSort(decoded, requestedSort).ShouldBeFalse();
    }

    [Test]
    public void MatchesSort_ReturnsFalseWhenKeyOrderDiffers()
    {
        var entity = new KeysetTestItem { Id = KeysetTestIds.Of(1), CreatedDate = DateTimeOffset.UtcNow, Priority = 3 };
        var originalSort = new List<(string Key, string Direction)> { ("Priority", "asc"), ("Id", "asc") };
        var requestedSort = new List<(string Key, string Direction)> { ("Id", "asc"), ("Priority", "asc") };

        var decoded = CursorCodec<KeysetTestItem>.Decode(CursorCodec<KeysetTestItem>.Encode(entity, originalSort, Config))!;

        CursorCodec<KeysetTestItem>.MatchesSort(decoded, requestedSort).ShouldBeFalse();
    }

    [Test]
    public void MatchesSort_ReturnsFalseWhenKeyCountDiffers()
    {
        var entity = new KeysetTestItem { Id = KeysetTestIds.Of(1), CreatedDate = DateTimeOffset.UtcNow };
        var originalSort = new List<(string Key, string Direction)> { ("CreatedDate", "desc"), ("Id", "desc") };
        var requestedSort = new List<(string Key, string Direction)> { ("Id", "desc") };

        var decoded = CursorCodec<KeysetTestItem>.Decode(CursorCodec<KeysetTestItem>.Encode(entity, originalSort, Config))!;

        CursorCodec<KeysetTestItem>.MatchesSort(decoded, requestedSort).ShouldBeFalse();
    }
}
