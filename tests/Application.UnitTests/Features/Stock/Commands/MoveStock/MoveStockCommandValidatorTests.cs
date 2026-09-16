using skestock.Application.Common.Errors;
using skestock.Application.Features.Stock.Commands.MoveStock;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Commands.MoveStock;

public class MoveStockCommandValidatorTests
{
    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;

        fixture.Context.StockBatches.Add(MoveStockTestData.CreateBatch(
            fixture,
            quantity: 20,
            expiryDate: new DateOnly(2026, 10, 1),
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 2.5m));
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await new MoveStockCommandValidator(fixture.Context).ValidateAsync(new MoveStockCommand
        {
            ClassId = fixture.Class.Id,
            ItemId = fixture.Item.Id,
            SourceLocationId = fixture.SourceLocation.Id,
            DestinationLocationId = fixture.DestinationLocation.Id,
            Quantity = 10
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldReportInvalidReferenceErrorsForUnknownEntities()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var validator = new MoveStockCommandValidator(fixture.Context);

        var result = await validator.ValidateAsync(new MoveStockCommand
        {
            ClassId = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            SourceLocationId = Guid.NewGuid(),
            DestinationLocationId = Guid.NewGuid(),
            Quantity = 1
        });

        result.Errors.Count(e => e.ErrorCode == ValidationErrorCodes.InvalidReference)
            .ShouldBe(4);
    }

    [Test]
    public async Task ShouldRejectSameSourceAndDestinationLocation()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var validator = new MoveStockCommandValidator(fixture.Context);

        var result = await validator.ValidateAsync(new MoveStockCommand
        {
            ClassId = fixture.Class.Id,
            ItemId = fixture.Item.Id,
            SourceLocationId = fixture.SourceLocation.Id,
            DestinationLocationId = fixture.SourceLocation.Id,
            Quantity = 1
        });

        result.Errors.ShouldContain(e =>
            e.PropertyName == "DestinationLocationId"
            && e.ErrorCode == ValidationErrorCodes.SelfReference);
    }

    [Test]
    public async Task ShouldRejectNonPositiveQuantity()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;
        var validator = new MoveStockCommandValidator(fixture.Context);

        var result = await validator.ValidateAsync(new MoveStockCommand
        {
            ClassId = fixture.Class.Id,
            ItemId = fixture.Item.Id,
            SourceLocationId = fixture.SourceLocation.Id,
            DestinationLocationId = fixture.DestinationLocation.Id,
            Quantity = 0
        });

        result.Errors.ShouldContain(e =>
            e.PropertyName == "Quantity"
            && e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }

    [Test]
    public async Task ShouldRejectQuantityAboveCurrentClassScopedSourceTotal()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;

        fixture.Context.StockBatches.Add(MoveStockTestData.CreateBatch(
            fixture,
            quantity: 5,
            expiryDate: null,
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 1));
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await new MoveStockCommandValidator(fixture.Context).ValidateAsync(new MoveStockCommand
        {
            ClassId = fixture.Class.Id,
            ItemId = fixture.Item.Id,
            SourceLocationId = fixture.SourceLocation.Id,
            DestinationLocationId = fixture.DestinationLocation.Id,
            Quantity = 6
        });

        result.Errors.ShouldContain(e =>
            e.PropertyName == "Quantity"
            && e.ErrorCode == ValidationErrorCodes.LessThanOrEqualTo);
    }

    [Test]
    public async Task ShouldNotCountAnotherClassStockWhenCheckingSourceQuantity()
    {
        var fixture = await MoveStockTestData.CreateFixtureAsync();
        await using var _ = fixture.Context;

        var otherClass = new SchoolClass
        {
            Name = "Spring 2027", StartDate = new DateOnly(2027, 1, 1), EndDate = new DateOnly(2027, 5, 31)
        };
        fixture.Context.SchoolClasses.Add(otherClass);
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        fixture.Context.StockBatches.Add(MoveStockTestData.CreateBatch(
            fixture,
            quantity: 5,
            expiryDate: null,
            receivedDate: new DateOnly(2026, 1, 1),
            unitPrice: 1,
            classId: otherClass.Id));
        await fixture.Context.SaveChangesAsync(CancellationToken.None);

        var result = await new MoveStockCommandValidator(fixture.Context).ValidateAsync(new MoveStockCommand
        {
            ClassId = fixture.Class.Id,
            ItemId = fixture.Item.Id,
            SourceLocationId = fixture.SourceLocation.Id,
            DestinationLocationId = fixture.DestinationLocation.Id,
            Quantity = 1
        });

        result.Errors.ShouldContain(e =>
            e.PropertyName == "Quantity"
            && e.ErrorCode == ValidationErrorCodes.LessThanOrEqualTo);
    }
}
