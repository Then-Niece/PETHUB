namespace PETHUB.ViewModels
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public string? ImagePath { get; set; }

        public string? RedirectUrl { get; set; }

        public bool IsRead { get; set; }
        public bool IsSeen { get; set; }

        public DateTime CreatedAt { get; set; }

        public string TimeAgo { get; set; }
    }
}