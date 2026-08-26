using skestock.Domain.Constants;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using skestock.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
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
        // Default roles
        var administratorRole = new IdentityRole<int>(Roles.Administrator);

        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        // Default users
        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await _userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
            {
                await _userManager.AddToRolesAsync(administrator, new [] { administratorRole.Name });
            }
        }

        administrator = await _userManager.FindByNameAsync(administrator.UserName!);

        // Ensure the administrator has a UserProfile - BaseAuditableEntity.CreatedBy/LastModifiedBy
        // (and StockTransaction.User) are FK'd to UserProfile.IdentityId, so the Identity user id
        // itself is what gets stored/passed below, not UserProfile.Id.
        var administratorProfile = await _context.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.IdentityId == administrator!.Id);

        if (administratorProfile is null)
        {
            administratorProfile = new UserProfile
            {
                IdentityId = administrator!.Id,
                FirstName = "Administrator",
                LastName = "Administrator"
            };
            _context.Set<UserProfile>().Add(administratorProfile);
            await _context.SaveChangesAsync(CancellationToken.None);
        }

        await SeedCategoriesAsync(administrator!.Id);
        await SeedLocationsAsync(administrator!.Id);
        await SeedSchoolClassesAsync(administrator!.Id);
        await SeedItemsAsync(administrator!.Id);
        await SeedGoodsReceiptAsync(administrator!.Id);
    }

    private static readonly (string Name, string Type)[] DefaultLocations =
    [
        ("Frigider", "Bucatarie"),
        ("Dulap servire", "Sala de mese"),
        ("Rafturi", "Bucatarie"),
        ("Camara", "Bucatarie"),
        ("Depozit", "Depozit")
    ];

    private static readonly string[] DefaultCategoryNames =
    [
        "Lactate și Ouă", 
        "Brânzeturi", 
        "Carne Proaspătă", 
        "Pește și Fructe de Mare", 
        "Mezeluri și Specialități", 
        "Legume Proaspete", 
        "Fructe Proaspete", 
        "Produse Congelate", 
        "Conserve", 
        "Semipreparate și Tartinabile", 
        "Alimente de Bază", 
        "Paste și Orez", 
        "Ulei și Oțet", 
        "Condimente și Ingrediente", 
        "Nuci și Fructe Uscate", 
        "Cereale și Mic Dejun", 
        "Dulciuri și Biscuiți", 
        "Ingrediente Patiserie", 
        "Băuturi Vegetale și Solubile", 
        "Detergenți și Dezinfectanți", 
        "Consumabile Menaj"
    ];

    private async Task SeedCategoriesAsync(int administratorIdentityId)
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

    private async Task SeedLocationsAsync(int administratorIdentityId)
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
            .Select(l => new Location { Name = l.Name, Type = l.Type })
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

    private static readonly (string Name, DateOnly StartDate, DateOnly EndDate, ClassStatus Status)[] DefaultSchoolClasses =
    [
        ("SKE 29", new DateOnly(2026, 7, 13), new DateOnly(2026, 9, 4), ClassStatus.Active)
    ];

    private async Task SeedSchoolClassesAsync(int administratorIdentityId)
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
                Name = c.Name,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status
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

    private static readonly (string Sku, string Name, string Description, string Unit, bool IsPerishable, string Category)[] DefaultItems =
    [
        ("301195", "200G PRESIDENT UNT 82%", "200G PRESIDENT UNT 82%", "BU", true, "Lactate și Ouă"),
        ("110285", "1L DORNA LAPTE UHT 3.5%", "1L DORNA LAPTE UHT 3.5%", "BU", true, "Lactate și Ouă"),
        ("375946", "320G DANONE SANA", "320G DANONE SANA", "BU", true, "Lactate și Ouă"),
        ("396048", "330G IAURT BAUT IN TIHNA 1.5%", "330G IAURT BAUT IN TIHNA 1.5%", "BU", true, "Lactate și Ouă"),
        ("292392", "4X10G PAKMAYA DROJDIE USCATA", "4X10G PAKMAYA DROJDIE USCATA", "IM", false, "Ingrediente Patiserie"),
        ("294576", "1X30 OUA L TONELI", "1X30 OUA L TONELI", "CA", true, "Lactate și Ouă"),
        ("212144", "180G HOCHLAND MARGELE BRANZA", "180G HOCHLAND MARGELE BRANZA", "BU", true, "Brânzeturi"),
        ("110049", "150G HOCHLAND CASCAV FELII CL", "150G HOCHLAND CASCAV FELII CL", "BU", true, "Brânzeturi"),
        ("307600", "350G HOCHLAND TELEMEA NATUR", "350G HOCHLAND TELEMEA NATUR", "BU", true, "Brânzeturi"),
        ("02929310000006", "LAMAI", "LAMAI", "KG", true, "Fructe Proaspete"),
        ("103483", "MORCOVI 1KG", "MORCOVI 1KG", "BU", true, "Legume Proaspete"),
        ("02917900000000", "ROSII CHERRY", "ROSII CHERRY", "KG", true, "Legume Proaspete"),
        ("02922510000005", "CARTOFI DULCI", "CARTOFI DULCI", "KG", true, "Legume Proaspete"),
        ("02807430000000", "DOVLECEI", "DOVLECEI", "KG", true, "Legume Proaspete"),
        ("404381", "MC SALATA CROCANTA 500G", "MC SALATA CROCANTA 500G", "CE", true, "Legume Proaspete"),
        ("407310", "MC SALATA MOZAIC 200G", "MC SALATA MOZAIC 200G", "CE", true, "Legume Proaspete"),
        ("411680", "2KG CEAPA GALBENA", "2KG CEAPA GALBENA", "SC", true, "Legume Proaspete"),
        ("096968", "400G CIRIO PULPA DE ROSII BAX", "400G CIRIO PULPA DE ROSII BAX", "IM", false, "Conserve"),
        ("339290", "340G BONDUELLE PORUMB GOLD", "340G BONDUELLE PORUMB GOLD", "CV", false, "Conserve"),
        ("353143", "425ML GIANA FASOLE ROSIE", "425ML GIANA FASOLE ROSIE", "BU", false, "Conserve"),
        ("116094", "300G AROVIT ZACUSCA CU VINETE", "300G AROVIT ZACUSCA CU VINETE", "BO", false, "Semipreparate și Tartinabile"),
        ("400142", "0.5L FINE LIFE ULEI MASLIN EXV", "0.5L FINE LIFE ULEI MASLIN EXV", "BU", false, "Ulei și Oțet"),
        ("031645", "1KG BARILLA PENNE RIGATE NR. 73", "1KG BARILLA PENNE RIGATE NR. 73", "BU", false, "Paste și Orez"),
        ("447356", "500G BARILLA FUSILLI NR.98", "500G BARILLA FUSILLI NR.98", "BU", false, "Paste și Orez"),
        ("093092", "340G MAXIM'S CREMA DE ARAHIDE", "340G MAXIM'S CREMA DE ARAHIDE", "BO", false, "Semipreparate și Tartinabile"),
        ("242535", "370G FL DULCEATA VISINE", "370G FL DULCEATA VISINE", "BO", false, "Semipreparate și Tartinabile"),
        ("273444", "200G NUTELLA CREMA ALUNE&CACAO", "200G NUTELLA CREMA ALUNE&CACAO", "CA", false, "Semipreparate și Tartinabile"),
        ("408197", "500G PIRIFAN FULGI OVAZ", "500G PIRIFAN FULGI OVAZ", "BU", false, "Cereale și Mic Dejun"),
        ("229937", "BG DR OETKER ZAHAR VANILINAT", "BG DR OETKER ZAHAR VANILINAT", "PI", false, "Ingrediente Patiserie"),
        ("281529", "1KG METRO CHEF CAJU CRUD", "1KG METRO CHEF CAJU CRUD", "IM", false, "Nuci și Fructe Uscate"),
        ("281535", "1KG METRO CHEF MIGDALE CRUDE", "1KG METRO CHEF MIGDALE CRUDE", "PG", false, "Nuci și Fructe Uscate"),
        ("226170", "FINO 30 COLI HAR COPT 42X38CM", "FINO 30 COLI HAR COPT 42X38CM", "BU", false, "Consumabile Menaj")
    ];

    private async Task SeedItemsAsync(int administratorIdentityId)
    {
        var existingSkus = await _context.Items
            .Select(i => i.Sku)
            .ToListAsync();

        var itemsToSeed = DefaultItems
            .Where(i => !existingSkus.Contains(i.Sku))
            .ToList();

        if (itemsToSeed.Count == 0)
        {
            return;
        }

        var categoryIdsByName = await _context.Categories
            .ToDictionaryAsync(c => c.Name, c => c.Id);

        var newItems = new List<Item>();
        foreach (var i in itemsToSeed)
        {
            if (!categoryIdsByName.TryGetValue(i.Category, out var categoryId))
            {
                _logger.LogWarning("Skipping item {Sku} - category {Category} not found.", i.Sku, i.Category);
                continue;
            }

            newItems.Add(new Item
            {
                Sku = i.Sku,
                Name = i.Name,
                Description = i.Description,
                Unit = i.Unit,
                IsPerishable = i.IsPerishable,
                CategoryId = categoryId
            });
        }

        if (newItems.Count == 0)
        {
            return;
        }

        _context.Items.AddRange(newItems);
        await _context.SaveChangesAsync(CancellationToken.None);

        // See SeedCategoriesAsync: bypass the AuditableEntityInterceptor (which would otherwise
        // null out CreatedById/LastModifiedById since there's no HTTP user during seeding).
        var newItemIds = newItems.Select(i => i.Id).ToList();
        await _context.Items
            .Where(i => newItemIds.Contains(i.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.CreatedById, administratorIdentityId)
                .SetProperty(i => i.LastModifiedById, administratorIdentityId));
    }

    private const string DefaultGoodsReceiptNote = "Comanda metro 26.08.2026";
    private const int DefaultGoodsReceiptQuantity = 10;
    private const int DefaultPerishableShelfLifeDays = 14;

    private async Task SeedGoodsReceiptAsync(int administratorIdentityId)
    {
        var alreadySeeded = await _context.Set<GoodsReceipt>()
            .AnyAsync(r => r.Note == DefaultGoodsReceiptNote);

        if (alreadySeeded)
        {
            return;
        }

        var schoolClass = await _context.SchoolClasses
            .FirstOrDefaultAsync(c => c.Name == "SKE 29");

        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Name == "Rafturi");

        if (schoolClass is null || location is null)
        {
            _logger.LogWarning("Skipping goods receipt seeding - required SchoolClass 'SKE 29' or Location 'Rafturi' not found.");
            return;
        }

        var items = await _context.Items
            .Where(i => DefaultItems.Select(d => d.Sku).Contains(i.Sku))
            .ToListAsync();

        if (items.Count == 0)
        {
            return;
        }

        var receivedDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var goodsReceipt = new GoodsReceipt
        {
            ReceivedAt = DateTime.UtcNow,
            ClassId = schoolClass.Id,
            Class = schoolClass,
            SupplierReference = "Metro",
            Note = DefaultGoodsReceiptNote
        };
        _context.Set<GoodsReceipt>().Add(goodsReceipt);
        await _context.SaveChangesAsync(CancellationToken.None);

        var newBatches = items
            .Select(item => new StockBatch
            {
                ItemId = item.Id,
                LocationId = location.Id,
                ReceivedClassId = schoolClass.Id,
                Quantity = DefaultGoodsReceiptQuantity,
                ExpiryDate = item.IsPerishable ? receivedDate.AddDays(DefaultPerishableShelfLifeDays) : null,
                ReceivedDate = receivedDate,
                GoodsReceiptId = goodsReceipt.Id
            })
            .ToList();

        _context.StockBatches.AddRange(newBatches);
        await _context.SaveChangesAsync(CancellationToken.None);

        var newTransactions = newBatches
            .Select(batch => new StockTransaction
            {
                ItemId = batch.ItemId,
                LocationId = batch.LocationId,
                BatchId = batch.Id,
                ClassId = schoolClass.Id,
                UserId = administratorIdentityId,
                Type = StockTransactionType.Order,
                QuantityChange = batch.Quantity,
                Reason = "Goods receipt",
                CreatedAt = goodsReceipt.ReceivedAt,
                GoodsReceiptId = goodsReceipt.Id
            })
            .ToList();

        _context.StockTransactions.AddRange(newTransactions);
        await _context.SaveChangesAsync(CancellationToken.None);

        // See SeedCategoriesAsync: bypass the AuditableEntityInterceptor (which would otherwise
        // null out CreatedById/LastModifiedById since there's no HTTP user during seeding).
        await _context.Set<GoodsReceipt>()
            .Where(r => r.Id == goodsReceipt.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.CreatedById, administratorIdentityId)
                .SetProperty(r => r.LastModifiedById, administratorIdentityId));

        var newBatchIds = newBatches.Select(b => b.Id).ToList();
        await _context.StockBatches
            .Where(b => newBatchIds.Contains(b.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.CreatedById, administratorIdentityId)
                .SetProperty(b => b.LastModifiedById, administratorIdentityId));

        var newTransactionIds = newTransactions.Select(t => t.Id).ToList();
        await _context.StockTransactions
            .Where(t => newTransactionIds.Contains(t.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.CreatedById, administratorIdentityId)
                .SetProperty(t => t.LastModifiedById, administratorIdentityId));
    }
}
