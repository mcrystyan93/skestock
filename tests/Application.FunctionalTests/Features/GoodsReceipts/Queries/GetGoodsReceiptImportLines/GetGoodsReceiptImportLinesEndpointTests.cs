using System.Net;
using System.Net.Http.Json;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Locations;
using skestock.Application.Features.Locations.Commands.UpdateLocation;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;

public class GetGoodsReceiptImportLinesEndpointTests : TestBase
{
    [Test]
    public async Task AnonymousRequest_IsRejected()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();
        (await client.GetAsync($"/api/GoodsReceipts/{Guid.NewGuid()}/import-lines"))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task MissingAndEmptyReceipts_HaveDistinctResponses()
    {
        using var client = await LoginAsync();
        (await client.GetAsync($"/api/GoodsReceipts/{Guid.NewGuid()}/import-lines"))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/GoodsReceipts/{Guid.Empty}/import-lines"))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var receipt = await AddReceiptAsync();
        var response = await client.GetAsync($"/api/GoodsReceipts/{receipt.Id}/import-lines");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<GoodsReceiptImportLineDto>>())!.ShouldBeEmpty();
    }

    [Test]
    public async Task ConfirmedSnapshots_AreProjectedFromSqlWithSavedValuesAndSplitRows()
    {
        using var client = await LoginAsync();
        var receipt = await AddReceiptAsync();
        var otherReceipt = await AddReceiptAsync();
        var import = await AddImportAsync(receipt.Id, receipt.ClassId);
        var otherImport = await AddImportAsync(otherReceipt.Id, otherReceipt.ClassId);
        var expiry = new DateOnly(2027, 1, 1);
        var first = new GoodsReceiptImportLine
        {
            ImportId = import.Id, RawItemText = "Saved invoice item", Quantity = 6,
            UnitPrice = 2.5m, ExpiryDate = expiry
        };
        await TestApp.AddAsync(first);
        var location = new Location { Name = "Original warehouse", Type = "StorageRoom" };
        await TestApp.AddAsync(location);
        await TestApp.AddAsync(new GoodsReceiptImportLine
        {
            ImportId = import.Id, RawItemText = "Saved invoice item", Quantity = 4,
            UnitPrice = 2.5m, LocationId = location.Id
        });
        await TestApp.AddAsync(new GoodsReceiptImportLine
        {
            ImportId = otherImport.Id, RawItemText = "Other receipt", Quantity = 99, UnitPrice = 99m
        });

        var rows = (await client.GetFromJsonAsync<List<GoodsReceiptImportLineDto>>(
            $"/api/GoodsReceipts/{receipt.Id}/import-lines"))!;
        rows.Count.ShouldBe(2);
        var saved = rows.Single(l => l.Id == first.Id);
        saved.ImportId.ShouldBe(import.Id);
        saved.RawItemText.ShouldBe("Saved invoice item");
        saved.Quantity.ShouldBe(6);
        saved.UnitPrice.ShouldBe(2.5m);
        saved.LineTotal.ShouldBe(15m);
        saved.ExpiryDate.ShouldBe(expiry);
        saved.LocationId.ShouldBeNull();
        saved.LocationName.ShouldBeNull();
        saved.MatchedItemId.ShouldBeNull();
        rows.Sum(l => l.Quantity).ShouldBe(10);
        rows.Single(l => l.LocationId == location.Id).LocationName.ShouldBe("Original warehouse");

        // The first HTTP read is cached; a normal location edit must invalidate its label.
        var update = await TestApp.SendAsync(new UpdateLocationCommand
        {
            Id = location.Id, Name = "Renamed warehouse", Type = "StorageRoom"
        });
        update.IsSuccess.ShouldBeTrue();
        var refreshed = (await client.GetFromJsonAsync<List<GoodsReceiptImportLineDto>>(
            $"/api/GoodsReceipts/{receipt.Id}/import-lines"))!;
        refreshed.Single(l => l.LocationId == location.Id).LocationName.ShouldBe("Renamed warehouse");
        refreshed.Select(l => (l.Id, l.RawItemText, l.Quantity, l.UnitPrice, l.ExpiryDate))
            .ShouldBe(rows.Select(l => (l.Id, l.RawItemText, l.Quantity, l.UnitPrice, l.ExpiryDate)));
    }

    private static async Task<HttpClient> LoginAsync()
    {
        var userId = (await TestApp.RunAsDefaultUserAsync())!.Value;
        await TestApp.AddAsync(new UserProfile { IdentityId = userId, FirstName = "Test", LastName = "User" });
        var client = FunctionalTestSetup.Factory.CreateClient();
        (await client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new { email = "test@local", password = "Testing1234!" })).StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    private static async Task<GoodsReceipt> AddReceiptAsync()
    {
        var schoolClass = new SchoolClass
        {
            Name = $"Snapshot {Guid.NewGuid():N}", StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 6, 30)
        };
        await TestApp.AddAsync(schoolClass);
        var receipt = new GoodsReceipt { ClassId = schoolClass.Id, Class = null!, Note = "Snapshot receipt" };
        await TestApp.AddAsync(receipt);
        return receipt;
    }

    private static async Task<GoodsReceiptImport> AddImportAsync(Guid receiptId, Guid classId)
    {
        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(), OriginalName = "receipt.pdf", BlobContainer = "app-files",
            BlobPath = $"imports/{Guid.NewGuid():N}.pdf", ContentType = "application/pdf",
            Status = FileStatus.Completed, SizeBytes = 100
        };
        await TestApp.AddAsync(file);
        var import = new GoodsReceiptImport
        {
            ClassId = classId, FileMetadataId = file.Id, BlobPath = file.BlobPath,
            UploadedByUserId = TestApp.GetUserId()!.Value, Status = GoodsReceiptImportStatus.Confirmed,
            ResultingGoodsReceiptId = receiptId
        };
        await TestApp.AddAsync(import);
        return import;
    }
}
