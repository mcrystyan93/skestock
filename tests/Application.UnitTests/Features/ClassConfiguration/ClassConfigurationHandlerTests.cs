using skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartments;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartment;
using skestock.Application.Features.ClassConfiguration.Queries.GetInvitationCount;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class ClassConfigurationHandlerTests
{
    [Test]
    public async Task Queries_WhenConfigurationIsMissing_ReturnEmptyDepartmentsAndZeroInvitations()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var departments = await new GetDepartmentsQueryHandler(context.ApplicationContext)
            .Handle(new GetDepartmentsQuery(), CancellationToken.None);
        var invitations = await new GetInvitationCountQueryHandler(context.ApplicationContext)
            .Handle(new GetInvitationCountQuery(), CancellationToken.None);

        departments.Value.ShouldBeEmpty();
        invitations.Value.InvitationCount.ShouldBe(0);
        context.Configurations.ShouldBeEmpty();
    }

    [Test]
    public async Task SaveDepartment_CreatesConfigurationAndTrimsValues()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new SaveDepartmentCommandHandler(context.ApplicationContext).Handle(
            new SaveDepartmentCommand { Name = " Logistics ", Responsibilities = " Manage stock " }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Name.ShouldBe("Logistics");
        result.Value.Responsibilities.ShouldBe("Manage stock");
        context.Configurations.Single().InvitationCount.ShouldBe(0);
    }

    [Test]
    public async Task SaveDepartment_UpdatesOnlySelectedDepartmentAndPreservesInvitations()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var department = new DepartmentTemplate { Name = "Logistics", Responsibilities = "Stock" };
        var other = new DepartmentTemplate { Name = "Events", Responsibilities = "Activities" };
        context.Configurations.Add(new SharedClassConfiguration { InvitationCount = 42, DepartmentTemplates = [department, other] });
        await context.SaveChangesAsync();

        var result = await new SaveDepartmentCommandHandler(context.ApplicationContext).Handle(
            new SaveDepartmentCommand { Id = department.Id, Name = "Purchasing", Responsibilities = "Orders" }, CancellationToken.None);

        result.Value.Id.ShouldBe(department.Id);
        department.Name.ShouldBe("Purchasing");
        other.Name.ShouldBe("Events");
        context.Configurations.Single().InvitationCount.ShouldBe(42);
        context.Configurations.Single().DepartmentTemplates.Count.ShouldBe(2);
    }

    [Test]
    public async Task SaveDepartment_UnknownIdReturnsNotFoundWithoutCreatingConfiguration()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new SaveDepartmentCommandHandler(context.ApplicationContext).Handle(
            new SaveDepartmentCommand { Id = Guid.NewGuid(), Name = "Missing", Responsibilities = "Work" }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is ClassConfigurationErrors.DepartmentNotFound);
        context.Configurations.ShouldBeEmpty();
    }

    [TestCase(0)]
    [TestCase(25)]
    [TestCase(int.MaxValue)]
    public async Task SaveInvitations_CreatesConfiguration(int count)
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new SaveInvitationCountCommandHandler(context.ApplicationContext).Handle(
            new SaveInvitationCountCommand { InvitationCount = count }, CancellationToken.None);

        result.Value.InvitationCount.ShouldBe(count);
        context.Configurations.Single().InvitationCount.ShouldBe(count);
    }

    [Test]
    public async Task SaveInvitations_PreservesDepartments()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var department = new DepartmentTemplate { Name = "Logistics", Responsibilities = "Stock" };
        context.Configurations.Add(new SharedClassConfiguration { InvitationCount = 10, DepartmentTemplates = [department] });
        await context.SaveChangesAsync();

        await new SaveInvitationCountCommandHandler(context.ApplicationContext).Handle(
            new SaveInvitationCountCommand { InvitationCount = 20 }, CancellationToken.None);
        var departments = await new GetDepartmentsQueryHandler(context.ApplicationContext)
            .Handle(new GetDepartmentsQuery(), CancellationToken.None);
        var invitations = await new GetInvitationCountQueryHandler(context.ApplicationContext)
            .Handle(new GetInvitationCountQuery(), CancellationToken.None);

        invitations.Value.InvitationCount.ShouldBe(20);
        departments.Value.Single().Name.ShouldBe("Logistics");
        departments.Value.Single().Id.ShouldBe(department.Id);
    }

    [Test]
    public async Task GetDepartments_OrdersByName()
    {
        await using var context = new ClassConfigurationTestDbContext();
        context.Configurations.Add(new SharedClassConfiguration { DepartmentTemplates = [
            new DepartmentTemplate { Name = "Zebra", Responsibilities = "Last" },
            new DepartmentTemplate { Name = "Alpha", Responsibilities = "First" }
        ] });
        await context.SaveChangesAsync();

        var result = await new GetDepartmentsQueryHandler(context.ApplicationContext).Handle(new GetDepartmentsQuery(), CancellationToken.None);
        result.Value.Select(department => department.Name).ShouldBe(["Alpha", "Zebra"]);
    }

    [Test]
    public async Task DeleteDepartment_PreservesOtherDepartmentsAndInvitationCount()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var first = new DepartmentTemplate { Name = "Logistics", Responsibilities = "Stock" };
        var second = new DepartmentTemplate { Name = "Events", Responsibilities = "Activities" };
        context.Configurations.Add(new SharedClassConfiguration { InvitationCount = 12, DepartmentTemplates = [first, second] });
        await context.SaveChangesAsync();
        var result = await new DeleteDepartmentCommandHandler(context.ApplicationContext).Handle(new DeleteDepartmentCommand { Id = first.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        context.Configurations.Single().DepartmentTemplates.Single().Id.ShouldBe(second.Id);
        context.Configurations.Single().InvitationCount.ShouldBe(12);
    }

    [Test]
    public async Task DeleteDepartment_UnknownIdReturnsNotFound()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new DeleteDepartmentCommandHandler(context.ApplicationContext).Handle(new DeleteDepartmentCommand { Id = Guid.NewGuid() }, CancellationToken.None);
        result.Errors.ShouldContain(error => error is ClassConfigurationErrors.DepartmentNotFound);
    }
    [Test]
    public async Task GetDepartment_ReturnsSelectedDepartment()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var department = new DepartmentTemplate { Name = "Logistics", Responsibilities = "Stock" };
        context.Configurations.Add(new SharedClassConfiguration { DepartmentTemplates = [department] });
        await context.SaveChangesAsync();

        var result = await new GetDepartmentQueryHandler(context.ApplicationContext)
            .Handle(new GetDepartmentQuery { Id = department.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(department.Id);
        result.Value.Name.ShouldBe("Logistics");
        result.Value.Responsibilities.ShouldBe("Stock");
    }

    [Test]
    public async Task GetDepartment_WhenMissing_ReturnsNotFoundWithoutCreatingConfiguration()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new GetDepartmentQueryHandler(context.ApplicationContext)
            .Handle(new GetDepartmentQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is ClassConfigurationErrors.DepartmentNotFound);
        context.Configurations.ShouldBeEmpty();
    }

}
