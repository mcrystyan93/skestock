using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.Commands.EnableItem;

public class EnableItemCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<EnableItemCommand, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(EnableItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .SingleOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (item is null)
            return Result.Fail(new ItemErrors.ItemNotFound(request.Id));

        // Repeated requests succeed; the existing event contract still emits on each request.
        item.IsActive = true;
        item.AddDomainEvent(new ItemEnabledEvent(item));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await ItemDtoMapper.FromTrackedItemAsync(dbContext, item, cancellationToken));
    }
}
