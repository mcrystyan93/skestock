using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;
using skestock.Application.Features.FruitDessertPlans.Models;

namespace skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertEmailSent;

public record SetFruitDessertEmailSentCommand
    : IRequest<Result<FruitDessertPlanEntryDto>>, ICacheInvalidation, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => ClassId;

    public Guid ClassId { get; init; }
    public Guid EntryId { get; init; }
    public bool EmailSent { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.PlanTag(ClassId)];
}
