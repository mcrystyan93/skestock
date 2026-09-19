using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToClassItemStockVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClassItemStockVisibilities_ClassId_ItemId",
                table: "ClassItemStockVisibilities");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "ClassItemStockVisibilities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM [ClassItemStockVisibilities])
                   AND NOT EXISTS (SELECT 1 FROM [Locations])
                BEGIN
                    THROW 50000, 'Cannot migrate stock visibility settings because no locations exist.', 1;
                END;

                UPDATE visibility
                SET [LocationId] = firstLocation.[Id]
                FROM [ClassItemStockVisibilities] AS visibility
                CROSS JOIN (
                    SELECT TOP (1) [Id]
                    FROM [Locations]
                    ORDER BY [Id]
                ) AS firstLocation
                WHERE visibility.[LocationId] IS NULL;

                INSERT INTO [ClassItemStockVisibilities] (
                    [Id],
                    [ClassId],
                    [ItemId],
                    [LocationId],
                    [HideWhenZeroStock],
                    [CreatedDate],
                    [CreatedById],
                    [LastModifiedDate],
                    [LastModifiedById])
                SELECT
                    NEWID(),
                    visibility.[ClassId],
                    visibility.[ItemId],
                    location.[Id],
                    visibility.[HideWhenZeroStock],
                    visibility.[CreatedDate],
                    visibility.[CreatedById],
                    visibility.[LastModifiedDate],
                    visibility.[LastModifiedById]
                FROM [ClassItemStockVisibilities] AS visibility
                CROSS JOIN [Locations] AS location
                WHERE location.[Id] <> visibility.[LocationId];
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "LocationId",
                table: "ClassItemStockVisibilities",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassItemStockVisibilities_ClassId_ItemId_LocationId",
                table: "ClassItemStockVisibilities",
                columns: new[] { "ClassId", "ItemId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassItemStockVisibilities_LocationId",
                table: "ClassItemStockVisibilities",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassItemStockVisibilities_Locations_LocationId",
                table: "ClassItemStockVisibilities",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassItemStockVisibilities_Locations_LocationId",
                table: "ClassItemStockVisibilities");

            migrationBuilder.DropIndex(
                name: "IX_ClassItemStockVisibilities_ClassId_ItemId_LocationId",
                table: "ClassItemStockVisibilities");

            migrationBuilder.DropIndex(
                name: "IX_ClassItemStockVisibilities_LocationId",
                table: "ClassItemStockVisibilities");

            migrationBuilder.Sql(
                """
                DELETE visibility
                FROM [ClassItemStockVisibilities] AS visibility
                WHERE EXISTS (
                    SELECT 1
                    FROM [ClassItemStockVisibilities] AS duplicate
                    WHERE duplicate.[ClassId] = visibility.[ClassId]
                      AND duplicate.[ItemId] = visibility.[ItemId]
                      AND duplicate.[Id] < visibility.[Id]);
                """);

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "ClassItemStockVisibilities");

            migrationBuilder.CreateIndex(
                name: "IX_ClassItemStockVisibilities_ClassId_ItemId",
                table: "ClassItemStockVisibilities",
                columns: new[] { "ClassId", "ItemId" },
                unique: true);
        }
    }
}
