using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemListingIndexesAndCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_CategoryId",
                table: "Items");

            // SQL Server can't ALTER a column's collation while an index depends on it,
            // so drop the filtered unique Sku index, re-collate, then recreate it.
            migrationBuilder.DropIndex(
                name: "IX_Items_Sku",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Items",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "Latin1_General_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Items",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "Latin1_General_100_CI_AI",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Sku",
                table: "Items",
                column: "Sku",
                unique: true,
                filter: "[Sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId_CreatedDate_Id",
                table: "Items",
                columns: new[] { "CategoryId", "CreatedDate", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Items_CreatedDate_Id",
                table: "Items",
                columns: new[] { "CreatedDate", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Items_IsActive_CreatedDate_Id",
                table: "Items",
                columns: new[] { "IsActive", "CreatedDate", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Items_LastModifiedDate_Id",
                table: "Items",
                columns: new[] { "LastModifiedDate", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Name_Id",
                table: "Items",
                columns: new[] { "Name", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_CategoryId_CreatedDate_Id",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_CreatedDate_Id",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_IsActive_CreatedDate_Id",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_LastModifiedDate_Id",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_Name_Id",
                table: "Items");

            // Mirror the Up fix: the Sku collation revert also can't run while its index exists.
            migrationBuilder.DropIndex(
                name: "IX_Items_Sku",
                table: "Items");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "Items",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true,
                oldCollation: "Latin1_General_100_CI_AI");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Items",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldCollation: "Latin1_General_100_CI_AI");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Sku",
                table: "Items",
                column: "Sku",
                unique: true,
                filter: "[Sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Items_CategoryId",
                table: "Items",
                column: "CategoryId");
        }
    }
}
