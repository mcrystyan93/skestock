using skestock.Application.Common.Exceptions;
using skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;
using skestock.Application.Features.SchoolClasses.Queries.GetAllSchoolClasses;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SchoolClasses.Commands.CreateSchoolClass;

public class CreateSchoolClassCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithValidData_PersistsSchoolClassAndReturnsDto()
    {
        var name = $"{_prefix}-Fall2026";

        var result = await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = name,
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 15),
            Status = ClassStatus.Upcoming
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(name);
        result.Value.StartDate.ShouldBe(new DateOnly(2026, 9, 1));
        result.Value.EndDate.ShouldBe(new DateOnly(2026, 12, 15));
        result.Value.Status.ShouldBe(ClassStatus.Upcoming);
        result.Value.Id.ShouldNotBe(Guid.Empty);

        var persisted = await TestApp.FindAsync<SchoolClass>(result.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe(name);
    }

    [Test]
    public async Task Handle_WithDuplicateName_ThrowsValidationException()
    {
        var name = $"{_prefix}-Fall2026";
        await TestApp.SendAsync(new CreateSchoolClassCommand { Name = name, StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 1) });

        var act = async () => await TestApp.SendAsync(new CreateSchoolClassCommand { Name = name, StartDate = new DateOnly(2027, 1, 1), EndDate = new DateOnly(2027, 6, 1) });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateSchoolClassCommand.Name));
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateSchoolClassCommand { Name = "", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 1) });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateSchoolClassCommand.Name));
    }

    [Test]
    public async Task Handle_WithStartDateAfterEndDate_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = $"{_prefix}-Invalid",
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 9, 1)
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateSchoolClassCommand.EndDate));
    }

    [Test]
    public async Task Handle_WithInvalidStatus_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = $"{_prefix}-Invalid",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1),
            Status = (ClassStatus)999
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateSchoolClassCommand.Status));
    }

    [Test]
    public async Task Handle_OnSuccess_InvalidatesGetAllSchoolClassesCacheForSameSearchTerm()
    {
        var before = await TestApp.SendAsync(new GetAllSchoolClassesQuery { SearchTerm = _prefix });
        before.Value.Data.Count().ShouldBe(0);

        var name = $"{_prefix}-Spring2027";
        await TestApp.SendAsync(new CreateSchoolClassCommand { Name = name, StartDate = new DateOnly(2027, 1, 1), EndDate = new DateOnly(2027, 6, 1) });

        var after = await TestApp.SendAsync(new GetAllSchoolClassesQuery { SearchTerm = _prefix });
        after.Value.Data.Count().ShouldBe(1);
        after.Value.Data.Single().Name.ShouldBe(name);
    }
}
