using System.Linq.Expressions;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.Statistics.Queries;

internal static class PurchaseStatisticProjection
{
    public static readonly Expression<Func<ItemPurchaseStatistic, PurchaseStatisticDto>> ToDto = s =>
        new PurchaseStatisticDto
        {
            ItemId = s.ItemId,
            ItemName = s.Item.Name,
            Sku = s.Item.Sku,
            Unit = s.Item.Unit,
            CategoryName = s.Item.Category.Name,
            TotalQuantity = s.TotalQuantity,
            TotalValue = s.TotalValue,
            PurchaseCount = s.PurchaseCount,
            AverageQuantity = s.AverageQuantity,
            AverageUnitPrice = s.AverageUnitPrice,
            LastPurchasedAt = s.LastPurchasedAt
        };
}
