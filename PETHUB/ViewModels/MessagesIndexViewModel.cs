namespace PETHUB.ViewModels
{
    public class MessagesIndexViewModel
    {

        public string CurrentUserId { get; set; } = null!;

        public List<ConversationListItemViewModel> Conversations { get; set; }
            = new List<ConversationListItemViewModel>();

        public ConversationViewModel? SelectedConversation { get; set; }

        public bool IsArchiveView { get; set; }

    }
}