using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Congregations.Commands.CreateCongregation;
using skestock.Application.Features.Congregations.Commands.DeleteCongregation;
using skestock.Application.Features.Congregations.Commands.UpdateCongregation;
using skestock.Application.Features.Congregations.Queries.GetAllCongregations;
using skestock.Application.Features.Congregations.Queries.GetCongregationById;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.FunctionalTests.Features.Congregations;

public class CongregationCommandsTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    private static CreateCongregationCommand NewCommand(string name) => new()
    {
        Name = name,
        Email = "congregation@example.com"
    };

    [Test]
    public async Task Create_ThenGetById_RoundTripsAllFields()
    {
        var created = await TestApp.SendAsync(NewCommand($"{_prefix}-Central") with
        {
            Coordinator = "Ana",
            CoordinatorPhone = "0711111111",
            DessertContactPerson = "Ion",
            DessertContactPhone = "0722222222",
            CanReceiveStudents = true,
            IsSmallCongregation = true,
            Message = "Hello"
        });

        var fetched = await TestApp.SendAsync(new GetCongregationByIdQuery { Id = created.Value.Id });

        fetched.IsSuccess.ShouldBeTrue();
        fetched.Value.Name.ShouldBe($"{_prefix}-Central");
        fetched.Value.Coordinator.ShouldBe("Ana");
        fetched.Value.DessertContactPerson.ShouldBe("Ion");
        fetched.Value.CanReceiveStudents.ShouldBeTrue();
        fetched.Value.IsSmallCongregation.ShouldBeTrue();
        fetched.Value.Message.ShouldBe("Hello");
        fetched.Value.CreatedDate.ShouldNotBe(default);
    }

    [Test]
    public async Task Create_WithDuplicateName_ThrowsValidationException()
    {
        await TestApp.SendAsync(NewCommand($"{_prefix}-Dup"));

        var act = async () => await TestApp.SendAsync(NewCommand($"{_prefix}-DUP"));

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateCongregationCommand.Name));
    }

    [Test]
    public async Task Create_WithNameDifferingOnlyByAccent_ThrowsValidationException()
    {
        await TestApp.SendAsync(NewCommand($"{_prefix}-Cafe"));

        var act = async () => await TestApp.SendAsync(NewCommand($"{_prefix}-Café"));

        await act.ShouldThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Create_WithInvalidEmail_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(NewCommand($"{_prefix}-Mail") with { Email = "nope" });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateCongregationCommand.Email));
    }

    [Test]
    public async Task UniqueIndex_RejectsTwoActiveRowsWithTheSameName_EvenWhenValidatorIsBypassed()
    {
        await TestApp.AddAsync(new Congregation { Name = $"{_prefix}-Index", Email = "a@example.com" });

        var act = async () => await TestApp.AddAsync(new Congregation { Name = $"{_prefix}-index", Email = "b@example.com" });

        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Test]
    public async Task Update_ChangesFields()
    {
        var created = await TestApp.SendAsync(NewCommand($"{_prefix}-Old"));

        var result = await TestApp.SendAsync(new UpdateCongregationCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-New",
            Email = "new@example.com",
            CanReceiveStudents = true
        });

        result.IsSuccess.ShouldBeTrue();
        var persisted = await TestApp.FindAsync<Congregation>(created.Value.Id);
        persisted!.Name.ShouldBe($"{_prefix}-New");
        persisted.Email.ShouldBe("new@example.com");
        persisted.CanReceiveStudents.ShouldBeTrue();
    }

    [Test]
    public async Task Update_WithNameOfAnotherCongregation_ThrowsValidationException()
    {
        await TestApp.SendAsync(NewCommand($"{_prefix}-First"));
        var second = await TestApp.SendAsync(NewCommand($"{_prefix}-Second"));

        var act = async () => await TestApp.SendAsync(new UpdateCongregationCommand
        {
            Id = second.Value.Id,
            Name = $"{_prefix}-First",
            Email = "x@example.com"
        });

        await act.ShouldThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        var result = await TestApp.SendAsync(new UpdateCongregationCommand
        {
            Id = Guid.NewGuid(),
            Name = $"{_prefix}-X",
            Email = "x@example.com"
        });

        result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
    }

    [Test]
    public async Task Update_InvalidatesGetAllCache()
    {
        var created = await TestApp.SendAsync(NewCommand($"{_prefix}-Before"));
        (await TestApp.SendAsync(new GetAllCongregationsQuery { SearchTerm = _prefix })).Value.Data.Single()
            .Name.ShouldBe($"{_prefix}-Before");

        await TestApp.SendAsync(new UpdateCongregationCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-After",
            Email = "x@example.com"
        });

        (await TestApp.SendAsync(new GetAllCongregationsQuery { SearchTerm = _prefix })).Value.Data.Single()
            .Name.ShouldBe($"{_prefix}-After");
    }

    [Test]
    public async Task Delete_HidesCongregationFromListGetByIdAndFind()
    {
        var created = await TestApp.SendAsync(NewCommand($"{_prefix}-Gone"));
        (await TestApp.SendAsync(new GetAllCongregationsQuery { SearchTerm = _prefix })).Value.Data.Count().ShouldBe(1);

        var result = await TestApp.SendAsync(new DeleteCongregationCommand { Id = created.Value.Id });

        result.IsSuccess.ShouldBeTrue();
        (await TestApp.SendAsync(new GetAllCongregationsQuery { SearchTerm = _prefix })).Value.Data.ShouldBeEmpty();
        (await TestApp.SendAsync(new GetCongregationByIdQuery { Id = created.Value.Id }))
            .Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
        (await TestApp.FindAsync<Congregation>(created.Value.Id)).ShouldBeNull(); // global query filter
    }

    [Test]
    public async Task Delete_ThenCreateWithSameName_Succeeds()
    {
        var created = await TestApp.SendAsync(NewCommand($"{_prefix}-Reuse"));
        await TestApp.SendAsync(new DeleteCongregationCommand { Id = created.Value.Id });

        var again = await TestApp.SendAsync(NewCommand($"{_prefix}-Reuse"));

        again.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task Delete_Twice_ReturnsNotFoundTheSecondTime()
    {
        var created = await TestApp.SendAsync(NewCommand($"{_prefix}-Twice"));
        await TestApp.SendAsync(new DeleteCongregationCommand { Id = created.Value.Id });

        var second = await TestApp.SendAsync(new DeleteCongregationCommand { Id = created.Value.Id });

        second.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
    }
}
