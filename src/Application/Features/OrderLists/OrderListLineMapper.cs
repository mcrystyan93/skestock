using skestock.Application.Common.Interfaces;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.OrderLists;

// Builds OrderListLine entities from the command input, snapshotting ProductName/Unit from the
// catalog Item when ItemId is set and ProductName/Unit are not explicitly provided.
internal static class OrderListLineMapper
{
    public static async Task<List<OrderListLine>> BuildLinesAsync(
        IApplicationDbContext dbContext,
        IReadOnlyCollection<OrderListLineInput> inputs,
        CancellationToken cancellationToken)
    {
        var itemIds = inputs
            .Where(i => i.ItemId.HasValue)
            .Select(i => i.ItemId!.Value)
            .Distinct()
            .ToList();

        var itemMap = itemIds.Count > 0
            ? await dbContext.Items
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Name, i.Unit })
                .ToDictionaryAsync(i => i.Id, cancellationToken)
            : new();

        var lines = new List<OrderListLine>(inputs.Count);

        foreach (var input in inputs)
        {
            string productName;
            string? unit = string.IsNullOrWhiteSpace(input.Unit) ? null : input.Unit.Trim();

            if (input.ItemId.HasValue && itemMap.TryGetValue(input.ItemId.Value, out var item))
            {
                productName = string.IsNullOrWhiteSpace(input.ProductName) ? item.Name : input.ProductName.Trim();
                unit ??= item.Unit;
            }
            else
            {
                productName = input.ProductName!.Trim();
            }

            lines.Add(new OrderListLine
            {
                ItemId = input.ItemId,
                ProductName = productName,
                Quantity = input.Quantity,
                Unit = unit,
                Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim()
            });
        }

        return lines;
    }
}
