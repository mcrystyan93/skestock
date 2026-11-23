using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Expenses.Commands.UpdateExpense;

namespace skestock.Application.UnitTests.Features.Expenses;

public class ExpenseValidatorTests
{
    private static UpdateExpenseCommandValidator CreateValidator() =>
        new(new ExpenseTestDbContext(new DbContextOptionsBuilder<ExpenseTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options));

    [Test]
    public async Task ValidFields_Pass()
    {
        var result = await CreateValidator().ValidateAsync(new UpdateExpenseCommand
        {
            Id = Guid.NewGuid(), SupplierName = "ACME", Currency = "RON", TotalAmount = 100m, VatAmount = 19m
        });

        result.IsValid.ShouldBeTrue();
    }

    [TestCase("RO")]
    [TestCase("")]
    public async Task InvalidCurrency_Fails(string currency)
    {
        var result = await CreateValidator().ValidateAsync(new UpdateExpenseCommand { Id = Guid.NewGuid(), Currency = currency });

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task VatGreaterThanTotal_Fails()
    {
        var result = await CreateValidator().ValidateAsync(new UpdateExpenseCommand
        {
            Id = Guid.NewGuid(), Currency = "RON", TotalAmount = 10m, VatAmount = 20m
        });

        result.IsValid.ShouldBeFalse();
    }

    [TestCase(1999, 12, 31, false)]
    [TestCase(2101, 1, 1, false)]
    [TestCase(1, 1, 1, false)]
    [TestCase(2000, 1, 1, true)]
    [TestCase(2026, 11, 23, true)]
    public async Task ExpenseDate_MustBeWithinSaneRange(int year, int month, int day, bool valid)
    {
        var result = await CreateValidator().ValidateAsync(new UpdateExpenseCommand
        {
            Id = Guid.NewGuid(), Currency = "RON", ExpenseDate = new DateOnly(year, month, day)
        });

        result.IsValid.ShouldBe(valid);
    }

    [Test]
    public async Task NegativeTotal_Fails()
    {
        var result = await CreateValidator().ValidateAsync(new UpdateExpenseCommand
        {
            Id = Guid.NewGuid(), Currency = "RON", TotalAmount = -1m
        });

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task UnknownCategory_Fails()
    {
        var result = await CreateValidator().ValidateAsync(new UpdateExpenseCommand
        {
            Id = Guid.NewGuid(), Currency = "RON", ExpenseCategoryId = Guid.NewGuid()
        });

        result.IsValid.ShouldBeFalse();
    }
}
