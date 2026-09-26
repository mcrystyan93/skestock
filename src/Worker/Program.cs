using skestock.Application;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;
using skestock.Infrastructure;
using skestock.ServiceDefaults;
using Worker.Queues;
using Worker.Services;
using Worker.Statistics;
using Microsoft.Extensions.Options;
using System.Globalization;
using skestock.Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.AddInfrastructureServices();

// The Worker has no HTTP context, so IUser is supplied per message from the queue envelope.
// A single scoped AmbientUser instance is resolvable both as itself (for the processor to
// populate) and as IUser (for handlers/behaviours to consume).
builder.Services.AddScoped<AmbientUser>();
builder.Services.AddScoped<IUser>(sp => sp.GetRequiredService<AmbientUser>());
builder.Services.AddScoped<IQueueMessageProcessor, QueueMessageProcessor>();

builder.Services
    .AddOptions<DailyStatisticsOptions>()
    .BindConfiguration(Services.DailyStatisticsSettings)
    .Validate(
        options => TimeOnly.TryParseExact(
            options.RunAt,
            DailyStatisticsOptions.RunAtFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _),
        "DailyStatistics:RunAt must use the HH:mm format.")
    .Validate(
        options =>
        {
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }
        },
        "DailyStatistics:TimeZone must identify a valid system time zone.")
    .Validate(
        options => options.MaxBackfillDays is >= 1 and <= MaterializeDailyConsumptionCommand.MaxDays,
        $"DailyStatistics:MaxBackfillDays must be between 1 and {MaterializeDailyConsumptionCommand.MaxDays}.")
    .ValidateOnStart();

builder.Services.AddHostedService<GoodsReceiptImportQueueProcessingService>();
builder.Services.AddHostedService<CategoryImportBatchQueueProcessingService>();
builder.Services.AddHostedService<ItemImportQueueProcessingService>();
builder.Services.AddHostedService<DailyStatisticsService>();

var host = builder.Build();
host.Run();
