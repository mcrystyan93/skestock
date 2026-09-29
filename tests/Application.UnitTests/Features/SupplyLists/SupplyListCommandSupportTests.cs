using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Application.Features.SupplyLists.Queries.GetSupplyListById;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using CategoryCache = skestock.Application.Features.Categories.CacheConstants;
using ItemCache = skestock.Application.Features.Items.CacheConstants;
using SupplyListCache = skestock.Application.Features.SupplyLists.CacheConstants;

namespace skestock.Application.UnitTests.Features.SupplyLists;

public class SupplyListCommandSupportTests
{
    private sealed class FailingSaveDbContext(DbContextOptions<SupplyListTestDbContext> options)
        : SupplyListTestDbContext(options)
    {
        public bool FailNextSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            FailNextSave
                ? throw new DbUpdateException("unique index violation")
                : base.SaveChangesAsync(cancellationToken);
    }

    private static FailingSaveDbContext CreateFailingContext() =>
        new(new DbContextOptionsBuilder<SupplyListTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CreateSupplyListCommand CreateCommand(string name) => new()
    {
        Name = name,
        Frequency = SupplyListFrequency.Weekly
    };

    [Test]
    public async Task Create_FailsForUnknownItem()
    {
        var (context, _, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var command = CreateCommand("List");
        var result = await new CreateSupplyListCommandHandler(context).Handle(
            new CreateSupplyListCommand
            {
                Name = command.Name,
                Frequency = command.Frequency,
                Lines = [new SupplyListLineInput { ItemId = Guid.NewGuid() }]
            }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is ItemErrors.ItemNotFound);
    }

    [Test]
    public async Task Create_MapsUniqueIndexRaceToDuplicateNameValidationError()
    {
        await using var context = CreateFailingContext();
        context.SupplyLists.Add(new SupplyList { Name = "Weekly", Frequency = SupplyListFrequency.Weekly });
        await context.SaveChangesAsync(CancellationToken.None);
        context.FailNextSave = true;

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            new CreateSupplyListCommandHandler(context).Handle(CreateCommand("weekly"), CancellationToken.None).AsTask());

        exception.Errors.ShouldContainKey(nameof(SupplyList.Name));
        exception.Errors[nameof(SupplyList.Name)].ShouldContain(e => e.Code == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task Create_RethrowsDbUpdateExceptionWhenNameIsNotTaken()
    {
        await using var context = CreateFailingContext();
        context.FailNextSave = true;

        await Should.ThrowAsync<DbUpdateException>(() =>
            new CreateSupplyListCommandHandler(context).Handle(CreateCommand("Unique"), CancellationToken.None).AsTask());
    }

    [Test]
    public void GetById_CacheTags_EvictOnItemAndCategoryChangesButNotOnUnrelatedListChanges()
    {
        var id = Guid.NewGuid();

        var tags = new GetSupplyListByIdQuery { Id = id }.Tags;

        tags.ShouldContain(SupplyListCache.SupplyListTag(id));
        tags.ShouldContain(ItemCache.ItemListTag);
        tags.ShouldContain(CategoryCache.CategoryListTag);
        tags.ShouldNotContain(SupplyListCache.SupplyListListTag);
    }
}
