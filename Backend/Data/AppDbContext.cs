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
        public DbSet<UserFollow> UserFollows { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<UserFollow>()
                .HasKey(k => new { k.SourceUserId, k.TargetUserId });

            // Relația: Un user are mulți "Following"
            builder.Entity<UserFollow>()
                .HasOne(f => f.SourceUser)
                .WithMany() // Poți adăuga o colecție în User dacă vrei: .WithMany(u => u.Followings)
                .HasForeignKey(f => f.SourceUserId)
                .OnDelete(DeleteBehavior.Restrict); // Important: Restrict ca să eviți ciclurile la ștergere

            // Relația: Un user are mulți "Followers"
            builder.Entity<UserFollow>()
                .HasOne(f => f.TargetUser)
                .WithMany()
                .HasForeignKey(f => f.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
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
    // --- Posts 1-5: Recent and Active ---
                new Posts {
                    Id = 1, OwnerID = initialUserId, Nr_likes = 15, Nr_Comms = 2,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "First seeded post! Beautiful day for a hike in the mountains. 🌲",
                    Created = new DateTime(2025, 12, 10, 15, 0, 0)
                },
                new Posts {
                    Id = 2, OwnerID = initialUserId + 1, Nr_likes = 50, Nr_Comms = 10,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "City lights always make for a perfect evening view. ✨",
                    Created = new DateTime(2025, 12, 10, 13, 0, 0)
                },
                new Posts {
                    Id = 3, OwnerID = initialUserId + 2, Nr_likes = 120, Nr_Comms = 25,
                    Image_path = "../assets/img/download.jpg",
                    Description = "Tried out a new pasta recipe tonight! Highly recommend! 🍝",
                    Created = new DateTime(2025, 12, 10, 11, 0, 0)
                },
                new Posts {
                    Id = 4, OwnerID = initialUserId, Nr_likes = 8, Nr_Comms = 0,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "My little furry friend enjoying the sunshine. ☀️",
                    Created = new DateTime(2025, 12, 10, 10, 0, 0)
                },
                new Posts {
                    Id = 5, OwnerID = initialUserId, Nr_likes = 250, Nr_Comms = 45,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "Finally finished building my new development setup! Ready to code. 💻",
                    Created = new DateTime(2025, 12, 10, 8, 0, 0)
                },

                // --- Posts 6-10: Moderate Activity, Older Dates ---
                new Posts {
                    Id = 6, OwnerID = initialUserId + 1, Nr_likes = 30, Nr_Comms = 5,
                    Image_path = "../assets/img/download.jpg",
                    Description = "Throwback to that incredible sunset on the beach last month. 🌅",
                    Created = new DateTime(2025, 11, 28)
                },
                new Posts {
                    Id = 7, OwnerID = initialUserId, Nr_likes = 90, Nr_Comms = 15,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "A thought-provoking visit to the local art gallery today. 🖼️",
                    Created = new DateTime(2025, 11, 15)
                },
                new Posts {
                    Id = 8, OwnerID = initialUserId, Nr_likes = 12, Nr_Comms = 1,
                    Image_path = "../assets/img/download.jpg",
                    Description = "Starting the day with a strong cup of coffee. Can't beat it. ☕",
                    Created = new DateTime(2025, 10, 30)
                },
                new Posts {
                    Id = 9, OwnerID = initialUserId, Nr_likes = 60, Nr_Comms = 8,
                    Image_path = "../assets/img/download.jpg",
                    Description = "Miss waking up to this view. Best vacation ever! 🌍",
                    Created = new DateTime(2025, 10, 10)
                },
                new Posts {
                    Id = 10, OwnerID = initialUserId + 2, Nr_likes = 180, Nr_Comms = 35,
                    Image_path = "../assets/img/download.jpg",
                    Description = "So excited to announce the launch of my new side project! Link in bio. 🎉",
                    Created = new DateTime(2025, 9, 25)
                },

                // --- Posts 11-15: Low Activity/Very Old ---
                new Posts {
                    Id = 11, OwnerID = initialUserId + 3, Nr_likes = 3, Nr_Comms = 0,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "Just testing the post upload feature. Ignore this! 🛠️",
                    Created = new DateTime(2025, 9, 1)
                },
                new Posts {
                    Id = 12, OwnerID = initialUserId + 3, Nr_likes = 45, Nr_Comms = 7,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "My little garden is finally blooming! So much hard work paid off. 🌸",
                    Created = new DateTime(2025, 8, 15)
                },
                new Posts {
                    Id = 13, OwnerID = initialUserId + 4, Nr_likes = 70, Nr_Comms = 11,
                    Image_path = "../assets/img/download.jpg",
                    Description = "Throwback to an amazing concert last summer. What a vibe! 🎶",
                    Created = new DateTime(2025, 7, 20)
                },
                new Posts {
                    Id = 14, OwnerID = initialUserId, Nr_likes = 15, Nr_Comms = 2,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "Finished this great book today. Definitely worth the read. 📚",
                    Created = new DateTime(2025, 6, 5)
                },
                new Posts {
                    Id = 15, OwnerID = initialUserId + 4, Nr_likes = 5, Nr_Comms = 0,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "A quiet moment of reflection. 🕊️",
                    Created = new DateTime(2025, 5, 1)
                },

                // --- Posts 16-20: Miscellaneous Data Points ---
                new Posts {
                    Id = 16, OwnerID = initialUserId, Nr_likes = 500, Nr_Comms = 80,
                    Image_path = "../assets/img/download.jpg",
                    Description = "I can't believe this photo went viral! Thanks everyone for the support! 🙏",
                    Created = new DateTime(2025, 4, 15) // High likes/comments
                },
                new Posts {
                    Id = 17, OwnerID = initialUserId, Nr_likes = 0, Nr_Comms = 0,
                    Image_path = "../assets/img/download.jpg",
                    Description = "This post was probably uploaded by mistake, no engagement. 👻",
                    Created = new DateTime(2025, 3, 10) // Zero engagement
                },
                new Posts {
                    Id = 18, OwnerID = initialUserId + 1, Nr_likes = 105, Nr_Comms = 20,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "Hitting a new personal record at the gym today! Hard work pays off. 💪",
                    Created = new DateTime(2025, 2, 28)
                },
                new Posts {
                    Id = 19, OwnerID = initialUserId, Nr_likes = 150, Nr_Comms = 18,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "Discovered the best food truck tacos today! Absolute perfection. 🌮",
                    Created = new DateTime(2025, 1, 20)
                },
                new Posts {
                    Id = 20, OwnerID = initialUserId + 2, Nr_likes = 22, Nr_Comms = 4,
                    Image_path = "../assets/img/ben1.jpg",
                    Description = "The first snowfall of the year! Everything looks so peaceful. ❄️",
                    Created = new DateTime(2024, 12, 5) // Oldest post
                }
            };

            builder.Entity<Posts>().HasData(posts);
        }
    }
}