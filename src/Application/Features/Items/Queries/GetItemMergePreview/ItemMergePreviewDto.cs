using skestock.Application.Features.Items.Commands.MergeItems.Mergers;

namespace skestock.Application.Features.Items.Queries.GetItemMergePreview;

/// <param name="Impacts">Only kinds with at least one affected row.</param>
public sealed record ItemMergePreviewDto(IReadOnlyList<ItemMergeImpact> Impacts);
