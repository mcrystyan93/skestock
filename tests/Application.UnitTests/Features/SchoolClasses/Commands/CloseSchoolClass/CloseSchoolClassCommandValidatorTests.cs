using FluentValidation.TestHelper;
using NUnit.Framework;
using skestock.Application.Features.SchoolClasses.Commands.CloseSchoolClass;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Commands.CloseSchoolClass;

public class CloseSchoolClassCommandValidatorTests
{
    private readonly CloseSchoolClassCommandValidator _validator = new();

    [Test]
    public void Validate_WithTwoDifferentIds_Passes() =>
        _validator.TestValidate(new CloseSchoolClassCommand { Id = Guid.NewGuid(), TargetClassId = Guid.NewGuid() })
            .ShouldNotHaveAnyValidationErrors();

    [Test]
    public void Validate_WithEmptyId_Fails() =>
        _validator.TestValidate(new CloseSchoolClassCommand { TargetClassId = Guid.NewGuid() })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Test]
    public void Validate_WithEmptyTarget_Fails() =>
        _validator.TestValidate(new CloseSchoolClassCommand { Id = Guid.NewGuid() })
            .ShouldHaveValidationErrorFor(x => x.TargetClassId);

    [Test]
    public void Validate_WhenTargetEqualsSource_Fails()
    {
        var id = Guid.NewGuid();
        _validator.TestValidate(new CloseSchoolClassCommand { Id = id, TargetClassId = id })
            .ShouldHaveValidationErrorFor(x => x.TargetClassId);
    }
}
