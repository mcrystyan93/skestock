using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Domain.Entities.Items;
using skestock.Infrastructure.Data;

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems;

public class ItemMergerCoverageTests
{
    // Entities that reference an item on purpose without being re-pointed by a merge.
    private static readonly Type[] Excluded = [];

    [Test]
    public void EveryEntityReferencingAnItem_HasAMerger()
    {
        // Builds the model only; the connection is never opened.
        using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=.").Options);

        var referencing = db.Model.GetEntityTypes()
            .Where(type => type.GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == typeof(Item))
                           || type.GetProperties().Any(p => p.Name is "ItemId" or "MatchedItemId"))
            .Select(type => type.ClrType)
            .Where(type => type != typeof(Item))
            .Except(Excluded)
            .ToList();

        var merged = typeof(IItemReferenceMerger).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IItemReferenceMerger)))
            .Select(EntityOf)
            .ToList();

        referencing.ShouldBe(merged, ignoreOrder: true);
    }

    private static Type EntityOf(Type merger)
    {
        for (var type = merger.BaseType; type is not null; type = type.BaseType)
            if (type.IsGenericType && type.GetGenericTypeDefinition() is var definition
                && (definition == typeof(KeyedItemMerger<,>) || definition == typeof(PlainItemMerger<>)))
                return type.GetGenericArguments()[0];

        throw new InvalidOperationException($"{merger.Name} does not derive from a known merger base.");
    }
}
