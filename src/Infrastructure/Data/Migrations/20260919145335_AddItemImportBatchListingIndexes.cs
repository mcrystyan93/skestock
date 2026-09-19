using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemImportBatchListingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ItemImportBatches_LastModifiedDate_Id",
                table: "ItemImportBatches",
                columns: new[] { "LastModifiedDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemImportBatches_Status_CreatedDate_Id",
                table: "ItemImportBatches",
                columns: new[] { "Status", "CreatedDate", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ItemImportBatches_LastModifiedDate_Id",
                table: "ItemImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_ItemImportBatches_Status_CreatedDate_Id",
                table: "ItemImportBatches");
        }
    }
}
