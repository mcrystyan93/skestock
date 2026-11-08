using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Infrastructure.Data;
using skestock.Infrastructure.Data.Interceptors;

namespace skestock.Infrastructure.IntegrationTests;

/// <summary>A context whose saves are stamped as <paramref name="userId"/> by the real audit interceptor.</summary>
internal static class ActingDbContext
{
    public static ApplicationDbContext Create(Guid userId)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(IntegrationTestSetup.DatabaseConnectionString)
            .AddInterceptors(new AuditableEntityInterceptor(new FixedUser(userId), TimeProvider.System))
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class FixedUser(Guid id) : IUser
    {
        public Guid? Id => id;
        public List<string>? Roles => null;
    }
}
