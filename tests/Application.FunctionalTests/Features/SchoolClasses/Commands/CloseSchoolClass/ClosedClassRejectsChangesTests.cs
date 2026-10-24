using skestock.Application.Common.Errors;
using skestock.Application.Features.GoodsReceipts.Commands.ConfirmGoodsReceiptImport;
using skestock.Application.Features.OrderLists.Commands.CreateOrderList;
using skestock.Application.Features.OrderLists.Commands.SubmitOrderList;
using skestock.Application.Features.SchoolClasses.Commands.UpdateClassDepartmentResponsiblePerson;
using skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;
using skestock.Application.Features.Stock.Commands.AdjustStock;
using skestock.Application.Features.Stock.Commands.BulkAdjustStock;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.SchoolClasses.Commands.CloseSchoolClass;

public class ClosedClassRejectsChangesTests : TestBase
{
    private static async Task<SchoolClass> AddClassAsync(ClassStatus status)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"cls-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20),
            Status = status
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    private static async Task RunAsAdministratorWithProfileAsync()
    {
        var userId = await TestApp.RunAsAdministratorAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
    }

    [Test]
    public async Task DirectClassId_IsRejected()
    {
        await RunAsAdministratorWithProfileAsync();
        var closed = await AddClassAsync(ClassStatus.Closed);

        var result = await TestApp.SendAsync(new AdjustStockCommand { ClassId = closed.Id });

        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
    }

    [Test]
    public async Task BulkAdjustStock_OnClosedClass_IsRejected()
    {
        await RunAsAdministratorWithProfileAsync();
        var closed = await AddClassAsync(ClassStatus.Closed);

        var result = await TestApp.SendAsync(new BulkAdjustStockCommand { ClassId = closed.Id });

        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
    }

    [Test]
    public async Task ClassNamedBySchoolClassId_IsRejected()
    {
        await RunAsAdministratorWithProfileAsync();
        var closed = await AddClassAsync(ClassStatus.Closed);

        var result = await TestApp.SendAsync(new UpdateClassDepartmentResponsiblePersonCommand
        {
            SchoolClassId = closed.Id, DepartmentId = Guid.NewGuid(), ResponsiblePerson = "Ion"
        });

        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
    }

    [Test]
    public async Task ClassFoundThroughTheOrderList_IsRejected()
    {
        await RunAsAdministratorWithProfileAsync();
        var closed = await AddClassAsync(ClassStatus.Closed);
        var orderList = OrderList.Create(closed.Id, "Comanda", null);
        await TestApp.AddAsync(orderList);

        var result = await TestApp.SendAsync(new SubmitOrderListCommand { Id = orderList.Id });

        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
    }

    [Test]
    public async Task ClassFoundThroughTheImport_IsRejectedBeforeValidation()
    {
        await RunAsAdministratorWithProfileAsync();
        var closed = await AddClassAsync(ClassStatus.Closed);
        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(), OriginalName = "receipt.pdf", BlobContainer = "app-files",
            BlobPath = $"imports/{Guid.NewGuid():N}/receipt.pdf", ContentType = "application/pdf",
            SizeBytes = 2048, Status = FileStatus.Completed
        };
        await TestApp.AddAsync(file);
        var import = GoodsReceiptImport.Create(closed.Id, file.Id, TestApp.GetUserId()!.Value, file.BlobPath);
        await TestApp.AddAsync(import);

        // No lines and no class id: validation would reject this if it ran first.
        var result = await TestApp.SendAsync(new ConfirmGoodsReceiptImportCommand { ImportId = import.Id });

        result.Errors.ShouldContain(e => e is SchoolClassErrors.SchoolClassClosed);
    }

    [Test]
    public async Task OpenClass_IsNotRejected()
    {
        await RunAsAdministratorWithProfileAsync();
        var open = await AddClassAsync(ClassStatus.Active);

        var result = await TestApp.SendAsync(new CreateOrderListCommand { ClassId = open.Id, Name = "Comanda" });

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task EditingTheNameOfAClosedClass_StillWorks()
    {
        await RunAsAdministratorWithProfileAsync();
        var closed = await AddClassAsync(ClassStatus.Closed);

        var result = await TestApp.SendAsync(new UpdateSchoolClassCommand
        {
            Id = closed.Id, Name = $"ren-{Guid.NewGuid():N}"[..20], StartDate = closed.StartDate,
            EndDate = closed.EndDate, Status = ClassStatus.Closed
        });

        result.IsSuccess.ShouldBeTrue();
    }
}
