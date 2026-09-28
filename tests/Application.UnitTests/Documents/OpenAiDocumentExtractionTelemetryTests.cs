using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Models.Options;
using skestock.Application.Documents.Models;
using skestock.Application.Documents.Schemas;
using skestock.Infrastructure.AI;

namespace skestock.Application.UnitTests.Documents;

[TestFixture]
[NonParallelizable]
public sealed class OpenAiDocumentExtractionTelemetryTests
{
    private const string Model = "test-model";
    private const string Prompt = "Find only new categories in this confidential prompt.";

    // Large enough that its base64 form is a distinctive string to search for in telemetry.
    private static readonly byte[] FileBytes = Enumerable.Range(0, 96).Select(i => (byte)(i * 7)).ToArray();
    private static readonly string FileBase64 = Convert.ToBase64String(FileBytes);

    private readonly List<Activity> _activities = [];
    private readonly List<(string Instrument, double Value, Dictionary<string, object?> Tags)> _measurements = [];
    private ActivityListener _activityListener = null!;
    private MeterListener _meterListener = null!;

    [SetUp]
    public void SetUp()
    {
        _activities.Clear();
        _measurements.Clear();
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == GenAiTelemetry.SourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == GenAiTelemetry.SourceName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        _meterListener.Start();
    }

    [TearDown]
    public void TearDown()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    [Test]
    public async Task Successful_extraction_emits_client_span_with_model_and_token_usage()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new JsonObject
            {
                ["id"] = "resp_123",
                ["model"] = "test-model-2026-01-01",
                ["status"] = "completed",
                ["usage"] = new JsonObject { ["input_tokens"] = 120, ["output_tokens"] = 30 },
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
            })
        });

        await ExtractAsync(client.Client);

        var activity = _activities.ShouldHaveSingleItem();
        activity.DisplayName.ShouldBe($"chat {Model}");
        activity.Kind.ShouldBe(ActivityKind.Client);
        activity.Status.ShouldNotBe(ActivityStatusCode.Error);
        activity.GetTagItem("gen_ai.operation.name").ShouldBe("chat");
        activity.GetTagItem("gen_ai.provider.name").ShouldBe("openai");
        activity.GetTagItem("gen_ai.request.model").ShouldBe(Model);
        activity.GetTagItem("gen_ai.response.model").ShouldBe("test-model-2026-01-01");
        activity.GetTagItem("gen_ai.response.id").ShouldBe("resp_123");
        activity.GetTagItem("gen_ai.usage.input_tokens").ShouldBe(120L);
        activity.GetTagItem("gen_ai.usage.output_tokens").ShouldBe(30L);
        activity.GetTagItem("skestock.extraction.result_type").ShouldBe(nameof(CategoryExtractionResult));

        var tokens = _measurements.Where(m => m.Instrument == "gen_ai.client.token.usage").ToList();
        tokens.Count.ShouldBe(2);
        tokens.Single(m => Equals(m.Tags["gen_ai.token.type"], "input")).Value.ShouldBe(120);
        tokens.Single(m => Equals(m.Tags["gen_ai.token.type"], "output")).Value.ShouldBe(30);
        tokens.ShouldAllBe(m => Equals(m.Tags["gen_ai.request.model"], Model));

        var duration = _measurements.Single(m => m.Instrument == "gen_ai.client.operation.duration");
        duration.Value.ShouldBeGreaterThanOrEqualTo(0);
        duration.Tags.ShouldNotContainKey("error.type");

        AssertNoPromptOrFileData();
    }

    [Test]
    public async Task Server_error_marks_span_failed_with_status_code_and_still_throws_transient()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent($"upstream failure echoing {Prompt}")
        });

        await Should.ThrowAsync<TransientExtractionException>(() => ExtractAsync(client.Client));

        var activity = _activities.ShouldHaveSingleItem();
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem("error.type").ShouldBe("500");

        var duration = _measurements.Single(m => m.Instrument == "gen_ai.client.operation.duration");
        duration.Tags["error.type"].ShouldBe("500");
        _measurements.ShouldNotContain(m => m.Instrument == "gen_ai.client.token.usage");

        AssertNoPromptOrFileData();
    }

    private void AssertNoPromptOrFileData()
    {
        var values = _activities
            .SelectMany(activity => activity.TagObjects.Select(tag => tag.Value?.ToString())
                .Append(activity.StatusDescription)
                .Append(activity.DisplayName))
            .Concat(_measurements.SelectMany(m => m.Tags.Values.Select(value => value?.ToString())))
            .OfType<string>()
            .ToList();

        values.ShouldNotBeEmpty();
        values.ShouldAllBe(value => !value.Contains("confidential") && !value.Contains(FileBase64));
    }

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            copy[tag.Key] = tag.Value;
        }

        _measurements.Add((instrument.Name, value, copy));
    }

    private static async Task ExtractAsync(OpenAiDocumentExtractionClient client)
    {
        await using var stream = new MemoryStream(FileBytes);
        await client.ExtractAsync<CategoryExtractionResult>(stream, "application/pdf", CancellationToken.None);
    }

    private static TestClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(respond))
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IExtractionSchemaFactory<CategoryExtractionResult>, TestSchemaFactory>()
            .BuildServiceProvider();
        var client = new OpenAiDocumentExtractionClient(
            httpClient,
            serviceProvider,
            Options.Create(new OpenAiApiSettings { Model = Model, ApiKey = "test-key" }));

        return new TestClient(client, httpClient, serviceProvider);
    }

    private sealed class TestClient(
        OpenAiDocumentExtractionClient client,
        HttpClient httpClient,
        ServiceProvider serviceProvider) : IDisposable
    {
        public OpenAiDocumentExtractionClient Client { get; } = client;

        public void Dispose()
        {
            httpClient.Dispose();
            serviceProvider.Dispose();
        }
    }

    private sealed class TestSchemaFactory : IExtractionSchemaFactory<CategoryExtractionResult>
    {
        public string Prompt => OpenAiDocumentExtractionTelemetryTests.Prompt;

        public Task<JsonObject> Create(CancellationToken cancellationToken) =>
            Task.FromResult(new JsonObject { ["type"] = "object" });
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
