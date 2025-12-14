using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class seederusesr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Posts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Created", "Description", "Image_path" },
                values: new object[] { new DateTime(2025, 12, 10, 15, 0, 29, 167, DateTimeKind.Local).AddTicks(1442), "First seeded post! Beautiful day for a hike in the mountains. ??", "../assets/img/nature_hike.jpg" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Created", "Description", "Image_path", "OwnerID" },
                values: new object[] { new DateTime(2025, 12, 10, 11, 0, 29, 168, DateTimeKind.Local).AddTicks(9775), "City lights always make for a perfect evening view. ?", "../assets/img/city_lights.jpg", "11" });

            migrationBuilder.InsertData(
                table: "Posts",
                columns: new[] { "Id", "Created", "Description", "Image_path", "Nr_Comms", "Nr_likes", "OwnerID", "UserId" },
                values: new object[,]
                {
                    { 3, new DateTime(2025, 12, 9, 16, 0, 29, 168, DateTimeKind.Local).AddTicks(9791), "Tried out a new pasta recipe tonight! Highly recommend! ??", "../assets/img/new_recipe.jpg", 25, 120, "12", null },
                    { 4, new DateTime(2025, 12, 8, 16, 0, 29, 168, DateTimeKind.Local).AddTicks(9799), "My little furry friend enjoying the sunshine. ??", "../assets/img/pet_photo.jpg", 0, 8, "1", null },
                    { 5, new DateTime(2025, 12, 7, 16, 0, 29, 168, DateTimeKind.Local).AddTicks(9801), "Finally finished building my new development setup! Ready to code. ??", "../assets/img/coding_setup.jpg", 45, 250, "1", null },
                    { 6, new DateTime(2025, 11, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Throwback to that incredible sunset on the beach last month. ??", "../assets/img/beach_sunset.jpg", 5, 30, "11", null },
                    { 7, new DateTime(2025, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "A thought-provoking visit to the local art gallery today. ???", "../assets/img/art_gallery.jpg", 15, 90, "1", null },
                    { 8, new DateTime(2025, 10, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Starting the day with a strong cup of coffee. Can't beat it. ?", "../assets/img/morning_coffee.jpg", 1, 12, "1", null },
                    { 9, new DateTime(2025, 10, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Miss waking up to this view. Best vacation ever! ??", "../assets/img/travel_memory.jpg", 8, 60, "1", null },
                    { 10, new DateTime(2025, 9, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "So excited to announce the launch of my new side project! Link in bio. ??", "../assets/img/project_launch.jpg", 35, 180, "12", null },
                    { 11, new DateTime(2025, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Just testing the post upload feature. Ignore this! ???", "../assets/img/test_post_1.jpg", 0, 3, "13", null },
                    { 12, new DateTime(2025, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "My little garden is finally blooming! So much hard work paid off. ??", "../assets/img/garden.jpg", 7, 45, "13", null },
                    { 13, new DateTime(2025, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Throwback to an amazing concert last summer. What a vibe! ??", "../assets/img/music_event.jpg", 11, 70, "14", null },
                    { 14, new DateTime(2025, 6, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finished this great book today. Definitely worth the read. ??", "../assets/img/book_review.jpg", 2, 15, "1", null },
                    { 15, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "A quiet moment of reflection. ???", "../assets/img/simple_photo.jpg", 0, 5, "14", null },
                    { 16, new DateTime(2025, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "I can't believe this photo went viral! Thanks everyone for the support! ??", "../assets/img/viral_hit.jpg", 80, 500, "1", null },
                    { 17, new DateTime(2025, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "This post was probably uploaded by mistake, no engagement. ??", "../assets/img/draft_post.jpg", 0, 0, "1", null },
                    { 18, new DateTime(2025, 2, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "Hitting a new personal record at the gym today! Hard work pays off. ??", "../assets/img/gym_progress.jpg", 20, 105, "11", null },
                    { 19, new DateTime(2025, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Discovered the best food truck tacos today! Absolute perfection. ??", "../assets/img/food_truck.jpg", 18, 150, "1", null },
                    { 20, new DateTime(2024, 12, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "The first snowfall of the year! Everything looks so peaceful. ??", "../assets/img/winter_snow.jpg", 4, 22, "12", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DropColumn(
                name: "Created",
                table: "Posts");

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Image_path" },
                values: new object[] { "This is the first seeded post for testing!", "../assets/img/ben1.jpg" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Image_path", "OwnerID" },
                values: new object[] { "A second post showing off the seeding feature.", "../assets/img/ben1.jpg", "1" });
        }
    }
}
