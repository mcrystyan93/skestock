using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCongregations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Congregations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_CI_AI"),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Coordinator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoordinatorPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DessertContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DessertContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CanReceiveStudents = table.Column<bool>(type: "bit", nullable: false),
                    IsSmallCongregation = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Congregations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Congregations_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Congregations_UserProfiles_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Congregations_CreatedById",
                table: "Congregations",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Congregations_CreatedDate_Id",
                table: "Congregations",
                columns: new[] { "CreatedDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Congregations_LastModifiedById",
                table: "Congregations",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_Congregations_LastModifiedDate_Id",
                table: "Congregations",
                columns: new[] { "LastModifiedDate", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Congregations_Name_Id",
                table: "Congregations",
                columns: new[] { "Name", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_Congregations_Name",
                table: "Congregations",
                column: "Name",
                unique: true,
                filter: "[DeletedDate] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Congregations");
        }
    }
}
