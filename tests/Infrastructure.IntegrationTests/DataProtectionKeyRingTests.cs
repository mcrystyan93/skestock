using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using skestock.Infrastructure;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class DataProtectionKeyRingTests
{
    private const string ProtectorPurpose = "skestock.Infrastructure.IntegrationTests.Cookie";

    [Test]
    public void AddWebAuthenticationServices_RequiresKeyDirectoryInProduction()
    {
        var builder = CreateApplicationBuilder();

        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.AddWebAuthenticationServices());

        exception!.Message.ShouldContain("DataProtection:KeysDirectory");
    }

    [Test]
    public void AddWebAuthenticationServices_ReusesKeyRingAcrossHostRestarts()
    {
        var keysDirectory = Path.Combine(
            Path.GetTempPath(),
            $"skestock-data-protection-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keysDirectory);

        try
        {
            string protectedPayload;
            using (var firstHost = CreateHost(keysDirectory))
            {
                var protector = firstHost.Services
                    .GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector(ProtectorPurpose);
                protectedPayload = protector.Protect("authenticated");
            }

            using var secondHost = CreateHost(keysDirectory);
            var secondProtector = secondHost.Services
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(ProtectorPurpose);

            secondProtector.Unprotect(protectedPayload).ShouldBe("authenticated");
        }
        finally
        {
            Directory.Delete(keysDirectory, recursive: true);
        }
    }

    private static HostApplicationBuilder CreateApplicationBuilder()
    {
        return Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
    }

    private static IHost CreateHost(string keysDirectory)
    {
        var builder = CreateApplicationBuilder();
        builder.Configuration["DataProtection:KeysDirectory"] = keysDirectory;
        builder.AddWebAuthenticationServices();
        return builder.Build();
    }
}
