using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Documents.Schemas;

namespace skestock.Infrastructure.AI.Schemas;

public sealed class CategoryExtractionSchemaFactory(IApplicationDbContext dbContext)
    : IExtractionSchemaFactory<CategoryExtractionResult>
{
    public string Prompt =>
        "Inspect the attached document and identify category names that are present in the document " +
        "but are not already in the existing category list. Return only newly discovered categories. " +
        "Compare names case-insensitively after trimming whitespace, and do not return duplicates. " +
        "If no new categories are found, return an empty categories array.";

    public async Task<JsonObject> Create(CancellationToken cancellationToken)
    {
        var existingCategoryNames = await dbContext.Categories
            .AsNoTracking()
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);

        var normalizedCategoryNames = existingCategoryNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existingCategoriesDescription = normalizedCategoryNames.Length == 0
            ? "There are no existing categories."
            : $"Existing categories (case-insensitive, whitespace-trimmed): {string.Join(", ", normalizedCategoryNames)}.";

        return new JsonObject
        {
            ["type"] = "object",
            ["description"] = $"{Prompt} {existingCategoriesDescription}",
            ["properties"] = new JsonObject
            {
                ["categories"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["name"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "The trimmed name of a newly discovered category."
                            }
                        },
                        ["required"] = new JsonArray { "name" },
                        ["additionalProperties"] = false
                    }
                }
            },
            ["required"] = new JsonArray { "categories" },
            ["additionalProperties"] = false
        };
    }
}
