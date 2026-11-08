using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.Commands.MergeItems;

public class MergeItemsCommandHandler(IApplicationDbContext dbContext, IEnumerable<IItemReferenceMerger> mergers)
    : IRequestHandler<MergeItemsCommand, Result>
{
    public async ValueTask<Result> Handle(MergeItemsCommand request, CancellationToken cancellationToken) =>
        await dbContext.InTransactionAsync(() => MergeAsync(request, cancellationToken), cancellationToken);

    private async Task<Result> MergeAsync(MergeItemsCommand request, CancellationToken cancellationToken)
    {
        // Not IRequiresOpenClass: the merge also rewrites closed classes. The exclusive lock waits for in-flight
        // class-scoped commands and holds new ones back until commit.
        await dbContext.ExclusiveLockAllClasses().ToListAsync(cancellationToken);

        var ids = request.ItemIds.Distinct().ToList();
        var items = await dbContext.Items.Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);

        if (ids.FirstOrDefault(id => items.All(item => item.Id != id)) is var missing && missing != Guid.Empty)
            return Result.Fail(new ItemErrors.ItemNotFound(missing));

        if (!items.Single(item => item.Id == request.PrimaryItemId).IsActive)
            return Result.Fail(new ItemErrors.MergePrimaryInactive(request.PrimaryItemId));

        var secondaryIds = ids.Where(id => id != request.PrimaryItemId).ToList();
        foreach (var merger in mergers)
            await merger.MergeAsync(request.PrimaryItemId, secondaryIds, cancellationToken);

        foreach (var secondary in items.Where(item => item.Id != request.PrimaryItemId))
        {
            secondary.IsActive = false;
            secondary.AddDomainEvent(new ItemDisabledEvent(secondary));
        }

        // One SaveChanges inside the transaction keeps the whole merge atomic; the StockBatch rowversion turns a
        // concurrent draw-down into a concurrency exception that rolls everything back.
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
