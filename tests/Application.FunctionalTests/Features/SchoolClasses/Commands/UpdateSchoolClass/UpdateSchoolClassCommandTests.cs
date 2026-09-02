using skestock.Application.Common.Exceptions;
using skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;
using skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SchoolClasses.Commands.UpdateSchoolClass;

public class UpdateSchoolClassCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithValidChanges_UpdatesSchoolClassAndReturnsDto()
    {
        var created = await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = $"{_prefix}-Old",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        });

        var result = await TestApp.SendAsync(new UpdateSchoolClassCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-New",
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 7, 1),
            Status = ClassStatus.Active
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe($"{_prefix}-New");
        result.Value.StartDate.ShouldBe(new DateOnly(2026, 2, 1));
        result.Value.EndDate.ShouldBe(new DateOnly(2026, 7, 1));
        result.Value.Status.ShouldBe(ClassStatus.Active);

        var persisted = await TestApp.FindAsync<SchoolClass>(created.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe($"{_prefix}-New");
        persisted.Status.ShouldBe(ClassStatus.Active);
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new UpdateSchoolClassCommand
        {
            Id = Guid.NewGuid(),
            Name = $"{_prefix}-X",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithDuplicateNameOfAnotherClass_ThrowsValidationException()
    {
        var first = await TestApp.SendAsync(new CreateSchoolClassCommand { Name = $"{_prefix}-First", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 6, 1) });
        var second = await TestApp.SendAsync(new CreateSchoolClassCommand { Name = $"{_prefix}-Second", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 6, 1) });

        var act = async () => await TestApp.SendAsync(new UpdateSchoolClassCommand
        {
            Id = second.Value.Id,
            Name = $"{_prefix}-First",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1)
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(UpdateSchoolClassCommand.Name));
    }

    [Test]
    public async Task Handle_WithSameNameOnSameClass_DoesNotThrow()
    {
        var created = await TestApp.SendAsync(new CreateSchoolClassCommand { Name = $"{_prefix}-Same", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 6, 1) });

        var result = await TestApp.SendAsync(new UpdateSchoolClassCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Same",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 1),
            Status = ClassStatus.Paused
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ClassStatus.Paused);
    }

    [Test]
    public async Task Handle_WithStartDateAfterEndDate_ThrowsValidationException()
    {
        var created = await TestApp.SendAsync(new CreateSchoolClassCommand { Name = $"{_prefix}-X", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 6, 1) });

        var act = async () => await TestApp.SendAsync(new UpdateSchoolClassCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-X",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 1, 1)
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(UpdateSchoolClassCommand.EndDate));
    }
}
