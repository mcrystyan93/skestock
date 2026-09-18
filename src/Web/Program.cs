using skestock.Infrastructure.Data;
using Scalar.AspNetCore;
using skestock.Application;
using skestock.Infrastructure;
using skestock.Infrastructure.Realtime;
using skestock.ServiceDefaults;
using skestock.Web;

var builder = WebApplication.CreateBuilder(args);

static void DisableClientCaching(HttpResponse response)
{
    response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    response.Headers.Pragma = "no-cache";
    response.Headers.Expires = "0";
}

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebAuthenticationServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    await app.InitialiseDatabaseAsync();
}

app.UseHttpsRedirection();

// AllowAnyOrigin() is incompatible with AllowCredentials() (required for cookie-based auth) —
// origins must be explicit. In Aspire, the Angular frontend's assigned origin is injected via
// the "Cors:AllowedOrigins" configuration section (see AppHost/Program.cs); a localhost fallback
// keeps `dotnet run` on Web alone usable outside Aspire orchestration.
var corsAllowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsAllowedOrigins is not { Length: > 0 } && app.Environment.IsDevelopment())
{
    corsAllowedOrigins = ["https://localhost:4200", "http://localhost:4200"];
}

app.UseCors(policy => policy
    .WithOrigins(corsAllowedOrigins ?? [])
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseAuthentication();
app.UseAuthorization();

// Must run after UseAuthentication/UseAuthorization so HttpContext.User is populated —
// antiforgery tokens are bound to the current principal.
// app.UseAntiforgeryValidation();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            DisableClientCaching(context.Context.Response);
        }
    }
});

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/login"));
app.MapHub<AppHub>("/hubs/app").RequireAuthorization();
app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);
app.MapFallback(async (HttpContext context) =>
{
    if (context.Request.Path.StartsWithSegments("/api")
        || context.Request.Path.StartsWithSegments("/scalar")
        || context.Request.Path.StartsWithSegments("/hubs"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html";
    DisableClientCaching(context.Response);
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath!, "index.html"));
});

app.Run();
