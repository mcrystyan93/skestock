using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.BulkAdjustStock;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Stock.Commands.BulkAdjustStock;

public class BulkAdjustStockCommandValidatorTests
{
    private static async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        BulkAdjustStockTestData d, Guid? classId = null, AdjustmentReason reason = AdjustmentReason.Miscount,
        params BulkAdjustStockCommand.Line[] lines) =>
        await new BulkAdjustStockCommandValidator(d.Context).ValidateAsync(new BulkAdjustStockCommand
        {
            ClassId = classId ?? d.Class.Id,
            Reason = reason,
            Lines = lines
        });

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d, lines:
            [new(d.Rice.Id, d.Pantry.Id, 3), new(d.Rice.Id, d.Cellar.Id, 0), new(d.Flour.Id, d.Pantry.Id, 9)]);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldRejectEmptyLines()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.EmptyLines);
    }

    [Test]
    public async Task ShouldRejectMoreThan1000Lines()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;
        var lines = Enumerable.Range(0, 1001)
            .Select(i => new BulkAdjustStockCommand.Line(Guid.NewGuid(), Guid.NewGuid(), i))
            .ToArray();

        var result = await ValidateAsync(d, lines: lines);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.LessThanOrEqualTo);
    }

    [Test]
    public async Task ShouldRejectDuplicateItemLocationPair()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d, lines:
            [new(d.Rice.Id, d.Pantry.Id, 1), new(d.Rice.Id, d.Pantry.Id, 2)]);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateReceiptLine);
    }

    [Test]
    public async Task ShouldRejectNegativeQuantity()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d, lines: [new(d.Rice.Id, d.Pantry.Id, -1)]);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [Test]
    public async Task ShouldRejectInvalidReason()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d, reason: (AdjustmentReason)999, lines: [new(d.Rice.Id, d.Pantry.Id, 1)]);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidEnum);
    }

    [Test]
    public async Task ShouldRejectUnknownClass()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d, classId: Guid.NewGuid(), lines: [new(d.Rice.Id, d.Pantry.Id, 1)]);

        result.Errors.ShouldContain(e => e.PropertyName == "ClassId" && e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldRejectUnknownItem()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d, lines: [new(Guid.NewGuid(), d.Pantry.Id, 1)]);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldRejectUnknownLocation()
    {
        var d = await BulkAdjustStockTestData.CreateAsync();
        await using var _ = d.Context;

        var result = await ValidateAsync(d, lines: [new(d.Rice.Id, Guid.NewGuid(), 1)]);

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }
}
