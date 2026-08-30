namespace PETHUB.ViewModels
{
    public class ConversationViewModel
    {
        public int ConversationId { get; set; }

        public string OtherUserId { get; set; }

        public string OtherUserName { get; set; }

        public string? OtherUserProfilePicture { get; set; }

        public string ContextTitle { get; set; }

        public string ContextType { get; set; }

        public int? ListingId { get; set; }

        public int? LostFoundId { get; set; }

        public bool ContextAvailable { get; set; }

        public int? OtherParticipantLastReadMessageId { get; set; }

        public List<MessageViewModel> Messages { get; set; }
            = new List<MessageViewModel>();
    }
}