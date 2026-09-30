using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skestock.Infrastructure.Data;

/// <summary>
/// One-shot deployment step: provisions the least-privilege logins as the admin login taken from
/// <c>ConnectionStrings:skestockDb</c>, then applies EF migrations as the migrator login.
/// </summary>
public static class DatabaseMigrationRunner
{
    private const int MaxConnectAttempts = 60;

    public static async Task RunAsync(
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var adminConnectionString = configuration.GetConnectionString(Services.Database);
        Guard.Against.NullOrWhiteSpace(adminConnectionString,
            message: $"Connection string '{Services.Database}' not found.");
        var appPassword = configuration[$"{Services.DatabaseMigrationSettings}:{Services.DatabaseAppPassword}"];
        var migratorPassword = configuration[$"{Services.DatabaseMigrationSettings}:{Services.DatabaseMigratorPassword}"];
        Guard.Against.NullOrWhiteSpace(appPassword,
            message: $"'{Services.DatabaseMigrationSettings}:{Services.DatabaseAppPassword}' is required.");
        Guard.Against.NullOrWhiteSpace(migratorPassword,
            message: $"'{Services.DatabaseMigrationSettings}:{Services.DatabaseMigratorPassword}' is required.");

        await WaitForServerAsync(adminConnectionString, logger, cancellationToken);

        logger.LogInformation("Provisioning database '{Database}' logins.", Services.Database);
        await DatabaseProvisioner.ProvisionAsync(
            adminConnectionString,
            Services.Database,
            new DatabaseLogin(Services.DatabaseAppLogin, appPassword),
            new DatabaseLogin(Services.DatabaseMigratorLogin, migratorPassword),
            cancellationToken);

        var migratorConnectionString = new SqlConnectionStringBuilder(adminConnectionString)
        {
            InitialCatalog = Services.Database,
            UserID = Services.DatabaseMigratorLogin,
            Password = migratorPassword,
            Pooling = false
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(migratorConnectionString)
            .Options;
        await using var context = new ApplicationDbContext(options);

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
        logger.LogInformation("Applying {Count} pending migration(s).", pending);
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database is up to date.");
    }

    // The compose dependency only guarantees the container started, not that SQL Server accepts logins yet.
    private static async Task WaitForServerAsync(
        string connectionString,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (SqlException ex) when (attempt < MaxConnectAttempts)
            {
                logger.LogInformation("SQL Server not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
