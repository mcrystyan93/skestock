using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;

namespace skestock.Application.Features.SupplyLists.Queries.GetSupplyListById;

public class GetSupplyListByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetSupplyListByIdQuery, Result<SupplyListDto>>
{
    public async ValueTask<Result<SupplyListDto>> Handle(GetSupplyListByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await SupplyListProjection.LoadAsync(dbContext, request.Id, cancellationToken);
        if (dto is null)
            return Result.Fail(new SupplyListErrors.SupplyListNotFound(request.Id));

        return Result.Ok(dto);
    }
}
