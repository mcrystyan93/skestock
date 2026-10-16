using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses.Commands.CloseSchoolClass;

public sealed class CloseSchoolClassCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CloseSchoolClassCommand, Result>
{
    public async ValueTask<Result> Handle(CloseSchoolClassCommand request, CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(
            user.Id,
            message: "Closing a school class requires an authenticated user.");

        var classes = await dbContext.SchoolClasses
            .Where(c => c.Id == request.Id || c.Id == request.TargetClassId)
            .ToListAsync(cancellationToken);

        var source = classes.SingleOrDefault(c => c.Id == request.Id);
        if (source is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.Id));

        var target = classes.SingleOrDefault(c => c.Id == request.TargetClassId);
        if (target is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.TargetClassId));

        if (source.Status == ClassStatus.Closed)
            return Result.Fail(new SchoolClassErrors.AlreadyClosed(source.Id));

        if (target.Status == ClassStatus.Closed)
            return Result.Fail(new SchoolClassErrors.TargetNotOpen(target.Id));

        var sourceBatches = await dbContext.StockBatches
            .Where(b => b.ReceivedClassId == source.Id && b.Quantity > 0)
            .ToListAsync(cancellationToken);

        var targetBatches = new List<StockBatch>(sourceBatches.Count);
        foreach (var sourceBatch in sourceBatches)
        {
            var quantity = sourceBatch.Quantity;
            var targetBatch = new StockBatch
            {
                ItemId = sourceBatch.ItemId,
                LocationId = sourceBatch.LocationId,
                ReceivedClassId = target.Id,
                Quantity = quantity,
                ExpiryDate = sourceBatch.ExpiryDate,
                ReceivedDate = sourceBatch.ReceivedDate,
                UnitPrice = sourceBatch.UnitPrice,
                GoodsReceiptId = sourceBatch.GoodsReceiptId
            };
            targetBatches.Add(targetBatch);
            sourceBatch.Quantity = 0;

            dbContext.StockTransactions.Add(RolloverTransaction(sourceBatch, source.Id, identityId, -quantity));
            dbContext.StockTransactions.Add(RolloverTransaction(targetBatch, target.Id, identityId, quantity));
        }

        dbContext.StockBatches.AddRange(targetBatches);
        source.Status = ClassStatus.Closed;

        // One SaveChangesAsync keeps the status change and the whole transfer atomic. The source-batch
        // rowversion turns a concurrent draw-down (or a second close) into a concurrency exception.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Fail(new StockErrors.ConcurrencyConflict(source.Id, Guid.Empty, Guid.Empty));
        }

        return Result.Ok();
    }

    private static StockTransaction RolloverTransaction(StockBatch batch, Guid classId, Guid userId, int change) =>
        new()
        {
            ItemId = batch.ItemId,
            LocationId = batch.LocationId,
            Batch = batch,
            ClassId = classId,
            UserId = userId,
            Type = StockTransactionType.Rollover,
            QuantityChange = change
        };
}
