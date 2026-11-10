using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationSplit;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class InvitationSplitValidatorTests
{
    private static readonly string[] Properties =
    [
        nameof(SaveInvitationSplitCommand.StudentInvitationCount),
        nameof(SaveInvitationSplitCommand.InstructorInvitationCount),
        nameof(SaveInvitationSplitCommand.OpeningPrayerInvitationCount),
        nameof(SaveInvitationSplitCommand.ExperienceInvitationCount),
        nameof(SaveInvitationSplitCommand.ChangeInvitationCount)
    ];

    private static IEnumerable<TestCaseData> Cases()
    {
        foreach (var property in Properties)
        {
            yield return new TestCaseData(property, null, ValidationErrorCodes.Required);
            yield return new TestCaseData(property, -1, ValidationErrorCodes.GreaterThanOrEqualTo);
            yield return new TestCaseData(property, 0, null);
            yield return new TestCaseData(property, 200, null);
            yield return new TestCaseData(property, 201, ValidationErrorCodes.LessThanOrEqualTo);
        }
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Save_ValidatesEachCount(string property, int? count, string? expectedErrorCode)
    {
        int? Value(string name) => name == property ? count : 1;
        var command = new SaveInvitationSplitCommand
        {
            StudentInvitationCount = Value(nameof(SaveInvitationSplitCommand.StudentInvitationCount)),
            InstructorInvitationCount = Value(nameof(SaveInvitationSplitCommand.InstructorInvitationCount)),
            OpeningPrayerInvitationCount = Value(nameof(SaveInvitationSplitCommand.OpeningPrayerInvitationCount)),
            ExperienceInvitationCount = Value(nameof(SaveInvitationSplitCommand.ExperienceInvitationCount)),
            ChangeInvitationCount = Value(nameof(SaveInvitationSplitCommand.ChangeInvitationCount))
        };

        var result = await new SaveInvitationSplitCommandValidator().ValidateAsync(command);

        if (expectedErrorCode is null)
        {
            result.IsValid.ShouldBeTrue();
            return;
        }

        result.Errors.ShouldHaveSingleItem();
        result.Errors.ShouldContain(error => error.PropertyName == property && error.ErrorCode == expectedErrorCode);
    }
}
