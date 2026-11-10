using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;

namespace skestock.Application.Features.Items.Queries.GetItemMergePreview;

public class GetItemMergePreviewQueryHandler(IApplicationDbContext dbContext, IEnumerable<IItemReferenceMerger> mergers)
    : IRequestHandler<GetItemMergePreviewQuery, Result<ItemMergePreviewDto>>
{
    public async ValueTask<Result<ItemMergePreviewDto>> Handle(
        GetItemMergePreviewQuery request, CancellationToken cancellationToken)
    {
        var ids = request.ItemIds.Distinct().ToList();
        var items = await dbContext.Items.AsNoTracking().Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);

        if (ids.FirstOrDefault(id => items.All(item => item.Id != id)) is var missing && missing != Guid.Empty)
            return Result.Fail(new ItemErrors.ItemNotFound(missing));

        if (!items.Single(item => item.Id == request.PrimaryItemId).IsActive)
            return Result.Fail(new ItemErrors.MergePrimaryInactive(request.PrimaryItemId));

        var secondaryIds = ids.Where(id => id != request.PrimaryItemId).ToList();
        var impacts = new List<ItemMergeImpact>();
        foreach (var merger in mergers)
        {
            var impact = await merger.PreviewAsync(request.PrimaryItemId, secondaryIds, cancellationToken);
            if (impact.Relinked + impact.Combined > 0)
                impacts.Add(impact);
        }

        return Result.Ok(new ItemMergePreviewDto(impacts));
    }
}
