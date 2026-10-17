using System.Runtime.CompilerServices;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Common.Behaviours;

/// <summary>
/// Database reader/writer lock on school class rows. Commands that change a class's data hold a shared lock
/// (<c>HOLDLOCK</c>) until their transaction ends; closing a class takes an exclusive one (<c>XLOCK</c>), so it
/// waits for in-flight changes and later changes see the class as closed.
/// </summary>
internal static class ClassLocking
{
    /// <summary>
    /// Runs <paramref name="operation"/> in a transaction that commits only when it succeeds, as one retriable unit
    /// of the configured execution strategy. Joins the caller's transaction when there is one, and runs plainly on
    /// providers without transactions (in-memory).
    /// </summary>
    public static async Task<TResult> InTransactionAsync<TResult>(
        this IApplicationDbContext dbContext, Func<Task<TResult>> operation, CancellationToken cancellationToken)
        where TResult : IResultBase
    {
        if (!dbContext.Database.IsRelational() || dbContext.Database.CurrentTransaction is not null)
            return await operation();

        return await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await operation();
            if (result.IsSuccess)
                await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public static IQueryable<SchoolClass> SharedLock(this IApplicationDbContext dbContext, Guid classId) =>
        dbContext.Database.IsRelational()
            ? dbContext.SchoolClasses.FromSqlInterpolated(
                $"SELECT * FROM [SchoolClasses] WITH (HOLDLOCK) WHERE [Id] = {classId}")
            : dbContext.SchoolClasses.Where(c => c.Id == classId);

    public static IQueryable<SchoolClass> ExclusiveLock(
        this IApplicationDbContext dbContext, Guid classId, Guid otherClassId) =>
        dbContext.Database.IsRelational()
            ? dbContext.SchoolClasses.FromSqlInterpolated(
                $"SELECT * FROM [SchoolClasses] WITH (XLOCK, HOLDLOCK) WHERE [Id] = {classId} OR [Id] = {otherClassId}")
            : dbContext.SchoolClasses.Where(c => c.Id == classId || c.Id == otherClassId);

    // ClassOpenBehaviour and ClassLockBehaviour both resolve the same request; remember the first answer.
    // A request's order list / import never changes class, and an unknown target is not remembered.
    private static readonly ConditionalWeakTable<IRequiresOpenClass, object> ResolvedClassIds = new();

    public static async Task<Guid?> ResolveClassIdAsync(
        this IApplicationDbContext dbContext, IRequiresOpenClass request, CancellationToken cancellationToken)
    {
        if (new object?[] { request.ClassId, request.OrderListId, request.GoodsReceiptImportId }.Count(v => v is not null) > 1)
            throw new InvalidOperationException(
                $"{request.GetType().Name} must implement exactly one {nameof(IRequiresOpenClass)} member.");

        if (request.ClassId is { } classId)
            return classId;

        if (ResolvedClassIds.TryGetValue(request, out var cached))
            return (Guid)cached;

        Guid? resolved = null;
        if (request.OrderListId is { } orderListId)
            resolved = await dbContext.OrderLists.AsNoTracking()
                .Where(o => o.Id == orderListId)
                .Select(o => (Guid?)o.ClassId)
                .SingleOrDefaultAsync(cancellationToken);
        else if (request.GoodsReceiptImportId is { } importId)
            resolved = await dbContext.GoodsReceiptImports.AsNoTracking()
                .Where(i => i.Id == importId)
                .Select(i => (Guid?)i.ClassId)
                .SingleOrDefaultAsync(cancellationToken);

        if (resolved is { } id)
            ResolvedClassIds.AddOrUpdate(request, id);
        return resolved;
    }
}
