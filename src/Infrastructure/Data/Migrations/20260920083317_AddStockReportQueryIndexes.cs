using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReportQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_ClassId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockBatches_ReceivedClassId",
                table: "StockBatches");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ClassId_ItemId_LocationId_CreatedAt",
                table: "StockTransactions",
                columns: new[] { "ClassId", "ItemId", "LocationId", "CreatedAt" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_ReceivedClassId_LocationId_ItemId",
                table: "StockBatches",
                columns: new[] { "ReceivedClassId", "LocationId", "ItemId" })
                .Annotation("SqlServer:Include", new[] { "Quantity", "ExpiryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_ClassId_ItemId_LocationId_CreatedAt",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockBatches_ReceivedClassId_LocationId_ItemId",
                table: "StockBatches");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ClassId",
                table: "StockTransactions",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBatches_ReceivedClassId",
                table: "StockBatches",
                column: "ReceivedClassId");
        }
    }
}
