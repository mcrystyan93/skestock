using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.GoodsReceipts;
using skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;
using skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;
using skestock.Application.Features.Locations.Commands.UpdateLocation;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;

public class GetGoodsReceiptImportLinesQueryTests
{
    [Test]
    public void Validator_RejectsEmptyReceiptId()
    {
        var errors = new GetGoodsReceiptImportLinesQueryValidator().Validate(new GetGoodsReceiptImportLinesQuery()).Errors;
        errors.Single().PropertyName.ShouldBe(nameof(GetGoodsReceiptImportLinesQuery.Id));
        errors.Single().ErrorCode.ShouldBe(ValidationErrorCodes.Required);
        new GetGoodsReceiptImportLinesQueryValidator().Validate(
            new GetGoodsReceiptImportLinesQuery { Id = Guid.NewGuid() }).IsValid.ShouldBeTrue();
    }

    [Test]
    public void Cache_SeparatesReceiptsAndSharesExistingInvalidationTags()
    {
        var first = new GetGoodsReceiptImportLinesQuery { Id = Guid.NewGuid() };
        var same = new GetGoodsReceiptImportLinesQuery { Id = first.Id };
        var other = new GetGoodsReceiptImportLinesQuery { Id = Guid.NewGuid() };
        first.BuildCacheKey().ShouldBe(same.BuildCacheKey());
        first.BuildCacheKey().ShouldNotBe(other.BuildCacheKey());
        first.Tags.ShouldContain(CacheConstants.BuildReceiptTag(first.Id));
        first.Tags.Intersect(new ConfirmGoodsReceiptImportCommand().Tags).ShouldNotBeEmpty();
        first.Tags.Intersect(new UpdateLocationCommand().Tags).ShouldNotBeEmpty();
        first.BypassCache.ShouldBeFalse();
        first.SlidingExpiration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        first.SlidingExpiration!.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(8));
    }
}
