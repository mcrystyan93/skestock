using Microsoft.EntityFrameworkCore;
using Moq;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

internal sealed class ClassConfigurationTestDbContext : DbContext
{
    public ClassConfigurationTestDbContext()
        : base(new DbContextOptionsBuilder<ClassConfigurationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        var mock = new Mock<IApplicationDbContext>();
        mock.SetupGet(context => context.SharedClassConfigurations).Returns(Configurations);
        mock.SetupGet(context => context.Database).Returns(Database);
        mock.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) => SaveChangesAsync(token));
        ApplicationContext = mock.Object;
    }

    public DbSet<SharedClassConfiguration> Configurations => Set<SharedClassConfiguration>();
    public IApplicationDbContext ApplicationContext { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SharedClassConfiguration>().Property(configuration => configuration.Id).ValueGeneratedNever();
        modelBuilder.Entity<DepartmentTemplate>().HasOne(department => department.SharedClassConfiguration)
            .WithMany(configuration => configuration.DepartmentTemplates)
            .HasForeignKey(department => department.SharedClassConfigurationId);
    }
}
