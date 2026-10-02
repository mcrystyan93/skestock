using Microsoft.EntityFrameworkCore;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;

namespace skestock.Application.UnitTests.Features.SupplyLists;

internal static class SupplyListTestData
{
    public static async Task<(SupplyListTestDbContext Context, Item Rice, Item Sugar, Item Disabled)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<SupplyListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SupplyListTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        var rice = new Item { Name = "Rice", Unit = "kg", Category = category, IsActive = true };
        var sugar = new Item { Name = "Sugar", Unit = "box", Category = category, IsActive = true };
        var disabled = new Item { Name = "Salt", Unit = "kg", Category = category, IsActive = false };
        context.Items.AddRange(rice, sugar, disabled);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, rice, sugar, disabled);
    }
}
