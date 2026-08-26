using skestock.Application.Common.Caching;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.DisableItem;

public class DisableItemCommand : IRequest<Result<ItemDto>>, ICacheInvalidation
{
    public int Id { get; init; }

    // Invalidate every cached GetAllItems page/filter/sort combination - an item's active state
    // can affect isActive-filtered results.
    public IReadOnlyCollection<string> Tags => [CacheConstants.ItemListTag];
}
