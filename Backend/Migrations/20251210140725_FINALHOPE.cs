using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class FINALHOPE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 10, 15, 0, 0, 0, DateTimeKind.Unspecified), "First seeded post! Beautiful day for a hike in the mountains. 🌲" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 10, 13, 0, 0, 0, DateTimeKind.Unspecified), "City lights always make for a perfect evening view. ✨" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 10, 11, 0, 0, 0, DateTimeKind.Unspecified), "Tried out a new pasta recipe tonight! Highly recommend! 🍝" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 10, 10, 0, 0, 0, DateTimeKind.Unspecified), "My little furry friend enjoying the sunshine. ☀️" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 10, 8, 0, 0, 0, DateTimeKind.Unspecified), "Finally finished building my new development setup! Ready to code. 💻" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "Throwback to that incredible sunset on the beach last month. 🌅");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 7,
                column: "Description",
                value: "A thought-provoking visit to the local art gallery today. 🖼️");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 8,
                column: "Description",
                value: "Starting the day with a strong cup of coffee. Can't beat it. ☕");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 9,
                column: "Description",
                value: "Miss waking up to this view. Best vacation ever! 🌍");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 10,
                column: "Description",
                value: "So excited to announce the launch of my new side project! Link in bio. 🎉");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 11,
                column: "Description",
                value: "Just testing the post upload feature. Ignore this! 🛠️");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 12,
                column: "Description",
                value: "My little garden is finally blooming! So much hard work paid off. 🌸");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 13,
                column: "Description",
                value: "Throwback to an amazing concert last summer. What a vibe! 🎶");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 14,
                column: "Description",
                value: "Finished this great book today. Definitely worth the read. 📚");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 15,
                column: "Description",
                value: "A quiet moment of reflection. 🕊️");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 16,
                column: "Description",
                value: "I can't believe this photo went viral! Thanks everyone for the support! 🙏");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 17,
                column: "Description",
                value: "This post was probably uploaded by mistake, no engagement. 👻");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 18,
                column: "Description",
                value: "Hitting a new personal record at the gym today! Hard work pays off. 💪");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 19,
                column: "Description",
                value: "Discovered the best food truck tacos today! Absolute perfection. 🌮");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 20,
                column: "Description",
                value: "The first snowfall of the year! Everything looks so peaceful. ❄️");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 10, 15, 1, 48, 373, DateTimeKind.Local).AddTicks(7884), "First seeded post! Beautiful day for a hike in the mountains. ??" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 10, 11, 1, 48, 375, DateTimeKind.Local).AddTicks(9969), "City lights always make for a perfect evening view. ?" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 9, 16, 1, 48, 375, DateTimeKind.Local).AddTicks(9983), "Tried out a new pasta recipe tonight! Highly recommend! ??" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 8, 16, 1, 48, 375, DateTimeKind.Local).AddTicks(9991), "My little furry friend enjoying the sunshine. ??" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Created", "Description" },
                values: new object[] { new DateTime(2025, 12, 7, 16, 1, 48, 375, DateTimeKind.Local).AddTicks(9993), "Finally finished building my new development setup! Ready to code. ??" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "Throwback to that incredible sunset on the beach last month. ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 7,
                column: "Description",
                value: "A thought-provoking visit to the local art gallery today. ???");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 8,
                column: "Description",
                value: "Starting the day with a strong cup of coffee. Can't beat it. ?");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 9,
                column: "Description",
                value: "Miss waking up to this view. Best vacation ever! ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 10,
                column: "Description",
                value: "So excited to announce the launch of my new side project! Link in bio. ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 11,
                column: "Description",
                value: "Just testing the post upload feature. Ignore this! ???");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 12,
                column: "Description",
                value: "My little garden is finally blooming! So much hard work paid off. ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 13,
                column: "Description",
                value: "Throwback to an amazing concert last summer. What a vibe! ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 14,
                column: "Description",
                value: "Finished this great book today. Definitely worth the read. ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 15,
                column: "Description",
                value: "A quiet moment of reflection. ???");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 16,
                column: "Description",
                value: "I can't believe this photo went viral! Thanks everyone for the support! ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 17,
                column: "Description",
                value: "This post was probably uploaded by mistake, no engagement. ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 18,
                column: "Description",
                value: "Hitting a new personal record at the gym today! Hard work pays off. ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 19,
                column: "Description",
                value: "Discovered the best food truck tacos today! Absolute perfection. ??");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 20,
                column: "Description",
                value: "The first snowfall of the year! Everything looks so peaceful. ??");
        }
    }
}
