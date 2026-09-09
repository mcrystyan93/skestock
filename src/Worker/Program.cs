using skestock.Application;
using skestock.Application.Common.Interfaces;
using skestock.Infrastructure;
using skestock.ServiceDefaults;
using Worker.Queues;
using Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.AddInfrastructureServices();

// The Worker has no HTTP context, so IUser is supplied per message from the queue envelope.
// A single scoped AmbientUser instance is resolvable both as itself (for the processor to
// populate) and as IUser (for handlers/behaviours to consume).
builder.Services.AddScoped<AmbientUser>();
builder.Services.AddScoped<IUser>(sp => sp.GetRequiredService<AmbientUser>());

builder.Services.AddHostedService<GoodsReceiptImportQueueProcessingService>();
builder.Services.AddHostedService<CategoryImportQueueProcessingService>();
builder.Services.AddHostedService<ItemImportQueueProcessingService>();

var host = builder.Build();
host.Run();
