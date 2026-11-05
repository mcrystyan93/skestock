using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Features.FruitDessertPlans.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;

public record SendFruitDessertMessageCommand
    : IRequest<Result<FruitDessertMessageDispatchDto>>, ICacheInvalidation, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => ClassId;

    public Guid ClassId { get; init; }
    public Guid EntryId { get; init; }
    public FruitDessertMessageType Type { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.PlanTag(ClassId)];
}
