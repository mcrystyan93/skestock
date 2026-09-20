using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageClaimLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_CreatedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<Guid>(
                name: "ClaimId",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedUntilUtc",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_ClaimedUntilUtc_CreatedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "ClaimedUntilUtc", "CreatedAtUtc" },
                filter: "[ProcessedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_ClaimedUntilUtc_CreatedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ClaimId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ClaimedUntilUtc",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_CreatedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "CreatedAtUtc" },
                filter: "[ProcessedAtUtc] IS NULL");
        }
    }
}
