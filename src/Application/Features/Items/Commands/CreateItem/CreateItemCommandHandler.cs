using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;
using skestock.Domain.Events.Items;

namespace skestock.Application.Features.Items.Commands.CreateItem;

public class CreateItemCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateItemCommand, Result<ItemDto>>
{
    public async ValueTask<Result<ItemDto>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Unit = request.Unit.Trim(),
            MinThreshold = request.MinThreshold,
            IsPerishable = request.IsPerishable,
            ShelfLifeDays = request.ShelfLifeDays,
            CategoryId = request.CategoryId,
            IsActive = true
        };

        dbContext.Items.Add(item);
        item.AddDomainEvent(new ItemCreatedEvent(item));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await ItemDtoMapper.FromTrackedItemAsync(dbContext, item, cancellationToken));
    }
}
