using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Schemas;
using skestock.Application.Features.Expenses.Models;
using skestock.Infrastructure.AI;
using skestock.Infrastructure.AI.Schemas;

namespace skestock.Application.UnitTests.Documents;

public class OpenAiDocumentExtractionContentTypeTests
{
    [TestCase("image/jpeg", "input_image", "image_url")]
    [TestCase("image/png", "input_image", "image_url")]
    [TestCase("application/pdf", "input_file", "file_data")]
    public async Task ExtractAsync_SendsImagesAsInputImageAndOtherFilesAsInputFile(
        string mimeType, string expectedType, string expectedDataField)
    {
        JsonNode? sentBody = null;
        var handler = new CapturingHandler(async request =>
        {
            sentBody = JsonNode.Parse(await request.Content!.ReadAsStringAsync());
            var response = new JsonObject
            {
                ["status"] = "completed",
                ["output"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["content"] = new JsonArray
                        {
                            new JsonObject { ["type"] = "output_text", ["text"] = """{"supplierName":"ACME"}""" }
                        }
                    }
                }
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) };
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IExtractionSchemaFactory<ExpenseExtractionResult>, ExpenseExtractionSchemaFactory>()
            .BuildServiceProvider();
        var client = new OpenAiDocumentExtractionClient(
            httpClient, serviceProvider,
            Options.Create(new OpenAiApiSettings { Model = "test-model", ApiKey = "test-key" }));

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await client.ExtractAsync<ExpenseExtractionResult>(stream, mimeType, CancellationToken.None);

        result.SupplierName.ShouldBe("ACME");
        var file = sentBody!["input"]![0]!["content"]![0]!;
        file["type"]!.GetValue<string>().ShouldBe(expectedType);
        file[expectedDataField]!.GetValue<string>().ShouldStartWith($"data:{mimeType};base64,");
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => responseFactory(request);
    }
}
