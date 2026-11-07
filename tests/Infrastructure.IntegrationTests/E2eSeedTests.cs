using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using skestock.Application;
using skestock.Application.Common.Interfaces;
using skestock.Infrastructure.Data;
using skestock.Web.Services;

namespace skestock.Infrastructure.IntegrationTests;

// Each test seeds its own throwaway database on the shared SQL server, so the e2e rows never leak
// into the database the other integration tests use.
public sealed class E2eSeedTests
{
    private string _databaseName = null!;
    private string _connectionString = null!;

    [SetUp]
    public async Task SetUp()
    {
        _databaseName = $"e2eseed_{Guid.NewGuid():N}";
        _connectionString = new SqlConnectionStringBuilder(IntegrationTestSetup.DatabaseConnectionString)
        {
            InitialCatalog = _databaseName
        }.ConnectionString;

        await ExecuteOnMasterAsync($"CREATE DATABASE [{_databaseName}];");
    }

    [TearDown]
    public async Task TearDown()
    {
        SqlConnection.ClearAllPools();
        await ExecuteOnMasterAsync($"""
            IF DB_ID(N'{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END
            """);
    }

    [Test]
    public async Task Seeding_twice_with_the_flag_on_fills_every_in_scope_table_and_adds_nothing_the_second_time()
    {
        var first = await SeedAsync(e2e: true);
        var second = await SeedAsync(e2e: true);

        foreach (var (table, count) in first)
        {
            count.ShouldBeGreaterThan(0, table);
        }

        second.ShouldBe(first);
    }

    [Test]
    public async Task Seeding_with_the_flag_off_adds_no_e2e_rows()
    {
        var counts = await SeedAsync(e2e: false);

        // MessageTemplates come from the model's HasData, so they exist regardless of the flag.
        foreach (var (table, count) in counts.Where(c => c.Key != "MessageTemplates"))
        {
            count.ShouldBe(0, table);
        }
    }

    [Test]
    public async Task Initialising_a_production_host_with_the_flag_on_adds_no_e2e_rows()
    {
        // The demo class exists (Development seed, flag off), so only the IsDevelopment gate stands in the way.
        await SeedAsync(e2e: false);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Services.Database}"] = _connectionString,
            [$"ConnectionStrings:{Services.Cache}"] = IntegrationTestSetup.CacheConnectionString,
            [$"ConnectionStrings:{Services.BlobService}"] = IntegrationTestSetup.BlobConnectionString,
            [$"ConnectionStrings:{Services.Queues}"] = IntegrationTestSetup.QueueConnectionString,
            [$"{Services.OpenApiSettings}:{Services.OpenApiKey}"] = "integration-test-key",
            [$"{Services.OpenApiSettings}:{Services.OpenApiModel}"] = "integration-test-model",
            [$"{Services.SeedAdministrator}:{Services.SeedAdministratorEmail}"] = "e2e-seed-test@skestock.test",
            [$"{Services.SeedAdministrator}:{Services.SeedAdministratorPassword}"] = "E2e-Seed-Pa55w0rd!1",
            [Services.E2EMode] = "true"
        });
        builder.AddApplicationServices();
        builder.AddInfrastructureServices();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUser, CurrentUser>();
        await using var app = builder.Build();

        await app.InitialiseDatabaseAsync(migrate: false);

        await using var scope = app.Services.CreateAsyncScope();
        var counts = await CountAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        foreach (var (table, count) in counts.Where(c => c.Key != "MessageTemplates"))
        {
            count.ShouldBe(0, table);
        }
    }

    private async Task<Dictionary<string, int>> SeedAsync(bool e2e)
    {
        using var host = InfrastructureHost.Build(builder =>
        {
            builder.AddApplicationServices();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IUser, CurrentUser>();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{Services.SeedAdministrator}:{Services.SeedAdministratorEmail}"] = "e2e-seed-test@skestock.test",
                [$"{Services.SeedAdministrator}:{Services.SeedAdministratorPassword}"] = "E2e-Seed-Pa55w0rd!1",
                [Services.E2EMode] = e2e ? "true" : "false"
            });
        }, _connectionString);

        await using var scope = host.Services.CreateAsyncScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
        await initialiser.SeedDemoClassAsync();
        await initialiser.SeedE2eAsync();

        return await CountAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    private static async Task<Dictionary<string, int>> CountAsync(ApplicationDbContext db)
    {
        return new Dictionary<string, int>
        {
            ["Congregations"] = await db.Congregations.CountAsync(),
            ["MessageTemplates"] = await db.MessageTemplates.CountAsync(),
            ["SharedClassConfigurations"] = await db.SharedClassConfigurations.CountAsync(),
            ["ClassItemStockVisibilities"] = await db.ClassItemStockVisibilities.CountAsync(),
            ["OrderLists"] = await db.OrderLists.CountAsync(),
            ["OrderListLines"] = await db.OrderListLines.CountAsync(),
            ["SupplyLists"] = await db.SupplyLists.CountAsync(),
            ["SupplyListLines"] = await db.SupplyListLines.CountAsync(),
            ["FruitDessertPlanEntries"] = await db.FruitDessertPlanEntries.CountAsync(),
            ["FruitDessertMessages"] = await db.FruitDessertMessages.CountAsync(),
            ["FileMetadata"] = await db.FileMetadata.CountAsync(),
            ["CategoryImportBatches"] = await db.CategoryImportBatches.CountAsync(),
            ["CategoryImportBatchFiles"] = await db.CategoryImportBatchFiles.CountAsync(),
            ["ItemImportBatches"] = await db.ItemImportBatches.CountAsync(),
            ["ItemImportBatchFiles"] = await db.ItemImportBatchFiles.CountAsync(),
            ["GoodsReceiptImports"] = await db.GoodsReceiptImports.CountAsync(),
            ["GoodsReceiptImportLines"] = await db.GoodsReceiptImportLines.CountAsync()
        };
    }

    private static async Task ExecuteOnMasterAsync(string sql)
    {
        var builder = new SqlConnectionStringBuilder(IntegrationTestSetup.DatabaseConnectionString)
        {
            InitialCatalog = "master"
        };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
