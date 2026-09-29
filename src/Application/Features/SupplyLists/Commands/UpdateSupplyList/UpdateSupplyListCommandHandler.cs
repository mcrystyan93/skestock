using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.Commands.UpdateSupplyList;

public class UpdateSupplyListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateSupplyListCommand, Result<SupplyListDto>>
{
    public async ValueTask<Result<SupplyListDto>> Handle(UpdateSupplyListCommand request, CancellationToken cancellationToken)
    {
        var supplyList = await dbContext.SupplyLists
            .Include(l => l.Lines)
            .SingleOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (supplyList is null)
            return Result.Fail(new SupplyListErrors.SupplyListNotFound(request.Id));

        if (!supplyList.IsActive)
            return Result.Fail(new SupplyListErrors.SupplyListNotEditable(supplyList.Id));

        supplyList.Name = request.Name.Trim();
        supplyList.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        supplyList.Frequency = request.Frequency;
        supplyList.IntervalWeeks = request.IntervalWeeks;

        var linesResult = await SupplyListLineMapper.ApplyLinesAsync(dbContext, supplyList, request.Lines, cancellationToken);
        if (linesResult.IsFailed)
            return linesResult.ToResult<SupplyListDto>();

        supplyList.AddDomainEvent(new SupplyListUpdatedEvent(supplyList));
        await SupplyListCommandSupport.SaveAsync(dbContext, supplyList, cancellationToken);

        return await SupplyListCommandSupport.LoadResultAsync(dbContext, supplyList.Id, cancellationToken);
    }
}
