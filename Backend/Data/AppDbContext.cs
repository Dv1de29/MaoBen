using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
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
        public DbSet<DirectMessage> DirectMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // --- USER FOLLOWS ---
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

            // --- POST LIKES ---
            builder.Entity<PostLike>()
                .HasKey(pl => new { pl.PostId, pl.UserId });

            builder.Entity<PostLike>()
                .HasOne(pl => pl.Post)
                .WithMany()
                .HasForeignKey(pl => pl.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PostLike>()
               .HasOne(pl => pl.User)
               .WithMany()
               .HasForeignKey(pl => pl.UserId)
               .OnDelete(DeleteBehavior.Restrict);

            // --- COMMENTS (FIX CRITIC PENTRU EROAREA DE CICLU) ---
            // Fără acest bloc, primești eroarea "multiple cascade paths"
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Important: Userul șters nu șterge comentariul automat

            builder.Entity<Comment>()
                 .HasOne(c => c.Post)
                 .WithMany()
                 .HasForeignKey(c => c.PostId)
                 .OnDelete(DeleteBehavior.Cascade); // Dacă ștergi postul, se șterg comentariile

            // --- GROUPS ---
            builder.Entity<GroupMember>()
                .HasKey(gm => new { gm.GroupId, gm.UserId });

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany()
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupMessage>()
                .HasOne(m => m.Group)
                .WithMany()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMessage>()
               .HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<DirectMessage>()
                .HasOne(dm => dm.Sender)
                .WithMany()
                .HasForeignKey(dm => dm.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DirectMessage>()
                .HasOne(dm => dm.Receiver)
                .WithMany()
                .HasForeignKey(dm => dm.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index pentru căutare rapidă de conversații
            builder.Entity<DirectMessage>()
                .HasIndex(dm => new { dm.SenderId, dm.ReceiverId });

            builder.Entity<DirectMessage>()
                .HasIndex(dm => new { dm.ReceiverId, dm.SenderId });
        }
    }
}