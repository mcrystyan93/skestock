namespace skestock.Application.Features.SupplyLists;

public static class CacheConstants
{
    public const string SupplyList = "supply_list";
    public const string SupplyListListTag = "supply_lists";

    public static string SupplyListTag(Guid id) => $"supply_list:{id}";
}
