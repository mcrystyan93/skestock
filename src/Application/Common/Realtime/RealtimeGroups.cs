namespace skestock.Application.Common.Realtime;

public static class RealtimeGroups
{
    public const string CategoriesList = "categories-list";
    public const string GoodsReceiptImportsList = "goods-receipts-import-list";

    public static string User(string userId) => $"user:{userId}";
}
