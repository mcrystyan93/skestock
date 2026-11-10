using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChangeInvitationCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceInvitationCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstructorInvitationCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpeningPrayerInvitationCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StudentInvitationCount",
                table: "SharedClassConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChangeInvitationCount",
                table: "SchoolClasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExperienceInvitationCount",
                table: "SchoolClasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstructorInvitationCount",
                table: "SchoolClasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OpeningPrayerInvitationCount",
                table: "SchoolClasses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentInvitationCount",
                table: "SchoolClasses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeInvitationCount",
                table: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "ExperienceInvitationCount",
                table: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "InstructorInvitationCount",
                table: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "OpeningPrayerInvitationCount",
                table: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "StudentInvitationCount",
                table: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "ChangeInvitationCount",
                table: "SchoolClasses");

            migrationBuilder.DropColumn(
                name: "ExperienceInvitationCount",
                table: "SchoolClasses");

            migrationBuilder.DropColumn(
                name: "InstructorInvitationCount",
                table: "SchoolClasses");

            migrationBuilder.DropColumn(
                name: "OpeningPrayerInvitationCount",
                table: "SchoolClasses");

            migrationBuilder.DropColumn(
                name: "StudentInvitationCount",
                table: "SchoolClasses");
        }
    }
}
