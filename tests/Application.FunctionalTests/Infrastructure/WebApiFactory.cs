using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace skestock.Application.FunctionalTests.Infrastructure;

public class WebApiFactory(string connectionString, string cacheConnectionString) : WebApplicationFactory<Program>
{
    private const string TestStorageConnectionString = "UseDevelopmentStorage=true";
    private const string TestOpenAiApiKey = "functional-test-api-key";
    private const string TestOpenAiModel = "functional-test-model";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseSetting("ConnectionStrings:skestockDb", connectionString)
            .UseSetting($"ConnectionStrings:{skestock.Shared.Services.Cache}", cacheConnectionString)
            .UseSetting($"ConnectionStrings:{skestock.Shared.Services.BlobService}", TestStorageConnectionString)
            .UseSetting($"ConnectionStrings:{skestock.Shared.Services.Queues}", TestStorageConnectionString)
            .UseSetting("OpenApiSettings:ApiKey", TestOpenAiApiKey)
            .UseSetting("OpenApiSettings:Model", TestOpenAiModel);

        builder.ConfigureTestServices(services =>
        {
            services
                .RemoveAll<IUser>()
                .AddTransient(provider =>
                {
                    var mock = new Mock<IUser>();
                    mock.SetupGet(x => x.Roles).Returns(TestApp.GetRoles());
                    mock.SetupGet(x => x.Id).Returns(TestApp.GetUserId());
                    return mock.Object;
                });

            services
                .RemoveAll<IBlobStorageService>()
                .AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }
}
