namespace skestock.Application.Features.Items;

public static class ItemImagePaths
{
    // Fixed per image file so the Worker job never needs to write to the database.
    public static string Thumbnail(Guid imageFileId) => $"thumbnails/{imageFileId}.webp";
}
