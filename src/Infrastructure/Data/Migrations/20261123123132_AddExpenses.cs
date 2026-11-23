using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class _20261123123132_AddExpenses : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExpenseCategories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExpenseCategories_UserProfiles_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "UserProfiles",
                    principalColumn: "IdentityId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExpenseCategories_UserProfiles_LastModifiedById",
                    column: x => x.LastModifiedById,
                    principalTable: "UserProfiles",
                    principalColumn: "IdentityId",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "ExpenseCategoriesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", null)
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateTable(
            name: "Expenses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Source = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                SupplierName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ExpenseDate = table.Column<DateOnly>(type: "date", nullable: true),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                ExpenseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                FileMetadataId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ExtractedDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Expenses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId",
                    column: x => x.ExpenseCategoryId,
                    principalTable: "ExpenseCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Expenses_FileMetadata_FileMetadataId",
                    column: x => x.FileMetadataId,
                    principalTable: "FileMetadata",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Expenses_SchoolClasses_SchoolClassId",
                    column: x => x.SchoolClassId,
                    principalTable: "SchoolClasses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Expenses_UserProfiles_CreatedById",
                    column: x => x.CreatedById,
                    principalTable: "UserProfiles",
                    principalColumn: "IdentityId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Expenses_UserProfiles_LastModifiedById",
                    column: x => x.LastModifiedById,
                    principalTable: "UserProfiles",
                    principalColumn: "IdentityId",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Expenses_UserProfiles_UploadedByUserId",
                    column: x => x.UploadedByUserId,
                    principalTable: "UserProfiles",
                    principalColumn: "IdentityId",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "ExpensesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", null)
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseCategories_CreatedById",
            table: "ExpenseCategories",
            column: "CreatedById");

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseCategories_LastModifiedById",
            table: "ExpenseCategories",
            column: "LastModifiedById");

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseCategories_Name",
            table: "ExpenseCategories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_Category_ExpenseDate",
            table: "Expenses",
            columns: new[] { "ExpenseCategoryId", "ExpenseDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_Class_CreatedDate_Id",
            table: "Expenses",
            columns: new[] { "SchoolClassId", "CreatedDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_Class_ExpenseDate_Id",
            table: "Expenses",
            columns: new[] { "SchoolClassId", "ExpenseDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_Class_Status_CreatedDate_Id",
            table: "Expenses",
            columns: new[] { "SchoolClassId", "Status", "CreatedDate", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_CreatedById",
            table: "Expenses",
            column: "CreatedById");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_FileMetadataId",
            table: "Expenses",
            column: "FileMetadataId");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_LastModifiedById",
            table: "Expenses",
            column: "LastModifiedById");

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_Supplier_ExpenseDate",
            table: "Expenses",
            columns: new[] { "SupplierName", "ExpenseDate" });

        migrationBuilder.CreateIndex(
            name: "IX_Expenses_UploadedByUserId",
            table: "Expenses",
            column: "UploadedByUserId");

        // EF cannot set temporal history retention; see SetTemporalRetention.
        migrationBuilder.Sql("ALTER TABLE [dbo].[ExpenseCategories] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
        migrationBuilder.Sql("CREATE INDEX [IX_ExpenseCategoriesHistory_Id] ON [dbo].[ExpenseCategoriesHistory] ([Id], [PeriodStart] DESC);");
        migrationBuilder.Sql("ALTER TABLE [dbo].[Expenses] SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 2 YEARS));");
        migrationBuilder.Sql("CREATE INDEX [IX_ExpensesHistory_Id] ON [dbo].[ExpensesHistory] ([Id], [PeriodStart] DESC);");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Expenses")
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "ExpensesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", null)
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.DropTable(
            name: "ExpenseCategories")
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "ExpenseCategoriesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", null)
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
    }
}
