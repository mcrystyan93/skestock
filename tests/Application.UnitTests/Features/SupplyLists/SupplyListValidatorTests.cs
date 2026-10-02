using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.SupplyLists.Commands.CreateSupplyList;
using skestock.Application.Features.SupplyLists.Commands.UpdateSupplyList;
using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.SupplyLists;

public class SupplyListValidatorTests
{
    private static CreateSupplyListCommand Valid(Guid itemId) => new()
    {
        Name = "Weekly",
        Frequency = SupplyListFrequency.Weekly,
        Lines = [new SupplyListLineInput { ItemId = itemId }]
    };

    private static string[] Codes(FluentValidation.Results.ValidationResult result) =>
        result.Errors.Select(e => e.ErrorCode).ToArray();

    [Test]
    public async Task Create_ValidCommandPasses()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var result = await new CreateSupplyListCommandValidator(context).ValidateAsync(Valid(rice.Id));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task Create_RejectsDuplicateNameCaseInsensitively()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;
        context.SupplyLists.Add(new SupplyList { Name = "WEEKLY", Frequency = SupplyListFrequency.Weekly });
        await context.SaveChangesAsync();

        var result = await new CreateSupplyListCommandValidator(context).ValidateAsync(Valid(rice.Id));

        Codes(result).ShouldContain(ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task Update_AllowsOwnName()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;
        var list = new SupplyList { Name = "Weekly", Frequency = SupplyListFrequency.Weekly };
        context.SupplyLists.Add(list);
        await context.SaveChangesAsync();

        var result = await new UpdateSupplyListCommandValidator(context).ValidateAsync(new UpdateSupplyListCommand
        {
            Id = list.Id,
            Name = "weekly",
            Frequency = SupplyListFrequency.Weekly,
            Lines = [new SupplyListLineInput { ItemId = rice.Id }]
        });

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(SupplyListFrequency.EveryXWeeks, null, false)]
    [TestCase(SupplyListFrequency.EveryXWeeks, 1, false)]
    [TestCase(SupplyListFrequency.EveryXWeeks, 53, false)]
    [TestCase(SupplyListFrequency.EveryXWeeks, 3, true)]
    [TestCase(SupplyListFrequency.Weekly, 3, false)]
    [TestCase(SupplyListFrequency.Once, null, true)]
    public async Task Create_ChecksIntervalAgainstFrequency(SupplyListFrequency frequency, int? interval, bool valid)
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var command = new CreateSupplyListCommand
        {
            Name = "List", Frequency = frequency, IntervalWeeks = interval,
            Lines = [new SupplyListLineInput { ItemId = rice.Id }]
        };

        (await new CreateSupplyListCommandValidator(context).ValidateAsync(command)).IsValid.ShouldBe(valid);
    }

    [Test]
    public async Task Create_RejectsBadLines()
    {
        var (context, rice, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;

        var command = new CreateSupplyListCommand
        {
            Name = "List",
            Frequency = SupplyListFrequency.Weekly,
            Lines =
            [
                new SupplyListLineInput { ItemId = rice.Id, Quantity = 0 },
                new SupplyListLineInput { ItemId = rice.Id, Unit = new string('u', 51) },
                new SupplyListLineInput { ItemId = Guid.NewGuid() }
            ]
        };

        var codes = Codes(await new CreateSupplyListCommandValidator(context).ValidateAsync(command));

        codes.ShouldContain(ValidationErrorCodes.GreaterThan);
        codes.ShouldContain(ValidationErrorCodes.MaxLength);
        codes.ShouldContain(ValidationErrorCodes.InvalidReference);
        codes.ShouldContain(ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task Create_RejectsEmptyAndTooLongName()
    {
        var (context, _, _, _) = await SupplyListTestData.CreateContextAsync();
        await using var _ = context;
        var validator = new CreateSupplyListCommandValidator(context);

        Codes(await validator.ValidateAsync(new CreateSupplyListCommand { Name = "" }))
            .ShouldContain(ValidationErrorCodes.Required);
        Codes(await validator.ValidateAsync(new CreateSupplyListCommand { Name = new string('n', 201) }))
            .ShouldContain(ValidationErrorCodes.MaxLength);
    }
}
