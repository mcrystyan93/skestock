using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Common.Behaviours;

/// <summary>
/// Innermost behaviour: holds a shared lock on the class until the handler's transaction commits, so closing the
/// class (exclusive lock) waits for in-flight changes and later changes see it closed. Being innermost, the commit
/// happens before <c>CacheInvalidationBehavior</c> invalidates.
/// </summary>
public class ClassLockBehaviour<TRequest, TResponse>(IApplicationDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage, IRequiresOpenClass
    where TResponse : ResultBase<TResponse>, new()
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ResolveClassIdAsync(request, cancellationToken) is not { } id)
            return await next(request, cancellationToken);

        return await dbContext.InTransactionAsync(async () =>
            await dbContext.SharedLock(id).AsNoTracking()
                .AnyAsync(c => c.Status == ClassStatus.Closed, cancellationToken)
                ? new TResponse().WithError(new SchoolClassErrors.SchoolClassClosed(id))
                : await next(request, cancellationToken), cancellationToken);
    }
}
