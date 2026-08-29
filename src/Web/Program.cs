using skestock.Infrastructure.Data;
using Scalar.AspNetCore;
using skestock.Application;
using skestock.Infrastructure;
using skestock.ServiceDefaults;
using skestock.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
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

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });

app.Map("/", () => Results.Redirect("/scalar"));

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);


app.Run();
