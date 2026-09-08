using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Documents.Models;
using skestock.Application.UnitTests.Features.Categories.Commands.CreateCategory;
using skestock.Domain.Entities;
using skestock.Infrastructure.AI.Schemas;

namespace skestock.Application.UnitTests.Documents.Schemas;

public class CategoryExtractionSchemaFactoryTests
{
    [Test]
    public async Task Create_IncludesOnlyNameForEachCategoryAndExistingCategoryGuidance()
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new CategoryTestDbContext(options);
        context.Categories.AddRange(
            new Category { Name = " Office Supplies " },
            new Category { Name = "office supplies" },
            new Category { Name = "Books" });
        await context.SaveChangesAsync(CancellationToken.None);

        var factory = new CategoryExtractionSchemaFactory(context);
        var schema = await factory.Create(CancellationToken.None);

        schema["type"]!.GetValue<string>().ShouldBe("object");
        schema["required"]!.AsArray().Select(value => value!.GetValue<string>())
            .ShouldBe(["categories"]);
        schema["additionalProperties"]!.GetValue<bool>().ShouldBeFalse();
        schema["description"]!.GetValue<string>().ShouldContain("Books, Office Supplies.");
        schema["description"]!.GetValue<string>().ShouldNotContain("Office Supplies, office supplies");
        factory.Prompt.ShouldContain("case-insensitively");
        factory.Prompt.ShouldContain("do not return duplicates");

        var categorySchema = schema["properties"]!["categories"]!.AsObject();
        var itemSchema = categorySchema["items"]!.AsObject();
        var itemProperties = itemSchema["properties"]!.AsObject();

        itemProperties.Count.ShouldBe(1);
        itemProperties.ContainsKey("name").ShouldBeTrue();
        itemSchema["required"]!.AsArray().Select(value => value!.GetValue<string>())
            .ShouldBe(["name"]);
        itemSchema["additionalProperties"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Test]
    public async Task Create_WhenThereAreNoExistingCategories_StatesThatExplicitly()
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new CategoryTestDbContext(options);
        var schema = await new CategoryExtractionSchemaFactory(context).Create(CancellationToken.None);

        schema["description"]!.GetValue<string>().ShouldContain("There are no existing categories.");
    }
}
