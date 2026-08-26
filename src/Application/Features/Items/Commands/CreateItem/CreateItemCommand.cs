using skestock.Application.Common.Caching;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.CreateItem;

public class CreateItemCommand : IRequest<Result<ItemDto>>, ICacheInvalidation
{
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Unit { get; init; } = "unit";
    public int MinThreshold { get; init; }
    public bool IsPerishable { get; init; }
    public int CategoryId { get; init; }

    // Invalidate every cached GetAllItems page/filter/sort combination - a new item can affect
    // any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.ItemListTag];
}
