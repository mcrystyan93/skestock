using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using skestock.Infrastructure.Data;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class DatabaseMigrationRunnerTests
{
    private const string AppPassword = "Runner-App-Pa55w0rd!1";
    private const string MigratorPassword = "Runner-Migr-Pa55w0rd!1";

    [TearDown]
    public async Task TearDown()
    {
        var builder = new SqlConnectionStringBuilder(IntegrationTestSetup.DatabaseConnectionString);
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{Services.DatabaseAppLogin}')
                DROP USER [{Services.DatabaseAppLogin}];
            IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{Services.DatabaseMigratorLogin}')
                DROP USER [{Services.DatabaseMigratorLogin}];
            """;
        await command.ExecuteNonQueryAsync();

        builder.InitialCatalog = "master";
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await using var dropLogins = master.CreateCommand();
        dropLogins.CommandText = $"""
            IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{Services.DatabaseAppLogin}')
                DROP LOGIN [{Services.DatabaseAppLogin}];
            IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{Services.DatabaseMigratorLogin}')
                DROP LOGIN [{Services.DatabaseMigratorLogin}];
            """;
        await dropLogins.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task Run_on_an_already_migrated_database_gives_the_app_login_data_access_only()
    {
        await DatabaseMigrationRunner.RunAsync(Configuration(), NullLogger.Instance);
        await DatabaseMigrationRunner.RunAsync(Configuration(), NullLogger.Instance);

        var app = new SqlConnectionStringBuilder(IntegrationTestSetup.DatabaseConnectionString)
        {
            UserID = Services.DatabaseAppLogin,
            Password = AppPassword,
            Pooling = false
        };
        await using var connection = new SqlConnection(app.ConnectionString);
        await connection.OpenAsync();

        await using var read = connection.CreateCommand();
        read.CommandText = "SELECT COUNT(*) FROM dbo.Items;";
        (await read.ExecuteScalarAsync()).ShouldNotBeNull();

        await using var ddl = connection.CreateCommand();
        ddl.CommandText = "CREATE TABLE dbo.RunnerDenied (Id int);";
        await Should.ThrowAsync<SqlException>(() => ddl.ExecuteNonQueryAsync());
    }

    [Test]
    public async Task Run_without_passwords_fails()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{Services.Database}"] = IntegrationTestSetup.DatabaseConnectionString
            })
            .Build();

        await Should.ThrowAsync<ArgumentException>(() =>
            DatabaseMigrationRunner.RunAsync(configuration, NullLogger.Instance));
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Services.Database}"] = IntegrationTestSetup.DatabaseConnectionString,
            [$"{Services.DatabaseMigrationSettings}:{Services.DatabaseAppPassword}"] = AppPassword,
            [$"{Services.DatabaseMigrationSettings}:{Services.DatabaseMigratorPassword}"] = MigratorPassword
        })
        .Build();
}
