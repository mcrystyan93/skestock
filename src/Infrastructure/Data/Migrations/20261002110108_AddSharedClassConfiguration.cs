using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedClassConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvitationCount",
                table: "SchoolClasses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassDepartment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Responsibilities = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsiblePerson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassDepartment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassDepartment_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SharedClassConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    InvitationCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedClassConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedClassConfigurationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Responsibilities = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentTemplate_SharedClassConfigurations_SharedClassConfigurationId",
                        column: x => x.SharedClassConfigurationId,
                        principalTable: "SharedClassConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassDepartment_SchoolClassId",
                table: "ClassDepartment",
                column: "SchoolClassId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentTemplate_SharedClassConfigurationId",
                table: "DepartmentTemplate",
                column: "SharedClassConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassDepartment");

            migrationBuilder.DropTable(
                name: "DepartmentTemplate");

            migrationBuilder.DropTable(
                name: "SharedClassConfigurations");

            migrationBuilder.DropColumn(
                name: "InvitationCount",
                table: "SchoolClasses");
        }
    }
}
