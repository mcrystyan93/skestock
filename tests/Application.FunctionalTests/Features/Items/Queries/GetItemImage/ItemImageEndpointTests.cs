using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using skestock.Application.Features.Items;
using skestock.Application.Features.Items.Models;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Interfaces;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Items.Queries.GetItemImage;

public class ItemImageEndpointTests : TestBase
{
    private const string Container = "app-files";

    private static FakeBlobStorageService Blobs =>
        (FakeBlobStorageService)FunctionalTestSetup.Factory.Services.GetRequiredService<IBlobStorageService>();

    private static byte[] Png()
    {
        using var bitmap = new SKBitmap(8, 8);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    private static async Task<HttpClient> LoginAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        // Audited rows (CreatedById) have an FK to UserProfile.IdentityId.
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
        var client = FunctionalTestSetup.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new { email = "test@local", password = "Testing1234!" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    private static async Task<Guid> SeedFileAsync(byte[] bytes)
    {
        var fileId = Guid.NewGuid();
        var path = $"2026/10/{fileId}.png";
        await Blobs.UploadAsync(new UploadDto(Container, path, new MemoryStream(bytes), "image/png"), CancellationToken.None);
        await TestApp.AddAsync(new FileMetadata
        {
            FileId = fileId, OriginalName = "i.png", BlobContainer = Container, BlobPath = path,
            ContentType = "image/png", SizeBytes = bytes.Length, Status = FileStatus.Completed
        });
        return fileId;
    }

    private static async Task<Item> SeedItemAsync()
    {
        var category = new Category { Name = $"FT{Guid.NewGuid():N}"[..10] };
        await TestApp.AddAsync(category);
        var item = new Item { Name = $"FT{Guid.NewGuid():N}"[..10], Unit = "unit", CategoryId = category.Id };
        await TestApp.AddAsync(item);
        return item;
    }

    private static Task<HttpResponseMessage> EditAsync(HttpClient client, Item item, Guid? fileId) =>
        client.PutAsJsonAsync($"/api/Items/{item.Id}", new
        {
            name = item.Name, unit = item.Unit, categoryId = item.CategoryId, imageFileId = fileId
        });

    private static async Task<HttpResponseMessage> EditOkAsync(HttpClient client, Item item, Guid? fileId)
    {
        var response = await EditAsync(client, item, fileId);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return response;
    }

    [Test]
    public async Task Edit_WithImage_ReturnsImageFileId_AndFullImageIsServedImmutableWithNosniff()
    {
        using var client = await LoginAsync();
        var item = await SeedItemAsync();
        var png = Png();
        var fileId = await SeedFileAsync(png);

        var edit = await EditOkAsync(client, item, fileId);
        (await edit.Content.ReadFromJsonAsync<ItemDto>())!.ImageFileId.ShouldBe(fileId);

        var response = await client.GetAsync($"/api/Items/{item.Id}/image?size=full");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(png);
        response.Headers.CacheControl!.ToString().ShouldContain("immutable");
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
    }

    [Test]
    public async Task Thumb_WithoutGeneratedThumbnail_FallsBackToOriginalWithNoCache()
    {
        using var client = await LoginAsync();
        var item = await SeedItemAsync();
        var png = Png();
        var fileId = await SeedFileAsync(png);
        await EditOkAsync(client, item, fileId);

        var response = await client.GetAsync($"/api/Items/{item.Id}/image?size=thumb");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(png);
        response.Headers.CacheControl!.NoCache.ShouldBeTrue();
        response.Headers.CacheControl.ToString().ShouldNotContain("immutable");
    }

    [Test]
    public async Task Thumb_WhenThumbnailExists_ServesWebpImmutable()
    {
        using var client = await LoginAsync();
        var item = await SeedItemAsync();
        var fileId = await SeedFileAsync(Png());
        await EditOkAsync(client, item, fileId);
        await Blobs.UploadAsync(
            new UploadDto(Container, ItemImagePaths.Thumbnail(fileId), new MemoryStream([1, 2, 3]), "image/webp"),
            CancellationToken.None);

        var response = await client.GetAsync($"/api/Items/{item.Id}/image?size=Thumb");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("image/webp");
        response.Headers.CacheControl!.ToString().ShouldContain("immutable");
    }

    [Test]
    public async Task Edit_WithUndecodableImage_Returns400InvalidImage()
    {
        using var client = await LoginAsync();
        var item = await SeedItemAsync();
        var fileId = await SeedFileAsync("this is not an image"u8.ToArray());

        var response = await EditAsync(client, item, fileId);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("validation.invalid_image");
    }

    [Test]
    public async Task Image_WithUnknownSize_Returns400()
    {
        using var client = await LoginAsync();
        var item = await SeedItemAsync();

        (await client.GetAsync($"/api/Items/{item.Id}/image?size=huge")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Image_ForItemWithoutImage_Returns404()
    {
        using var client = await LoginAsync();
        var item = await SeedItemAsync();

        var response = await client.GetAsync($"/api/Items/{item.Id}/image");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
