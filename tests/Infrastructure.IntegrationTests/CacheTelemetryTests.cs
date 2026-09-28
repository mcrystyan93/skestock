using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using skestock.ServiceDefaults;
using ZiggyCreatures.Caching.Fusion;

namespace skestock.Infrastructure.IntegrationTests;

[NonParallelizable]
public sealed class CacheTelemetryTests
{
    [Test]
    public async Task HybridCacheCalls_AreExportedAsFusionCacheSpansAndMetrics_WithoutKeysInMetricTags()
    {
        var spans = new CollectingProcessor();
        var metrics = new CollectingMetricExporter();

        using var host = InfrastructureHost.Build(builder =>
        {
            builder.ConfigureOpenTelemetry();
            builder.Services.ConfigureOpenTelemetryTracerProvider(t => t.AddProcessor(spans));
            builder.Services.ConfigureOpenTelemetryMeterProvider(m =>
                m.AddReader(new BaseExportingMetricReader(metrics)));
        });
        var tracerProvider = host.Services.GetRequiredService<TracerProvider>();
        var meterProvider = host.Services.GetRequiredService<MeterProvider>();

        var key = $"cache-telemetry-{Guid.NewGuid():N}";
        var cache = host.Services.GetRequiredService<HybridCache>();
        await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult("value"));
        await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult("value"));

        tracerProvider.ForceFlush();
        meterProvider.ForceFlush();

        spans.Sources.ShouldContain(FusionCacheDiagnostics.ActivitySourceName);
        metrics.MeterNames.ShouldContain(FusionCacheDiagnostics.MeterName);
        metrics.TagValues.ShouldNotContain(value => value.Contains(key));
    }

    private sealed class CollectingProcessor : BaseProcessor<Activity>
    {
        public HashSet<string> Sources { get; } = [];

        public override void OnEnd(Activity data)
        {
            lock (Sources) Sources.Add(data.Source.Name);
        }
    }

    private sealed class CollectingMetricExporter : BaseExporter<Metric>
    {
        public HashSet<string> MeterNames { get; } = [];
        public List<string> TagValues { get; } = [];

        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
            {
                MeterNames.Add(metric.MeterName);
                foreach (ref readonly var point in metric.GetMetricPoints())
                {
                    foreach (var tag in point.Tags)
                    {
                        TagValues.Add(tag.Value?.ToString() ?? string.Empty);
                    }
                }
            }

            return ExportResult.Success;
        }
    }
}
