using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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

        public DbSet<Comment> Comments { get; set; }

        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            builder.Entity<UserFollow>()
                 .HasKey(k => new { k.SourceUserId, k.TargetUserId });

            builder.Entity<UserFollow>()
                .HasOne(f => f.SourceUser)
                .WithMany()
                .HasForeignKey(f => f.SourceUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserFollow>()
                .HasOne(f => f.TargetUser)
                .WithMany()
                .HasForeignKey(f => f.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- CONFIGURARE NOUA PENTRU LIKES ---
            // Cheie compusă: Combinația PostId + UserId este unică
            builder.Entity<PostLike>()
                .HasKey(pl => new { pl.PostId, pl.UserId });

            builder.Entity<PostLike>()
                .HasOne(pl => pl.Post)
                .WithMany()
                .HasForeignKey(pl => pl.PostId)
                .OnDelete(DeleteBehavior.Cascade); // Dacă ștergi postul, se șterg și like-urile

            builder.Entity<PostLike>()
               .HasOne(pl => pl.User)
               .WithMany()
               .HasForeignKey(pl => pl.UserId)
               .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Comment>()
                 .HasOne(c => c.Post)
                 .WithMany()
                 .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupMember>()
                .HasKey(gm => new { gm.GroupId, gm.UserId });

            // Relații Membri
            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany()
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Dacă șterg grupul, șterg membrii

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relații Mesaje
            builder.Entity<GroupMessage>()
                .HasOne(m => m.Group)
                .WithMany()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade); // Dacă șterg grupul, șterg mesajele

            builder.Entity<GroupMessage>()
               .HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
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