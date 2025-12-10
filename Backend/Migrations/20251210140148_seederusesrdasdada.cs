using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class seederusesrdasdada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                column: "Created",
                value: new DateTime(2025, 12, 10, 15, 1, 48, 373, DateTimeKind.Local).AddTicks(7884));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                column: "Created",
                value: new DateTime(2025, 12, 10, 11, 1, 48, 375, DateTimeKind.Local).AddTicks(9969));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                column: "Created",
                value: new DateTime(2025, 12, 9, 16, 1, 48, 375, DateTimeKind.Local).AddTicks(9983));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                column: "Created",
                value: new DateTime(2025, 12, 8, 16, 1, 48, 375, DateTimeKind.Local).AddTicks(9991));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 5,
                column: "Created",
                value: new DateTime(2025, 12, 7, 16, 1, 48, 375, DateTimeKind.Local).AddTicks(9993));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                column: "Created",
                value: new DateTime(2025, 12, 10, 15, 1, 2, 992, DateTimeKind.Local).AddTicks(8732));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                column: "Created",
                value: new DateTime(2025, 12, 10, 11, 1, 2, 994, DateTimeKind.Local).AddTicks(7888));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                column: "Created",
                value: new DateTime(2025, 12, 9, 16, 1, 2, 994, DateTimeKind.Local).AddTicks(7908));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                column: "Created",
                value: new DateTime(2025, 12, 8, 16, 1, 2, 994, DateTimeKind.Local).AddTicks(7916));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 5,
                column: "Created",
                value: new DateTime(2025, 12, 7, 16, 1, 2, 994, DateTimeKind.Local).AddTicks(7918));
        }
    }
}
