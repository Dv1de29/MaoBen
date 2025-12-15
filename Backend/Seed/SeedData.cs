using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Identity;

namespace Backend.Seed
{
    public static class SeedData
    {
        public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
        {
            // Get the required services
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Define the 5 users to be created
            var usersToSeed = new List<UserSeedData>
        {
            new UserSeedData("David", "Barbu", "david_florian", "david@test.com", "David29!", "be_assets/img/ben1.jpg"),
            new UserSeedData("Ionut", "FIlote", "filote_ionut", "fifi@test.com", "Fifi7cm!"),
            new UserSeedData("Alex", "Popescu", "alex.p", "alex.p@example.com", "SecurePwd1!", "be_assets/img/ben1.jpg"),
            new UserSeedData("Maria", "Ionescu", "maria.i", "maria.i@example.com", "SecurePwd2!", "be_assets/img/ben1.jpg"),
            new UserSeedData("Ionut", "Vasilescu", "ionut.v", "ionut.v@example.com", "SecurePwd3!", "be_assets/img/ben1.jpg"),
            new UserSeedData("Elena", "Gheorghe", "elena.g", "elena.g@example.com", "SecurePwd4!", "be_assets/img/ben1.jpg"),
            new UserSeedData("Radu", "Dumitru", "radu.d", "radu.d@example.com", "SecurePwd5!", "be_assets/img/ben1.jpg"),
        };

            foreach (var seedUser in usersToSeed)
            {
                // 1. Check if the user already exists by email
                if (await userManager.FindByEmailAsync(seedUser.Email) == null)
                {
                    // 2. Create the ApplicationUser object
                    var user = new ApplicationUser
                    {
                        FirstName = seedUser.FirstName,
                        LastName = seedUser.LastName,
                        UserName = seedUser.Username,
                        Email = seedUser.Email,
                        EmailConfirmed = true, // To simulate a fully registered user
                        Description = $"Profile for {seedUser.FirstName} {seedUser.LastName}",
                        // The ProfilePictureUrl will use the default value you defined in ApplicationUser
                    };

                    // 3. Create the user and hash the password
                    var result = await userManager.CreateAsync(user, seedUser.Password);

                    if (result.Succeeded)
                    {
                        // 4. Assign the default 'User' role as done in your Register logic
                        await userManager.AddToRoleAsync(user, "User");
                        // Note: Identity automatically assigns sequential IDs (GUIDs by default). 
                        // To force specific integer IDs (1, 2, 3, 4, 5) requires customizing 
                        // the IdentityUser class and using a specific database seeding approach (like Entity Framework's HasData), 
                        // which bypasses UserManager for ID assignment but is not recommended 
                        // for password hashing. The UserManager method is safer for passwords.
                    }
                    else
                    {
                        // Log or handle errors if user creation fails
                        Console.WriteLine($"Error creating user {seedUser.Username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }
}
