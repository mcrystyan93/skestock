using skestock.Application.Features.FruitDessertPlans.Models;

namespace skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;

// Not ICacheableQuery: the handler creates the missing days, so it is not a pure read.
public record GetFruitDessertPlanQuery : IRequest<Result<IReadOnlyList<FruitDessertPlanEntryDto>>>
{
    public Guid ClassId { get; init; }
}
