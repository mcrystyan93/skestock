namespace skestock.Application.Features.FruitDessertPlans;

public static class CacheConstants
{
    // Per class: plan commands only affect their own class. The GET is uncached today, so this tag is
    // inert until a cached read exists.
    public static string PlanTag(Guid classId) => $"fruit-dessert-plan:{classId}";
}
