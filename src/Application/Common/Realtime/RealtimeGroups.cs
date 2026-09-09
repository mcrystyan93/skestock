namespace skestock.Application.Common.Realtime;

public static class RealtimeGroups
{
    public const string CategoriesList = "categories-list";
    public const string CategoryImportsList = "category-imports-list";
    public const string GoodsReceiptImportsList = "goods-receipts-import-list";
    public const string ItemsList = "items-list";
    public const string ItemImportsList = "item-imports-list";

    public static string User(string userId) => $"user:{userId}";
}
