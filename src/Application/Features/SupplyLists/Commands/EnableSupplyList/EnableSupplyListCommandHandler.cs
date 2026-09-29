using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.Commands.EnableSupplyList;

public class EnableSupplyListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<EnableSupplyListCommand, Result<SupplyListDto>>
{
    public async ValueTask<Result<SupplyListDto>> Handle(EnableSupplyListCommand request, CancellationToken cancellationToken) =>
        await SupplyListCommandSupport.SetActiveAsync(
            dbContext, request.Id, true, l => new SupplyListEnabledEvent(l), cancellationToken);
}
