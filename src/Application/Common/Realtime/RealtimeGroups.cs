namespace skestock.Application.Common.Realtime;

public static class RealtimeGroups
{
    public const string CategoriesList = "categories-list";
    public const string CategoryImportBatchesList = "category-import-batches-list";
    public const string GoodsReceiptImportsList = "goods-receipts-import-list";
    public const string ItemsList = "items-list";
    public const string ItemImportBatchesList = "item-import-batches-list";

    public static string User(string userId) => $"user:{userId}";
    public static string SchoolClass(Guid classId) => $"school-class:{classId}";
}
