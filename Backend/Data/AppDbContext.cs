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
        }
    }
}