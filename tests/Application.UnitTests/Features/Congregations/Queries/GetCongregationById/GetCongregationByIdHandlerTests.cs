using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Congregations.Queries.GetCongregationById;
using skestock.Domain.Entities.Congregations;

namespace skestock.Application.UnitTests.Features.Congregations.Queries.GetCongregationById;

public class GetCongregationByIdHandlerTests
{
    private static CongregationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CongregationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CongregationTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithExistingCongregation_ReturnsAllFields()
    {
        await using var context = CreateContext();
        var congregation = new Congregation
        {
            Name = "Central",
            Email = "central@example.com",
            Coordinator = "Ana",
            CoordinatorPhone = "0711111111",
            DessertContactPerson = "Ion",
            DessertContactPhone = "0722222222",
            CanReceiveStudents = true,
            IsSmallCongregation = true,
            Message = "Hello"
        };
        context.Congregations.Add(congregation);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetCongregationByIdHandler(context).Handle(
            new GetCongregationByIdQuery { Id = congregation.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value;
        dto.Id.ShouldBe(congregation.Id);
        dto.Name.ShouldBe("Central");
        dto.Email.ShouldBe("central@example.com");
        dto.Coordinator.ShouldBe("Ana");
        dto.CoordinatorPhone.ShouldBe("0711111111");
        dto.DessertContactPerson.ShouldBe("Ion");
        dto.DessertContactPhone.ShouldBe("0722222222");
        dto.CanReceiveStudents.ShouldBeTrue();
        dto.IsSmallCongregation.ShouldBeTrue();
        dto.Message.ShouldBe("Hello");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsNotFound()
    {
        await using var context = CreateContext();

        var result = await new GetCongregationByIdHandler(context).Handle(
            new GetCongregationByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
    }

    [Test]
    public async Task Handle_WithSoftDeletedCongregation_ReturnsNotFound()
    {
        await using var context = CreateContext();
        var congregation = new Congregation
        {
            Name = "Gone",
            Email = "gone@example.com",
            DeletedDate = DateTimeOffset.UtcNow
        };
        context.Congregations.Add(congregation);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetCongregationByIdHandler(context).Handle(
            new GetCongregationByIdQuery { Id = congregation.Id }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is CongregationErrors.CongregationNotFound);
    }

    [Test]
    public void Validator_RejectsEmptyId() =>
        new GetCongregationByIdQueryValidator().Validate(new GetCongregationByIdQuery { Id = Guid.Empty })
            .Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
}
