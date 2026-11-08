using skestock.Application.Common.Interfaces;
using skestock.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace skestock.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor(
    IUser user,
    TimeProvider dateTime) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        StampDeleter(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        await StampDeleterAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                var utcNow = dateTime.GetUtcNow();
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedById = user.Id;
                    entry.Entity.CreatedDate = utcNow;
                } 
                entry.Entity.LastModifiedById = user.Id;
                entry.Entity.LastModifiedDate = utcNow;
            }
        }

        // Temporal tables without BaseAuditableEntity (thresholds, Identity, owned import history...) carry a
        // shadow LastModifiedById. Null user = system actor.
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is BaseAuditableEntity || entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            if (entry.Metadata.FindProperty(ApplicationDbContext.LastModifiedById) is not null)
                entry.Property(ApplicationDbContext.LastModifiedById).CurrentValue = user.Id;
        }
    }

    // A hard DELETE leaves history rows that only name the last *modifier*. Writing the deleter into the row just
    // before EF's DELETE makes the deleter the modifier of the newest history version.
    // shortcut: one UPDATE per deleted row, fine for the handful of rows deleted per save, batch if that grows.
    // shortcut: the stamp is only atomic with the DELETE inside an ambient transaction (class-scoped commands,
    // queue processing). Without one (DeleteDepartment, Identity deletes) it autocommits first, so a failed DELETE
    // leaves the row with the would-be deleter as modifier plus one extra version: no data loss. Wrap the caller in
    // a transaction if that misattribution ever matters.
    // Not stamped: DB-side cascades (their parent's stamped version shares the deletion instant), raw SQL.
    private void StampDeleter(DbContext? context)
    {
        if (context is null || !context.Database.IsRelational()) return;

        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted).ToList())
        {
            if (BuildDeleterStamp(context, entry) is not { } stamp) continue;

            if (stamp.Version is null)
            {
                context.Database.ExecuteSqlRaw(stamp.Sql, stamp.Parameters);
                continue;
            }

            var token = context.Database.SqlQueryRaw<byte[]>(stamp.Sql, stamp.Parameters).ToList();
            ApplyVersion(entry, stamp.Version, token);
        }
    }

    private async Task StampDeleterAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || !context.Database.IsRelational()) return;

        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted).ToList())
        {
            if (BuildDeleterStamp(context, entry) is not { } stamp) continue;

            if (stamp.Version is null)
            {
                await context.Database.ExecuteSqlRawAsync(stamp.Sql, stamp.Parameters, cancellationToken);
                continue;
            }

            var token = await context.Database.SqlQueryRaw<byte[]>(stamp.Sql, stamp.Parameters)
                .ToListAsync(cancellationToken);
            ApplyVersion(entry, stamp.Version, token);
        }
    }

    // The UPDATE bumps a store-generated rowversion, so EF's DELETE must use the new value as the original one,
    // otherwise it reports a false concurrency conflict. No row updated: leave it, EF raises the real conflict.
    private static void ApplyVersion(EntityEntry entry, IProperty version, List<byte[]> token)
    {
        if (token.Count == 1)
            entry.Property(version.Name).OriginalValue = token[0];
    }

    private sealed record DeleterStamp(string Sql, object[] Parameters, IProperty? Version);

    // Identifiers come only from EF metadata and are delimited by the provider; every value is a parameter.
    private DeleterStamp? BuildDeleterStamp(DbContext context, EntityEntry entry)
    {
        var entityType = entry.Metadata;
        var table = entityType.GetTableName();
        if (!entityType.IsTemporal() || table is null || entityType.IsMappedToJson()
            || entityType.FindProperty(ApplicationDbContext.LastModifiedById) is null
            || entityType.FindPrimaryKey() is not { } key)
            return null;

        var schema = entityType.GetSchema();
        var store = StoreObjectIdentifier.Table(table, schema);
        var sql = context.GetService<ISqlGenerationHelper>();
        var parameters = new List<object> { (object?)user.Id ?? DBNull.Value };

        var version = entityType.GetProperties().SingleOrDefault(p =>
            p.IsConcurrencyToken && p.ValueGenerated.HasFlag(ValueGenerated.OnUpdate));

        var where = new List<string>();
        foreach (var property in key.Properties)
        {
            where.Add($"{sql.DelimitIdentifier(property.GetColumnName(store)!)} = {{{parameters.Count}}}");
            parameters.Add(entry.Property(property.Name).CurrentValue!);
        }

        if (version is not null)
        {
            where.Add($"{sql.DelimitIdentifier(version.GetColumnName(store)!)} = {{{parameters.Count}}}");
            parameters.Add(entry.Property(version.Name).OriginalValue!);
        }

        var modifier = sql.DelimitIdentifier(entityType.FindProperty(ApplicationDbContext.LastModifiedById)!.GetColumnName(store)!);
        var output = version is null
            ? ""
            : $" OUTPUT inserted.{sql.DelimitIdentifier(version.GetColumnName(store)!)} AS [Value]";

        return new DeleterStamp(
            $"UPDATE {sql.DelimitIdentifier(table, schema)} SET {modifier} = {{0}}{output} WHERE {string.Join(" AND ", where)}",
            parameters.ToArray(),
            version);
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r => 
            r.TargetEntry != null && 
            r.TargetEntry.Metadata.IsOwned() && 
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
