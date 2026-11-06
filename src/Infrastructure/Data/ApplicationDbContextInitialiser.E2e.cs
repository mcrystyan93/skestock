using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using skestock.Domain.Common;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Imports;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Enums;
using skestock.Domain.Entities.Stock;

namespace skestock.Infrastructure.Data;

// Extra demo rows for the Playwright e2e stack. Runs only when E2E=true (and Development, see
// InitialiserExtensions), after SeedDemoClassAsync. Rows are inserted through the DbContext, not
// Mediator, so no domain events, outbox messages or class locks are involved; entities that raise
// events clear them before saving. Import rows are terminal (Confirmed/Failed) so the Worker never
// reprocesses them. Do not add goods receipts here: purchase statistics were already materialized.
public partial class ApplicationDbContextInitialiser
{
    private const string E2eMarker = "E2E ";
    private const string E2eBlobContainer = "e2e-seed";

    public async Task SeedE2eAsync()
    {
        if (!string.Equals(_configuration[Services.E2EMode], "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            await TrySeedE2eAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the e2e data.");
            throw;
        }
    }

    private async Task TrySeedE2eAsync()
    {
        var administrator = await FindAdministratorAsync()
                            ?? throw new InvalidOperationException("Seed administrator not found.");
        var profileId = administrator.Id;

        var classId = await _context.SchoolClasses
            .Where(c => c.Name == DemoClassName)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
        if (classId is null)
        {
            _logger.LogWarning("Demo class not found; skipping e2e seed.");
            return;
        }

        var demoItems = await _context.Items
            .Where(i => i.Sku != null && i.Sku.StartsWith("DEMO-"))
            .OrderBy(i => i.Sku)
            .ToListAsync();

        var congregations = await SeedE2eCongregationsAsync(profileId);
        await SeedE2eSharedClassConfigurationAsync();
        await SeedE2eStockVisibilityAsync(classId.Value, demoItems, profileId);
        await SeedE2eOrderListsAsync(classId.Value, demoItems, profileId);
        await SeedE2eSupplyListsAsync(demoItems, profileId);
        await SeedE2eFruitDessertAsync(classId.Value, congregations, profileId);
        await SeedE2eImportsAsync(classId.Value, demoItems, profileId);

        _logger.LogInformation("Seeded e2e demo data.");
    }

    // The audit interceptor leaves CreatedById/LastModifiedById null without an HTTP user, so stamp
    // them afterwards with ExecuteUpdate (see SeedCategoriesAsync).
    // Only the rows inserted by this seed are stamped, never other rows of the same table.
    private static Task StampE2eAsync<T>(IQueryable<T> rows, IEnumerable<T> inserted, Guid profileId)
        where T : BaseAuditableEntity
    {
        var ids = inserted.Select(e => e.Id).ToList();
        return rows.Where(e => ids.Contains(e.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.CreatedById, profileId)
                .SetProperty(e => e.LastModifiedById, profileId));
    }

    private async Task<List<Congregation>> SeedE2eCongregationsAsync(Guid profileId)
    {
        if (!await _context.Congregations.AnyAsync(c => c.Name.StartsWith(E2eMarker)))
        {
            string[] names = ["Nord", "Sud", "Est", "Vest"];
            var inserted = names.Select((name, n) => new Congregation
            {
                Name = $"{E2eMarker}Congregația {name}",
                Email = $"e2e-{name.ToLowerInvariant()}@example.test",
                Coordinator = $"Coordonator {name}",
                CoordinatorPhone = $"07000000{n + 1:00}",
                DessertContactPerson = $"Contact {name}",
                DessertContactPhone = $"07100000{n + 1:00}",
                CanReceiveStudents = n % 2 == 0,
                IsSmallCongregation = n == 3,
                Message = n == 0 ? "Mesaj demo pentru e2e." : null
            }).ToList();
            _context.Congregations.AddRange(inserted);
            await _context.SaveChangesAsync(CancellationToken.None);
            await StampE2eAsync(_context.Congregations, inserted, profileId);
        }

        return await _context.Congregations
            .Where(c => c.Name.StartsWith(E2eMarker))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    // MessageTemplates are seeded by the model (HasData), so nothing to add for them.
    private async Task SeedE2eSharedClassConfigurationAsync()
    {
        if (await _context.SharedClassConfigurations.AnyAsync())
        {
            return;
        }

        _context.SharedClassConfigurations.Add(new SharedClassConfiguration
        {
            InvitationCount = 120,
            Room2SeatCount = 6,
            Room4SeatCount = 10,
            Room6SeatCount = 4,
            DepartmentTemplates =
            [
                new DepartmentTemplate { Name = $"{E2eMarker}Bucătărie", Responsibilities = "Pregătirea meselor." },
                new DepartmentTemplate { Name = $"{E2eMarker}Curățenie", Responsibilities = "Curățenia spațiilor comune." }
            ]
        });
        await _context.SaveChangesAsync(CancellationToken.None);
    }

    // Hidden items are the last demo ones, so DEMO-01 always stays visible for the specs.
    private async Task SeedE2eStockVisibilityAsync(Guid classId, List<Item> demoItems, Guid profileId)
    {
        if (await _context.ClassItemStockVisibilities.AnyAsync(v => v.ClassId == classId))
        {
            return;
        }

        var hiddenItemIds = demoItems.TakeLast(3).Select(i => i.Id).ToList();
        var pairs = await _context.StockBatches
            .Where(b => b.ReceivedClassId == classId && hiddenItemIds.Contains(b.ItemId))
            .Select(b => new { b.ItemId, b.LocationId })
            .Distinct()
            .ToListAsync();

        var inserted = pairs.Select(p => new ClassItemStockVisibility
        {
            ClassId = classId, ItemId = p.ItemId, LocationId = p.LocationId, HideWhenZeroStock = true
        }).ToList();
        _context.ClassItemStockVisibilities.AddRange(inserted);
        await _context.SaveChangesAsync(CancellationToken.None);
        await StampE2eAsync(_context.ClassItemStockVisibilities, inserted, profileId);
    }

    private async Task SeedE2eOrderListsAsync(Guid classId, List<Item> demoItems, Guid profileId)
    {
        if (await _context.OrderLists.AnyAsync(o => o.Name != null && o.Name.StartsWith(E2eMarker)))
        {
            return;
        }

        OrderList NewList(string name, int firstItem)
        {
            var list = OrderList.Create(classId, $"{E2eMarker}{name}", "Listă demo pentru e2e.");
            foreach (var item in demoItems.Skip(firstItem).Take(3))
            {
                list.Lines.Add(new OrderListLine
                {
                    ItemId = item.Id, ProductName = item.Name, Quantity = 5 + firstItem, Unit = item.Unit
                });
            }

            list.Lines.Add(new OrderListLine { ProductName = "Produs în afara catalogului", Quantity = 2, Unit = "buc" });
            return list;
        }

        var draft = NewList("Comandă ciornă", 0);
        var submitted = NewList("Comandă trimisă", 3);
        submitted.Submit();
        var cancelled = NewList("Comandă anulată", 6);
        cancelled.Cancel();

        var lists = new[] { draft, submitted, cancelled };
        foreach (var list in lists)
        {
            list.ClearDomainEvents();
            _context.OrderLists.Add(list);
        }

        await _context.SaveChangesAsync(CancellationToken.None);
        await StampE2eAsync(_context.OrderLists, lists, profileId);
        await StampE2eAsync(_context.OrderListLines, lists.SelectMany(l => l.Lines), profileId);
    }

    private async Task SeedE2eSupplyListsAsync(List<Item> demoItems, Guid profileId)
    {
        if (await _context.SupplyLists.AnyAsync(l => l.Name.StartsWith(E2eMarker)))
        {
            return;
        }

        SupplyList NewList(string name, SupplyListFrequency frequency, int? intervalWeeks, int firstItem)
        {
            var list = new SupplyList
            {
                Name = $"{E2eMarker}{name}", Note = "Listă de aprovizionare demo.", Frequency = frequency,
                IntervalWeeks = intervalWeeks
            };
            foreach (var item in demoItems.Skip(firstItem).Take(4))
            {
                list.Lines.Add(new SupplyListLine { ItemId = item.Id, Quantity = 10, Unit = item.Unit });
            }

            return list;
        }

        SupplyList[] lists =
        [
            NewList("Aprovizionare săptămânală", SupplyListFrequency.Weekly, null, 0),
            NewList("Aprovizionare la două săptămâni", SupplyListFrequency.EveryXWeeks, 2, 4)
        ];
        _context.SupplyLists.AddRange(lists);
        await _context.SaveChangesAsync(CancellationToken.None);
        await StampE2eAsync(_context.SupplyLists, lists, profileId);
        await StampE2eAsync(_context.SupplyListLines, lists.SelectMany(l => l.Lines), profileId);
    }

    private async Task SeedE2eFruitDessertAsync(Guid classId, List<Congregation> congregations, Guid profileId)
    {
        if (congregations.Count == 0 || await _context.FruitDessertPlanEntries.AnyAsync(e => e.SchoolClassId == classId))
        {
            return;
        }

        var reminder = await _context.MessageTemplates
            .FirstOrDefaultAsync(t => t.Type == FruitDessertMessageType.Reminder && t.IsActive);
        var thanks = await _context.MessageTemplates
            .FirstOrDefaultAsync(t => t.Type == FruitDessertMessageType.Thanks && t.IsActive);
        if (reminder is null || thanks is null)
        {
            return;
        }

        var sentAt = DateTimeOffset.UtcNow;
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = new List<FruitDessertPlanEntry>();
        while (entries.Count < 5)
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                var congregation = entries.Count < 4 ? congregations[entries.Count % congregations.Count] : null;
                entries.Add(new FruitDessertPlanEntry
                {
                    SchoolClassId = classId, Date = day, CongregationId = congregation?.Id, EmailSent = entries.Count == 0
                });
            }

            day = day.AddDays(1);
        }

        FruitDessertMessage NewMessage(FruitDessertPlanEntry entry, Congregation congregation, MessageTemplate template,
            FruitDessertMessageType type, FruitDessertMessageStatus status) => new()
        {
            Type = type,
            Status = status,
            SentAt = sentAt,
            RecipientPhone = congregation.DessertContactPhone!,
            Body = template.Body,
            TemplateId = template.Id,
            Entry = entry
        };

        entries[0].Messages.Add(NewMessage(entries[0], congregations[0], reminder, FruitDessertMessageType.Reminder,
            FruitDessertMessageStatus.Sent));
        entries[0].Messages.Add(NewMessage(entries[0], congregations[0], thanks, FruitDessertMessageType.Thanks,
            FruitDessertMessageStatus.Opened));
        entries[1].Messages.Add(NewMessage(entries[1], congregations[1 % congregations.Count], reminder,
            FruitDessertMessageType.Reminder, FruitDessertMessageStatus.Opened));

        _context.FruitDessertPlanEntries.AddRange(entries);
        await _context.SaveChangesAsync(CancellationToken.None);
        await StampE2eAsync(_context.FruitDessertPlanEntries, entries, profileId);
        await StampE2eAsync(_context.FruitDessertMessages, entries.SelectMany(e => e.Messages), profileId);
    }

    private async Task SeedE2eImportsAsync(Guid classId, List<Item> demoItems, Guid profileId)
    {
        if (await _context.FileMetadata.AnyAsync(f => f.BlobContainer == E2eBlobContainer))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var files = new List<FileMetadata>();

        FileMetadata NewFile(string name, string contentType)
        {
            var file = new FileMetadata
            {
                Id = Guid.CreateVersion7(), // batches reference it before the save
                FileId = Guid.NewGuid(),
                OriginalName = name,
                BlobContainer = E2eBlobContainer,
                BlobPath = $"e2e/{name}",
                ContentType = contentType,
                SizeBytes = 2048,
                Status = FileStatus.Completed,
                CompletedDate = now
            };
            files.Add(file);
            return file;
        }

        var categoryConfirmed = CategoryImportBatch.Create(profileId, null, [NewFile("categorii-confirmat.pdf", "application/pdf").Id]);
        categoryConfirmed.RecordAttempt(now.AddMinutes(5));
        categoryConfirmed.MarkAsConfirmed("{}");
        categoryConfirmed.History.Add(ImportBatchHistory.Confirmed());

        var categoryFailed = CategoryImportBatch.Create(profileId, null, [NewFile("categorii-eșuat.pdf", "application/pdf").Id]);
        categoryFailed.RecordAttempt(now.AddMinutes(5));
        categoryFailed.MarkAsFailed("Fișier demo ilizibil.");
        categoryFailed.History.Add(ImportBatchHistory.Failed(1, "Fișier demo ilizibil."));

        var itemConfirmed = ItemImportBatch.Create(profileId, null, [NewFile("articole-confirmat.pdf", "application/pdf").Id]);
        itemConfirmed.RecordAttempt(now.AddMinutes(5));
        itemConfirmed.MarkAsConfirmed("{}");
        itemConfirmed.History.Add(ImportBatchHistory.Confirmed());

        var itemFailed = ItemImportBatch.Create(profileId, null, [NewFile("articole-eșuat.pdf", "application/pdf").Id]);
        itemFailed.RecordAttempt(now.AddMinutes(5));
        itemFailed.MarkAsFailed("Fișier demo ilizibil.");
        itemFailed.History.Add(ImportBatchHistory.Failed(1, "Fișier demo ilizibil."));

        // The first demo receipt (PO-0001) is a fixed target for the confirmed import.
        var receiptId = await _context.GoodsReceipts
            .Where(r => r.ClassId == classId && r.SupplierReference == "PO-0001")
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync();

        var receiptFile = NewFile("recepție-confirmată.pdf", "application/pdf");
        var receiptConfirmed = GoodsReceiptImport.Create(classId, receiptFile.Id, profileId, receiptFile.BlobPath);
        if (receiptId is { } resultingReceiptId)
        {
            receiptConfirmed.MarkAsConfirmed(resultingReceiptId);

            var locationId = await _context.Locations.Where(l => l.IsDefault).Select(l => (Guid?)l.Id).FirstOrDefaultAsync();
            foreach (var item in demoItems.Take(3))
            {
                receiptConfirmed.Lines.Add(new GoodsReceiptImportLine
                {
                    RawItemText = item.Name, MatchedItemId = item.Id, LocationId = locationId, Quantity = 10, UnitPrice = 4.5m
                });
            }
        }
        else
        {
            // Failed rows carry no lines, so this stays consistent when the demo receipts are missing.
            receiptConfirmed.MarkAsFailed("Fișier demo ilizibil.");
        }

        var failedFile = NewFile("recepție-eșuată.pdf", "application/pdf");
        var receiptFailed = GoodsReceiptImport.Create(classId, failedFile.Id, profileId, failedFile.BlobPath);
        receiptFailed.MarkAsFailed("Fișier demo ilizibil.");

        _context.FileMetadata.AddRange(files);
        _context.CategoryImportBatches.AddRange(categoryConfirmed, categoryFailed);
        _context.ItemImportBatches.AddRange(itemConfirmed, itemFailed);
        _context.GoodsReceiptImports.AddRange(receiptConfirmed, receiptFailed);
        foreach (var entity in new BaseEntity[]
                 {
                     categoryConfirmed, categoryFailed, itemConfirmed, itemFailed, receiptConfirmed, receiptFailed
                 })
        {
            entity.ClearDomainEvents();
        }

        await _context.SaveChangesAsync(CancellationToken.None);

        var categoryBatches = new[] { categoryConfirmed, categoryFailed };
        var itemBatches = new[] { itemConfirmed, itemFailed };
        var receiptImports = new[] { receiptConfirmed, receiptFailed };
        await StampE2eAsync(_context.FileMetadata, files, profileId);
        await StampE2eAsync(_context.CategoryImportBatches, categoryBatches, profileId);
        await StampE2eAsync(_context.CategoryImportBatchFiles, categoryBatches.SelectMany(b => b.Files), profileId);
        await StampE2eAsync(_context.ItemImportBatches, itemBatches, profileId);
        await StampE2eAsync(_context.ItemImportBatchFiles, itemBatches.SelectMany(b => b.Files), profileId);
        await StampE2eAsync(_context.GoodsReceiptImports, receiptImports, profileId);
        await StampE2eAsync(_context.GoodsReceiptImportLines, receiptImports.SelectMany(r => r.Lines), profileId);
    }
}
