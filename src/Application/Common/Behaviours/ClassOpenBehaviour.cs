using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Enums;

namespace skestock.Application.Common.Behaviours;

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
        var classId = await ResolveClassIdAsync(request, cancellationToken);

        // An unknown class falls through so the handler keeps returning its own not-found error.
        if (classId is { } id
            && await dbContext.SchoolClasses.AsNoTracking()
                .AnyAsync(c => c.Id == id && c.Status == ClassStatus.Closed, cancellationToken))
            return new TResponse().WithError(new SchoolClassErrors.SchoolClassClosed(id));

        return await next(request, cancellationToken);
    }

    private async Task<Guid?> ResolveClassIdAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (request.ClassId is { } classId)
            return classId;

        if (request.OrderListId is { } orderListId)
            return await dbContext.OrderLists.AsNoTracking()
                .Where(o => o.Id == orderListId)
                .Select(o => (Guid?)o.ClassId)
                .SingleOrDefaultAsync(cancellationToken);

        if (request.GoodsReceiptImportId is { } importId)
            return await dbContext.GoodsReceiptImports.AsNoTracking()
                .Where(i => i.Id == importId)
                .Select(i => (Guid?)i.ClassId)
                .SingleOrDefaultAsync(cancellationToken);

        return null;
    }
}
