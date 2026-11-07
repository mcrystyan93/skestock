using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skestock.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInformareTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MessageTemplates",
                columns: new[] { "Id", "Body", "CreatedById", "CreatedDate", "IsActive", "LastModifiedById", "LastModifiedDate", "Name", "Type" },
                values: new object[] { new Guid("0197c0de-0000-7000-8000-000000000003"), "<p>Bună ziua, {ContactPerson}!</p><p>Vă informăm că {Date} congregația {Congregation} aduce fructe și desert pentru {ClassName}.</p><p>Vă mulțumim!</p>", null, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, null, new DateTimeOffset(new DateTime(2026, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Informare fructe și desert", 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MessageTemplates",
                keyColumn: "Id",
                keyValue: new Guid("0197c0de-0000-7000-8000-000000000003"));
        }
    }
}
