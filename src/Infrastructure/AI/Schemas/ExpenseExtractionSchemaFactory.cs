using System.Text.Json.Nodes;
using skestock.Application.Documents.Schemas;
using skestock.Application.Features.Expenses.Models;

namespace skestock.Infrastructure.AI.Schemas;

public class ExpenseExtractionSchemaFactory : IExtractionSchemaFactory<ExpenseExtractionResult>
{
    public string Prompt =>
        "Extract the expense data from the attached document (a Romanian receipt 'bon fiscal' or invoice 'factură') " +
        "into JSON that matches the provided schema. The document describes ONE expense: if it contains several, " +
        "use the first one. Use null for anything not present on the document; never guess.";

    public Task<JsonObject> Create(CancellationToken cancellationToken)
    {
        static JsonObject Field(string type, string description) =>
            new() { ["type"] = new JsonArray(type, "null"), ["description"] = description };

        JsonObject schema = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["documentType"] = new JsonObject
                {
                    ["type"] = new JsonArray("string", "null"),
                    ["enum"] = new JsonArray("receipt", "invoice", null),
                    ["description"] = "'receipt' for a bon fiscal, 'invoice' for a factura"
                },
                ["supplierName"] = Field("string", "Seller / issuer name"),
                ["documentNumber"] = Field("string", "Receipt or invoice number"),
                ["expenseDate"] = Field("string", "Issue date, ISO 8601 (yyyy-MM-dd)"),
                ["currency"] = Field("string", "ISO 4217 code, e.g. RON"),
                ["totalAmount"] = Field("number", "Total to pay, VAT included"),
                ["vatAmount"] = Field("number", "Total VAT (TVA) amount")
            },
            ["required"] = new JsonArray(
                "documentType", "supplierName", "documentNumber", "expenseDate", "currency", "totalAmount", "vatAmount"),
            ["additionalProperties"] = false
        };

        return Task.FromResult(schema);
    }
}
