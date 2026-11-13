using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemDefaultLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "Items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_LocationId",
                table: "Items",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Locations_LocationId",
                table: "Items",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill: location holding the most remaining stock; items with no stock stay NULL.
            migrationBuilder.Sql("""
                UPDATE i SET i.[LocationId] = x.[LocationId]
                FROM [Items] i
                CROSS APPLY (
                    SELECT TOP 1 b.[LocationId] FROM [StockBatches] b
                    WHERE b.[ItemId] = i.[Id]
                    GROUP BY b.[LocationId]
                    HAVING SUM(b.[Quantity]) > 0
                    ORDER BY SUM(b.[Quantity]) DESC, b.[LocationId]
                ) x;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Locations_LocationId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_LocationId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Items");
        }
    }
}
