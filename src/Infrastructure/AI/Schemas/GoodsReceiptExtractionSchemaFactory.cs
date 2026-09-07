using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Schemas;
using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Infrastructure.AI.Schemas;

public class GoodsReceiptExtractionSchemaFactory(IApplicationDbContext dbContext) : IExtractionSchemaFactory<GoodsReceiptExtractionResult>
{
    public string Prompt =>
        "Extract the goods-receipt / delivery-note data from the attached document into JSON " +
        "that matches the provided schema. The document is typically a Romanian invoice or " +
        "delivery note ('aviz de \u00eenso\u021bire a m\u0103rfii' / 'factur\u0103'). ";

    public async Task<JsonObject> Create(CancellationToken cancellationToken)
    {
        var categories = await GetCategories(cancellationToken);
        
        return new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["supplierReference"] =
                    new JsonObject { ["type"] = "string", ["description"] = "'Aviz nr livrare' or invoice number" },
                ["receivedAt"] =
                    new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The date and time of 'Data vanzare' or 'Data emitere'"
                    },
                ["lineItems"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["productCode"] =
                                new JsonObject()
                                {
                                    ["type"] = "string", ["description"] = "Value from 'Cod articol' column"
                                },
                            ["name"] =
                                new JsonObject()
                                {
                                    ["type"] = "string", ["description"] = "Value from 'Denumire articol' column"
                                },
                            ["unit"] =
                                new JsonObject()
                                {
                                    ["type"] = "string", ["description"] = "Value from 'Mod amb' column"
                                },
                            ["quantity"] =
                                new JsonObject() { ["type"] = "number", ["description"] = "Value from 'Cant.' column" },
                            ["unitPrice"] =
                                new JsonObject()
                                {
                                    ["type"] = "number",
                                    ["description"] = "Value from 'Valoare totala cu TVA' column divided by 'Cant.' column'"
                                },
                            ["category"] = new JsonObject() { ["type"] = "string", ["description"] = $"Value from 'Category' column. Possible values are: {categories}" },
                            ["isPerishable"] =
                                new JsonObject()
                                {
                                    ["type"] = "boolean", ["description"] = "Decide if item is perishable"
                                }
                        },
                        ["required"] = new JsonArray
                        {
                            "productCode",
                            "name",
                            "unit",
                            "quantity",
                            "unitPrice",
                            "category",
                            "isPerishable"
                        },
                        ["additionalProperties"] = false
                    }
                }
            },
            ["required"] = new JsonArray { "supplierReference", "receivedAt", "lineItems" },
            ["additionalProperties"] = false
        };
    }

    private async Task<string> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await dbContext.Categories
            .Select(c => c.Name)
            .ToListAsync(cancellationToken);
        
        return string.Join(", ", categories);
    }
}
