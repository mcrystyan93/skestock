using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.Features.Congregations.Commands.DeleteCongregation;

public class DeleteCongregationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteCongregationCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteCongregationCommand request, CancellationToken cancellationToken)
    {
        // The global query filter hides already-deleted rows, so a second delete is "not found".
        var congregation = await dbContext.Congregations
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (congregation is null)
            return Result.Fail(new CongregationErrors.CongregationNotFound(request.Id));

        congregation.DeletedDate = DateTimeOffset.UtcNow;
        congregation.AddDomainEvent(new CongregationDeletedEvent(congregation));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
