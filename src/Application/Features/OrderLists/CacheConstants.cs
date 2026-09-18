namespace skestock.Application.Features.OrderLists;

public static class CacheConstants
{
    public const string OrderList = "order_list";
    public const string OrderListListTag = "order_lists";

    public static string OrderListTag(Guid id) => $"order_list:{id}";
}
