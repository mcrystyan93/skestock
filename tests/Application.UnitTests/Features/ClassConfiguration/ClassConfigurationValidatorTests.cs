using skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;
using NUnit.Framework;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartment;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class ClassConfigurationValidatorTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task Department_RejectsMissingName(string? name)
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new SaveDepartmentCommandValidator(context.ApplicationContext).ValidateAsync(
            new SaveDepartmentCommand { Name = name!, Responsibilities = "Work" });
        result.Errors.ShouldContain(error => error.PropertyName == "Name" && error.ErrorCode == ValidationErrorCodes.Required);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task Department_RejectsMissingResponsibilities(string? responsibilities)
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new SaveDepartmentCommandValidator(context.ApplicationContext).ValidateAsync(
            new SaveDepartmentCommand { Name = "Logistics", Responsibilities = responsibilities! });
        result.Errors.ShouldContain(error => error.PropertyName == "Responsibilities" && error.ErrorCode == ValidationErrorCodes.Required);
    }

    [TestCase(100, true)]
    [TestCase(101, false)]
    public async Task Department_EnforcesNameLength(int length, bool valid)
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new SaveDepartmentCommandValidator(context.ApplicationContext).ValidateAsync(
            new SaveDepartmentCommand { Name = new string('n', length), Responsibilities = "Work" });
        result.IsValid.ShouldBe(valid);
    }

    [Test]
    public async Task Department_RejectsEmptyId()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var result = await new SaveDepartmentCommandValidator(context.ApplicationContext).ValidateAsync(
            new SaveDepartmentCommand { Id = Guid.Empty, Name = "Logistics", Responsibilities = "Work" });
        result.Errors.ShouldContain(error => error.PropertyName == "Id");
    }

    [Test]
    public async Task Department_RejectsDuplicateTrimmedCaseInsensitiveNameButAllowsOwnName()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var department = new DepartmentTemplate { Name = "Logistics", Responsibilities = "Stock" };
        context.Configurations.Add(new SharedClassConfiguration { DepartmentTemplates = [department] });
        await context.SaveChangesAsync();
        var validator = new SaveDepartmentCommandValidator(context.ApplicationContext);

        var duplicate = await validator.ValidateAsync(new SaveDepartmentCommand { Name = " LOGISTICS ", Responsibilities = "Work" });
        var ownName = await validator.ValidateAsync(new SaveDepartmentCommand { Id = department.Id, Name = " LOGISTICS ", Responsibilities = "Work" });

        duplicate.Errors.ShouldContain(error => error.ErrorCode == ValidationErrorCodes.DuplicateName);
        ownName.IsValid.ShouldBeTrue();
    }

    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(int.MaxValue, true)]
    public async Task Invitations_RequiresNonNegativeCount(int count, bool valid)
    {
        var result = await new SaveInvitationCountCommandValidator().ValidateAsync(new SaveInvitationCountCommand { InvitationCount = count });
        result.IsValid.ShouldBe(valid);
    }

    [Test]
    public async Task DeleteDepartment_RejectsEmptyId()
    {
        var result = await new DeleteDepartmentCommandValidator().ValidateAsync(new DeleteDepartmentCommand { Id = Guid.Empty });
        result.Errors.ShouldContain(error => error.PropertyName == "Id" && error.ErrorCode == ValidationErrorCodes.Required);
    }
    [Test]
    public async Task GetDepartment_RejectsEmptyId()
    {
        var result = await new GetDepartmentQueryValidator().ValidateAsync(new GetDepartmentQuery { Id = Guid.Empty });
        result.Errors.ShouldContain(error => error.PropertyName == "Id" && error.ErrorCode == ValidationErrorCodes.Required);
    }

}
