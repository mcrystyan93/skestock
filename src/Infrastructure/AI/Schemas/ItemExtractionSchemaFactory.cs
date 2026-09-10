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
        """
        Extract and list every catalog item from the attached document (commonly a product catalog, price list, or inventory sheet, usually in Romanian). For each item found, identify and record the following attributes:

        - SKU/product code (use "" if not present)
        - Name
        - Unit of measure (e.g. 'buc', 'kg', 'box'; default to 'buc' if not listed)
        - Short description ("" if not present)
        - Is perishable good (true/false) — reason about the product to determine perishability
        - Minimum stock threshold (integer, default 0 if not listed)
        - Shelf-life in days (if perishable and reason about the product to determine the shelf-life, otherwise "", leave blank for non-perishable)
        - Category (based on the item listing; provide best-guess if not explicit)

        Before concluding and generating the output, internally reason step-by-step to identify, extract, and determine the value for each attribute, especially perishability and category. Only include the final JSON output at the end.

        Persist with reasoning and extraction until all catalog items are processed.

        **Edge Cases:**
        - If an attribute is missing or not explicit (e.g., unit of measure or description), use defaults as above.
        - When in doubt about perishability or category, make a reasoned best guess, erring toward broad, clear categories.
        - For missing or ambiguous product codes, leave the 'sku' field empty.
        - For shelf-life, only fill if perishable and stated; else empty string.

        **REMINDER:**
        Extract all items; for each, systematically reason through attribute selection before making conclusions. Output only the JSON as specified.
        """;

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
                                        "The unit of measure, e.g. 'unit', 'kg', 'box'. Most likely it's in romanian. For Metro receipts, you find it in the column 'Mod amb'"
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
