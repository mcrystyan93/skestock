using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Documents.Schemas;

namespace skestock.Infrastructure.AI.Schemas;

public sealed class ItemExtractionSchemaFactory(IApplicationDbContext dbContext)
    : IExtractionSchemaFactory<ItemExtractionResult>
{
    public string Prompt =>
        "Inspect the attached document (typically a product catalog, price list, or inventory sheet) " +
        "and identify every catalog item listed. For each item capture its SKU/product code when " +
        "present (empty string if not present), its name, the unit of measure (e.g. 'unit', 'kg', " +
        "'box'; default to 'unit' if not present), a short description if present (empty string " +
        "otherwise), whether it is a perishable good, its minimum stock threshold (default 0), " +
        "shelf-life in days when perishable (empty when unknown), and the category it belongs to.";

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
            ? "There are no existing categories yet - infer a sensible category name for each item."
            : "Prefer matching one of the existing categories (case-insensitive, whitespace-trimmed) " +
              $"when the item clearly belongs to it: {string.Join(", ", normalizedCategoryNames)}. " +
              "Otherwise use a new, sensible category name.";

        return new JsonObject
        {
            ["type"] = "object",
            ["description"] = $"{Prompt} {existingCategoriesDescription}",
            ["properties"] = new JsonObject
            {
                ["items"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["sku"] =
                                new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] =
                                        "The item's SKU/product code, empty string if not present."
                                },
                            ["name"] =
                                new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The trimmed name of the item."
                                },
                            ["categoryName"] =
                                new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "The category this item belongs to."
                                },
                            ["unit"] =
                                new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] =
                                        "The unit of measure, e.g. 'unit', 'kg', 'box'."
                                },
                            ["description"] =
                                new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] =
                                        "A short description of the item, empty string if not present."
                                },
                            ["isPerishable"] =
                                new JsonObject
                                {
                                    ["type"] = "boolean",
                                    ["description"] = "Whether the item is perishable."
                            }
                        },
                        ["required"] = new JsonArray
                        {
                            "sku",
                            "name",
                            "categoryName",
                            "unit",
                            "description",
                            "isPerishable"
                        },
                        ["additionalProperties"] = false
                    }
                }
            },
            ["required"] = new JsonArray { "items" },
            ["additionalProperties"] = false
        };
    }
}
