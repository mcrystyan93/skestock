using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Congregations.Models;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Events.Congregations;

namespace skestock.Application.Features.Congregations.Commands.CreateCongregation;

public class CreateCongregationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateCongregationCommand, Result<CongregationMutationDto>>
{
    public async ValueTask<Result<CongregationMutationDto>> Handle(
        CreateCongregationCommand request, CancellationToken cancellationToken)
    {
        var congregation = new Congregation
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Coordinator = request.Coordinator?.Trim(),
            CoordinatorPhone = request.CoordinatorPhone?.Trim(),
            DessertContactPerson = request.DessertContactPerson?.Trim(),
            DessertContactPhone = request.DessertContactPhone?.Trim(),
            CanReceiveStudents = request.CanReceiveStudents,
            IsSmallCongregation = request.IsSmallCongregation,
            Message = request.Message?.Trim()
        };

        congregation.AddDomainEvent(new CongregationCreatedEvent(congregation));
        dbContext.Congregations.Add(congregation);
        await CongregationCommandSupport.SaveAsync(dbContext, congregation, cancellationToken);

        return Result.Ok(CongregationMutationDto.From(congregation));
    }
}
