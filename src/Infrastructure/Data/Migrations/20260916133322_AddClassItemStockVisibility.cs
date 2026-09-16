using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClassItemStockVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassItemStockVisibilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HideWhenZeroStock = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassItemStockVisibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassItemStockVisibilities_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassItemStockVisibilities_SchoolClasses_ClassId",
                        column: x => x.ClassId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassItemStockVisibilities_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassItemStockVisibilities_UserProfiles_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassItemStockVisibilities_ClassId_ItemId",
                table: "ClassItemStockVisibilities",
                columns: new[] { "ClassId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassItemStockVisibilities_CreatedById",
                table: "ClassItemStockVisibilities",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClassItemStockVisibilities_ItemId",
                table: "ClassItemStockVisibilities",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassItemStockVisibilities_LastModifiedById",
                table: "ClassItemStockVisibilities",
                column: "LastModifiedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassItemStockVisibilities");
        }
    }
}
