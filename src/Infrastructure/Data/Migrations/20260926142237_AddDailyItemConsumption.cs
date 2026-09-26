using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyItemConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyItemConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyItemConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyItemConsumptions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyItemConsumptions_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyItemConsumptions_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyItemConsumptions_ClassId_Date",
                table: "DailyItemConsumptions",
                columns: new[] { "ClassId", "Date" })
                .Annotation("SqlServer:Include", new[] { "Quantity", "TotalValue" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyItemConsumptions_Date_ItemId_ClassId_LocationId",
                table: "DailyItemConsumptions",
                columns: new[] { "Date", "ItemId", "ClassId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyItemConsumptions_ItemId_Date",
                table: "DailyItemConsumptions",
                columns: new[] { "ItemId", "Date" })
                .Annotation("SqlServer:Include", new[] { "Quantity", "TotalValue" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyItemConsumptions_LocationId",
                table: "DailyItemConsumptions",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyItemConsumptions");
        }
    }
}
