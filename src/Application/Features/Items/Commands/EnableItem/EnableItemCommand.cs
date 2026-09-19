using skestock.Application.Common.Caching;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.EnableItem;

public class EnableItemCommand : IRequest<Result<ItemDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }

    // Invalidate every cached GetAllItems page/filter/sort combination - an item's active state
    // can affect isActive-filtered results - plus this item's own cached by-id entry.
    public IReadOnlyCollection<string> Tags => [CacheConstants.ItemListTag, CacheConstants.ItemTag(Id)];
}
