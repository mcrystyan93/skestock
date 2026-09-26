using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemPurchaseStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemPurchaseStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseCount = table.Column<int>(type: "int", nullable: false),
                    AverageQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AverageUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastPurchasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPurchaseStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemPurchaseStatistics_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemPurchaseStatistics_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPurchaseStatistics_ClassId",
                table: "ItemPurchaseStatistics",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemPurchaseStatistics_ItemId_Scope",
                table: "ItemPurchaseStatistics",
                columns: new[] { "ItemId", "Scope" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemPurchaseStatistics_Scope_ClassId_ItemId",
                table: "ItemPurchaseStatistics",
                columns: new[] { "Scope", "ClassId", "ItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemPurchaseStatistics");
        }
    }
}
