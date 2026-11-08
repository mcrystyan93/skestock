using Microsoft.AspNetCore.Identity;
using NUnit.Framework;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using skestock.Domain.Entities.ScheduledJobs;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Queues;
using skestock.Infrastructure.Data;

namespace skestock.Application.UnitTests.Infrastructure.Data;

[TestFixture]
public class TemporalModelTests
{
    private static readonly Type[] Excluded =
        [typeof(OutboxMessage), typeof(ProcessedMessage), typeof(ScheduledJobRun), typeof(DailyItemConsumption), typeof(ItemPurchaseStatistic), typeof(IdentityUserToken<Guid>)];

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=model-only")
            .Options;
        using var context = new ApplicationDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    [Test]
    public void EveryEntityTypeIsTemporalExceptTheExclusions()
    {
        var model = BuildModel();

        foreach (var entityType in model.GetEntityTypes())
        {
            var expected = !Excluded.Contains(entityType.ClrType) && !entityType.IsMappedToJson();
            entityType.IsTemporal().ShouldBe(expected, entityType.DisplayName());
        }

        model.GetEntityTypes().Count(t => t.IsTemporal()).ShouldBeGreaterThan(30);
    }

    [Test]
    public void EveryTemporalTableHasLastModifiedById()
    {
        var model = BuildModel();

        var tables = model.GetEntityTypes()
            .Where(t => t.IsTemporal())
            .GroupBy(t => (t.GetSchema(), t.GetTableName()));

        foreach (var table in tables)
        {
            var name = table.Key.Item2;
            var property = table.Select(t => t.FindProperty(ApplicationDbContext.LastModifiedById)).FirstOrDefault(p => p != null);
            property.ShouldNotBeNull(name);
            property.IsNullable.ShouldBeTrue();

            // BaseAuditableEntity tables keep their existing FK; the shadow column added here must not get one.
            if (property.IsShadowProperty())
            {
                property.GetContainingForeignKeys().ShouldBeEmpty(name);
                property.GetContainingIndexes().ShouldBeEmpty(name);
            }
        }
    }
}
