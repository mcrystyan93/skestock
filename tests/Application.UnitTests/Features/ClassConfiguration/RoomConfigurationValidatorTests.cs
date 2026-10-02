using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class RoomConfigurationValidatorTests
{
    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(int.MaxValue, true)]
    public async Task Save_ValidatesRoom4SeatCount(int? count, bool valid)
    {
        var result = await new SaveRoomConfigurationCommandValidator().ValidateAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = count, Room1SeatCount = 1, Room6SeatCount = 1
        });

        result.IsValid.ShouldBe(valid);
        if (!valid)
            result.Errors.ShouldContain(error =>
                error.PropertyName == nameof(SaveRoomConfigurationCommand.Room4SeatCount)
                && error.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(int.MaxValue, true)]
    public async Task Save_ValidatesRoom1SeatCount(int? count, bool valid)
    {
        var result = await new SaveRoomConfigurationCommandValidator().ValidateAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = 1, Room1SeatCount = count, Room6SeatCount = 1
        });

        result.IsValid.ShouldBe(valid);
        if (!valid)
            result.Errors.ShouldContain(error =>
                error.PropertyName == nameof(SaveRoomConfigurationCommand.Room1SeatCount)
                && error.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(int.MaxValue, true)]
    public async Task Save_ValidatesRoom6SeatCount(int? count, bool valid)
    {
        var result = await new SaveRoomConfigurationCommandValidator().ValidateAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = 1, Room1SeatCount = 1, Room6SeatCount = count
        });

        result.IsValid.ShouldBe(valid);
        if (!valid)
            result.Errors.ShouldContain(error =>
                error.PropertyName == nameof(SaveRoomConfigurationCommand.Room6SeatCount)
                && error.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [TestCase("Room4SeatCount")]
    [TestCase("Room1SeatCount")]
    [TestCase("Room6SeatCount")]
    public async Task Save_RequiresEverySeatCount(string missingProperty)
    {
        var command = new SaveRoomConfigurationCommand
        {
            Room4SeatCount = missingProperty == nameof(SaveRoomConfigurationCommand.Room4SeatCount) ? null : 1,
            Room1SeatCount = missingProperty == nameof(SaveRoomConfigurationCommand.Room1SeatCount) ? null : 1,
            Room6SeatCount = missingProperty == nameof(SaveRoomConfigurationCommand.Room6SeatCount) ? null : 1
        };

        var result = await new SaveRoomConfigurationCommandValidator().ValidateAsync(command);

        result.Errors.ShouldContain(error => error.PropertyName == missingProperty
                                             && error.ErrorCode == ValidationErrorCodes.Required);
    }
}
