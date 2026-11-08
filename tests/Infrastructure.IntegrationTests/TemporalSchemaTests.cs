using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace skestock.Infrastructure.IntegrationTests;

public sealed class TemporalSchemaTests
{
    [Test]
    public async Task Temporal_tables_match_the_model_with_two_year_retention_and_a_history_index()
    {
        await using var dbContext = IntegrationTestSetup.CreateDbContext();
        var expected = dbContext.Model.GetEntityTypes()
            .Where(t => t.IsTemporal())
            .Select(t => t.GetTableName()!)
            .Distinct()
            .Order()
            .ToList();
        expected.Count.ShouldBeGreaterThan(30);

        await using var connection = new SqlConnection(IntegrationTestSetup.DatabaseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.name, h.name, t.history_retention_period, t.history_retention_period_unit_desc,
                   (SELECT COUNT(*) FROM sys.indexes i
                    WHERE i.object_id = h.object_id AND i.type_desc = 'NONCLUSTERED' AND i.name LIKE 'IX[_]%')
            FROM sys.tables t
            JOIN sys.tables h ON h.object_id = t.history_table_id
            WHERE t.temporal_type = 2
            """;

        var actual = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var table = reader.GetString(0);
            actual.Add(table);
            reader.GetInt32(2).ShouldBe(2, $"{table} retention");
            reader.GetString(3).ShouldBe("YEAR", $"{table} retention unit");
            reader.GetInt32(4).ShouldBeGreaterThanOrEqualTo(1, $"{table} history index");
        }

        actual.Order().ToList().ShouldBe(expected);
    }
}
