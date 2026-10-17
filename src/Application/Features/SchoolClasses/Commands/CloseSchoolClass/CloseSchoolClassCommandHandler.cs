using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses.Commands.CloseSchoolClass;

public sealed class CloseSchoolClassCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CloseSchoolClassCommand, Result>
{
    public async ValueTask<Result> Handle(CloseSchoolClassCommand request, CancellationToken cancellationToken) =>
        await dbContext.InTransactionAsync(() => CloseAsync(request, cancellationToken), cancellationToken);

    private async Task<Result> CloseAsync(CloseSchoolClassCommand request, CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(
            user.Id,
            message: "Closing a school class requires an authenticated user.");

        // The exclusive lock waits for in-flight changes to either class and blocks new ones until commit.
        var classes = await dbContext.ExclusiveLock(request.Id, request.TargetClassId)
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

        // Zero-quantity batches are loaded too: their (item, location) pairs still get a ClosingQty of 0.
        var sourceBatches = await dbContext.StockBatches
            .Where(b => b.ReceivedClassId == source.Id)
            .ToListAsync(cancellationToken);

        var balances = await dbContext.ClassBalances
            .Where(b => b.ClassId == source.Id || b.ClassId == target.Id)
            .ToListAsync(cancellationToken);

        var targetBatches = new List<StockBatch>();
        foreach (var sourceBatch in sourceBatches)
        {
            var quantity = sourceBatch.Quantity;

            var sourceBalance = GetOrAddBalance(balances, source.Id, sourceBatch);
            sourceBalance.ClosingQty ??= 0;
            if (quantity <= 0)
                continue;

            sourceBalance.ClosingQty += quantity;
            GetOrAddBalance(balances, target.Id, sourceBatch).OpeningQty += quantity;

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
            return Result.Fail(new SchoolClassErrors.ConcurrentChange(source.Id));
        }

        return Result.Ok();
    }

    // ClassBalance has no unique index, so look the row up here rather than relying on the database.
    private ClassBalance GetOrAddBalance(List<ClassBalance> balances, Guid classId, StockBatch batch)
    {
        var balance = balances.FirstOrDefault(b =>
            b.ClassId == classId && b.ItemId == batch.ItemId && b.LocationId == batch.LocationId);
        if (balance is not null)
            return balance;

        balance = new ClassBalance { ClassId = classId, ItemId = batch.ItemId, LocationId = batch.LocationId };
        balances.Add(balance);
        dbContext.ClassBalances.Add(balance);
        return balance;
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
