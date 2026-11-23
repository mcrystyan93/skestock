using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Filtering;
using skestock.Application.Features.Expenses.Queries.GetAllExpenses;
using skestock.Domain.Entities.Expenses;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.UnitTests.Features.Expenses;

public class GetAllExpensesHandlerTests
{
    private static ExpenseTestDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ExpenseTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SchoolClass NewClass(string name) =>
        new() { Name = name, StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 20) };

    private static Expense NewExpense(SchoolClass schoolClass, int minutesAgo, string supplier)
    {
        var expense = Expense.CreateManual(schoolClass.Id, Guid.NewGuid());
        expense.SchoolClass = schoolClass;
        expense.SupplierName = supplier;
        expense.CreatedDate = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo);
        return expense;
    }

    private static GetAllExpensesQuery ForClass(Guid classId, int pageSize, string? cursor = null, string? search = null) =>
        new()
        {
            Filters = [new ColumnFilter("classId", FilterOperator.Equals, classId)],
            PageSize = pageSize,
            Cursor = cursor,
            SearchTerm = search
        };

    [Test]
    public async Task Handle_PagesWithKeysetCursorNewestFirstAndStaysInsideTheClass()
    {
        await using var context = CreateContext();
        var classA = NewClass("A");
        var classB = NewClass("B");
        context.SchoolClasses.AddRange(classA, classB);
        context.Expenses.AddRange(
            NewExpense(classA, 3, "oldest"), NewExpense(classA, 2, "middle"), NewExpense(classA, 1, "newest"),
            NewExpense(classB, 0, "other class"));
        await context.SaveChangesAsync();
        var handler = new GetAllExpensesHandler(context);

        var first = await handler.Handle(ForClass(classA.Id, 2), CancellationToken.None);
        var second = await handler.Handle(ForClass(classA.Id, 2, first.Value.NextCursor), CancellationToken.None);

        first.Value.Data.Select(e => e.SupplierName).ShouldBe(["newest", "middle"]);
        first.Value.HasNextPage.ShouldBeTrue();
        second.Value.Data.Select(e => e.SupplierName).ShouldBe(["oldest"]);
        second.Value.HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_HidesSoftDeletedExpenses()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass("A");
        context.SchoolClasses.Add(schoolClass);
        var deleted = NewExpense(schoolClass, 1, "deleted");
        deleted.DeletedDate = DateTimeOffset.UtcNow;
        context.Expenses.AddRange(deleted, NewExpense(schoolClass, 2, "kept"));
        await context.SaveChangesAsync();

        var result = await new GetAllExpensesHandler(context).Handle(ForClass(schoolClass.Id, 10), CancellationToken.None);

        result.Value.Data.Select(e => e.SupplierName).ShouldBe(["kept"]);
    }

    [Test]
    public async Task Handle_SearchMatchesSupplierName()
    {
        await using var context = CreateContext();
        var schoolClass = NewClass("A");
        context.SchoolClasses.Add(schoolClass);
        context.Expenses.AddRange(NewExpense(schoolClass, 1, "ACME SRL"), NewExpense(schoolClass, 2, "Librarie"));
        await context.SaveChangesAsync();

        var result = await new GetAllExpensesHandler(context)
            .Handle(ForClass(schoolClass.Id, 10, search: "ACME"), CancellationToken.None);

        result.Value.Data.Select(e => e.SupplierName).ShouldBe(["ACME SRL"]);
    }
}
