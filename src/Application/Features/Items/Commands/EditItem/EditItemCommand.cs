using skestock.Application.Common.Caching;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Commands.EditItem;

public class EditItemCommand : IRequest<Result<ItemDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public string? Sku { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Unit { get; init; } = "unit";
    public int MinThreshold { get; init; }
    public bool IsPerishable { get; init; }
    public Guid CategoryId { get; init; }

    // Invalidate every cached GetAllItems page/filter/sort combination - an edited item can
    // affect any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.ItemListTag];
}
