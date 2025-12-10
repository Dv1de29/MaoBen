using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    // Crucial: Inherit from IdentityDbContext<ApplicationUser>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Posts> Posts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // This is where we can enforce specific database rules if we want to be strict.
            // For example, making sure FirstName is never null at the database level.

            //builder.Entity<ApplicationUser>(entity =>
            //{
            //    entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            //    entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            //    entity.Property(e => e.Description).HasMaxLength(500); // Limit description length
            //});
            var initialUserId = "1";

            var posts = new List<Posts>
            {
                new Posts
                {
                    Id = 1,
                    OwnerID = initialUserId, // Use the ID of the seeded user
                    Nr_likes = 15,
                    Nr_Comms = 2,
                    Image_path = "/images/post1.jpg",
                    Description = "This is the first seeded post for testing!"
                },
                new Posts
                {
                    Id = 2,
                    OwnerID = initialUserId,
                    Nr_likes = 50,
                    Nr_Comms = 10,
                    Image_path = "/images/post2.jpg",
                    Description = "A second post showing off the seeding feature."
                }
            };

            builder.Entity<Posts>().HasData(posts);
        }
    }
}