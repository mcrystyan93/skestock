using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplyLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupplyLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IntervalWeeks = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyLists", x => x.Id);
                    table.CheckConstraint("CK_SupplyLists_IntervalWeeks", "([Frequency] = 'EveryXWeeks' AND [IntervalWeeks] BETWEEN 2 AND 52) OR ([Frequency] <> 'EveryXWeeks' AND [IntervalWeeks] IS NULL)");
                    table.ForeignKey(
                        name: "FK_SupplyLists_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyLists_UserProfiles_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplyListLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplyListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyListLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplyListLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyListLines_SupplyLists_SupplyListId",
                        column: x => x.SupplyListId,
                        principalTable: "SupplyLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplyListLines_UserProfiles_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplyListLines_UserProfiles_LastModifiedById",
                        column: x => x.LastModifiedById,
                        principalTable: "UserProfiles",
                        principalColumn: "IdentityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyListLines_CreatedById",
                table: "SupplyListLines",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyListLines_ItemId",
                table: "SupplyListLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyListLines_LastModifiedById",
                table: "SupplyListLines",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyListLines_SupplyListId_ItemId",
                table: "SupplyListLines",
                columns: new[] { "SupplyListId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyLists_CreatedById",
                table: "SupplyLists",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyLists_LastModifiedById",
                table: "SupplyLists",
                column: "LastModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyLists_Name",
                table: "SupplyLists",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplyListLines");

            migrationBuilder.DropTable(
                name: "SupplyLists");
        }
    }
}
