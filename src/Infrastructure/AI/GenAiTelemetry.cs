using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace skestock.Infrastructure.AI;

/// <summary>
/// Traces and metrics for OpenAI calls, following the OpenTelemetry GenAI semantic conventions.
/// </summary>
/// <remarks>
/// Only metadata is recorded (model, token counts, outcome). Prompts, schemas, file names, file
/// data and model output must never be attached to spans or metrics: documents may contain
/// personal data.
/// </remarks>
public static class GenAiTelemetry
{
    public const string SourceName = "skestock.AI";

    public const string OperationNameTag = "gen_ai.operation.name";
    public const string ProviderNameTag = "gen_ai.provider.name";
    public const string RequestModelTag = "gen_ai.request.model";
    public const string ResponseModelTag = "gen_ai.response.model";
    public const string ResponseIdTag = "gen_ai.response.id";
    public const string InputTokensTag = "gen_ai.usage.input_tokens";
    public const string OutputTokensTag = "gen_ai.usage.output_tokens";
    public const string TokenTypeTag = "gen_ai.token.type";
    public const string ErrorTypeTag = "error.type";
    public const string ResultTypeTag = "skestock.extraction.result_type";

    public const string ChatOperation = "chat";
    public const string OpenAiProvider = "openai";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName);

    // Bucket boundaries recommended by the GenAI semantic conventions.
    public static readonly Histogram<long> TokenUsage = Meter.CreateHistogram<long>(
        "gen_ai.client.token.usage",
        unit: "{token}",
        description: "Number of input and output tokens used.",
        tags: null,
        advice: new InstrumentAdvice<long>
        {
            HistogramBucketBoundaries =
                [1, 4, 16, 64, 256, 1024, 4096, 16384, 65536, 262144, 1048576, 4194304, 16777216, 67108864]
        });

    public static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        "gen_ai.client.operation.duration",
        unit: "s",
        description: "GenAI operation duration.",
        tags: null,
        advice: new InstrumentAdvice<double>
        {
            HistogramBucketBoundaries =
                [0.01, 0.02, 0.04, 0.08, 0.16, 0.32, 0.64, 1.28, 2.56, 5.12, 10.24, 20.48, 40.96, 81.92]
        });
}
