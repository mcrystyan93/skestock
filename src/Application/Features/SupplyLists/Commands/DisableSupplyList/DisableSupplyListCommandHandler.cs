using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.Commands.DisableSupplyList;

public class DisableSupplyListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DisableSupplyListCommand, Result<SupplyListDto>>
{
    public async ValueTask<Result<SupplyListDto>> Handle(DisableSupplyListCommand request, CancellationToken cancellationToken) =>
        await SupplyListCommandSupport.SetActiveAsync(
            dbContext, request.Id, false, l => new SupplyListDisabledEvent(l), cancellationToken);
}
