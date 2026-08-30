using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public class ConversationParticipant
    {
        [Key]
        public int ConversationParticipantId { get; set; }

        // Conversation this participant belongs to
        public int ConversationId { get; set; }

        public Conversation Conversation { get; set; }


        // User participating in the conversation
        public string UserId { get; set; }

        public ApplicationUser User { get; set; }


        // When the member joined/was added to the conversation
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;


        // ID of the most recent message this user has read
        public int? LastReadMessageId { get; set; }

        // Whether the conversation is archived for this participant
        public bool IsArchived { get; set; } = false;


    }
}
