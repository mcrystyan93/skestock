using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFruitDessertPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FruitDessertPlanEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CongregationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FruitDessertPlanEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FruitDessertPlanEntries_Congregations_CongregationId",
                        column: x => x.CongregationId,
                        principalTable: "Congregations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FruitDessertPlanEntries_SchoolClasses_SchoolClassId",
                        column: x => x.SchoolClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FruitDessertPlanEntries_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FruitDessertPlanEntries_UserProfiles_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageTemplates_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageTemplates_UserProfiles_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FruitDessertMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecipientPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FruitDessertMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FruitDessertMessages_FruitDessertPlanEntries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "FruitDessertPlanEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FruitDessertMessages_MessageTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "MessageTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FruitDessertMessages_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FruitDessertMessages_UserProfiles_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MessageTemplates",
                columns: new[] { "Id", "Body", "CreatedById", "CreatedDate", "IsActive", "LastModifiedById", "LastModifiedDate", "Name", "Type" },
                values: new object[,]
                {
                    { new Guid("0197c0de-0000-7000-8000-000000000001"), "Bună ziua, {ContactPerson}! Vă reamintim că {Date} congregația {Congregation} aduce fructe și desert pentru {ClassName}. Vă mulțumim!", null, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Reminder fructe și desert", 1 },
                    { new Guid("0197c0de-0000-7000-8000-000000000002"), "Bună ziua, {ContactPerson}! Vă mulțumim că {Date} congregația {Congregation} a adus fructe și desert pentru {ClassName}. Dumnezeu să vă binecuvânteze!", null, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mulțumire fructe și desert", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FruitDessertMessages_CreatedById",
                table: "FruitDessertMessages",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FruitDessertMessages_EntryId_Type",
                table: "FruitDessertMessages",
                columns: new[] { "EntryId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_FruitDessertMessages_LastModifiedById",
                table: "FruitDessertMessages",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_FruitDessertMessages_TemplateId",
                table: "FruitDessertMessages",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FruitDessertPlanEntries_CongregationId",
                table: "FruitDessertPlanEntries",
                column: "CongregationId");

            migrationBuilder.CreateIndex(
                name: "IX_FruitDessertPlanEntries_CreatedById",
                table: "FruitDessertPlanEntries",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FruitDessertPlanEntries_LastModifiedById",
                table: "FruitDessertPlanEntries",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "UX_FruitDessertPlanEntries_SchoolClassId_Date",
                table: "FruitDessertPlanEntries",
                columns: new[] { "SchoolClassId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_CreatedById",
                table: "MessageTemplates",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_LastModifiedById",
                table: "MessageTemplates",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "UX_MessageTemplates_Type_Active",
                table: "MessageTemplates",
                column: "Type",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FruitDessertMessages");

            migrationBuilder.DropTable(
                name: "FruitDessertPlanEntries");

            migrationBuilder.DropTable(
                name: "MessageTemplates");
        }
    }
}
