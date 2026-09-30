using Microsoft.Data.SqlClient;
using skestock.Infrastructure.Data;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class DatabaseProvisionerTests
{
    private const string AppPassword = "App-Pa55w0rd!x'y";
    private const string MigratorPassword = "Migr-Pa55w0rd!z";

    private string _database = null!;
    private DatabaseLogin _app = null!;
    private DatabaseLogin _migrator = null!;

    [SetUp]
    public void SetUp()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        _database = $"prov_{suffix}";
        _app = new DatabaseLogin($"prov_app_{suffix}", AppPassword);
        _migrator = new DatabaseLogin($"prov_mig_{suffix}", MigratorPassword);
    }

    [TearDown]
    public async Task TearDown()
    {
        await using var connection = await OpenAsync(Admin("master"));
        await ExecuteAsync(connection, $"""
            IF DB_ID(N'{_database}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_database}];
            END
            IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{_app.Name}') DROP LOGIN [{_app.Name}];
            IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'{_migrator.Name}') DROP LOGIN [{_migrator.Name}];
            """);
    }

    [Test]
    public async Task App_login_can_read_and_write_but_not_change_schema()
    {
        await ProvisionAsync();
        await using (var migrator = await OpenAsync(As(_migrator)))
        {
            await ExecuteAsync(migrator, "CREATE TABLE dbo.Probe (Id int NOT NULL, Name nvarchar(50) NOT NULL);");
        }

        await using var app = await OpenAsync(As(_app));
        await ExecuteAsync(app, "INSERT INTO dbo.Probe (Id, Name) VALUES (1, N'a');");
        await ExecuteAsync(app, "UPDATE dbo.Probe SET Name = N'b' WHERE Id = 1;");
        (await ScalarAsync(app, "SELECT Name FROM dbo.Probe WHERE Id = 1;")).ShouldBe("b");
        await ExecuteAsync(app, "DELETE FROM dbo.Probe WHERE Id = 1;");

        await Should.ThrowAsync<SqlException>(() => ExecuteAsync(app, "CREATE TABLE dbo.Denied (Id int);"));
        await Should.ThrowAsync<SqlException>(() => ExecuteAsync(app, "ALTER TABLE dbo.Probe ADD Extra int NULL;"));
        await Should.ThrowAsync<SqlException>(() => ExecuteAsync(app, "DROP TABLE dbo.Probe;"));
    }

    [Test]
    public async Task Migrator_login_can_change_schema()
    {
        await ProvisionAsync();

        await using var migrator = await OpenAsync(As(_migrator));
        await ExecuteAsync(migrator, "CREATE TABLE dbo.Probe (Id int); ALTER TABLE dbo.Probe ADD Extra int NULL;");
    }

    [Test]
    public async Task Provisioning_twice_is_idempotent_and_applies_password_changes()
    {
        await ProvisionAsync();
        var rotated = _app with { Password = "Rotated-Pa55w0rd!1" };

        await DatabaseProvisioner.ProvisionAsync(Admin().ConnectionString, _database, rotated, _migrator);

        await using var connection = await OpenAsync(As(rotated));
        (await ScalarAsync(connection, "SELECT 1;")).ShouldBe(1);
        await Should.ThrowAsync<SqlException>(() => OpenAsync(As(_app)));
    }

    [Test]
    public async Task App_login_that_was_db_owner_loses_the_role()
    {
        await ProvisionAsync();
        await using (var admin = await OpenAsync(Admin(_database)))
        {
            await ExecuteAsync(admin, $"ALTER ROLE db_owner ADD MEMBER [{_app.Name}];");
        }

        await ProvisionAsync();

        await using var app = await OpenAsync(As(_app));
        await Should.ThrowAsync<SqlException>(() => ExecuteAsync(app, "CREATE TABLE dbo.Denied (Id int);"));
    }

    [TestCase("bad name")]
    [TestCase("x];DROP LOGIN sa;--")]
    public async Task Invalid_identifiers_are_rejected(string name)
    {
        await Should.ThrowAsync<ArgumentException>(() => DatabaseProvisioner.ProvisionAsync(
            Admin().ConnectionString, _database, new DatabaseLogin(name, AppPassword), _migrator));
    }

    private Task ProvisionAsync() =>
        DatabaseProvisioner.ProvisionAsync(Admin().ConnectionString, _database, _app, _migrator);

    private static SqlConnectionStringBuilder Admin(string? database = null)
    {
        var builder = new SqlConnectionStringBuilder(IntegrationTestSetup.DatabaseConnectionString);
        if (database is not null)
        {
            builder.InitialCatalog = database;
        }

        return builder;
    }

    private SqlConnectionStringBuilder As(DatabaseLogin login)
    {
        var builder = Admin(_database);
        builder.UserID = login.Name;
        builder.Password = login.Password;
        builder.Pooling = false;
        return builder;
    }

    private static async Task<SqlConnection> OpenAsync(SqlConnectionStringBuilder builder)
    {
        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }
}
