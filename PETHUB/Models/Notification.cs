using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{

    public enum NotificationType
    {
        // Marketplace
        MarketplaceApproved,
        MarketplaceRejected,

        // Lost & Found (Owner)
        LostFoundApproved,
        LostFoundRejected,

        // Lost & Found (Community)
        LostPetNearby,
        FoundPetNearby,

        // PetFeed
        NewAnnouncement,
        NewPetTip,

        // Admin
        NewMarketplaceSubmission,
        NewLostFoundSubmission,
        NewUserReport,

        // Member
        UserReportAccepted,
        UserReportRejected,
        ReportedPostRemoved
    }

    public class Notification
    {
        public int NotificationId { get; set; }

        // Receiver
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Notification category
        [Required]
        public NotificationType Type { get; set; }

        // Display
        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public string? ImagePath { get; set; }

        // Navigation
        public string? RedirectUrl { get; set; }

        // Optional references
        public int? LostFoundId { get; set; }

        public int? ListingId { get; set; }

        public int? PetFeedId { get; set; }

        // Read status
        public bool IsRead { get; set; } = false;

        // Seen status
        public bool IsSeen { get; set; } = false;

        // Created date
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
