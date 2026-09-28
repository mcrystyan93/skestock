using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using skestock.ServiceDefaults;

namespace skestock.Application.FunctionalTests.Health;

public class HealthEndpointTests
{
    [Test]
    public async Task Health_WithAllDependenciesUp_ReturnsHealthyAnonymously()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Test]
    public async Task Alive_ReturnsHealthyAnonymously()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();

        var response = await client.GetAsync("/alive");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Test]
    public async Task Health_InProductionWithAFailingCheck_Returns503WithoutDetails()
    {
        const string checkName = "secret-dependency";
        const string failureText = "connection refused to 10.0.0.5";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.AddDefaultHealthChecks();
        builder.Services.AddHealthChecks()
            .AddCheck(checkName, () => HealthCheckResult.Unhealthy(failureText, new InvalidOperationException(failureText)));

        await using var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();
        using var client = app.GetTestClient();

        var health = await client.GetAsync("/health");
        var alive = await client.GetAsync("/alive");

        health.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var body = await health.Content.ReadAsStringAsync();
        body.ShouldBe("Unhealthy");
        body.ShouldNotContain(checkName);
        body.ShouldNotContain(failureText);
        alive.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TestCase("/health", true)]
    [TestCase("/alive", true)]
    [TestCase("/HEALTH", true)]
    [TestCase("/api/items", false)]
    [TestCase("/healthy-items", false)]
    public void IsHealthEndpoint_MatchesOnlyHealthRoutes(string path, bool expected)
    {
        Extensions.IsHealthEndpoint(new PathString(path)).ShouldBe(expected);
    }
}
