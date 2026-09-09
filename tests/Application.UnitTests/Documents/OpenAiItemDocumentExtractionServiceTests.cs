using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Documents.Schemas;
using skestock.Infrastructure.AI;

namespace skestock.Application.UnitTests.Documents;

public class OpenAiItemDocumentExtractionServiceTests
{
    [Test]
    public async Task ExtractAsync_DeserializesItemFields()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = new JsonObject
            {
                ["status"] = "completed",
                ["output"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["content"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "output_text",
                                ["text"] =
                                     """{"items":[{"sku":"SKU-1","name":"Milk","categoryName":"Dairy","unit":"L","description":"Whole milk","isPerishable":true}]}"""
                            }
                        }
                    }
                }
            };

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response) });
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IExtractionSchemaFactory<ItemExtractionResult>, TestSchemaFactory>()
            .BuildServiceProvider();
        var client = new OpenAiDocumentExtractionClient(
            httpClient,
            serviceProvider,
            Options.Create(new OpenAiApiSettings { Model = "test-model", ApiKey = "test-key" }));
        var service = new OpenAiItemDocumentExtractionService(client);

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await service.ExtractAsync<ItemExtractionResult>(
            stream, "application/pdf", CancellationToken.None);

        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Milk");
        result.Items[0].IsPerishable.ShouldBeTrue();
    }

    private sealed class TestSchemaFactory : IExtractionSchemaFactory<ItemExtractionResult>
    {
        public string Prompt => "Extract items.";

        public Task<JsonObject> Create(CancellationToken cancellationToken) =>
            Task.FromResult(new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject { ["items"] = new JsonObject { ["type"] = "array" } },
                ["required"] = new JsonArray { "items" }
            });
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory(request);
    }
}
