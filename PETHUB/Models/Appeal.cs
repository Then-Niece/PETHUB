using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    // Represents an owner's request to reconsider the removal of their post.
    // The Appeal does not duplicate the post itself; it only stores the owner's
    // explanation and the Admin's decision about that existing removed post.
    public class Appeal
    {
        // Primary key for the Appeal record.
        public int AppealId { get; set; }


        // Stores the Identity ID of the Member who submitted the appeal.
        // This connects the appeal to the owner of the removed post.
        [Required]
        public string MemberId { get; set; } = string.Empty;


        // Identifies the existing Marketplace listing being appealed.
        // This remains null when the appeal is for a Lost & Found post.
        public int? ListingId { get; set; }


        // Identifies the existing Lost & Found post being appealed.
        // This remains null when the appeal is for a Marketplace listing.
        public int? LostFoundId { get; set; }


        // Stores the explanation written by the Member.
        // This is the actual appeal message displayed alongside the
        // existing removed post.
        [Required]
        [StringLength(2000)]
        public string AppealMessage { get; set; } = string.Empty;


        // Tracks the current state of the appeal.
        // Pending means the Admin has not decided yet.
        // Approved means the appeal was confirmed and the post is restored.
        // Rejected means the appeal was denied and the post remains Removed.
        public AppealStatus Status { get; set; } = AppealStatus.Pending;


        // Stores the Admin's response when the appeal is approved or rejected.
        // This is optional because a Pending appeal does not have an Admin response.
        [StringLength(2000)]
        public string? AdminActionReason { get; set; }


        // Records when the Member submitted the appeal.
        public DateTime DateCreated { get; set; } = DateTime.Now;


        // Records when an Admin makes the final decision.
        // This remains null while the appeal is still Pending.
        public DateTime? DateResolved { get; set; }
    }


    // Represents the moderation state of an Appeal.
    public enum AppealStatus
    {
        // The Member has submitted an appeal and an Admin has not decided yet.
        Pending,

        // The Admin confirmed the appeal.
        // The associated post will be changed from Removed to Approved.
        Approved,

        // The Admin rejected the appeal.
        // The associated post remains Removed.
        Rejected
    }
}