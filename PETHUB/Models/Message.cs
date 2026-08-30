using System.ComponentModel.DataAnnotations;

namespace PETHUB.Models
{
    public enum MessageType
    {
        Text,
        Image,
        TextAndImage
    }

    public class Message
    {
        [Key]
        public int MessageId { get; set; }


        // =========================================================
        // CONVERSATION
        // =========================================================

        public int ConversationId { get; set; }

        public Conversation Conversation { get; set; }


        // =========================================================
        // SENDER
        // =========================================================

        public string SenderId { get; set; }

        public ApplicationUser Sender { get; set; }


        // =========================================================
        // MESSAGE CONTENT
        // =========================================================

        public MessageType MessageType { get; set; }

        // Nullable because image-only messages are allowed
        public string? Content { get; set; }

        // Multiple images per message
        public ICollection<MessageImage> Images { get; set; }
            = new List<MessageImage>();


        // =========================================================
        // TIMESTAMP
        // =========================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
