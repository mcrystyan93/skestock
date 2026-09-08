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

public class OpenAiCategoryDocumentExtractionServiceTests
{
    [Test]
    public async Task ExtractAsync_SendsSchemaFactoryPromptAndDeserializesCategoryResult()
    {
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();

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
                                ["text"] = """{"categories":[{"name":"Stationery"}]}"""
                            }
                        }
                    }
                }
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response)
            };
        });

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IExtractionSchemaFactory<CategoryExtractionResult>, TestSchemaFactory>()
            .BuildServiceProvider();
        var client = new OpenAiDocumentExtractionClient(
            httpClient,
            serviceProvider,
            Options.Create(new OpenAiApiSettings { Model = "test-model", ApiKey = "test-key" }));
        var service = new OpenAiCategoryDocumentExtractionService(client);

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.ExtractAsync<CategoryExtractionResult>(
            stream,
            "application/pdf",
            CancellationToken.None);

        result.Categories.Count.ShouldBe(1);
        result.Categories[0].Name.ShouldBe("Stationery");

        var request = JsonNode.Parse(requestBody!)!.AsObject();
        request["model"]!.GetValue<string>().ShouldBe("test-model");
        request["text"]!["format"]!["name"]!.GetValue<string>().ShouldBe("CategoryExtractionResult");
        request["input"]![0]!["content"]![1]!["text"]!.GetValue<string>()
            .ShouldBe("Find only new categories.");
    }

    private sealed class TestSchemaFactory : IExtractionSchemaFactory<CategoryExtractionResult>
    {
        public string Prompt => "Find only new categories.";

        public Task<JsonObject> Create(CancellationToken cancellationToken) =>
            Task.FromResult(new JsonObject
            {
                ["type"] = "object",
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
                                ["name"] = new JsonObject { ["type"] = "string" }
                            },
                            ["required"] = new JsonArray { "name" },
                            ["additionalProperties"] = false
                        }
                    }
                },
                ["required"] = new JsonArray { "categories" },
                ["additionalProperties"] = false
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
