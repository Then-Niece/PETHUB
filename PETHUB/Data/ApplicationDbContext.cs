using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PETHUB.Models;

namespace PETHUB.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }

        public DbSet<LostFound> LostFounds { get; set; }

        public DbSet<LostFoundImage> LostFoundImages { get; set; }

        // This DbSet stores reports submitted by members against
        // Marketplace listings and Lost & Found reports.
        public DbSet<UserReport> UserReports { get; set; }
        // Provides EF Core access to Member appeals against removed Marketplace
        // listings and Lost & Found posts.
        public DbSet<Appeal> Appeals { get; set; }
        //This is for the PETFEED Feature
        public DbSet<PetFeed> PetFeeds { get; set; }
        public DbSet<PetFeedComment> PetFeedComments { get; set; }
        public DbSet<PetFeedImage> PetFeedImages { get; set; }
        public DbSet<SavedPetFeed> SavedPetFeeds { get; set; }
        public DbSet<PetFeedPaw> PetFeedPaws { get; set; }


        // This is for the Notification Feature
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PetFeed>()
                .HasOne(p => p.Admin)
                .WithMany()
                .HasForeignKey(p => p.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PetFeedComment>()
                .HasOne(c => c.Member)
                .WithMany()
                .HasForeignKey(c => c.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PetFeedComment>()
                .HasOne(c => c.PetFeed)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PetFeedImage>()
                .HasOne(i => i.PetFeed)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedPetFeed>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SavedPetFeed>()
                .HasOne(s => s.PetFeed)
                .WithMany(p => p.SavedByMembers)
                .HasForeignKey(s => s.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<PetFeedPaw>()
                .HasOne(p => p.PetFeed)
                .WithMany(f => f.Paws)
                .HasForeignKey(p => p.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PetFeedPaw>()
                .HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserReport>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserReport>()
                .HasOne(r => r.Listing)
                .WithMany()
                .HasForeignKey(r => r.ListingId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserReport>()
                .HasOne(r => r.LostFound)
                .WithMany()
                .HasForeignKey(r => r.LostFoundId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
