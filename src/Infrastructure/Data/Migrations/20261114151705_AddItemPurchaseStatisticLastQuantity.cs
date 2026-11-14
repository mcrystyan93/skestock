using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddItemPurchaseStatisticLastQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastQuantity",
                table: "ItemPurchaseStatistics",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastQuantity",
                table: "ItemPurchaseStatistics");
        }
    }
}
