using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace skestock.Infrastructure.Data;

public sealed record DatabaseLogin(string Name, string Password);

/// <summary>
/// Creates the application database and two least-privilege logins, idempotently:
/// the app login can only read/write data, the migrator login owns the schema.
/// Passwords are re-applied on every run so rotating a secret is a redeploy.
/// </summary>
public static partial class DatabaseProvisioner
{
    public static async Task ProvisionAsync(
        string adminConnectionString,
        string databaseName,
        DatabaseLogin app,
        DatabaseLogin migrator,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(adminConnectionString);
        ValidateIdentifier(databaseName);
        ValidateIdentifier(app.Name);
        ValidateIdentifier(migrator.Name);
        Guard.Against.NullOrWhiteSpace(app.Password);
        Guard.Against.NullOrWhiteSpace(migrator.Password);
        if (string.Equals(app.Name, migrator.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The app and migrator logins must be different.");
        }

        var master = new SqlConnectionStringBuilder(adminConnectionString) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, $"""
                IF DB_ID(N'{databaseName}') IS NULL CREATE DATABASE [{databaseName}];
                """, cancellationToken);
            await ExecuteAsync(connection, UpsertLogin(app), cancellationToken);
            await ExecuteAsync(connection, UpsertLogin(migrator), cancellationToken);
        }

        var database = new SqlConnectionStringBuilder(adminConnectionString) { InitialCatalog = databaseName };
        await using (var connection = new SqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteAsync(connection, $"""
                {CreateUser(app.Name)}
                {CreateUser(migrator.Name)}
                IF IS_ROLEMEMBER(N'db_owner', N'{app.Name}') = 1 ALTER ROLE db_owner DROP MEMBER [{app.Name}];
                ALTER ROLE db_datareader ADD MEMBER [{app.Name}];
                ALTER ROLE db_datawriter ADD MEMBER [{app.Name}];
                ALTER ROLE db_owner ADD MEMBER [{migrator.Name}];
                """, cancellationToken);
        }
    }

    // CREATE/ALTER LOGIN do not accept parameters for the password, so it is escaped as a literal.
    private static string UpsertLogin(DatabaseLogin login)
    {
        var password = login.Password.Replace("'", "''");
        return $"""
            IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{login.Name}')
                CREATE LOGIN [{login.Name}] WITH PASSWORD = N'{password}';
            ELSE
                ALTER LOGIN [{login.Name}] WITH PASSWORD = N'{password}';
            """;
    }

    private static string CreateUser(string name) => $"""
        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{name}')
            CREATE USER [{name}] FOR LOGIN [{name}];
        """;

    private static async Task ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ValidateIdentifier(string value)
    {
        Guard.Against.NullOrWhiteSpace(value);
        if (!IdentifierPattern().IsMatch(value))
        {
            throw new ArgumentException($"'{value}' is not a valid SQL identifier (letters, digits, underscore).");
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]{0,127}$")]
    private static partial Regex IdentifierPattern();
}
