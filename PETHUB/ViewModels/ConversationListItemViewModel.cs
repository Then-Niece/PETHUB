namespace PETHUB.ViewModels
{
    public class ConversationListItemViewModel
    {
        public int ConversationId { get; set; }

        public string OtherUserId { get; set; } = string.Empty;

        public string OtherUserName { get; set; } = string.Empty;

        public string ContextTitle { get; set; } = string.Empty;

        public string ContextType { get; set; } = string.Empty;

        public string? OtherUserProfilePicture { get; set; }

        public string? LastMessage { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public int UnreadCount { get; set; }

        public bool IsOtherUserActive { get; set; }

        public string? OtherUserFirstName { get; set; }

        public string? ContextImagePath { get; set; }

        public string? ContextStatus { get; set; }
    }
}