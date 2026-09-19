using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;
using skestock.Domain.Events.Stock;

namespace skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;

public class SetClassItemStockVisibilityCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SetClassItemStockVisibilityCommand, Result>
{
    public async ValueTask<Result> Handle(
        SetClassItemStockVisibilityCommand request,
        CancellationToken cancellationToken)
    {
        var visibility = await dbContext.ClassItemStockVisibilities
            .SingleOrDefaultAsync(
                x => x.ClassId == request.ClassId
                     && x.ItemId == request.ItemId
                     && x.LocationId == request.LocationId,
                cancellationToken);

        if (visibility is not null && visibility.HideWhenZeroStock == request.HideWhenZeroStock)
            return Result.Ok();

        if (visibility is null)
        {
            if (!request.HideWhenZeroStock)
                return Result.Ok();

            visibility = new ClassItemStockVisibility
            {
                ClassId = request.ClassId,
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                HideWhenZeroStock = true
            };

            dbContext.ClassItemStockVisibilities.Add(visibility);
        }
        else
        {
            visibility.HideWhenZeroStock = request.HideWhenZeroStock;
        }

        visibility.AddDomainEvent(new ClassItemStockVisibilityChangedEvent(
            request.ClassId,
            request.ItemId,
            request.LocationId,
            request.HideWhenZeroStock));

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
