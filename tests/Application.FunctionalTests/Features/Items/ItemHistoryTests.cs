using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Users;
using skestock.Infrastructure.Data;

namespace skestock.Application.FunctionalTests.Features.Items;

public class ItemHistoryTests : TestBase
{
    [Test]
    public async Task EditingAnItem_WritesAHistoryVersionWithTheActingUserAsModifier()
    {
        var userId = (await TestApp.RunAsDefaultUserAsync())!.Value;
        // LastModifiedById is an FK to UserProfile.IdentityId.
        await TestApp.AddAsync(new UserProfile { IdentityId = userId, FirstName = "Staff", LastName = "Member" });
        var category = new Category { Name = $"FT{Guid.NewGuid():N}"[..10] };
        await TestApp.AddAsync(category);

        var created = await TestApp.SendAsync(new CreateItemCommand { Name = "History-Old", Unit = "unit", CategoryId = category.Id });
        await TestApp.SendAsync(new EditItemCommand
        {
            Id = created.Value.Id, Name = "History-New", Unit = "unit", CategoryId = category.Id
        });

        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var versions = await context.Items.TemporalAll()
            .Where(i => i.Id == created.Value.Id)
            .OrderBy(i => EF.Property<DateTime>(i, "PeriodStart"))
            .Select(i => new { i.Name, i.LastModifiedById })
            .ToListAsync();

        versions.Count.ShouldBe(2);
        versions[0].Name.ShouldBe("History-Old");
        versions[1].Name.ShouldBe("History-New");
        versions[1].LastModifiedById.ShouldBe(userId);
    }
}
