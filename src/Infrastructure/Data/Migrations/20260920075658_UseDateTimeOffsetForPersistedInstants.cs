using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations;

/// <summary>
/// Changes persisted instants from datetime2 to datetimeoffset while explicitly treating the
/// legacy datetime2 values as UTC. The temporary-column approach avoids relying on a provider
/// conversion that could interpret legacy values in the server's local time zone.
/// </summary>
public partial class UseDateTimeOffsetForPersistedInstants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CategoryImportBatches_UploadedAt_Id",
            table: "CategoryImportBatches");
        migrationBuilder.DropIndex(
            name: "IX_ItemImportBatches_UploadedAt_Id",
            table: "ItemImportBatches");
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_ClaimedUntilUtc_CreatedAtUtc",
            table: "OutboxMessages");

        ChangeToDateTimeOffset(migrationBuilder, "CategoryImportBatches", "UploadedAt", nullable: false);
        ChangeToDateTimeOffset(migrationBuilder, "CategoryImportBatches", "ProcessedAt", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "CategoryImportBatches", "ProcessingLeaseUntilUtc", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "GoodsReceiptImports", "UploadedAt", nullable: false);
        ChangeToDateTimeOffset(migrationBuilder, "GoodsReceiptImports", "ProcessedAt", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "GoodsReceipts", "ReceivedAt", nullable: false);
        ChangeToDateTimeOffset(migrationBuilder, "ItemImportBatches", "UploadedAt", nullable: false);
        ChangeToDateTimeOffset(migrationBuilder, "ItemImportBatches", "ProcessedAt", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "ItemImportBatches", "ProcessingLeaseUntilUtc", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "OrderLists", "SubmittedAt", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "OutboxMessages", "CreatedAtUtc", nullable: false);
        ChangeToDateTimeOffset(migrationBuilder, "OutboxMessages", "ProcessedAtUtc", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "OutboxMessages", "ClaimedUntilUtc", nullable: true);
        ChangeToDateTimeOffset(migrationBuilder, "ProcessedMessages", "ProcessedAtUtc", nullable: false);
        ChangeToDateTimeOffset(migrationBuilder, "StockTransactions", "CreatedAt", nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CategoryImportBatches_UploadedAt_Id",
            table: "CategoryImportBatches",
            columns: new[] { "UploadedAt", "Id" });
        migrationBuilder.CreateIndex(
            name: "IX_ItemImportBatches_UploadedAt_Id",
            table: "ItemImportBatches",
            columns: new[] { "UploadedAt", "Id" });
        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_ClaimedUntilUtc_CreatedAtUtc",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAtUtc", "ClaimedUntilUtc", "CreatedAtUtc" },
            filter: "[ProcessedAtUtc] IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CategoryImportBatches_UploadedAt_Id",
            table: "CategoryImportBatches");
        migrationBuilder.DropIndex(
            name: "IX_ItemImportBatches_UploadedAt_Id",
            table: "ItemImportBatches");
        migrationBuilder.DropIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_ClaimedUntilUtc_CreatedAtUtc",
            table: "OutboxMessages");

        ChangeToDateTime(migrationBuilder, "CategoryImportBatches", "UploadedAt", nullable: false);
        ChangeToDateTime(migrationBuilder, "CategoryImportBatches", "ProcessedAt", nullable: true);
        ChangeToDateTime(migrationBuilder, "CategoryImportBatches", "ProcessingLeaseUntilUtc", nullable: true);
        ChangeToDateTime(migrationBuilder, "GoodsReceiptImports", "UploadedAt", nullable: false);
        ChangeToDateTime(migrationBuilder, "GoodsReceiptImports", "ProcessedAt", nullable: true);
        ChangeToDateTime(migrationBuilder, "GoodsReceipts", "ReceivedAt", nullable: false);
        ChangeToDateTime(migrationBuilder, "ItemImportBatches", "UploadedAt", nullable: false);
        ChangeToDateTime(migrationBuilder, "ItemImportBatches", "ProcessedAt", nullable: true);
        ChangeToDateTime(migrationBuilder, "ItemImportBatches", "ProcessingLeaseUntilUtc", nullable: true);
        ChangeToDateTime(migrationBuilder, "OrderLists", "SubmittedAt", nullable: true);
        ChangeToDateTime(migrationBuilder, "OutboxMessages", "CreatedAtUtc", nullable: false);
        ChangeToDateTime(migrationBuilder, "OutboxMessages", "ProcessedAtUtc", nullable: true);
        ChangeToDateTime(migrationBuilder, "OutboxMessages", "ClaimedUntilUtc", nullable: true);
        ChangeToDateTime(migrationBuilder, "ProcessedMessages", "ProcessedAtUtc", nullable: false);
        ChangeToDateTime(migrationBuilder, "StockTransactions", "CreatedAt", nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CategoryImportBatches_UploadedAt_Id",
            table: "CategoryImportBatches",
            columns: new[] { "UploadedAt", "Id" });
        migrationBuilder.CreateIndex(
            name: "IX_ItemImportBatches_UploadedAt_Id",
            table: "ItemImportBatches",
            columns: new[] { "UploadedAt", "Id" });
        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_ProcessedAtUtc_ClaimedUntilUtc_CreatedAtUtc",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAtUtc", "ClaimedUntilUtc", "CreatedAtUtc" },
            filter: "[ProcessedAtUtc] IS NULL");
    }

    private static void ChangeToDateTimeOffset(
        MigrationBuilder migrationBuilder,
        string table,
        string column,
        bool nullable)
    {
        var temporaryColumn = $"{column}_DateTimeOffset";
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: temporaryColumn,
            table: table,
            type: "datetimeoffset",
            nullable: true);
        migrationBuilder.Sql($"""
            UPDATE [{table}]
            SET [{temporaryColumn}] = [{column}] AT TIME ZONE 'UTC';
            """);
        migrationBuilder.DropColumn(name: column, table: table);
        migrationBuilder.RenameColumn(
            name: temporaryColumn,
            table: table,
            newName: column);

        if (!nullable)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: column,
                table: table,
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }

    private static void ChangeToDateTime(
        MigrationBuilder migrationBuilder,
        string table,
        string column,
        bool nullable)
    {
        var temporaryColumn = $"{column}_DateTime";
        migrationBuilder.AddColumn<DateTime>(
            name: temporaryColumn,
            table: table,
            type: "datetime2",
            nullable: true);
        migrationBuilder.Sql($"""
            UPDATE [{table}]
            SET [{temporaryColumn}] = CONVERT(datetime2, SWITCHOFFSET([{column}], '+00:00'));
            """);
        migrationBuilder.DropColumn(name: column, table: table);
        migrationBuilder.RenameColumn(
            name: temporaryColumn,
            table: table,
            newName: column);

        if (!nullable)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: column,
                table: table,
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
