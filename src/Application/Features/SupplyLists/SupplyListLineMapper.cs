using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.SupplyLists;

internal static class SupplyListLineMapper
{
    // Applies the request lines to the list: existing lines are updated in place (matched by
    // item), missing ones removed and new ones added. Items already on the list may since have
    // been disabled and are kept; only newly added items must be active.
    public static async Task<Result> ApplyLinesAsync(
        IApplicationDbContext dbContext,
        SupplyList supplyList,
        IReadOnlyCollection<SupplyListLineInput> inputs,
        CancellationToken cancellationToken)
    {
        var itemIds = inputs.Select(i => i.ItemId).ToList();

        var items = await dbContext.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Unit, i.IsActive })
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var existing = supplyList.Lines.ToDictionary(l => l.ItemId);

        foreach (var input in inputs)
        {
            if (!items.TryGetValue(input.ItemId, out var item))
                return Result.Fail(new ItemErrors.ItemNotFound(input.ItemId));

            if (!existing.ContainsKey(input.ItemId) && !item.IsActive)
                return Result.Fail(new SupplyListErrors.SupplyListItemInactive(input.ItemId));
        }

        var removed = supplyList.Lines.Where(l => !itemIds.Contains(l.ItemId)).ToList();
        dbContext.SupplyListLines.RemoveRange(removed);
        foreach (var line in removed)
            supplyList.Lines.Remove(line);

        foreach (var input in inputs)
        {
            var unit = string.IsNullOrWhiteSpace(input.Unit) ? items[input.ItemId].Unit : input.Unit.Trim();
            var notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

            if (existing.TryGetValue(input.ItemId, out var line))
            {
                line.Quantity = input.Quantity;
                line.Unit = unit;
                line.Notes = notes;
            }
            else
            {
                supplyList.Lines.Add(new SupplyListLine
                {
                    ItemId = input.ItemId,
                    Quantity = input.Quantity,
                    Unit = unit,
                    Notes = notes
                });
            }
        }

        return Result.Ok();
    }
}
