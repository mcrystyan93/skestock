using skestock.Application.Common.Exceptions;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SchoolClasses.Queries.GetSchoolClassById;

public class GetSchoolClassByIdQueryTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithExistingId_ReturnsMatchingSchoolClassDto()
    {
        var schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Fall2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1),
            Status = ClassStatus.Active
        };
        await TestApp.AddAsync(schoolClass);

        var result = await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = schoolClass.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(schoolClass.Id);
        result.Value.Name.ShouldBe(schoolClass.Name);
        result.Value.StartDate.ShouldBe(schoolClass.StartDate);
        result.Value.EndDate.ShouldBe(schoolClass.EndDate);
        result.Value.Status.ShouldBe(schoolClass.Status);
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = int.MaxValue });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithInvalidId_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = 0 });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetSchoolClassByIdQuery.Id));
    }
}
