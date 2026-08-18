using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    // Identifies which type of PETHUB content the member reported.
    // This is stored separately from the nullable Listing/LostFound foreign keys
    // so the report can still remember what type of content was reported even
    // if the original post is later deleted by an administrator.
    public enum ReportedContentType
    {
        Listing,
        LostFound
    }

    // Defines the fixed reasons that members can select when submitting a report.
    // The "Other" option is handled separately with the OtherReason text field.
    public enum UserReportReason
    {
        ScamFraud,
        InappropriateContent,
        FalseInformation,
        AnimalAbuse,
        DuplicateListing,
        AlreadySoldAdopted,
        SuspiciousActivity,
        Other
    }

    // Represents the moderation state of a member-submitted report.
    // This is separate from Marketplace approval status and Lost & Found
    // lifecycle status because a user report is a different business process.
    public enum UserReportStatus
    {
        Pending,
        Reviewed,
        Resolved,
        Dismissed
    }

    public class UserReport
    {
        // Primary key for the user report.
        [Key]
        public int UserReportId { get; set; }

        // Stores the Identity ID of the member who submitted the report.
        // This connects the report to ApplicationUser.
        [Required]
        public string ReporterId { get; set; }

        // Navigation property used later by the administrator report page
        // to display who submitted the report.
        public ApplicationUser? Reporter { get; set; }

        // Identifies whether the report concerns a Marketplace listing
        // or a Lost & Found report.
        [Required]
        public ReportedContentType ContentType { get; set; }

        // Foreign key for a reported Marketplace listing.
        // This remains nullable because a report can instead target LostFound.
        public int? ListingId { get; set; }

        // Navigation property for the reported Marketplace listing.
        public Listing? Listing { get; set; }

        // Foreign key for a reported Lost & Found report.
        // This remains nullable because a report can instead target Listing.
        public int? LostFoundId { get; set; }

        // Navigation property for the reported Lost & Found report.
        public LostFound? LostFound { get; set; }

        // Stores the member-selected reason for the report.
        [Required]
        public UserReportReason Reason { get; set; }

        // Stores the member's custom reason when "Other" is selected.
        // This is optional at the database/model level because it is only
        // required when Reason == Other. The controller will enforce that rule.
        public string? OtherReason { get; set; }

        // Stores additional information supplied by the member explaining
        // why they believe the listing or report should be reviewed.
        public string? Description { get; set; }

        // Stores the current moderation state of the report.
        // New reports start as Pending and are reviewed by administrators later.
        [Required]
        public UserReportStatus Status { get; set; } = UserReportStatus.Pending;

        // Records when the member submitted the report.
        // UTC is used so the stored timestamp is consistent regardless of server location.
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    }
}