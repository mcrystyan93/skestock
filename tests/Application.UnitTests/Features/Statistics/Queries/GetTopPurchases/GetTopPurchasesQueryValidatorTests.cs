using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Queries.GetItemsPurchaseHistory;
using skestock.Application.Features.Statistics.Queries.GetTopPurchases;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetTopPurchases;

[TestFixture]
public sealed class GetTopPurchasesQueryValidatorTests
{
    private readonly GetTopPurchasesQueryValidator _validator = new();

    [Test]
    public void Defaults_are_valid() => _validator.Validate(new GetTopPurchasesQuery()).IsValid.ShouldBeTrue();

    [Test]
    public void Class_scope_requires_class_id()
    {
        var result = _validator.Validate(new GetTopPurchasesQuery { Scope = PurchaseStatisticsScope.Class });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(GetTopPurchasesQuery.ClassId));
    }

    [TestCase(0)]
    [TestCase(GetTopPurchasesQuery.MaxTop + 1)]
    public void Top_out_of_range_fails(int top) =>
        _validator.Validate(new GetTopPurchasesQuery { Top = top }).IsValid.ShouldBeFalse();

    [Test]
    public void Cache_key_varies_by_filters()
    {
        var categoryId = Guid.NewGuid();
        new GetTopPurchasesQuery().BuildCacheKey()
            .ShouldNotBe(new GetTopPurchasesQuery { CategoryId = categoryId }.BuildCacheKey());
        new GetTopPurchasesQuery { Scope = PurchaseStatisticsScope.Last90Days }.BuildCacheKey()
            .ShouldNotBe(new GetTopPurchasesQuery().BuildCacheKey());
    }

    [Test]
    public void Items_history_rejects_too_many_items()
    {
        var query = new GetItemsPurchaseHistoryQuery
        {
            ItemIds = Enumerable.Range(0, GetItemsPurchaseHistoryQuery.MaxItems + 1).Select(_ => Guid.NewGuid()).ToList()
        };

        new GetItemsPurchaseHistoryQueryValidator().Validate(query).IsValid.ShouldBeFalse();
    }

    [Test]
    public void Items_history_cache_key_ignores_order()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        new GetItemsPurchaseHistoryQuery { ItemIds = [a, b] }.BuildCacheKey()
            .ShouldBe(new GetItemsPurchaseHistoryQuery { ItemIds = [b, a] }.BuildCacheKey());
    }
}
