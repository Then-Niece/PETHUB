using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public enum ConversationType
    {
        Marketplace,

        [Display(Name = "Lost & Found")]
        LostFound
    }

    public class Conversation
    {
        [Key]
        public int ConversationId { get; set; }

        // When the conversation was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public ConversationType Type { get; set; }

        [Required]
        public string ContextTitle { get; set; }

        // =========================================================
        // OPTIONAL MARKETPLACE CONTEXT
        // =========================================================

        public int? ListingId { get; set; }

        public Listing? Listing { get; set; }


        // =========================================================
        // OPTIONAL LOST & FOUND CONTEXT
        // =========================================================

        public int? LostFoundId { get; set; }

        public LostFound? LostFound { get; set; }


        // =========================================================
        // PARTICIPANTS
        // =========================================================

        public ICollection<ConversationParticipant> Participants { get; set; }
            = new List<ConversationParticipant>();


        // =========================================================
        // MESSAGES
        // =========================================================

        public ICollection<Message> Messages { get; set; }
            = new List<Message>();
    }
}