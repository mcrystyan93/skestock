using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Common.Behaviours;

/// <summary>
/// Rejects a change to a closed class early, before validation. <see cref="ClassLockBehaviour{TRequest,TResponse}"/>
/// repeats the check under a lock, close to the handler, so closing the class cannot race it.
/// </summary>
public class ClassOpenBehaviour<TRequest, TResponse>(IApplicationDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage, IRequiresOpenClass
    where TResponse : ResultBase<TResponse>, new()
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        // An unknown class falls through so the handler keeps returning its own not-found error.
        if (await dbContext.ResolveClassIdAsync(request, cancellationToken) is { } id
            && await dbContext.SchoolClasses.AsNoTracking()
                .AnyAsync(c => c.Id == id && c.Status == ClassStatus.Closed, cancellationToken))
            return new TResponse().WithError(new SchoolClassErrors.SchoolClassClosed(id));

        return await next(request, cancellationToken);
    }
}
