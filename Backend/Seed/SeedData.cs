using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Seed
{
    // Helper record
    public record UserSeedData(string FirstName, string LastName, string Username, string Email, string Password, string? ProfilePictureUrl = null);

    public static class SeedData
    {
        public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 1. Ensure Roles Exist
            string[] roleNames = { "User", "Admin" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Define Users
            var usersToSeed = new List<UserSeedData>
            {
                new UserSeedData("Admin", "System", "admin_master", "admin@test.com", "AdminPass123!", "be_assets/img/ben1.jpg"),
                new UserSeedData("David", "Barbu", "david_florian", "david@test.com", "David29!", "be_assets/img/ben1.jpg"),
                new UserSeedData("Ionut", "FIlote", "filote_ionut", "fifi@test.com", "Fifi7cm!", "be_assets/img/download.jpg"),
                new UserSeedData("Alex", "Popescu", "alex.p", "alex.p@example.com", "SecurePwd1!", "be_assets/img/ben1.jpg"),
                new UserSeedData("Maria", "Ionescu", "maria.i", "maria.i@example.com", "SecurePwd2!", "be_assets/img/ben1.jpg"),
                new UserSeedData("Ionut", "Vasilescu", "ionut.v", "ionut.v@example.com", "SecurePwd3!", "be_assets/img/download.jpg"),
                new UserSeedData("Elena", "Gheorghe", "elena.g", "elena.g@example.com", "SecurePwd4!", "be_assets/img/ben1.jpg"),
                new UserSeedData("Radu", "Dumitru", "radu.d", "radu.d@example.com", "SecurePwd5!", "be_assets/img/download.jpg"),
            };

            // 3. Create Users
            foreach (var seedUser in usersToSeed)
            {
                if (await userManager.FindByEmailAsync(seedUser.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        FirstName = seedUser.FirstName,
                        LastName = seedUser.LastName,
                        UserName = seedUser.Username,
                        Email = seedUser.Email,
                        EmailConfirmed = true,
                        Description = $"Profile for {seedUser.FirstName} {seedUser.LastName}",
                        ProfilePictureUrl = seedUser.ProfilePictureUrl ?? "be_assets/img/no_user.png"
                    };

                    var result = await userManager.CreateAsync(user, seedUser.Password);

                    if (result.Succeeded)
                    {
                        if (seedUser.Username == "admin_master")
                            await userManager.AddToRoleAsync(user, "Admin");
                        else
                            await userManager.AddToRoleAsync(user, "User");
                    }
                }
            }

            // 4. Seed 10 Posts for EVERY User
            // We check if the database is empty of posts to avoid duplicating on every run
            if (!context.Posts.Any())
            {
                // Fetch all users we just created/confirmed
                var allUsers = await userManager.Users.ToListAsync();

                var postsList = new List<Posts>();
                var random = new Random();

                // List of sample images to rotate through
                string[] sampleImages = {
                    "/be_assets/img/ben1.jpg",
                    "/be_assets/img/download.jpg",
                    "/be_assets/img/ben1.jpg",
                    "/be_assets/img/download.jpg"
                };

                foreach (var user in allUsers)
                {
                    // Generate 10 posts for this specific user
                    for (int i = 1; i <= 10; i++)
                    {
                        postsList.Add(new Posts
                        {
                            OwnerID = user.Id, // Link to the user
                            Nr_likes = random.Next(0, 500), // Random likes between 0-500
                            Nr_Comms = random.Next(0, 50),  // Random comments between 0-50

                            // Cycle through images or pick random
                            Image_path = sampleImages[random.Next(sampleImages.Length)],

                            Description = $"{user.FirstName}'s post number {i}. This is automatically generated content to fill the database.",

                            // Spread dates out over the last 30 days
                            Created = DateTime.UtcNow.AddDays(-random.Next(1, 30)).AddHours(random.Next(-12, 12))
                        });
                    }
                }

                context.Posts.AddRange(postsList);
                await context.SaveChangesAsync();
            }
        }
    }
}