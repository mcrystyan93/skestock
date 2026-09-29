using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;
using skestock.Application.Features.SupplyLists.Commands.DisableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.EnableSupplyList;
using skestock.Application.Features.SupplyLists.Commands.UpdateSupplyList;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.SupplyLists;

public class SupplyListCommandHandlerTests
{
    [Test]
    public async Task Create_DefaultsQuantityToOneAndUnitFromItem()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var result = await new CreateSupplyListCommandHandler(context).Handle(new CreateSupplyListCommand
        {
            Name = "  Weekly  ",
            Frequency = SupplyListFrequency.Weekly,
            Lines = [new SupplyListLineInput { ItemId = rice.Id }]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Weekly");
        result.Value.IsActive.ShouldBeTrue();
        var line = result.Value.Lines.ShouldHaveSingleItem();
        line.Quantity.ShouldBe(1);
        line.Unit.ShouldBe("kg");
        line.ItemName.ShouldBe("Rice");
        line.CategoryName.ShouldBe("Pantry");
    }

    [Test]
    public async Task Create_KeepsExplicitQuantityAndUnit()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var result = await new CreateSupplyListCommandHandler(context).Handle(new CreateSupplyListCommand
        {
            Name = "Every 2 weeks",
            Frequency = SupplyListFrequency.EveryXWeeks,
            IntervalWeeks = 2,
            Lines = [new SupplyListLineInput { ItemId = rice.Id, Quantity = 2.5m, Unit = "g", Notes = " fine " }]
        }, CancellationToken.None);

        var line = result.Value.Lines.ShouldHaveSingleItem();
        line.Quantity.ShouldBe(2.5m);
        line.Unit.ShouldBe("g");
        line.Notes.ShouldBe("fine");
        result.Value.IntervalWeeks.ShouldBe(2);
    }

    [Test]
    public async Task Create_FailsForInactiveItem()
    {
        var (context, _, _, disabled) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var result = await new CreateSupplyListCommandHandler(context).Handle(new CreateSupplyListCommand
        {
            Name = "List",
            Frequency = SupplyListFrequency.Once,
            Lines = [new SupplyListLineInput { ItemId = disabled.Id }]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListItemInactive);
        context.SupplyLists.Count().ShouldBe(0);
    }

    private static async Task<SupplyList> SeedListAsync(SupplyListTestDbContext context, params Item[] items)
    {
        var list = new SupplyList { Name = "Existing", Frequency = SupplyListFrequency.Weekly };
        foreach (var item in items)
            list.Lines.Add(new SupplyListLine { ItemId = item.Id, Quantity = 3, Unit = item.Unit });
        context.SupplyLists.Add(list);
        await context.SaveChangesAsync(CancellationToken.None);
        return list;
    }

    [Test]
    public async Task Update_ReplacesLinesAndKeepsDisabledExistingItems()
    {
        var (context, rice, sugar, disabled) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;
        var list = await SeedListAsync(context, rice, disabled);

        var result = await new UpdateSupplyListCommandHandler(context).Handle(new UpdateSupplyListCommand
        {
            Id = list.Id,
            Name = "Renamed",
            Frequency = SupplyListFrequency.StartOfMonth,
            Lines =
            [
                new SupplyListLineInput { ItemId = disabled.Id, Quantity = 4 },
                new SupplyListLineInput { ItemId = sugar.Id }
            ]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Renamed");
        result.Value.Frequency.ShouldBe(nameof(SupplyListFrequency.StartOfMonth));
        result.Value.Lines.Select(l => l.ItemId).ShouldBe([sugar.Id, disabled.Id], ignoreOrder: true);
        result.Value.Lines.Single(l => l.ItemId == disabled.Id).Quantity.ShouldBe(4);
        context.SupplyListLines.Count().ShouldBe(2);
    }

    [Test]
    public async Task Update_FailsWhenAddingInactiveItem()
    {
        var (context, rice, _, disabled) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;
        var list = await SeedListAsync(context, rice);

        var result = await new UpdateSupplyListCommandHandler(context).Handle(new UpdateSupplyListCommand
        {
            Id = list.Id,
            Name = "Existing",
            Frequency = SupplyListFrequency.Weekly,
            Lines = [new SupplyListLineInput { ItemId = disabled.Id }]
        }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListItemInactive);
    }

    [Test]
    public async Task Update_FailsWhenListNotFound()
    {
        var (context, _, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var result = await new UpdateSupplyListCommandHandler(context).Handle(
            new UpdateSupplyListCommand { Id = Guid.NewGuid(), Name = "x" }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListNotFound);
    }

    [Test]
    public async Task DisableEnable_ToggleAndBlockUpdates()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;
        var list = await SeedListAsync(context, rice);

        var disabledResult = await new DisableSupplyListCommandHandler(context).Handle(
            new DisableSupplyListCommand { Id = list.Id }, CancellationToken.None);
        var again = await new DisableSupplyListCommandHandler(context).Handle(
            new DisableSupplyListCommand { Id = list.Id }, CancellationToken.None);

        disabledResult.Value.IsActive.ShouldBeFalse();
        again.IsSuccess.ShouldBeTrue();

        var update = await new UpdateSupplyListCommandHandler(context).Handle(new UpdateSupplyListCommand
        {
            Id = list.Id, Name = "Existing", Frequency = SupplyListFrequency.Weekly
        }, CancellationToken.None);
        update.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListNotEditable);

        var enabled = await new EnableSupplyListCommandHandler(context).Handle(
            new EnableSupplyListCommand { Id = list.Id }, CancellationToken.None);
        enabled.Value.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task DisableEnable_FailWhenNotFound()
    {
        var (context, _, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var disable = await new DisableSupplyListCommandHandler(context).Handle(
            new DisableSupplyListCommand { Id = Guid.NewGuid() }, CancellationToken.None);
        var enable = await new EnableSupplyListCommandHandler(context).Handle(
            new EnableSupplyListCommand { Id = Guid.NewGuid() }, CancellationToken.None);

        disable.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListNotFound);
        enable.Errors.ShouldContain(e => e is SupplyListErrors.SupplyListNotFound);
    }
}
