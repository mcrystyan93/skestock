using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Events.SupplyLists;

namespace skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;

public class CreateSupplyListCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateSupplyListCommand, Result<SupplyListDto>>
{
    public async ValueTask<Result<SupplyListDto>> Handle(CreateSupplyListCommand request, CancellationToken cancellationToken)
    {
        var supplyList = new SupplyList
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Frequency = request.Frequency,
            IntervalWeeks = request.IntervalWeeks
        };

        var linesResult = await SupplyListLineMapper.ApplyLinesAsync(dbContext, supplyList, request.Lines, cancellationToken);
        if (linesResult.IsFailed)
            return linesResult.ToResult<SupplyListDto>();

        supplyList.AddDomainEvent(new SupplyListCreatedEvent(supplyList));
        dbContext.SupplyLists.Add(supplyList);
        await SupplyListCommandSupport.SaveAsync(dbContext, supplyList, cancellationToken);

        return await SupplyListCommandSupport.LoadResultAsync(dbContext, supplyList.Id, cancellationToken);
    }
}
