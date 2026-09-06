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

        public DbSet<UserReport> UserReports { get; set; }
        // Provides EF Core access to Member appeals against removed Marketplace
        // listings and Lost & Found posts.
        public DbSet<Appeal> Appeals { get; set; }
        //This is for the PETFEED Feature

        // PETFEED
        public DbSet<PetFeed> PetFeeds { get; set; }
        public DbSet<PetFeedComment> PetFeedComments { get; set; }
        public DbSet<PetFeedImage> PetFeedImages { get; set; }
        public DbSet<PetFeedPaw> PetFeedPaws { get; set; }

        // =========================================================
        // SAVED ITEMS
        // =========================================================

        public DbSet<SavedListing> SavedListings { get; set; }

        public DbSet<SavedLostFound> SavedLostFounds { get; set; }

        public DbSet<SavedPetFeed> SavedPetFeeds { get; set; }

        // NOTIFICATIONS
        public DbSet<Notification> Notifications { get; set; }

        // AUDIT LOGS
        public DbSet<AuditLog> AuditLogs { get; set; }

        // MESSAGING
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageImage> MessageImages { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================================================
            // ADMIN → PETFEED
            // =========================================================
            // If an admin is deleted, keep the PetFeed.
            // AdminId becomes NULL.

            builder.Entity<PetFeed>()
                .HasOne(p => p.Admin)
                .WithMany()
                .HasForeignKey(p => p.AdminId)
                .OnDelete(DeleteBehavior.SetNull);


            // =========================================================
            // MEMBER → PETFEED COMMENTS
            // =========================================================

            builder.Entity<PetFeedComment>()
                .HasOne(c => c.Member)
                .WithMany()
                .HasForeignKey(c => c.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PetFeedComment>()
                .HasOne(c => c.PetFeed)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // PETFEED IMAGES
            // =========================================================

            builder.Entity<PetFeedImage>()
                .HasOne(i => i.PetFeed)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);



            // =========================================================
            // PETFEED PAWS
            // =========================================================

            builder.Entity<PetFeedPaw>()
                .HasOne(p => p.PetFeed)
                .WithMany(f => f.Paws)
                .HasForeignKey(p => p.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PetFeedPaw>()
                .HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // NOTIFICATIONS
            // =========================================================

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // USER REPORTS
            // =========================================================

            // Reports are manually deleted by the controller
            // before the member, Listing, or LostFound is deleted.

            builder.Entity<UserReport>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserReport>()
                .HasOne(r => r.Listing)
                .WithMany()
                .HasForeignKey(r => r.ListingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserReport>()
                .HasOne(r => r.LostFound)
                .WithMany()
                .HasForeignKey(r => r.LostFoundId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // MEMBER → LISTING
            // =========================================================

            builder.Entity<Listing>()
                .HasOne(l => l.Member)
                .WithMany()
                .HasForeignKey(l => l.MemberId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // LISTING → IMAGES
            // =========================================================

            builder.Entity<ListingImage>()
                .HasOne(i => i.Listing)
                .WithMany(l => l.Images)
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // MEMBER → LOST & FOUND
            // =========================================================

            builder.Entity<LostFound>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // LOST & FOUND → IMAGES
            // =========================================================

            builder.Entity<LostFoundImage>()
                .HasOne(i => i.LostFound)
                .WithMany(l => l.Images)
                .HasForeignKey(i => i.LostFoundId)
                .OnDelete(DeleteBehavior.Cascade);




            // =========================================================
            // MESSAGING
            // =========================================================


            // =========================================================
            // CONVERSATION → LISTING
            // =========================================================

            builder.Entity<Conversation>()
                .HasOne(c => c.Listing)
                .WithMany()
                .HasForeignKey(c => c.ListingId)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================================
            // CONVERSATION → LOST & FOUND
            // =========================================================

            builder.Entity<Conversation>()
                .HasOne(c => c.LostFound)
                .WithMany()
                .HasForeignKey(c => c.LostFoundId)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================================
            // CONVERSATION → PARTICIPANTS
            // =========================================================

            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // PARTICIPANT → APPLICATION USER
            // =========================================================

            builder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // CONVERSATION → MESSAGES
            // =========================================================

            builder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);


            // =========================================================
            // MESSAGE → SENDER
            // =========================================================

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // UNIQUE PARTICIPANT PER CONVERSATION
            // =========================================================

            builder.Entity<ConversationParticipant>()
                .HasIndex(cp => new
                {
                    cp.ConversationId,
                    cp.UserId
                })
                .IsUnique();


            // =========================================================
            // MESSAGE QUERY INDEX
            // =========================================================

            builder.Entity<Message>()
                .HasIndex(m => new
                {
                    m.ConversationId,
                    m.CreatedAt
                });



            builder.Entity<MessageImage>()
                .HasOne(mi => mi.Message)
                .WithMany(m => m.Images)
                .HasForeignKey(mi => mi.MessageId)
                .OnDelete(DeleteBehavior.Cascade);



            // =========================================================
            // SAVED MARKETPLACE LISTINGS
            // =========================================================

            builder.Entity<SavedListing>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Entity<SavedListing>()
                .HasOne(s => s.Listing)
                .WithMany()
                .HasForeignKey(s => s.ListingId)
                .OnDelete(DeleteBehavior.Cascade);


            // Prevent a Member from saving the same Listing twice.
            builder.Entity<SavedListing>()
                .HasIndex(s => new
                {
                    s.MemberId,
                    s.ListingId
                })
                .IsUnique();


            // =========================================================
            // SAVED MARKETPLACE LISTINGS
            // =========================================================

            builder.Entity<SavedListing>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedListing>()
                .HasOne(s => s.Listing)
                .WithMany(l => l.SavedByMembers)
                .HasForeignKey(s => s.ListingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SavedListing>()
                .HasIndex(s => new
                {
                    s.MemberId,
                    s.ListingId
                })
                .IsUnique();


            // =========================================================
            // SAVED LOST & FOUND REPORTS
            // =========================================================

            builder.Entity<SavedLostFound>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedLostFound>()
                .HasOne(s => s.LostFound)
                .WithMany(l => l.SavedByMembers)
                .HasForeignKey(s => s.LostFoundId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SavedLostFound>()
                .HasIndex(s => new
                {
                    s.MemberId,
                    s.LostFoundId
                })
                .IsUnique();


            // =========================================================
            // SAVED PETFEEDS
            // =========================================================

            builder.Entity<SavedPetFeed>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedPetFeed>()
                .HasOne(s => s.PetFeed)
                .WithMany(p => p.SavedByMembers)
                .HasForeignKey(s => s.PetFeedId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SavedPetFeed>()
                .HasIndex(s => new
                {
                    s.MemberId,
                    s.PetFeedId
                })
                .IsUnique();

        }
    }
}
