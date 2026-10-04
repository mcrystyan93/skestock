using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomSeatCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Room2SeatCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Room4SeatCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Room6SeatCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Room2SeatCount",
                table: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "Room4SeatCount",
                table: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "Room6SeatCount",
                table: "SharedClassConfigurations");
        }
    }
}
