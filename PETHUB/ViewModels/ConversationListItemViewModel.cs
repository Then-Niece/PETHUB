namespace PETHUB.ViewModels
{
    public class ConversationListItemViewModel
    {
        public int ConversationId { get; set; }

        public string OtherUserId { get; set; }

        public string OtherUserName { get; set; }

        public string? OtherUserProfilePicture { get; set; }

        public string ContextTitle { get; set; }

        public string ContextType { get; set; }

        public string? LastMessage { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public int UnreadCount { get; set; }
    }
}