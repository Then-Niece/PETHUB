namespace PETHUB.ViewModels
{
    public class ConversationViewModel
    {
        public int ConversationId { get; set; }

        public string OtherUserId { get; set; } = string.Empty;

        public string OtherUserName { get; set; } = string.Empty;

        public string ContextTitle { get; set; } = string.Empty;

        public string ContextType { get; set; } = string.Empty;

        public string? OtherUserProfilePicture { get; set; }

        public int? ListingId { get; set; }

        public int? LostFoundId { get; set; }

        public bool ContextAvailable { get; set; }

        public int? OtherParticipantLastReadMessageId { get; set; }

        public bool IsOtherUserActive { get; set; }

        public string? ContextImagePath { get; set; }

        public string? ContextStatus { get; set; }

        public List<MessageViewModel> Messages { get; set; }
            = new List<MessageViewModel>();
    }
}