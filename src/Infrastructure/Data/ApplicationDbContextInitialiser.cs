using skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;
using skestock.Domain.Constants;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Infrastructure.Identity;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace skestock.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();

        if (app.Environment.IsDevelopment())
        {
            await initialiser.SeedDemoClassAsync();
        }
    }
}

public class ApplicationDbContextInitialiser
{
    private static readonly AdministratorSeed[] DefaultAdministrators =
    [
        new("administrator@localhost", "Administrator1!", "Administrator", "Administrator"),
        new("cosmin@local", "Cosmin9!", "Cosmin", "Gherendi")
    ];

    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ISender _sender;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager, ISender sender)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _sender = sender;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.GetMigrations().Any())
            {
                await _context.Database.MigrateAsync();
            }
            else
            {
                // This repository has no migrations;
                // await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        if (DefaultAdministrators.Length == 0)
        {
            throw new InvalidOperationException(
                "At least one administrator must be listed in DefaultAdministrators.");
        }

        if (!await _roleManager.RoleExistsAsync(Roles.Administrator))
        {
            var roleResult = await _roleManager.CreateAsync(
                new IdentityRole<Guid>(Roles.Administrator) { Id = Guid.CreateVersion7() });
            EnsureIdentityResultSucceeded(roleResult, $"create role '{Roles.Administrator}'");
        }

        var seedAdministrator = await EnsureAdministratorAsync(DefaultAdministrators[0]);
        foreach (var administrator in DefaultAdministrators.Skip(1))
        {
            await EnsureAdministratorAsync(administrator);
        }

        await SeedCategoriesAsync(seedAdministrator.Id);
        await SeedLocationsAsync(seedAdministrator.Id);
        // await SeedSchoolClassesAsync(seedAdministrator.Id);
        // await SeedItemsAsync(seedAdministrator.Id);
        // await SeedGoodsReceiptAsync(seedAdministrator.Id);
    }

    private async Task<ApplicationUser> EnsureAdministratorAsync(AdministratorSeed seed)
    {
        var administrator = await _userManager.FindByNameAsync(seed.UserName);
        if (administrator is null)
        {
            var newAdministrator = new ApplicationUser { UserName = seed.UserName, Email = seed.UserName };

            var createResult = await _userManager.CreateAsync(newAdministrator, seed.Password);
            EnsureIdentityResultSucceeded(createResult, $"create administrator '{seed.UserName}'");

            administrator = await _userManager.FindByNameAsync(seed.UserName)
                            ?? throw new InvalidOperationException(
                                $"Administrator '{seed.UserName}' could not be found after creation.");
        }

        if (!await _userManager.IsInRoleAsync(administrator, Roles.Administrator))
        {
            var roleResult = await _userManager.AddToRoleAsync(administrator, Roles.Administrator);
            EnsureIdentityResultSucceeded(
                roleResult, $"assign role '{Roles.Administrator}' to '{seed.UserName}'");
        }

        var administratorProfile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.IdentityId == administrator.Id);

        if (administratorProfile is null)
        {
            administratorProfile = new UserProfile
            {
                IdentityId = administrator.Id, FirstName = seed.FirstName, LastName = seed.LastName
            };
            _context.Set<UserProfile>().Add(administratorProfile);
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        return administrator;
    }

    private static void EnsureIdentityResultSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Failed to {operation}. Identity errors: {errors}");
    }

    private sealed record AdministratorSeed(
        string UserName,
        string Password,
        string FirstName,
        string LastName);

    private static readonly (string Name, string Type, bool IsDefault)[] DefaultLocations =
    [
        ("Frigider", "Bucatarie", false),
        ("Congelator", "Bucatarie", false),
        ("Dulapuri", "Sala de mese", false),
        ("Rafturi", "Bucatarie", false),
        ("Camara", "Bucatarie", true),
        ("Depozit", "Depozit", false)
    ];

    private static readonly string[] DefaultCategoryNames =
    [
        // Lactate și derivate
        "Lapte",
        "Lapte fără lactoză",
        "Iaurt",
        "Brânză (cașcaval, telemea, brânză proaspătă)",
        "Unt",
        "Smântână/frișcă",
        "Ouă",
        "Produse lactate fermentate (chefir, sana)",

        // Carne și pește
        "Carne de porc",
        "Carne de vită",
        "Carne de pui",
        "Carne de curcan",
        "Carne de miel",
        "Mezeluri (șuncă, salam, cârnați)",
        "Pește proaspăt",
        "Fructe de mare",
        "Pește afumat/sărat",
        "Carne tocată",

        // Congelate
        "Legume congelate",
        "Fructe congelate",
        "Pește/fructe de mare congelate",
        "Pizza congelată",
        "Înghețată",
        "Semipreparate congelate (chiftele, șnițele)",
        "Aluaturi congelate",
        "Cartofi congelați (pommes frites)",
        "Deserturi congelate",

        // Fructe și legume proaspete
        "Legume cu frunze (salată, spanac)",
        "Legume rădăcinoase (morcov, sfeclă)",
        "Roșii, ardei, castraveți",
        "Ceapă, usturoi, praz",
        "Cartofi",
        "Fructe autohtone (mere, pere)",
        "Fructe exotice (banane, kiwi)",
        "Citrice",
        "Ciuperci",
        "Verdețuri și ierburi aromatice",

        // Panificație și cofetărie
        "Pâine albă",
        "Pâine integrală/specială",
        "Produse de patiserie",
        "Cereale de mic dejun",
        "Biscuiți",
        "Prăjituri/torturi",
        "Covrigi/lipii",
        "Batoane de cereale",

        // Băuturi
        "Apă plată",
        "Apă minerală",
        "Sucuri naturale",
        "Băuturi carbogazoase",
        "Cafea boabe/măcinată",
        "Cafea instant",
        "Ceai",

        // Băcănie/produse de bază
        "Paste făinoase",
        "Orez",
        "Ulei de gătit",
        "Oțet",
        "Zahăr",
        "Îndulcitori",
        "Făină",
        "Conserve legume",
        "Conserve pește",
        "Conserve carne",
        "Condimente și mirodenii",
        "Sosuri (ketchup, maioneză, muștar)",
        "Muraturi",
        "Miere și gemuri",
        "Cereale/leguminoase uscate (linte, fasole)",

        // Snacksuri și dulciuri
        "Chipsuri",
        "Nuci și semințe",
        "Ciocolată",

        // Îngrijire personală
        "Șampon",
        "Balsam de păr",
        "Gel de duș",
        "Săpun solid",
        "Săpun lichid",
        "Pastă de dinți",
        "Periuțe de dinți",
        "Deodorant",
        "Produse cosmetice de bază (creme, loțiuni)",

        // Curățenie casă
        "Detergent de rufe",
        "Balsam de rufe",
        "Detergent de vase",
        "Produse de curățat universale",
        "Produse de curățat geamuri",
        "Produse de curățat baie/WC",
        "Pungi de gunoi",
        "Șervețele de bucătărie/hârtie igienică",

        // Diverse
        "Produse pentru animale de companie",
        "Baterii/consumabile pentru casă"
    ];

    private async Task SeedCategoriesAsync(Guid administratorIdentityId)
    {
        var existingNames = await _context.Categories
            .Select(c => c.Name)
            .ToListAsync();

        var namesToSeed = DefaultCategoryNames
            .Where(name => !existingNames.Contains(name))
            .ToList();

        if (namesToSeed.Count == 0)
        {
            return;
        }

        var newCategories = namesToSeed
            .Select(name => new Category { Name = name })
            .ToList();

        _context.Categories.AddRange(newCategories);
        await _context.SaveChangesAsync(CancellationToken.None);

        // The AuditableEntityInterceptor stamps CreatedById/LastModifiedById from the current
        // HTTP user, which is unavailable during seeding (both are left null). Use ExecuteUpdate
        // to set them to the administrator's profile without re-triggering that interceptor.
        var newCategoryIds = newCategories.Select(c => c.Id).ToList();
        await _context.Categories
            .Where(c => newCategoryIds.Contains(c.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.CreatedById, administratorIdentityId)
                .SetProperty(c => c.LastModifiedById, administratorIdentityId));
    }

    private async Task SeedLocationsAsync(Guid administratorIdentityId)
    {
        var existingNames = await _context.Locations
            .Select(l => l.Name)
            .ToListAsync();

        var locationsToSeed = DefaultLocations
            .Where(l => !existingNames.Contains(l.Name))
            .ToList();

        if (locationsToSeed.Count == 0)
        {
            return;
        }

        var newLocations = locationsToSeed
            .Select(l => new Location { Name = l.Name, Type = l.Type, IsDefault = l.IsDefault })
            .ToList();

        _context.Locations.AddRange(newLocations);
        await _context.SaveChangesAsync(CancellationToken.None);

        // See SeedCategoriesAsync: bypass the AuditableEntityInterceptor (which would otherwise
        // null out CreatedById/LastModifiedById since there's no HTTP user during seeding).
        var newLocationIds = newLocations.Select(l => l.Id).ToList();
        await _context.Locations
            .Where(l => newLocationIds.Contains(l.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.CreatedById, administratorIdentityId)
                .SetProperty(l => l.LastModifiedById, administratorIdentityId));
    }

    private static readonly (string Name, DateOnly StartDate, DateOnly EndDate, ClassStatus Status)[]
        DefaultSchoolClasses =
        [
            ("SKE 29", new DateOnly(2026, 7, 13), new DateOnly(2026, 9, 4), ClassStatus.Active)
        ];

    private async Task SeedSchoolClassesAsync(Guid administratorIdentityId)
    {
        var existingNames = await _context.SchoolClasses
            .Select(c => c.Name)
            .ToListAsync();

        var classesToSeed = DefaultSchoolClasses
            .Where(c => !existingNames.Contains(c.Name))
            .ToList();

        if (classesToSeed.Count == 0)
        {
            return;
        }

        var newSchoolClasses = classesToSeed
            .Select(c => new SchoolClass
            {
                Name = c.Name, StartDate = c.StartDate, EndDate = c.EndDate, Status = c.Status
            })
            .ToList();

        _context.SchoolClasses.AddRange(newSchoolClasses);
        await _context.SaveChangesAsync(CancellationToken.None);

        // See SeedCategoriesAsync: bypass the AuditableEntityInterceptor (which would otherwise
        // null out CreatedById/LastModifiedById since there's no HTTP user during seeding).
        var newSchoolClassIds = newSchoolClasses.Select(c => c.Id).ToList();
        await _context.SchoolClasses
            .Where(c => newSchoolClassIds.Contains(c.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.CreatedById, administratorIdentityId)
                .SetProperty(c => c.LastModifiedById, administratorIdentityId));
    }

    private const string DemoClassName = "SKE Demo";

    public async Task SeedDemoClassAsync()
    {
        try
        {
            await TrySeedDemoClassAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the demo class.");
            throw;
        }
    }

    private async Task TrySeedDemoClassAsync()
    {
        var existingClassId = await _context.SchoolClasses
            .Where(c => c.Name == DemoClassName)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
        if (existingClassId is { } classIdToBackfill)
        {
            await SeedDemoAnalyticsAsync(classIdToBackfill);
            await SeedDemoGoodsReceiptsAsync(classIdToBackfill);
            return;
        }

        var administrator = await _userManager.FindByNameAsync(DefaultAdministrators[0].UserName)
                            ?? throw new InvalidOperationException("Seed administrator not found.");
        // UserProfile FKs (UserId, CreatedById, ...) reference UserProfiles.IdentityId.
        var profileId = administrator.Id;

        var categoryIds = await _context.Categories.Select(c => c.Id).ToListAsync();
        var locationIds = await _context.Locations.Select(l => l.Id).ToListAsync();
        if (categoryIds.Count == 0 || locationIds.Count == 0)
        {
            return;
        }

        var random = new Random(20260926);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddMonths(-6);

        var schoolClass = new SchoolClass
        {
            Name = DemoClassName, StartDate = startDate, EndDate = today.AddMonths(1), Status = ClassStatus.Active
        };
        _context.SchoolClasses.Add(schoolClass);

        string[] units = ["buc", "kg", "l", "cutie"];
        var items = new List<(Item Item, Guid LocationId, decimal BasePrice)>();
        for (var n = 1; n <= 30; n++)
        {
            var isPerishable = random.Next(2) == 0;
            var item = new Item
            {
                Sku = $"DEMO-{n:00}",
                Name = $"Demo item {n:00}",
                Unit = units[random.Next(units.Length)],
                MinThreshold = random.Next(5, 31),
                IsPerishable = isPerishable,
                ShelfLifeDays = isPerishable ? random.Next(7, 91) : null,
                CategoryId = categoryIds[random.Next(categoryIds.Count)]
            };
            items.Add((item, locationIds[random.Next(locationIds.Count)],
                Math.Round((decimal)(random.NextDouble() * 148 + 2), 2)));
        }

        _context.Items.AddRange(items.Select(i => i.Item));

        var batches = new List<StockBatch>();
        var transactions = new List<StockTransaction>();

        foreach (var (item, locationId, basePrice) in items)
        {
            var open = new List<StockBatch>();

            void Order(DateOnly day)
            {
                var quantity = random.Next(20, 201);
                var price = Math.Round(basePrice * (decimal)(0.85 + random.NextDouble() * 0.3), 2);
                var batch = new StockBatch
                {
                    Item = item,
                    LocationId = locationId,
                    ReceivedClass = schoolClass,
                    Quantity = quantity,
                    UnitPrice = price,
                    ReceivedDate = day,
                    ExpiryDate = item.ShelfLifeDays is { } shelfLife ? day.AddDays(shelfLife) : null
                };
                batches.Add(batch);
                open.Add(batch);
                transactions.Add(NewTransaction(item, locationId, batch, StockTransactionType.Order, quantity, "restock", day));
            }

            StockTransaction NewTransaction(Item txItem, Guid txLocationId, StockBatch batch,
                StockTransactionType type, int change, string reason, DateOnly day) => new()
            {
                Item = txItem,
                LocationId = txLocationId,
                Batch = batch,
                Class = schoolClass,
                UserId = profileId,
                Type = type,
                QuantityChange = change,
                Reason = reason,
                CreatedAt = new DateTimeOffset(day.ToDateTime(new TimeOnly(random.Next(6, 18), random.Next(60))),
                    TimeSpan.Zero)
            };

            Order(startDate);

            for (var day = startDate.AddDays(1); day <= today; day = day.AddDays(1))
            {
                if (random.NextDouble() < 0.6)
                {
                    var toUse = random.Next(1, 11);
                    foreach (var batch in open.Where(b => b.Quantity > 0).ToList())
                    {
                        if (toUse == 0)
                        {
                            break;
                        }

                        var taken = Math.Min(toUse, batch.Quantity);
                        batch.Quantity -= taken;
                        toUse -= taken;
                        transactions.Add(NewTransaction(item, locationId, batch, StockTransactionType.Usage, -taken,
                            "usage", day));
                    }

                    open.RemoveAll(b => b.Quantity == 0);
                }

                if (open.Sum(b => b.Quantity) < item.MinThreshold || random.NextDouble() < 0.02)
                {
                    Order(day);
                }
            }
        }

        _context.StockBatches.AddRange(batches);
        _context.StockTransactions.AddRange(transactions);
        await _context.SaveChangesAsync(CancellationToken.None);

        // See SeedCategoriesAsync: stamp audit fields without the HTTP-user interceptor.
        var classId = schoolClass.Id;
        await _context.SchoolClasses.Where(c => c.Id == classId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.CreatedById, profileId)
                .SetProperty(c => c.LastModifiedById, profileId));
        var itemIds = items.Select(i => i.Item.Id).ToList();
        await _context.Items.Where(i => itemIds.Contains(i.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.CreatedById, profileId)
                .SetProperty(i => i.LastModifiedById, profileId));
        await _context.StockBatches.Where(b => b.ReceivedClassId == classId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.CreatedById, profileId)
                .SetProperty(b => b.LastModifiedById, profileId));
        await _context.StockTransactions.Where(t => t.ClassId == classId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedById, profileId)
                .SetProperty(t => t.LastModifiedById, profileId));

        await SeedDemoAnalyticsAsync(classId);
        await SeedDemoGoodsReceiptsAsync(classId);

        _logger.LogInformation("Seeded demo class with {Batches} batches and {Transactions} transactions.",
            batches.Count, transactions.Count);
    }

    // Materializes ClassBalance and DailyItemConsumption rows for the demo class so the statistics
    // pages have data. Mirrors MaterializeDailyConsumptionCommandHandler: negative, non-transfer
    // transactions grouped by local business day, valued at the batch unit price.
    private async Task SeedDemoAnalyticsAsync(Guid classId)
    {
        var adminId = (await _userManager.FindByNameAsync(DefaultAdministrators[0].UserName))?.Id;

        // Insert only the item/location pairs that don't have a balance row yet, so re-running this
        // after seeding new demo items (e.g. the goods-receipt seed) backfills the missing pairs
        // instead of skipping entirely because the class already has some balances.
        var pairs = await _context.StockTransactions
            .Where(t => t.ClassId == classId)
            .Select(t => new { t.ItemId, t.LocationId })
            .Distinct()
            .ToListAsync();

        var existingPairs = await _context.ClassBalances
            .Where(b => b.ClassId == classId)
            .Select(b => new { b.ItemId, b.LocationId })
            .ToListAsync();
        var existingPairSet = existingPairs.Select(p => (p.ItemId, p.LocationId)).ToHashSet();

        var missingPairs = pairs.Where(p => !existingPairSet.Contains((p.ItemId, p.LocationId))).ToList();
        if (missingPairs.Count > 0)
        {
            _context.ClassBalances.AddRange(missingPairs.Select(p => new ClassBalance
            {
                ClassId = classId, ItemId = p.ItemId, LocationId = p.LocationId, OpeningQty = 0
            }));
            await _context.SaveChangesAsync(CancellationToken.None);

            var missingPairIds = missingPairs.Select(p => p.ItemId).ToList();
            await _context.ClassBalances
                .Where(b => b.ClassId == classId && missingPairIds.Contains(b.ItemId)
                    && b.CreatedById == null)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.CreatedById, adminId)
                    .SetProperty(b => b.LastModifiedById, adminId));
        }

        if (await _context.DailyItemConsumptions.AnyAsync(c => c.ClassId == classId))
        {
            return;
        }

        var consumed = await _context.StockTransactions
            .AsNoTracking()
            .Where(t => t.ClassId == classId
                && t.QuantityChange < 0
                && t.Type != StockTransactionType.Transfer)
            .Select(t => new
            {
                t.ItemId,
                t.LocationId,
                t.CreatedAt,
                t.QuantityChange,
                UnitPrice = t.Batch != null ? t.Batch.UnitPrice : 0m
            })
            .ToListAsync();

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(skestock.Shared.Services.BusinessTimeZoneId);
        var computedAt = DateTimeOffset.UtcNow;

        var rows = consumed
            .GroupBy(t => new
            {
                t.ItemId,
                t.LocationId,
                Date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(t.CreatedAt, timeZone).DateTime)
            })
            .Select(g => new DailyItemConsumption
            {
                Date = g.Key.Date,
                ItemId = g.Key.ItemId,
                ClassId = classId,
                LocationId = g.Key.LocationId,
                Quantity = g.Sum(t => -t.QuantityChange),
                TotalValue = g.Sum(t => -t.QuantityChange * t.UnitPrice),
                ComputedAt = computedAt
            })
            .ToList();

        _context.DailyItemConsumptions.AddRange(rows);
        await _context.SaveChangesAsync(CancellationToken.None);
    }

    // Seeds 20 weekly goods receipts (real GoodsReceipt aggregates, unlike the daily-usage seed
    // above whose Order transactions are left unlinked) across 50 dedicated demo items, so the
    // "Top achiziții" purchase statistics have realistic historic frequency/quantity/value data.
    // Idempotent: skipped once any goods receipt already exists for the demo class.
    private async Task SeedDemoGoodsReceiptsAsync(Guid classId)
    {
        if (await _context.GoodsReceipts.AnyAsync(gr => gr.ClassId == classId))
        {
            return;
        }

        var administrator = await _userManager.FindByNameAsync(DefaultAdministrators[0].UserName)
                            ?? throw new InvalidOperationException("Seed administrator not found.");
        var profileId = administrator.Id;

        var schoolClass = await _context.SchoolClasses.FirstAsync(c => c.Id == classId);
        var categoryIds = await _context.Categories.Select(c => c.Id).ToListAsync();
        var locationIds = await _context.Locations.Select(l => l.Id).ToListAsync();
        if (categoryIds.Count == 0 || locationIds.Count == 0)
        {
            return;
        }

        var random = new Random(20261107);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        string[] units = ["buc", "kg", "l", "cutie"];
        var items = new List<(Item Item, Guid LocationId, decimal BasePrice)>();
        for (var n = 1; n <= 50; n++)
        {
            var isPerishable = random.Next(2) == 0;
            var item = new Item
            {
                Sku = $"PUR-{n:00}",
                Name = $"Achiziție demo {n:00}",
                Unit = units[random.Next(units.Length)],
                MinThreshold = random.Next(5, 31),
                IsPerishable = isPerishable,
                ShelfLifeDays = isPerishable ? random.Next(7, 91) : null,
                CategoryId = categoryIds[random.Next(categoryIds.Count)]
            };
            items.Add((item, locationIds[random.Next(locationIds.Count)],
                Math.Round((decimal)(random.NextDouble() * 148 + 2), 2)));
        }

        _context.Items.AddRange(items.Select(i => i.Item));

        var receipts = new List<GoodsReceipt>();
        var batches = new List<StockBatch>();
        var transactions = new List<StockTransaction>();

        // 20 receipts, one per week, ending today.
        for (var week = 0; week < 20; week++)
        {
            var receiptDay = today.AddDays(-7 * (19 - week));
            var receivedAt = new DateTimeOffset(
                receiptDay.ToDateTime(new TimeOnly(random.Next(7, 16), random.Next(60))), TimeSpan.Zero);

            var lineCount = random.Next(5, 16);
            var lineItems = items.OrderBy(_ => random.Next()).Take(lineCount).ToList();

            var receipt = new GoodsReceipt
            {
                ClassId = classId,
                Class = schoolClass,
                ReceivedAt = receivedAt,
                SupplierReference = $"PO-{week + 1:0000}",
                Note = "Comandă săptămânală (demo)"
            };

            decimal totalAmount = 0;
            foreach (var (item, locationId, basePrice) in lineItems)
            {
                var quantity = random.Next(5, 51);
                var price = Math.Round(basePrice * (decimal)(0.85 + random.NextDouble() * 0.3), 2);
                totalAmount += quantity * price;

                var batch = new StockBatch
                {
                    Item = item,
                    LocationId = locationId,
                    ReceivedClass = schoolClass,
                    Quantity = quantity,
                    UnitPrice = price,
                    ReceivedDate = receiptDay,
                    ExpiryDate = item.ShelfLifeDays is { } shelfLife ? receiptDay.AddDays(shelfLife) : null,
                    GoodsReceipt = receipt
                };
                batches.Add(batch);

                transactions.Add(new StockTransaction
                {
                    Item = item,
                    LocationId = locationId,
                    Batch = batch,
                    Class = schoolClass,
                    UserId = profileId,
                    Type = StockTransactionType.Order,
                    QuantityChange = quantity,
                    Reason = "goods-receipt",
                    CreatedAt = receivedAt,
                    GoodsReceipt = receipt
                });
            }

            receipt.TotalAmount = Math.Round(totalAmount, 2);
            receipts.Add(receipt);
        }

        _context.GoodsReceipts.AddRange(receipts);
        _context.StockBatches.AddRange(batches);
        _context.StockTransactions.AddRange(transactions);
        await _context.SaveChangesAsync(CancellationToken.None);

        // See SeedCategoriesAsync: stamp audit fields without the HTTP-user interceptor.
        var itemIds = items.Select(i => i.Item.Id).ToList();
        await _context.Items.Where(i => itemIds.Contains(i.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.CreatedById, profileId)
                .SetProperty(i => i.LastModifiedById, profileId));
        var receiptIds = receipts.Select(r => r.Id).ToList();
        await _context.GoodsReceipts.Where(r => receiptIds.Contains(r.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.CreatedById, profileId)
                .SetProperty(r => r.LastModifiedById, profileId));
        await _context.StockBatches.Where(b => b.GoodsReceiptId != null && receiptIds.Contains(b.GoodsReceiptId!.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.CreatedById, profileId)
                .SetProperty(b => b.LastModifiedById, profileId));
        await _context.StockTransactions
            .Where(t => t.GoodsReceiptId != null && receiptIds.Contains(t.GoodsReceiptId!.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.CreatedById, profileId)
                .SetProperty(t => t.LastModifiedById, profileId));

        // The new items introduce new item/location pairs for the demo class.
        await SeedDemoAnalyticsAsync(classId);

        // Recompute purchase statistics over the whole database (rolling windows + per-class
        // history) so "Top achiziții" reflects this historic data immediately, without waiting
        // for the nightly DailyStatisticsService job.
        await _sender.Send(new MaterializePurchaseStatisticsCommand
        {
            TimeZoneId = skestock.Shared.Services.BusinessTimeZoneId
        });

        _logger.LogInformation(
            "Seeded {Receipts} weekly demo goods receipts with {Batches} batches and {Transactions} transactions.",
            receipts.Count, batches.Count, transactions.Count);
    }
}
