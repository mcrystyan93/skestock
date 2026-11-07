using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Application.Features.FruitDessertPlans.Queries.GetInformarePdf;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans.Queries;

public class GetInformarePdfHandlerTests
{
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];

    private Mock<IInformarePdfRenderer> _pdf = null!;

    [SetUp]
    public void SetUp()
    {
        _pdf = new Mock<IInformarePdfRenderer>();
        _pdf.Setup(p => p.Render(It.IsAny<string>(), It.IsAny<string>())).Returns(Pdf);
    }

    private GetInformarePdfQueryHandler CreateHandler(FruitDessertTestDbContext context) =>
        new(context, new MessageTemplateRenderer(), _pdf.Object);

    private sealed record Seed(SchoolClass Class, FruitDessertPlanEntry Entry, Congregation Congregation);

    private static async Task<Seed> SeedAsync(
        FruitDessertTestDbContext context,
        string? contactPerson = "Maria",
        string? templateBody = "Bună {ContactPerson}, {Congregation} la {Date} pentru {ClassName}",
        bool withCongregation = true)
    {
        var schoolClass = new SchoolClass { Name = "Toamna" };
        var congregation = new Congregation
        {
            Name = "Nord", Email = "n@example.com", DessertContactPerson = contactPerson, DessertContactPhone = null
        };
        context.SchoolClasses.Add(schoolClass);
        context.Congregations.Add(congregation);
        if (templateBody is not null)
            context.MessageTemplates.Add(new MessageTemplate
            {
                Type = FruitDessertMessageType.Informare, Name = "Informare fructe", Body = templateBody, IsActive = true,
                CreatedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        var entry = new FruitDessertPlanEntry
        {
            SchoolClassId = schoolClass.Id,
            Date = new DateOnly(2026, 11, 9),
            CongregationId = withCongregation ? congregation.Id : null
        };
        context.FruitDessertPlanEntries.Add(entry);
        await context.SaveChangesAsync();
        return new Seed(schoolClass, entry, congregation);
    }

    private static GetInformarePdfQuery QueryFor(Seed seed) => new() { ClassId = seed.Class.Id, EntryId = seed.Entry.Id };

    [Test]
    public async Task Handle_Success_ReturnsPdfFile_AndRendersWithDayValues()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(QueryFor(seed), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.FileName.ShouldBe("Informare-fructe-si-desert-2026-11-09.pdf");
        result.Value.ContentType.ShouldBe("application/pdf");
        result.Value.Content.ShouldBe(Pdf);
        _pdf.Verify(p => p.Render("Informare fructe", "Bună Maria, Nord la luni, 9 noiembrie 2026 pentru Toamna"), Times.Once);
        context.FruitDessertMessages.Count().ShouldBe(0);
    }

    [Test]
    public async Task Handle_UnknownEntry_ReturnsEntryNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(
            new GetInformarePdfQuery { ClassId = seed.Class.Id, EntryId = Guid.NewGuid() }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.EntryNotFound);
    }

    [Test]
    public async Task Handle_EntryOfAnotherClass_ReturnsEntryNotFound()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);

        var result = await CreateHandler(context).Handle(
            new GetInformarePdfQuery { ClassId = Guid.NewGuid(), EntryId = seed.Entry.Id }, CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.EntryNotFound);
    }

    [Test]
    public async Task Handle_NoCongregation_ReturnsContactMissing()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, withCongregation: false);

        var result = await CreateHandler(context).Handle(QueryFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.ContactMissing);
        _pdf.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_SoftDeletedCongregation_StillSucceeds()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context);
        seed.Congregation.DeletedDate = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(QueryFor(seed), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_MissingContactPerson_ReturnsContactMissing()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, contactPerson: null);

        var result = await CreateHandler(context).Handle(QueryFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.ContactMissing);
        _pdf.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Handle_NoActiveTemplate_ReturnsActiveTemplateMissing()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, templateBody: null);

        var result = await CreateHandler(context).Handle(QueryFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.ActiveTemplateMissing);
    }

    [Test]
    public async Task Handle_TemplateWithUnknownPlaceholder_Fails()
    {
        await using var context = FruitDessertTestDbContext.Create();
        var seed = await SeedAsync(context, templateBody: "Salut {Foo}");

        var result = await CreateHandler(context).Handle(QueryFor(seed), CancellationToken.None);

        result.Errors.ShouldContain(e => e is FruitDessertErrors.UnknownPlaceholder);
        _pdf.VerifyNoOtherCalls();
    }
}
