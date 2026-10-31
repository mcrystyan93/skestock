using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Congregations.Models;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.Features.Congregations.Commands.UpdateCongregation;

public class UpdateCongregationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateCongregationCommand, Result<CongregationMutationDto>>
{
    public async ValueTask<Result<CongregationMutationDto>> Handle(
        UpdateCongregationCommand request, CancellationToken cancellationToken)
    {
        var congregation = await dbContext.Congregations
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (congregation is null)
            return Result.Fail(new CongregationErrors.CongregationNotFound(request.Id));

        congregation.Name = request.Name.Trim();
        congregation.Email = request.Email.Trim();
        congregation.Coordinator = request.Coordinator?.Trim();
        congregation.CoordinatorPhone = request.CoordinatorPhone?.Trim();
        congregation.DessertContactPerson = request.DessertContactPerson?.Trim();
        congregation.DessertContactPhone = request.DessertContactPhone?.Trim();
        congregation.CanReceiveStudents = request.CanReceiveStudents;
        congregation.IsSmallCongregation = request.IsSmallCongregation;
        congregation.Message = request.Message?.Trim();

        congregation.AddDomainEvent(new CongregationUpdatedEvent(congregation));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(CongregationMutationDto.From(congregation));
    }
}
