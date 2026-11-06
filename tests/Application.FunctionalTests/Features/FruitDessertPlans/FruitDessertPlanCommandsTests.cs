using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertCongregation;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertEmailSent;
using skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.FruitDessertPlans;

public class FruitDessertPlanCommandsTests : TestBase
{
    private static async Task<SchoolClass> AddClassAsync(ClassStatus status = ClassStatus.Active)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"cls-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 11, 2),
            EndDate = new DateOnly(2026, 11, 13),
            Status = status
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    private static async Task<Congregation> AddCongregationAsync()
    {
        var congregation = new Congregation
        {
            Name = $"FT{Guid.NewGuid():N}"[..12],
            Email = "c@example.com",
            DessertContactPerson = "Maria",
            DessertContactPhone = "0722 123 456"
        };
        await TestApp.AddAsync(congregation);
        return congregation;
    }

    private static async Task<(SchoolClass Class, Guid EntryId)> AddClassWithEntryAsync()
    {
        var schoolClass = await AddClassAsync();
        var plan = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });
        return (schoolClass, plan.Value[0].Id);
    }

    [Test]
    public async Task SendMessage_AppendsHistoryRowWithSnapshot_AndResendAppendsAnother()
    {
        var (schoolClass, entryId) = await AddClassWithEntryAsync();
        var congregation = await AddCongregationAsync();
        await TestApp.SendAsync(new SetFruitDessertCongregationCommand
        {
            ClassId = schoolClass.Id, EntryId = entryId, CongregationId = congregation.Id
        });
        var command = new SendFruitDessertMessageCommand
        {
            ClassId = schoolClass.Id, EntryId = entryId, Type = FruitDessertMessageType.Reminder
        };

        var first = await TestApp.SendAsync(command);
        var second = await TestApp.SendAsync(command);

        first.IsSuccess.ShouldBeTrue();
        first.Value.Url.ShouldStartWith("https://wa.me/40722123456?text=");
        second.IsSuccess.ShouldBeTrue();
        var messages = await FruitDessertPlanQueryTests.ExecuteAsync(context =>
            context.FruitDessertMessages.Where(m => m.EntryId == entryId).ToListAsync());
        messages.Count.ShouldBe(2);
        messages.ShouldAllBe(m => m.RecipientPhone == "40722123456" && m.Body.Contains("Maria")
                                  && m.Status == FruitDessertMessageStatus.Opened);

        var plan = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });
        var entry = plan.Value.Single(e => e.Id == entryId);
        entry.ReminderSent.ShouldBeTrue();
        entry.ThanksSent.ShouldBeFalse();
    }

    [Test]
    public async Task SetCongregationAndEmailSent_PersistOnOpenClass()
    {
        var (schoolClass, entryId) = await AddClassWithEntryAsync();
        var congregation = await AddCongregationAsync();

        var set = await TestApp.SendAsync(new SetFruitDessertCongregationCommand
        {
            ClassId = schoolClass.Id, EntryId = entryId, CongregationId = congregation.Id
        });
        var email = await TestApp.SendAsync(new SetFruitDessertEmailSentCommand
        {
            ClassId = schoolClass.Id, EntryId = entryId, EmailSent = true
        });

        set.Value.CongregationName.ShouldBe(congregation.Name);
        email.Value.EmailSent.ShouldBeTrue();
    }

    [Test]
    public async Task ClosedClass_RejectsAllThreeCommands()
    {
        var closed = await AddClassAsync(ClassStatus.Closed);
        var entryId = Guid.NewGuid();

        var congregation = await TestApp.SendAsync(new SetFruitDessertCongregationCommand
        {
            ClassId = closed.Id, EntryId = entryId
        });
        var email = await TestApp.SendAsync(new SetFruitDessertEmailSentCommand
        {
            ClassId = closed.Id, EntryId = entryId, EmailSent = true
        });
        var message = await TestApp.SendAsync(new SendFruitDessertMessageCommand
        {
            ClassId = closed.Id, EntryId = entryId, Type = FruitDessertMessageType.Reminder
        });

        congregation.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
        email.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
        message.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
    }
}
