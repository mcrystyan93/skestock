using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.StockBatches.Commands.CreateStockBatch;
using skestock.Application.Features.Statistics;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.StockBatches.Commands.CreateStockBatch;

public class CreateStockBatchCommandTagsTests
{
    [Test]
    public void Donation_invalidates_the_donation_statistics()
    {
        var command = new CreateStockBatchCommand { Source = StockBatchSource.Donation };

        command.Tags.ShouldContain(CacheConstants.DonationStatisticsTag);
    }

    [Test]
    public void Order_leaves_the_donation_statistics_cached()
    {
        var command = new CreateStockBatchCommand { Source = StockBatchSource.Order };

        command.Tags.ShouldNotContain(CacheConstants.DonationStatisticsTag);
    }
}
