using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans.Commands;

public class SendFruitDessertMessageCommandValidatorTests
{
    [TestCase(FruitDessertMessageType.Reminder, true)]
    [TestCase(FruitDessertMessageType.Thanks, true)]
    [TestCase(FruitDessertMessageType.Informare, false)]
    public void Validator_AllowsOnlyWhatsAppTypes(FruitDessertMessageType type, bool valid)
    {
        var result = new SendFruitDessertMessageCommandValidator().Validate(
            new SendFruitDessertMessageCommand { ClassId = Guid.NewGuid(), EntryId = Guid.NewGuid(), Type = type });

        result.IsValid.ShouldBe(valid);
    }
}
