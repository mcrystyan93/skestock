using System.Data;
using System.Data.Common;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

namespace skestock.Infrastructure.Data;

/// <summary>
/// Reads row history from any temporal table. The queryable tables and columns come from the EF relational model
/// (an allowlist), never from the request: the request only selects an entry, and ids are SQL parameters.
/// </summary>
public sealed class EntityHistoryReader(ApplicationDbContext dbContext) : IEntityHistoryReader
{
    // Authenticator keys and recovery codes: never readable through history.
    private static readonly HashSet<string> DeniedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "AspNetUserTokens"
    };

    private static readonly HashSet<string> DeniedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "ProviderKey"
    };

    // CreatedDate is set in the app a moment before the transaction that stamps PeriodStart.
    private static readonly TimeSpan CreatedTolerance = TimeSpan.FromMinutes(1);

    private static Dictionary<string, TemporalTable>? _tables;

    private sealed record TemporalTable(
        string Name, string Schema, string HistoryName, string HistorySchema,
        string KeyColumn, Type KeyType, string PeriodStart, string PeriodEnd, IReadOnlyList<string> Columns);

    public async Task<Result<EntityHistoryPage>> ReadAsync(
        string entity, string id, int skip, int take, CancellationToken cancellationToken)
    {
        if (!Tables().TryGetValue(entity, out var table))
            return Result.Fail(new EntityHistoryErrors.UnknownEntity(entity));

        if (!TryParseId(table.KeyType, id, out var key))
            return Result.Fail(new EntityHistoryErrors.InvalidEntityId(entity, id));

        var sql = dbContext.GetService<ISqlGenerationHelper>();
        var columns = string.Join(", ", table.Columns.Select(sql.DelimitIdentifier)
            .Append(sql.DelimitIdentifier(table.PeriodStart)).Append(sql.DelimitIdentifier(table.PeriodEnd)));
        var keyColumn = sql.DelimitIdentifier(table.KeyColumn);
        var start = sql.DelimitIdentifier(table.PeriodStart);
        var end = sql.DelimitIdentifier(table.PeriodEnd);
        // Versions of one key can tie on both period columns (several saves in one transaction), so every column
        // breaks the tie: tied rows are then identical in content and offset paging is deterministic.
        var orderTail = string.Join(", ", table.Columns.Select(sql.DelimitIdentifier));

        // UNION ALL of the current and history table instead of FOR SYSTEM_TIME ALL, so zero-duration versions
        // (a delete stamp written in the same transaction as the DELETE) are returned too.
        var commandText = $"""
            SELECT {columns} FROM {sql.DelimitIdentifier(table.Name, table.Schema)} WHERE {keyColumn} = @id
            UNION ALL
            SELECT {columns} FROM {sql.DelimitIdentifier(table.HistoryName, table.HistorySchema)} WHERE {keyColumn} = @id
            ORDER BY {start} DESC, {end} DESC, {orderTail}
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """;

        var rows = new List<Dictionary<string, object?>>();
        var connection = dbContext.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            AddParameter(command, "@id", key);
            AddParameter(command, "@skip", skip);
            AddParameter(command, "@take", take + 1);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(reader.FieldCount);
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }
        }
        finally
        {
            if (closeAfter)
                await dbContext.Database.CloseConnectionAsync();
        }

        var hasNextPage = rows.Count > take;
        if (hasNextPage)
            rows.RemoveAt(rows.Count - 1);

        var names = await ResolveNamesAsync(rows, cancellationToken);
        var versions = rows.Select((row, index) => ToVersion(row, table, skip, index, rows.Count, hasNextPage, names)).ToList();
        return Result.Ok(new EntityHistoryPage(versions, hasNextPage));
    }

    private static EntityHistoryVersionDto ToVersion(
        Dictionary<string, object?> row, TemporalTable table, int skip, int index, int count, bool hasNextPage,
        Dictionary<Guid, List<ProfileName>> names)
    {
        var validFrom = ToUtc((DateTime)row[table.PeriodStart]!);
        var periodEnd = ToUtc((DateTime)row[table.PeriodEnd]!);
        var current = periodEnd.Year == 9999;
        var modifierId = row.GetValueOrDefault(ApplicationDbContext.LastModifiedById) as Guid?;

        // No current row at the top means the entity is gone and the newest version is the stamped pre-delete one.
        // Created only when the oldest surviving version was written at the entity's CreatedDate: otherwise it may
        // be a remainder after retention pruning (or a pre-temporal backfill), and tables without CreatedDate
        // (no audit base) never get the label. Everything else is Updated.
        var isOldest = !hasNextPage && index == count - 1;
        var createdHere = isOldest && row.GetValueOrDefault("CreatedDate") is DateTimeOffset created
                          && (validFrom - created).Duration() <= CreatedTolerance;
        var operation = skip == 0 && index == 0 && !current
            ? EntityHistoryOperation.Deleted
            : createdHere
                ? EntityHistoryOperation.Created
                : EntityHistoryOperation.Updated;

        var values = row
            .Where(pair => pair.Key != table.PeriodStart && pair.Key != table.PeriodEnd
                           && pair.Key != ApplicationDbContext.LastModifiedById)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return new EntityHistoryVersionDto
        {
            ValidFrom = validFrom,
            ValidTo = current ? null : periodEnd,
            Operation = operation,
            ModifiedById = modifierId,
            ModifiedByName = modifierId is { } id ? NameAt(names, id, validFrom) : null,
            Values = values
        };
    }

    private sealed record ProfileName(string Name, DateTimeOffset From, DateTimeOffset To);

    // Names at write time come from the temporal UserProfiles history; the newest profile is the fallback.
    private async Task<Dictionary<Guid, List<ProfileName>>> ResolveNamesAsync(
        List<Dictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Select(r => r.GetValueOrDefault(ApplicationDbContext.LastModifiedById) as Guid?)
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var profiles = await dbContext.UserProfiles.TemporalAll()
            .AsNoTracking()
            .Where(p => ids.Contains(p.IdentityId))
            .Select(p => new
            {
                p.IdentityId,
                p.FirstName,
                p.LastName,
                From = EF.Property<DateTime>(p, "PeriodStart"),
                To = EF.Property<DateTime>(p, "PeriodEnd")
            })
            .ToListAsync(cancellationToken);

        return profiles
            .GroupBy(p => p.IdentityId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(p => p.From)
                    .Select(p => new ProfileName($"{p.FirstName} {p.LastName}".Trim(), ToUtc(p.From), ToUtc(p.To)))
                    .ToList());
    }

    private static string? NameAt(Dictionary<Guid, List<ProfileName>> names, Guid id, DateTimeOffset at)
    {
        if (!names.TryGetValue(id, out var versions))
            return null;
        return (versions.FirstOrDefault(p => p.From <= at && at <= p.To) ?? versions[0]).Name;
    }

    private static DateTimeOffset ToUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static bool TryParseId(Type keyType, string id, out object value)
    {
        value = id;
        if (keyType == typeof(Guid) && Guid.TryParse(id, out var guid))
            value = guid;
        else if (keyType == typeof(int) && int.TryParse(id, out var number))
            value = number;
        else if (keyType != typeof(string))
            return false;
        return true;
    }

    private Dictionary<string, TemporalTable> Tables() => _tables ??= BuildTables();

    private Dictionary<string, TemporalTable> BuildTables()
    {
        var model = dbContext.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        var tables = new Dictionary<string, TemporalTable>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in model.Tables)
        {
            var temporal = table.EntityTypeMappings
                .Select(m => m.TypeBase).OfType<IEntityType>().FirstOrDefault(t => t.IsTemporal());
            // Composite keys (AspNetUserRoles, AspNetUserLogins) have no single id to look up: not readable.
            if (temporal is null || DeniedTables.Contains(table.Name)
                || table.PrimaryKey is not { Columns.Count: 1 } primaryKey)
                continue;

            var key = primaryKey.Columns[0];
            var schema = table.Schema ?? "dbo";
            tables[table.Name] = new TemporalTable(
                table.Name, schema,
                temporal.GetHistoryTableName()!, temporal.GetHistoryTableSchema() ?? schema,
                key.Name, key.ProviderClrType,
                temporal.GetPeriodStartPropertyName()!, temporal.GetPeriodEndPropertyName()!,
                table.Columns
                    .Select(c => c.Name)
                    .Where(c => c != temporal.GetPeriodStartPropertyName() && c != temporal.GetPeriodEndPropertyName()
                                && !DeniedColumns.Contains(c) && !c.Contains("Token", StringComparison.OrdinalIgnoreCase))
                    .ToList());
        }

        return tables;
    }
}
