using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.Commands.EditItem;

public class EditItemCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<EditItemCommand, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(EditItemCommand request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .SingleOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (item is null)
            return Result.Fail(new ItemErrors.ItemNotFound(request.Id));

        item.Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        item.Name = request.Name.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        item.Unit = request.Unit.Trim();
        item.MinThreshold = request.MinThreshold;
        item.IsPerishable = request.IsPerishable;
        item.ShelfLifeDays = request.ShelfLifeDays;
        item.CategoryId = request.CategoryId;
        item.AddDomainEvent(new ItemUpdatedEvent(item));

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await ItemDtoMapper.FromTrackedItemAsync(dbContext, item, cancellationToken));
    }
}
